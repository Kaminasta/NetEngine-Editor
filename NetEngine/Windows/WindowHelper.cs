using Silk.NET.Core;
using Silk.NET.Windowing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace NetEngine;

public static class WindowHelper
{
    public static void SetIcon(this IWindow window, string path)
    {
        using Image<Rgba32> image = Image.Load<Rgba32>(path);

        byte[] pixels = new byte[image.Width * image.Height * 4];
        image.CopyPixelDataTo(pixels);

        var rawImage = new RawImage(image.Width, image.Height, pixels);

        window.SetWindowIcon(new ReadOnlySpan<RawImage>([rawImage]));
    }
}
