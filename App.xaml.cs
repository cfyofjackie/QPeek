using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;

namespace QuickLook;

public partial class App : Application
{
    private GlobalKeyboardHook? _keyboardHook;
    private MainWindow? _previewWindow;
    private bool _isOpeningPreview;

    private void App_Startup(object sender, StartupEventArgs e)
    {
        _keyboardHook = new GlobalKeyboardHook
        {
            SpacePressed = HandleSpacePressed
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
        Dispatcher.BeginInvoke(() => OpenSelectedExplorerJpg(foregroundWindow));
        return true;
    }

    private void OpenSelectedExplorerJpg(nint explorerWindowHandle)
    {
        _isOpeningPreview = false;

        var imagePath = GetSelectedExplorerJpgFilePath(explorerWindowHandle);
        if (imagePath is null)
        {
            return;
        }

        _previewWindow = new MainWindow(imagePath);
        _previewWindow.Closed += (_, _) => _previewWindow = null;
        _previewWindow.Show();
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

    private static string? GetSelectedExplorerJpgFilePath(nint explorerWindowHandle)
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
                    if (IsJpg(selectedPath))
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

    private static bool IsJpg(string? path)
    {
        var extension = Path.GetExtension(path);
        return string.Equals(extension, ".jpg", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(extension, ".jpeg", StringComparison.OrdinalIgnoreCase);
    }

    [DllImport("user32.dll")]
    private static extern nint GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(nint windowHandle, out uint processId);
}
