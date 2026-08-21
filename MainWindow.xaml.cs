using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace QuickLook;

public partial class MainWindow : Window
{
    private const double MinimumWindowWidth = 320;
    private const double MinimumWindowHeight = 240;
    private const double WindowPadding = 24;
    private const double StatusTextHeight = 36;
    private const double MaximumScreenFraction = 0.8;

    public MainWindow(string? imagePath)
    {
        InitializeComponent();

        if (string.IsNullOrWhiteSpace(imagePath))
        {
            StatusText.Text = "Select an image in Explorer, then run the app.";
            return;
        }

        if (!File.Exists(imagePath))
        {
            StatusText.Text = "The image file was not found.";
            return;
        }

        if (!IsSupportedImage(imagePath))
        {
            StatusText.Text = "Step 3 supports JPG, JPEG, PNG, and WEBP files.";
            return;
        }

        BitmapImage image;
        try
        {
            image = new BitmapImage();
            image.BeginInit();
            image.UriSource = new Uri(Path.GetFullPath(imagePath));
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.EndInit();
        }
        catch (Exception) when (IsWebp(imagePath))
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
        Title = $"Windows Quick Preview - {Path.GetFileName(imagePath)}";
        StatusText.Text = Path.GetFileName(imagePath);
        SetInitialWindowSize(image);
    }

    private void SetInitialWindowSize(BitmapImage image)
    {
        var workArea = SystemParameters.WorkArea;
        var maximumWidth = Math.Max(MinimumWindowWidth, workArea.Width * MaximumScreenFraction);
        var maximumHeight = Math.Max(MinimumWindowHeight, workArea.Height * MaximumScreenFraction);
        var maximumImageWidth = maximumWidth - WindowPadding;
        var maximumImageHeight = maximumHeight - WindowPadding - StatusTextHeight;

        var scale = Math.Min(1, Math.Min(
            maximumImageWidth / image.PixelWidth,
            maximumImageHeight / image.PixelHeight));

        Width = Math.Clamp(image.PixelWidth * scale + WindowPadding, MinimumWindowWidth, maximumWidth);
        Height = Math.Clamp(image.PixelHeight * scale + WindowPadding + StatusTextHeight, MinimumWindowHeight, maximumHeight);
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

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
        }
    }
}
