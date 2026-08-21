using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace QuickLook;

public partial class MainWindow : Window
{
    public MainWindow(string? imagePath)
    {
        InitializeComponent();

        if (string.IsNullOrWhiteSpace(imagePath))
        {
            StatusText.Text = "Select a JPG in Explorer, then run the app.";
            return;
        }

        if (!File.Exists(imagePath))
        {
            StatusText.Text = "The JPG file was not found.";
            return;
        }

        var extension = Path.GetExtension(imagePath);
        if (!string.Equals(extension, ".jpg", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(extension, ".jpeg", StringComparison.OrdinalIgnoreCase))
        {
            StatusText.Text = "Step 1 supports JPG files only.";
            return;
        }

        var image = new BitmapImage();
        image.BeginInit();
        image.UriSource = new Uri(Path.GetFullPath(imagePath));
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.EndInit();

        PreviewImage.Source = image;
        Title = $"Windows Quick Preview - {Path.GetFileName(imagePath)}";
        StatusText.Text = Path.GetFileName(imagePath);
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
        }
    }
}
