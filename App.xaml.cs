using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace QuickLook;

public partial class App : Application
{
    private const string WindowPositionFormat = "center-v1";
    private GlobalKeyboardHook? _keyboardHook;
    private MainWindow? _previewWindow;
    private bool _isOpeningPreview;
    private nint _previewExplorerWindowHandle;

    private void App_Startup(object sender, StartupEventArgs e)
    {
        _keyboardHook = new GlobalKeyboardHook
        {
            SpacePressed = HandleSpacePressed,
            EnterPressed = HandleEnterPressed,
            CopyPressed = HandleCopyPressed
        };
        _keyboardHook.Start();
    }

    private void App_Exit(object sender, ExitEventArgs e)
    {
        _keyboardHook?.Dispose();
    }

    private bool HandleSpacePressed()
    {
        if (_previewWindow is not null)
        {
            Dispatcher.BeginInvoke(_previewWindow.Close);
            return true;
        }

        if (_isOpeningPreview)
        {
            return true;
        }

        var foregroundWindow = GetForegroundWindow();
        if (!IsExplorerWindow(foregroundWindow))
        {
            return false;
        }

        _isOpeningPreview = true;
        Dispatcher.BeginInvoke(() => OpenSelectedExplorerImage(foregroundWindow));
        return true;
    }

    private bool HandleEnterPressed()
    {
        if (_previewWindow is null)
        {
            return false;
        }

        var previewWindow = _previewWindow;
        var explorerWindowHandle = _previewExplorerWindowHandle;

        if (Dispatcher.CheckAccess())
        {
            previewWindow.Close();
        }
        else
        {
            Dispatcher.Invoke(previewWindow.Close);
        }

        if (explorerWindowHandle != 0)
        {
            SetForegroundWindow(explorerWindowHandle);
        }

        // Let the original Enter continue to Explorer so Windows performs
        // its normal "open with the default app" behavior.
        return false;
    }

    private bool HandleCopyPressed()
    {
        if (_previewWindow is null)
        {
            return false;
        }

        var previewWindow = _previewWindow;
        Dispatcher.BeginInvoke(previewWindow.CopyFileToClipboard);
        return true;
    }

    private void OpenSelectedExplorerImage(nint explorerWindowHandle)
    {
        _isOpeningPreview = false;

        var imagePath = GetSelectedExplorerImageFilePath(explorerWindowHandle);
        if (imagePath is null)
        {
            return;
        }

        _previewWindow = new MainWindow(imagePath);
        var previewWindow = _previewWindow;
        ApplySavedPreviewWindowPosition(previewWindow);
        _previewExplorerWindowHandle = explorerWindowHandle;
        previewWindow.Closed += (_, _) =>
        {
            SavePreviewWindowPosition(previewWindow);
            _previewWindow = null;
            _previewExplorerWindowHandle = 0;
        };
        var previewWindowHandle = new WindowInteropHelper(previewWindow).EnsureHandle();
        var previewThreadId = GetWindowThreadProcessId(previewWindowHandle, out _);
        var explorerThreadId = GetWindowThreadProcessId(explorerWindowHandle, out _);
        var inputThreadsAttached =
            previewThreadId != 0 &&
            explorerThreadId != 0 &&
            previewThreadId != explorerThreadId &&
            AttachThreadInput(previewThreadId, explorerThreadId, true);

        try
        {
            // Temporarily share input state with Explorer so Windows accepts
            // the foreground hand-off. The preview remains a normal window.
            previewWindow.Show();
            SetForegroundWindow(previewWindowHandle);
            previewWindow.Activate();
        }
        finally
        {
            if (inputThreadsAttached)
            {
                AttachThreadInput(previewThreadId, explorerThreadId, false);
            }
        }
    }

    private static void ApplySavedPreviewWindowPosition(MainWindow previewWindow)
    {
        try
        {
            var positionFilePath = GetWindowPositionFilePath();
            if (!File.Exists(positionFilePath))
            {
                return;
            }

            var coordinates = File.ReadAllLines(positionFilePath);
            double left;
            double top;

            if (coordinates.Length >= 3 &&
                string.Equals(coordinates[0], WindowPositionFormat, StringComparison.Ordinal) &&
                double.TryParse(coordinates[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var centerX) &&
                double.TryParse(coordinates[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var centerY))
            {
                left = centerX - previewWindow.Width / 2;
                top = centerY - previewWindow.Height / 2;
            }
            else if (coordinates.Length >= 2 &&
                     double.TryParse(coordinates[0], NumberStyles.Float, CultureInfo.InvariantCulture, out left) &&
                     double.TryParse(coordinates[1], NumberStyles.Float, CultureInfo.InvariantCulture, out top))
            {
                // The original two-line format stored Left and Top. It is
                // converted to the center format the next time the window closes.
            }
            else
            {
                return;
            }

            if (!IsVisibleOnCurrentDesktop(previewWindow, left, top))
            {
                return;
            }

            previewWindow.WindowStartupLocation = WindowStartupLocation.Manual;
            previewWindow.Left = left;
            previewWindow.Top = top;
        }
        catch (Exception)
        {
            // A missing or unreadable optional setting must not block preview.
        }
    }

    private static void SavePreviewWindowPosition(MainWindow previewWindow)
    {
        var centerX = previewWindow.Left + previewWindow.ActualWidth / 2;
        var centerY = previewWindow.Top + previewWindow.ActualHeight / 2;

        if (!double.IsFinite(centerX) || !double.IsFinite(centerY))
        {
            return;
        }

        try
        {
            var positionFilePath = GetWindowPositionFilePath();
            Directory.CreateDirectory(Path.GetDirectoryName(positionFilePath)!);
            File.WriteAllLines(positionFilePath,
            [
                WindowPositionFormat,
                centerX.ToString(CultureInfo.InvariantCulture),
                centerY.ToString(CultureInfo.InvariantCulture)
            ]);
        }
        catch (Exception)
        {
            // Saving the optional position must not interfere with closing.
        }
    }

    private static string GetWindowPositionFilePath()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "QuickLook",
            "window-position.txt");
    }

    private static bool IsVisibleOnCurrentDesktop(MainWindow previewWindow, double left, double top)
    {
        const double minimumVisibleSize = 50;
        var virtualScreenRight = SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth;
        var virtualScreenBottom = SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight;

        return double.IsFinite(left) &&
               double.IsFinite(top) &&
               left + previewWindow.Width >= SystemParameters.VirtualScreenLeft + minimumVisibleSize &&
               left <= virtualScreenRight - minimumVisibleSize &&
               top + previewWindow.Height >= SystemParameters.VirtualScreenTop + minimumVisibleSize &&
               top <= virtualScreenBottom - minimumVisibleSize;
    }

    private static bool IsExplorerWindow(nint windowHandle)
    {
        if (windowHandle == 0)
        {
            return false;
        }

        GetWindowThreadProcessId(windowHandle, out var processId);
        if (processId == 0)
        {
            return false;
        }

        using var process = Process.GetProcessById((int)processId);
        return string.Equals(process.ProcessName, "explorer", StringComparison.OrdinalIgnoreCase);
    }

    private static string? GetSelectedExplorerImageFilePath(nint explorerWindowHandle)
    {
        var shellType = Type.GetTypeFromProgID("Shell.Application");
        if (shellType is null)
        {
            return null;
        }

        dynamic shell = Activator.CreateInstance(shellType)!;
        dynamic windows = shell.Windows();

        for (var index = 0; index < windows.Count; index++)
        {
            try
            {
                dynamic explorerWindow = windows.Item(index);
                if ((nint)explorerWindow.HWND != explorerWindowHandle)
                {
                    continue;
                }

                dynamic selectedItems = explorerWindow.Document.SelectedItems();
                for (var itemIndex = 0; itemIndex < selectedItems.Count; itemIndex++)
                {
                    var selectedPath = (string?)selectedItems.Item(itemIndex).Path;
                    if (IsSupportedImage(selectedPath))
                    {
                        return selectedPath;
                    }
                }
            }
            catch
            {
                // Explorer can close or change its view while this loop is running.
            }
        }

        return null;
    }

    private static bool IsSupportedImage(string? path)
    {
        var extension = Path.GetExtension(path);
        return string.Equals(extension, ".jpg", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(extension, ".jpeg", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(extension, ".png", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(extension, ".webp", StringComparison.OrdinalIgnoreCase);
    }

    [DllImport("user32.dll")]
    private static extern nint GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(nint windowHandle);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool AttachThreadInput(
        uint firstThreadId,
        uint secondThreadId,
        bool attach);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(nint windowHandle, out uint processId);
}
