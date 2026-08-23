using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace QuickLook;

public partial class App : Application
{
    private static readonly nint HwndTopmost = -1;
    private static readonly nint HwndNotTopmost = -2;
    private const uint SwpNoSize = 0x0001;
    private const uint SwpNoMove = 0x0002;
    private const uint SwpNoActivate = 0x0010;

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
        _previewExplorerWindowHandle = explorerWindowHandle;
        previewWindow.Closed += (_, _) =>
        {
            _previewWindow = null;
            _previewExplorerWindowHandle = 0;
        };
        previewWindow.Show();
        previewWindow.Activate();
        var previewWindowHandle = new WindowInteropHelper(previewWindow).Handle;

        // Raise the preview above Explorer, then immediately return it to
        // the normal window level so later clicks can cover it naturally.
        SetWindowPos(
            previewWindowHandle,
            HwndTopmost,
            0,
            0,
            0,
            0,
            SwpNoSize | SwpNoMove | SwpNoActivate);
        SetWindowPos(
            previewWindowHandle,
            HwndNotTopmost,
            0,
            0,
            0,
            0,
            SwpNoSize | SwpNoMove | SwpNoActivate);
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
    private static extern bool SetWindowPos(
        nint windowHandle,
        nint insertAfterWindow,
        int x,
        int y,
        int width,
        int height,
        uint flags);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(nint windowHandle, out uint processId);
}
