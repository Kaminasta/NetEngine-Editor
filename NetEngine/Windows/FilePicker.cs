using System;
using System.Runtime.InteropServices;

namespace NetEngine.Windows;

public class FilePicker
{
    private static readonly Guid CLSID_FileOpenDialog = new Guid("DC1C5A9C-E88A-4DDE-A5A1-60F82A20AEF7");
    private static readonly Guid IID_IFileOpenDialog = new Guid("d57c7288-d4ad-4768-be02-9d969532d960");

    [Flags]
    public enum FOS : uint
    {
        FOS_FORCEFILESYSTEM = 0x00000040,
        FOS_FILEMUSTEXIST = 0x00001000,
        FOS_PATHMUSTEXIST = 0x00000800,
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct COMDLG_FILTERSPEC
    {
        public string pszName;
        public string pszSpec;
    }

    [ComImport]
    [Guid("d57c7288-d4ad-4768-be02-9d969532d960")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IFileOpenDialog
    {
        [PreserveSig]
        int Show(IntPtr parent);

        void SetFileTypes(uint cFileTypes, [MarshalAs(UnmanagedType.LPArray)] COMDLG_FILTERSPEC[] rgFilterSpec);
        void SetFileTypeIndex(uint iFileType);
        void GetFileTypeIndex(out uint piFileType);
        void Advise(IntPtr pfde, out uint pdwCookie);
        void Unadvise(uint dwCookie);
        void SetOptions(FOS fos);
        void GetOptions(out FOS pfos);
        void SetDefaultFolder(IShellItem psi);
        void SetFolder(IShellItem psi);
        void GetFolder(out IShellItem ppsi);
        void GetCurrentSelection(out IShellItem ppsi);
        void SetFileName([MarshalAs(UnmanagedType.LPWStr)] string pszName);
        void GetFileName([MarshalAs(UnmanagedType.LPWStr)] out string pszName);
        void SetTitle([MarshalAs(UnmanagedType.LPWStr)] string pszTitle);
        void SetOkButtonLabel([MarshalAs(UnmanagedType.LPWStr)] string pszText);
        void SetFileNameLabel([MarshalAs(UnmanagedType.LPWStr)] string pszLabel);
        void GetResult(out IShellItem ppsi);
        void AddPlace(IShellItem psi, uint fdap);
        void SetDefaultExtension([MarshalAs(UnmanagedType.LPWStr)] string pszDefaultExtension);
        void Close(int hr);
        void SetClientGuid(ref Guid guid);
        void ClearClientData();
        void SetFilter(IntPtr pFilter);
    }

    [ComImport]
    [Guid("43826d1e-e718-42ee-bc55-a1e261c37bfe")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellItem
    {
        void BindToHandler(IntPtr pbc, ref Guid bhid, ref Guid riid, out IntPtr ppv);
        void GetParent(out IShellItem ppsi);
        void GetDisplayName(SIGDN sigdnName, [MarshalAs(UnmanagedType.LPWStr)] out string ppszName);
        void GetAttributes(uint sfgaoMask, out uint psfgaoAttribs);
        void Compare(IShellItem psi, uint hint, out int piOrder);
    }

    private enum SIGDN : uint
    {
        SIGDN_FILESYSPATH = 0x80058000
    }

    public static string PickFile(string title = null, string defaultExtension = null, (string name, string spec)[] filters = null)
    {
        IFileOpenDialog dialog = (IFileOpenDialog)Activator.CreateInstance(Type.GetTypeFromCLSID(CLSID_FileOpenDialog));

        // Set dialog options
        dialog.GetOptions(out FOS options);
        options |= FOS.FOS_FORCEFILESYSTEM | FOS.FOS_FILEMUSTEXIST | FOS.FOS_PATHMUSTEXIST;
        dialog.SetOptions(options);

        if (!string.IsNullOrEmpty(title))
        {
            dialog.SetTitle(title);
        }

        if (!string.IsNullOrEmpty(defaultExtension))
        {
            dialog.SetDefaultExtension(defaultExtension);
        }

        if (filters != null && filters.Length > 0)
        {
            var specs = new COMDLG_FILTERSPEC[filters.Length];
            for (int i = 0; i < filters.Length; i++)
            {
                specs[i] = new COMDLG_FILTERSPEC { pszName = filters[i].name, pszSpec = filters[i].spec };
            }
            dialog.SetFileTypes((uint)specs.Length, specs);
        }

        int hr = dialog.Show(IntPtr.Zero);
        if (hr == 0) // S_OK
        {
            dialog.GetResult(out IShellItem item);
            item.GetDisplayName(SIGDN.SIGDN_FILESYSPATH, out string path);
            return path;
        }

        return null;
    }
}
