using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace QuickLook;

internal static class ExplorerViewOrder
{
    private const uint AllItemsInViewOrder = 0x80000002;
    private const uint FileSystemPath = 0x80058000;

    public static string[] GetFilePaths(nint explorerWindowHandle)
    {
        try
        {
            var shellType = Type.GetTypeFromProgID("Shell.Application");
            if (shellType is null)
            {
                return [];
            }

            dynamic shell = Activator.CreateInstance(shellType)!;
            dynamic windows = shell.Windows();

            for (var index = 0; index < windows.Count; index++)
            {
                dynamic explorerWindow = windows.Item(index);
                if ((nint)explorerWindow.HWND == explorerWindowHandle)
                {
                    return ReadFilePaths(explorerWindow);
                }
            }
        }
        catch
        {
            // Navigation can fall back to filename order if Explorer changes.
        }

        return [];
    }

    private static string[] ReadFilePaths(object explorerWindow)
    {
        var services = (IServiceProvider)explorerWindow;
        var topLevelBrowserService = new Guid("4C96BE40-915C-11CF-99D3-00AA004AE837");
        var shellBrowserId = typeof(IShellBrowser).GUID;
        Marshal.ThrowExceptionForHR(services.QueryService(
            ref topLevelBrowserService,
            ref shellBrowserId,
            out var shellBrowserPointer));

        IShellBrowser shellBrowser;
        try
        {
            shellBrowser = (IShellBrowser)Marshal.GetObjectForIUnknown(shellBrowserPointer);
        }
        finally
        {
            Marshal.Release(shellBrowserPointer);
        }

        Marshal.ThrowExceptionForHR(shellBrowser.QueryActiveShellView(out var shellView));
        var folderView = (IFolderView)shellView;
        var shellItemArrayId = typeof(IShellItemArray).GUID;
        Marshal.ThrowExceptionForHR(folderView.Items(
            AllItemsInViewOrder,
            ref shellItemArrayId,
            out var value));

        var items = (IShellItemArray)value;
        Marshal.ThrowExceptionForHR(items.GetCount(out var count));
        var paths = new List<string>((int)count);

        for (uint index = 0; index < count; index++)
        {
            Marshal.ThrowExceptionForHR(items.GetItemAt(index, out var item));
            Marshal.ThrowExceptionForHR(item.GetDisplayName(FileSystemPath, out var pathPointer));
            try
            {
                var path = Marshal.PtrToStringUni(pathPointer);
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    paths.Add(path);
                }
            }
            finally
            {
                Marshal.FreeCoTaskMem(pathPointer);
            }
        }

        return [.. paths];
    }

    [ComImport]
    [Guid("6d5140c1-7436-11ce-8034-00aa006009fa")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IServiceProvider
    {
        [PreserveSig]
        int QueryService(ref Guid service, ref Guid interfaceId, out nint result);
    }

    [ComImport]
    [Guid("000214E3-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellView;

    [ComImport]
    [Guid("000214E2-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellBrowser
    {
        [PreserveSig] int GetWindow(out nint window);
        [PreserveSig] int ContextSensitiveHelp([MarshalAs(UnmanagedType.Bool)] bool enterMode);
        [PreserveSig] int InsertMenusSB(nint sharedMenu, nint menuWidths);
        [PreserveSig] int SetMenuSB(nint sharedMenu, nint activeObject, nint activeWindow);
        [PreserveSig] int RemoveMenusSB(nint sharedMenu);
        [PreserveSig] int SetStatusTextSB([MarshalAs(UnmanagedType.LPWStr)] string statusText);
        [PreserveSig] int EnableModelessSB([MarshalAs(UnmanagedType.Bool)] bool enable);
        [PreserveSig] int TranslateAcceleratorSB(nint message, ushort commandId);
        [PreserveSig] int BrowseObject(nint itemIdList, uint flags);
        [PreserveSig] int GetViewStateStream(uint mode, out nint stream);
        [PreserveSig] int GetControlWindow(uint controlId, out nint window);
        [PreserveSig] int SendControlMsg(uint controlId, uint message, nint wordParameter, nint longParameter, out nint result);
        [PreserveSig] int QueryActiveShellView(out IShellView shellView);
    }

    [ComImport]
    [Guid("cde725b0-ccc9-4519-917e-325d72fab4ce")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IFolderView
    {
        [PreserveSig] int GetCurrentViewMode(out uint viewMode);
        [PreserveSig] int SetCurrentViewMode(uint viewMode);
        [PreserveSig] int GetFolder(ref Guid interfaceId, [MarshalAs(UnmanagedType.Interface)] out object folder);
        [PreserveSig] int Item(int itemIndex, out nint itemIdList);
        [PreserveSig] int ItemCount(uint flags, out int itemCount);
        [PreserveSig] int Items(uint flags, ref Guid interfaceId, [MarshalAs(UnmanagedType.Interface)] out object items);
    }

    [ComImport]
    [Guid("b63ea76d-1f85-456f-a19c-48159efa858b")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellItemArray
    {
        [PreserveSig] int BindToHandler(nint bindContext, ref Guid handlerId, ref Guid interfaceId, out nint result);
        [PreserveSig] int GetPropertyStore(int flags, ref Guid interfaceId, out nint result);
        [PreserveSig] int GetPropertyDescriptionList(nint propertyKey, ref Guid interfaceId, out nint result);
        [PreserveSig] int GetAttributes(uint flags, uint mask, out uint attributes);
        [PreserveSig] int GetCount(out uint itemCount);
        [PreserveSig] int GetItemAt(uint index, out IShellItem item);
    }

    [ComImport]
    [Guid("43826d1e-e718-42ee-bc55-a1e261c37bfe")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellItem
    {
        [PreserveSig] int BindToHandler(nint bindContext, ref Guid handlerId, ref Guid interfaceId, out nint result);
        [PreserveSig] int GetParent(out IShellItem parent);
        [PreserveSig] int GetDisplayName(uint displayNameType, out nint name);
    }
}
