using System.Runtime.InteropServices;

namespace NetEngine;

class Program
{
    const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20; // Windows 10 1809+

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    public static void ChangeTitleBarTheme(IntPtr hwnd, WindowTheme theme)
    {
        int useDark = theme == WindowTheme.Dark ? 1 : 0;
        DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useDark, sizeof(int));
    }

    static void Main(string[] args)
    {
        Editor editor = new Editor();
        if (args != null && args.Length > 0 && !string.IsNullOrEmpty(args[0]))
            editor.ProjectFilePath = args[0];
        //editor.ProjectFilePath = @"D:\UserFolders\Desktop\test\test.nproj";
        editor.Run();
    }
}
