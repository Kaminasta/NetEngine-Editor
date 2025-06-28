using System;
using System.Runtime.InteropServices;

namespace NetEngine.Windows
{
    public class FileSavePicker
    {
        private static readonly Guid CLSID_FileSaveDialog = new Guid("C0B4E2F3-BA21-4773-8DBA-335EC946EB8B");
        private static readonly Guid IID_IFileSaveDialog = new Guid("84BCCD23-5FDE-4CDB-AEA4-AF64B83D78AB");

        [Flags]
        public enum FOS : uint
        {
            FOS_OVERWRITEPROMPT = 0x00000002,
            FOS_NOCHANGEDIR = 0x00000008,
            FOS_FORCEFILESYSTEM = 0x00000040,
            FOS_PATHMUSTEXIST = 0x00000800,
            FOS_FILEMUSTEXIST = 0x00001000,
            // Другие опции можно добавить при необходимости
        }

        [ComImport]
        [Guid("84BCCD23-5FDE-4CDB-AEA4-AF64B83D78AB")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IFileSaveDialog
        {
            // IModalWindow
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

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct COMDLG_FILTERSPEC
        {
            public string pszName;
            public string pszSpec;
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

        public static string PickFileToSave(string title = null, string defaultFileName = null, (string name, string spec)[] filters = null)
        {
            IFileSaveDialog dialog = (IFileSaveDialog)Activator.CreateInstance(Type.GetTypeFromCLSID(CLSID_FileSaveDialog));

            dialog.GetOptions(out FOS options);
            options |= FOS.FOS_OVERWRITEPROMPT | FOS.FOS_FORCEFILESYSTEM;
            dialog.SetOptions(options);

            if (!string.IsNullOrEmpty(title))
            {
                dialog.SetTitle(title);
            }

            if (filters != null && filters.Length > 0)
            {
                var nativeFilters = new COMDLG_FILTERSPEC[filters.Length];
                for (int i = 0; i < filters.Length; i++)
                {
                    nativeFilters[i].pszName = filters[i].name;
                    nativeFilters[i].pszSpec = filters[i].spec;
                }
                dialog.SetFileTypes((uint)filters.Length, nativeFilters);
                dialog.SetFileTypeIndex(1);
            }

            //if (!string.IsNullOrEmpty(defaultExtension))
            //{
            //    dialog.SetDefaultExtension(defaultExtension);
            //}

            if (!string.IsNullOrEmpty(defaultFileName))
            {
                dialog.SetFileName(defaultFileName);
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
}
