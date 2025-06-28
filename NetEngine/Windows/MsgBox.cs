using System.Runtime.InteropServices;

namespace NetEngine.Windows;
public static class MsgBox
{
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);

    // Удобный метод-обертка
    public static void Show(string text, string caption = "Сообщение", uint type = 0)
    {
        MessageBox(IntPtr.Zero, text, caption, type);
    }
}
