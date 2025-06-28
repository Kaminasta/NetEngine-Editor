using ImGuiNET;

public class ImGuiFontAtlasConfig
{
    public List<ImGuiFontConfig> Fonts { get; } = new();

    public ImGuiFontConfig AddFont(string path, int size, ImFontConfigPtr? fontConfig = null, Func<ImGuiIOPtr, IntPtr> getGlyphRange = null)
    {
        var font = new ImGuiFontConfig(path, size, fontConfig, getGlyphRange);
        Fonts.Add(font);
        return font;
    }
}
