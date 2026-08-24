using System;
using System.Collections.Specialized;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

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
    private readonly string? _filePath;

    public MainWindow(string? filePath)
    {
        InitializeComponent();

        if (string.IsNullOrWhiteSpace(filePath))
        {
            StatusText.Text = "Select a supported file in Explorer, then run the app.";
            return;
        }

        if (!File.Exists(filePath))
        {
            StatusText.Text = "The file was not found.";
            return;
        }

        _filePath = Path.GetFullPath(filePath);

        if (IsTextPreviewFile(filePath))
        {
            ShowTextPreview(filePath);
            return;
        }

        if (!IsSupportedImage(filePath))
        {
            StatusText.Text = "This file type is not supported.";
            return;
        }

        BitmapImage image;
        try
        {
            var imageInfo = ReadImageInfo(filePath);
            var decodeScale = GetDecodeScale(imageInfo);

            image = new BitmapImage();
            image.BeginInit();
            image.UriSource = new Uri(Path.GetFullPath(filePath));
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.Rotation = imageInfo.Rotation;
            if (decodeScale < 1)
            {
                image.DecodePixelWidth = (int)Math.Ceiling(imageInfo.PixelWidth * decodeScale);
            }
            image.EndInit();

            SetInitialImageWindowSize(imageInfo, decodeScale);
        }
        catch (Exception) when (IsWebp(filePath))
        {
            StatusText.Text = "WEBP preview requires a Windows WebP image codec.";
            return;
        }
        catch (Exception)
        {
            StatusText.Text = "The image could not be decoded.";
            return;
        }

        PreviewImage.Source = image;
        Title = $"Windows Quick Preview - {Path.GetFileName(filePath)}";
        StatusText.Text = Path.GetFileName(filePath);
    }

    private void ShowTextPreview(string textPath)
    {
        SetInitialTextWindowSize();
        PreviewImage.Visibility = Visibility.Collapsed;
        PreviewText.Visibility = Visibility.Visible;
        Title = $"Windows Quick Preview - {Path.GetFileName(textPath)}";

        if (IsMarkdownFile(textPath))
        {
            PreviewText.FontFamily = new FontFamily("Consolas");
        }

        try
        {
            PreviewText.Text = File.ReadAllText(textPath);
            var fileName = Path.GetFileName(textPath);
            StatusText.Text = PreviewText.Text.Length == 0
                ? $"{fileName} (empty file)"
                : fileName;
        }
        catch (Exception)
        {
            StatusText.Text = "The text file could not be read.";
        }
    }

    private void SetInitialTextWindowSize()
    {
        var workArea = SystemParameters.WorkArea;
        var maximumWidth = Math.Max(MinimumWindowWidth, workArea.Width * MaximumScreenFraction);
        var maximumHeight = Math.Max(MinimumWindowHeight, workArea.Height * MaximumScreenFraction);
        Width = Math.Clamp(DefaultTextWindowWidth, MinimumWindowWidth, maximumWidth);
        Height = Math.Clamp(DefaultTextWindowHeight, MinimumWindowHeight, maximumHeight);
    }

    private void SetInitialImageWindowSize((int PixelWidth, int PixelHeight, Rotation Rotation) imageInfo, double scale)
    {
        var (maximumImageWidth, maximumImageHeight) = GetMaximumImageSize();
        var imageWidth = imageInfo.PixelWidth * scale;
        var imageHeight = imageInfo.PixelHeight * scale;

        if (imageInfo.Rotation is Rotation.Rotate90 or Rotation.Rotate270)
        {
            (imageWidth, imageHeight) = (imageHeight, imageWidth);
        }

        Width = Math.Clamp(imageWidth + WindowPadding, MinimumWindowWidth, maximumImageWidth + WindowPadding);
        Height = Math.Clamp(imageHeight + WindowPadding + StatusTextHeight, MinimumWindowHeight, maximumImageHeight + WindowPadding + StatusTextHeight);
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

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
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
            Clipboard.SetFileDropList(files);
            StatusText.Text = $"Copied {Path.GetFileName(_filePath)}";
        }
        catch (Exception)
        {
            StatusText.Text = "The file could not be copied.";
        }
    }
}
