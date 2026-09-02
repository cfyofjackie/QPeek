using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using ICSharpCode.AvalonEdit;
using MdXaml;

namespace QuickLook;

public partial class MainWindow : Window
{
    private const double MinimumWindowWidth = 320;
    private const double MinimumWindowHeight = 240;
    private const double WindowPadding = 24;
    private const double StatusTextHeight = 36;
    private const double MaximumScreenFraction = 0.8;
    private const double DefaultTextWindowWidth = 800;
    private const double DefaultTextWindowHeight = 600;
    private const int PreviewFadeOutMilliseconds = 60;
    private const int PreviewFadeInMilliseconds = 160;
    private string? _filePath;
    private string[] _previewFilePaths = [];
    private int _currentPreviewFileIndex = -1;
    private int _requestedPreviewFileIndex = -1;
    private bool _isPreviewTransitioning;
    private Rect? _animatedWindowBounds;
    private System.Windows.Point? _transitionCenter;
    private readonly Markdown _markdown = new()
    {
        HyperlinkCommand = null,
        OnHyperLinkClicked = OpenMarkdownLink,
        AssetPathRoot = null,
        DisabledContextMenu = true
    };

    public event Action<string>? PreviewFileChanged;

    public MainWindow(string? filePath, string[]? previewFilePaths = null)
    {
        InitializeComponent();
        _markdown.DocumentStyle = (Style)FindResource("QPeekMarkdownDocumentStyle");

        if (string.IsNullOrWhiteSpace(filePath))
        {
            ShowStatus("Select a supported file in Explorer, then run the app.");
            return;
        }

        if (!File.Exists(filePath))
        {
            ShowStatus("The file was not found.");
            return;
        }

        _filePath = Path.GetFullPath(filePath);

        if (!IsSupportedPreviewFile(filePath))
        {
            ShowStatus("This file type is not supported.");
            return;
        }

        InitializePreviewNavigation(filePath, previewFilePaths);
        ShowPreview(filePath);
    }

    private void InitializePreviewNavigation(string filePath, string[]? explorerViewFilePaths)
    {
        var fullFilePath = Path.GetFullPath(filePath);
        var directoryPath = Path.GetDirectoryName(fullFilePath);
        if (directoryPath is null)
        {
            return;
        }

        if (explorerViewFilePaths is { Length: > 0 })
        {
            _previewFilePaths = explorerViewFilePaths
                .Where(File.Exists)
                .Where(IsSupportedPreviewFile)
                .Select(Path.GetFullPath)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            _currentPreviewFileIndex = Array.FindIndex(
                _previewFilePaths,
                path => string.Equals(path, fullFilePath, StringComparison.OrdinalIgnoreCase));
        }

        if (_currentPreviewFileIndex < 0)
        {
            try
            {
                _previewFilePaths = Directory
                    .EnumerateFiles(directoryPath)
                    .Where(IsSupportedPreviewFile)
                    .OrderBy(path => Path.GetFileName(path), StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                _currentPreviewFileIndex = Array.FindIndex(
                    _previewFilePaths,
                    path => string.Equals(path, fullFilePath, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception)
            {
                // Folder navigation is optional; the selected file can still open.
            }
        }

        if (_currentPreviewFileIndex < 0)
        {
            _previewFilePaths = [fullFilePath];
            _currentPreviewFileIndex = 0;
        }

        _requestedPreviewFileIndex = _currentPreviewFileIndex;
    }

    private void ShowPreview(string filePath)
    {
        if (IsTextPreviewFile(filePath))
        {
            ShowTextPreview(filePath);
        }
        else
        {
            ShowImagePreview(filePath);
        }

        PreviewFileChanged?.Invoke(Path.GetFullPath(filePath));
    }

    internal bool TryShowExplorerSelection(string filePath, string[]? previewFilePaths)
    {
        if (_isPreviewTransitioning || !File.Exists(filePath) || !IsSupportedPreviewFile(filePath))
        {
            return false;
        }

        var fullFilePath = Path.GetFullPath(filePath);
        if (IsPreviewingFile(fullFilePath))
        {
            return false;
        }

        InitializePreviewNavigation(fullFilePath, previewFilePaths);
        ShowPreview(fullFilePath);
        return true;
    }

    internal bool IsPreviewingFile(string filePath)
    {
        return string.Equals(
            _filePath,
            Path.GetFullPath(filePath),
            StringComparison.OrdinalIgnoreCase);
    }

    private void ShowImagePreview(string imagePath)
    {
        BitmapImage? image = null;
        var targetSize = (Width, Height);
        string? errorMessage = null;

        try
        {
            var imageInfo = ReadImageInfo(imagePath);
            var decodeScale = GetDecodeScale(imageInfo);

            image = new BitmapImage();
            image.BeginInit();
            image.UriSource = new Uri(Path.GetFullPath(imagePath));
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.Rotation = imageInfo.Rotation;
            if (decodeScale < 1)
            {
                image.DecodePixelWidth = (int)Math.Ceiling(imageInfo.PixelWidth * decodeScale);
            }
            image.EndInit();

            targetSize = GetInitialImageWindowSize(imageInfo, decodeScale);
        }
        catch (Exception) when (IsWebp(imagePath))
        {
            errorMessage = "WEBP preview requires a Windows WebP image codec.";
        }
        catch (Exception)
        {
            errorMessage = "The image could not be decoded.";
        }

        if (image is not null)
        {
            SetPreviewWindowSize(targetSize.Width, targetSize.Height);
        }

        _filePath = Path.GetFullPath(imagePath);
        PreviewText.Visibility = Visibility.Collapsed;
        PreviewText.Text = string.Empty;
        PreviewMarkdownBackground.Visibility = Visibility.Collapsed;
        PreviewMarkdown.Visibility = Visibility.Collapsed;
        PreviewMarkdown.Document = null;
        PreviewImage.Visibility = Visibility.Visible;
        PreviewImage.Source = image;
        Title = $"QPeek - {Path.GetFileName(imagePath)}";

        if (image is null)
        {
            ShowStatus(errorMessage ?? "The image could not be displayed.");
            return;
        }

        HideStatus();
    }

    private void NavigatePreview(int offset)
    {
        var targetIndex = _requestedPreviewFileIndex + offset;
        if (_requestedPreviewFileIndex < 0 || targetIndex < 0 || targetIndex >= _previewFilePaths.Length)
        {
            return;
        }

        _requestedPreviewFileIndex = targetIndex;
        ApplyRequestedPreview();
    }

    private void ApplyRequestedPreview()
    {
        if (_isPreviewTransitioning ||
            _requestedPreviewFileIndex < 0 ||
            _requestedPreviewFileIndex == _currentPreviewFileIndex)
        {
            return;
        }

        var currentPath = _previewFilePaths[_currentPreviewFileIndex];
        var requestedPath = _previewFilePaths[_requestedPreviewFileIndex];
        if (IsTextPreviewFile(currentPath) == IsTextPreviewFile(requestedPath))
        {
            _currentPreviewFileIndex = _requestedPreviewFileIndex;
            ShowPreview(requestedPath);
            return;
        }

        BeginCrossTypeTransition();
    }

    private void BeginCrossTypeTransition()
    {
        _isPreviewTransitioning = true;
        _transitionCenter = GetCurrentWindowCenter();
        var fadeOut = new DoubleAnimation(
            PreviewContent.Opacity,
            0,
            TimeSpan.FromMilliseconds(PreviewFadeOutMilliseconds))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        };

        fadeOut.Completed += (_, _) =>
        {
            if (!IsVisible)
            {
                return;
            }

            _currentPreviewFileIndex = _requestedPreviewFileIndex;
            ShowPreview(_previewFilePaths[_currentPreviewFileIndex]);

            if (_animatedWindowBounds is null)
            {
                BeginPreviewFadeIn();
            }
        };

        PreviewContent.BeginAnimation(OpacityProperty, fadeOut);
    }

    private void BeginPreviewFadeIn()
    {
        var fadeIn = new DoubleAnimation(
            0,
            1,
            TimeSpan.FromMilliseconds(PreviewFadeInMilliseconds))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        fadeIn.Completed += (_, _) =>
        {
            if (!IsVisible)
            {
                return;
            }

            PreviewContent.BeginAnimation(OpacityProperty, null);
            PreviewContent.Opacity = 1;
            _isPreviewTransitioning = false;
            _transitionCenter = null;
            ApplyRequestedPreview();
        };
        PreviewContent.BeginAnimation(OpacityProperty, fadeIn);
    }

    private void ShowTextPreview(string textPath)
    {
        string? text = null;
        string? errorMessage = null;
        try
        {
            text = File.ReadAllText(textPath);
        }
        catch (Exception)
        {
            errorMessage = "The text file could not be read.";
        }

        var targetSize = GetInitialTextWindowSize();
        SetPreviewWindowSize(targetSize.Width, targetSize.Height);
        _filePath = Path.GetFullPath(textPath);
        PreviewImage.Visibility = Visibility.Collapsed;
        PreviewImage.Source = null;
        ShowTextContent(textPath, text, ref errorMessage);
        Title = $"QPeek - {Path.GetFileName(textPath)}";

        if (text is not null)
        {
            if (errorMessage is not null)
            {
                ShowStatus(errorMessage);
            }
            else if (text.Length == 0)
            {
                ShowStatus($"{Path.GetFileName(textPath)} (empty file)");
            }
            else
            {
                HideStatus();
            }
        }
        else
        {
            ShowStatus(errorMessage ?? "The text file could not be read.");
        }
    }

    private void ShowTextContent(string textPath, string? text, ref string? errorMessage)
    {
        PreviewMarkdownBackground.Visibility = Visibility.Collapsed;
        PreviewMarkdown.Visibility = Visibility.Collapsed;
        PreviewMarkdown.Document = null;

        if (text is not null && IsMarkdownFile(textPath))
        {
            try
            {
                var document = _markdown.Transform(NormalizeMarkdownListIndentation(text));
                FormatMarkdownTaskLists(document.Blocks);
                FormatMarkdownCodeBlocks(document.Blocks);
                PreviewMarkdown.Document = document;
                PreviewMarkdownBackground.Visibility = Visibility.Visible;
                PreviewMarkdown.Visibility = Visibility.Visible;
                PreviewText.Visibility = Visibility.Collapsed;
                PreviewText.Text = string.Empty;
                return;
            }
            catch (Exception)
            {
                errorMessage = "Markdown rendering failed; showing source.";
            }
        }

        PreviewText.Visibility = Visibility.Visible;
        PreviewText.Text = text ?? string.Empty;
        PreviewText.FontFamily = IsMarkdownFile(textPath)
            ? new System.Windows.Media.FontFamily("Consolas")
            : new System.Windows.Media.FontFamily("Segoe UI");
    }

    private static string NormalizeMarkdownListIndentation(string markdown)
    {
        var lines = markdown.Split('\n');
        char? fenceCharacter = null;
        var fenceLength = 0;
        var listIndentationUnit = 0;
        var isInList = false;

        for (var index = 0; index < lines.Length; index++)
        {
            var line = lines[index];
            if (TryReadMarkdownFence(line, out var currentFenceCharacter, out var currentFenceLength, out var isClosingFence))
            {
                if (fenceCharacter is null)
                {
                    fenceCharacter = currentFenceCharacter;
                    fenceLength = currentFenceLength;
                }
                else if (currentFenceCharacter == fenceCharacter &&
                         currentFenceLength >= fenceLength &&
                         isClosingFence)
                {
                    fenceCharacter = null;
                    fenceLength = 0;
                }

                continue;
            }

            if (fenceCharacter is null && TryGetListItemIndentation(line, out var indentation))
            {
                if (indentation > 0)
                {
                    if (!isInList || listIndentationUnit == 0)
                    {
                        listIndentationUnit = indentation;
                    }

                    if (indentation % listIndentationUnit == 0)
                    {
                        var normalizedIndentation = indentation / listIndentationUnit * 4;
                        lines[index] = new string(' ', normalizedIndentation) + line[indentation..];
                    }
                }

                isInList = true;
                continue;
            }

            if (fenceCharacter is null &&
                !string.IsNullOrWhiteSpace(line) &&
                line[0] is not ' ' and not '\t')
            {
                isInList = false;
                listIndentationUnit = 0;
            }
        }

        return string.Join('\n', lines);
    }

    private static bool TryReadMarkdownFence(
        string line,
        out char fenceCharacter,
        out int fenceLength,
        out bool isClosingFence)
    {
        fenceCharacter = default;
        fenceLength = 0;
        isClosingFence = false;

        var leadingSpaces = 0;
        while (leadingSpaces < line.Length && line[leadingSpaces] == ' ')
        {
            leadingSpaces++;
        }

        if (leadingSpaces > 3 || leadingSpaces >= line.Length ||
            (line[leadingSpaces] != '`' && line[leadingSpaces] != '~'))
        {
            return false;
        }

        fenceCharacter = line[leadingSpaces];
        var position = leadingSpaces;
        while (position < line.Length && line[position] == fenceCharacter)
        {
            fenceLength++;
            position++;
        }

        if (fenceLength < 3)
        {
            return false;
        }

        isClosingFence = line.AsSpan(position).Trim().IsEmpty;
        return true;
    }

    private static bool TryGetListItemIndentation(string line, out int indentation)
    {
        indentation = 0;
        while (indentation < line.Length && line[indentation] == ' ')
        {
            indentation++;
        }

        if (indentation >= line.Length || line[indentation] == '\t')
        {
            return false;
        }

        var markerStart = indentation;
        if (line[markerStart] is '-' or '+' or '*')
        {
            return markerStart + 1 < line.Length && char.IsWhiteSpace(line[markerStart + 1]);
        }

        var position = markerStart;
        while (position < line.Length && char.IsDigit(line[position]))
        {
            position++;
        }

        return position > markerStart &&
               position + 1 < line.Length &&
               line[position] is '.' or ')' &&
               char.IsWhiteSpace(line[position + 1]);
    }

    private static void FormatMarkdownTaskLists(BlockCollection blocks)
    {
        foreach (var block in blocks.Cast<Block>().ToArray())
        {
            switch (block)
            {
                case System.Windows.Documents.List list:
                    FormatMarkdownTaskList(list);
                    foreach (var item in list.ListItems.Cast<ListItem>().ToArray())
                    {
                        FormatMarkdownTaskLists(item.Blocks);
                    }
                    break;
                case Section section:
                    FormatMarkdownTaskLists(section.Blocks);
                    break;
            }
        }
    }

    private static void FormatMarkdownTaskList(System.Windows.Documents.List list)
    {
        if (!IsUnorderedList(list.MarkerStyle))
        {
            return;
        }

        if (!list.ListItems.Cast<ListItem>().Any(IsMarkdownTaskListItem))
        {
            list.MarkerStyle = TextMarkerStyle.Disc;
            list.MarkerOffset = 12;
            return;
        }

        list.MarkerStyle = TextMarkerStyle.None;
        list.MarkerOffset = 0;

        foreach (var item in list.ListItems.Cast<ListItem>().ToArray())
        {
            if (TryReplaceMarkdownTaskMarker(item))
            {
                continue;
            }

            AddPlainListMarker(item);
        }
    }

    private static void FormatMarkdownCodeBlocks(BlockCollection blocks)
    {
        foreach (var block in blocks.Cast<Block>().ToArray())
        {
            switch (block)
            {
                case BlockUIContainer { Tag: "CodeBlock", Child: TextEditor editor }:
                    editor.FontFamily = new System.Windows.Media.FontFamily("Consolas");
                    editor.FontSize = 13;
                    editor.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x24, 0x29, 0x2F));
                    editor.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0xF1, 0xF3, 0xF5));
                    editor.Padding = new Thickness(14, 10, 14, 10);
                    editor.BorderThickness = new Thickness(0);
                    editor.WordWrap = true;
                    editor.ShowLineNumbers = false;
                    break;
                case System.Windows.Documents.List list:
                    foreach (var item in list.ListItems.Cast<ListItem>().ToArray())
                    {
                        FormatMarkdownCodeBlocks(item.Blocks);
                    }
                    break;
                case Section section:
                    FormatMarkdownCodeBlocks(section.Blocks);
                    break;
            }
        }
    }

    private static bool IsUnorderedList(TextMarkerStyle markerStyle)
    {
        return markerStyle is TextMarkerStyle.Disc or
               TextMarkerStyle.Circle or
               TextMarkerStyle.Square or
               TextMarkerStyle.Box;
    }

    private static bool IsMarkdownTaskListItem(ListItem item)
    {
        return TryReadMarkdownTaskMarker(item, out _, out _, out _);
    }

    private static bool TryReplaceMarkdownTaskMarker(ListItem item)
    {
        if (!TryReadMarkdownTaskMarker(item, out var paragraph, out var markerRun, out var isChecked))
        {
            return false;
        }

        markerRun.Text = markerRun.Text[3..].TrimStart();
        var checkbox = new System.Windows.Controls.CheckBox
        {
            IsChecked = isChecked,
            IsHitTestVisible = false,
            Focusable = false,
            IsTabStop = false,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 7, 0)
        };
        var container = new InlineUIContainer(checkbox)
        {
            BaselineAlignment = BaselineAlignment.Center
        };
        var firstInline = paragraph.Inlines.FirstInline;
        if (firstInline is null)
        {
            paragraph.Inlines.Add(container);
        }
        else
        {
            paragraph.Inlines.InsertBefore(firstInline, container);
        }

        return true;
    }

    private static bool TryReadMarkdownTaskMarker(
        ListItem item,
        out Paragraph paragraph,
        out Run markerRun,
        out bool isChecked)
    {
        paragraph = null!;
        markerRun = null!;
        isChecked = false;

        if (item.Blocks.FirstBlock is not Paragraph firstParagraph ||
            FindFirstRun(firstParagraph.Inlines) is not Run firstRun ||
            firstRun.Text.Length < 3 ||
            firstRun.Text[0] != '[' ||
            firstRun.Text[2] != ']' ||
            (firstRun.Text.Length > 3 && !char.IsWhiteSpace(firstRun.Text[3])) ||
            firstRun.Text[1] is not ('x' or 'X' or ' '))
        {
            return false;
        }

        paragraph = firstParagraph;
        markerRun = firstRun;
        isChecked = firstRun.Text[1] is 'x' or 'X';
        return true;
    }

    private static Run? FindFirstRun(InlineCollection inlines)
    {
        foreach (var inline in inlines)
        {
            if (inline is Run run)
            {
                return run;
            }

            if (inline is Span span && FindFirstRun(span.Inlines) is Run nestedRun)
            {
                return nestedRun;
            }
        }

        return null;
    }

    private static void AddPlainListMarker(ListItem item)
    {
        if (item.Blocks.FirstBlock is not Paragraph paragraph)
        {
            return;
        }

        var marker = new Run("• ")
        {
            Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(87, 96, 106))
        };
        var firstInline = paragraph.Inlines.FirstInline;
        if (firstInline is null)
        {
            paragraph.Inlines.Add(marker);
        }
        else
        {
            paragraph.Inlines.InsertBefore(firstInline, marker);
        }
    }

    private static void OpenMarkdownLink(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
        }
        catch (Exception)
        {
            // A broken link or missing browser should not interrupt the preview.
        }
    }

    private void SetPreviewWindowSize(double width, double height)
    {
        if (!IsLoaded)
        {
            Width = width;
            Height = height;
            return;
        }

        var center = _transitionCenter ?? GetCurrentWindowCenter();

        if (double.IsFinite(center.X) && double.IsFinite(center.Y))
        {
            var targetBounds = KeepPreviewWindowVisible(new Rect(
                center.X - width / 2,
                center.Y - height / 2,
                width,
                height));

            if (_isPreviewTransitioning)
            {
                AnimatePreviewWindow(targetBounds);
                return;
            }

            Left = targetBounds.Left;
            Top = targetBounds.Top;
        }

        Width = width;
        Height = height;
    }

    private void AnimatePreviewWindow(Rect targetBounds)
    {
        _animatedWindowBounds = targetBounds;
        var duration = TimeSpan.FromMilliseconds(PreviewFadeInMilliseconds);
        var heightAnimation = CreateWindowAnimation(ActualHeight, targetBounds.Height, duration);
        heightAnimation.Completed += (_, _) =>
        {
            if (!IsVisible)
            {
                return;
            }

            FinishPreviewWindowAnimation();
            BeginPreviewFadeIn();
        };

        BeginAnimation(LeftProperty, CreateWindowAnimation(Left, targetBounds.Left, duration));
        BeginAnimation(TopProperty, CreateWindowAnimation(Top, targetBounds.Top, duration));
        BeginAnimation(WidthProperty, CreateWindowAnimation(ActualWidth, targetBounds.Width, duration));
        BeginAnimation(HeightProperty, heightAnimation);
    }

    private static DoubleAnimation CreateWindowAnimation(double from, double to, TimeSpan duration)
    {
        return new DoubleAnimation(from, to, duration)
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
        };
    }

    private void FinishPreviewWindowAnimation()
    {
        if (_animatedWindowBounds is not Rect targetBounds)
        {
            return;
        }

        BeginAnimation(LeftProperty, null);
        BeginAnimation(TopProperty, null);
        BeginAnimation(WidthProperty, null);
        BeginAnimation(HeightProperty, null);
        Left = targetBounds.Left;
        Top = targetBounds.Top;
        Width = targetBounds.Width;
        Height = targetBounds.Height;
        _animatedWindowBounds = null;
    }

    private System.Windows.Point GetCurrentWindowCenter()
    {
        var width = double.IsFinite(Width) ? Width : ActualWidth;
        var height = double.IsFinite(Height) ? Height : ActualHeight;
        return new System.Windows.Point(Left + width / 2, Top + height / 2);
    }

    private static Rect KeepPreviewWindowVisible(Rect bounds)
    {
        const double minimumVisibleSize = 50;
        var virtualScreenRight = SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth;
        var virtualScreenBottom = SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight;
        var left = Math.Clamp(
            bounds.Left,
            SystemParameters.VirtualScreenLeft - bounds.Width + minimumVisibleSize,
            virtualScreenRight - minimumVisibleSize);
        var top = Math.Clamp(
            bounds.Top,
            SystemParameters.VirtualScreenTop - bounds.Height + minimumVisibleSize,
            virtualScreenBottom - minimumVisibleSize);
        return new Rect(left, top, bounds.Width, bounds.Height);
    }

    protected override void OnClosed(EventArgs e)
    {
        FinishPreviewWindowAnimation();
        base.OnClosed(e);
    }

    private static (double Width, double Height) GetInitialTextWindowSize()
    {
        var workArea = SystemParameters.WorkArea;
        var maximumWidth = Math.Max(MinimumWindowWidth, workArea.Width * MaximumScreenFraction);
        var maximumHeight = Math.Max(MinimumWindowHeight, workArea.Height * MaximumScreenFraction);
        return (
            Math.Clamp(DefaultTextWindowWidth, MinimumWindowWidth, maximumWidth),
            Math.Clamp(DefaultTextWindowHeight, MinimumWindowHeight, maximumHeight));
    }

    private static (double Width, double Height) GetInitialImageWindowSize(
        (int PixelWidth, int PixelHeight, Rotation Rotation) imageInfo,
        double scale)
    {
        var (maximumImageWidth, maximumImageHeight) = GetMaximumImageSize();
        var imageWidth = imageInfo.PixelWidth * scale;
        var imageHeight = imageInfo.PixelHeight * scale;

        if (imageInfo.Rotation is Rotation.Rotate90 or Rotation.Rotate270)
        {
            (imageWidth, imageHeight) = (imageHeight, imageWidth);
        }

        return (
            Math.Clamp(imageWidth + WindowPadding, MinimumWindowWidth, maximumImageWidth + WindowPadding),
            Math.Clamp(imageHeight + WindowPadding + StatusTextHeight, MinimumWindowHeight, maximumImageHeight + WindowPadding + StatusTextHeight));
    }

    private static double GetDecodeScale((int PixelWidth, int PixelHeight, Rotation Rotation) imageInfo)
    {
        var (maximumImageWidth, maximumImageHeight) = GetMaximumImageSize();
        var displayWidth = imageInfo.PixelWidth;
        var displayHeight = imageInfo.PixelHeight;

        if (imageInfo.Rotation is Rotation.Rotate90 or Rotation.Rotate270)
        {
            (displayWidth, displayHeight) = (displayHeight, displayWidth);
        }

        return Math.Min(1, Math.Min(
            maximumImageWidth / displayWidth,
            maximumImageHeight / displayHeight));
    }

    private static (double Width, double Height) GetMaximumImageSize()
    {
        var workArea = SystemParameters.WorkArea;
        var maximumWindowWidth = Math.Max(MinimumWindowWidth, workArea.Width * MaximumScreenFraction);
        var maximumWindowHeight = Math.Max(MinimumWindowHeight, workArea.Height * MaximumScreenFraction);
        return (maximumWindowWidth - WindowPadding, maximumWindowHeight - WindowPadding - StatusTextHeight);
    }

    private static bool IsSupportedImage(string path)
    {
        var extension = Path.GetExtension(path);
        return string.Equals(extension, ".jpg", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(extension, ".jpeg", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(extension, ".png", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(extension, ".webp", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSupportedPreviewFile(string path)
    {
        return IsSupportedImage(path) || IsTextPreviewFile(path);
    }

    private static bool IsWebp(string path)
    {
        return string.Equals(Path.GetExtension(path), ".webp", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsTextPreviewFile(string path)
    {
        var extension = Path.GetExtension(path);
        return string.Equals(extension, ".txt", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(extension, ".md", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsMarkdownFile(string path)
    {
        return string.Equals(Path.GetExtension(path), ".md", StringComparison.OrdinalIgnoreCase);
    }

    private static (int PixelWidth, int PixelHeight, Rotation Rotation) ReadImageInfo(string imagePath)
    {
        var decoder = BitmapDecoder.Create(
            new Uri(Path.GetFullPath(imagePath)),
            BitmapCreateOptions.DelayCreation,
            BitmapCacheOption.None);
        var frame = decoder.Frames[0];
        var rotation = frame.Metadata as BitmapMetadata;

        return (frame.PixelWidth, frame.PixelHeight, rotation?.GetQuery("/app1/ifd/{ushort=274}") switch
        {
            ushort value when value == 3 => Rotation.Rotate180,
            ushort value when value == 6 => Rotation.Rotate90,
            ushort value when value == 8 => Rotation.Rotate270,
            _ => Rotation.Rotate0
        });
    }

    private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Escape:
                Close();
                e.Handled = true;
                break;
            case Key.Left when e.KeyboardDevice.Modifiers == ModifierKeys.None:
                NavigatePreview(-1);
                e.Handled = true;
                break;
            case Key.Right when e.KeyboardDevice.Modifiers == ModifierKeys.None:
                NavigatePreview(1);
                e.Handled = true;
                break;
        }
    }

    internal void CopyFileToClipboard()
    {
        if (_filePath is null)
        {
            return;
        }

        try
        {
            var files = new StringCollection
            {
                _filePath
            };
            System.Windows.Clipboard.SetFileDropList(files);
            ShowStatus($"Copied {Path.GetFileName(_filePath)}");
        }
        catch (Exception)
        {
            ShowStatus("The file could not be copied.");
        }
    }

    private void ShowStatus(string message)
    {
        StatusText.Text = message;
        StatusText.Visibility = Visibility.Visible;
    }

    private void HideStatus()
    {
        StatusText.Text = string.Empty;
        StatusText.Visibility = Visibility.Collapsed;
    }
}
