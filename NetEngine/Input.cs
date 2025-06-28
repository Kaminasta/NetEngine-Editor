using System.Numerics;
using Silk.NET.Input;

namespace NetEngine;

public static class Input
{
    private static float _scrollDelta;

    public static IKeyboard Keyboard;
    private static IMouse _mouse;
    public static IMouse Mouse
    {
        get => _mouse;
        set
        {
            if (_mouse != null)
                _mouse.Scroll -= OnMouseScroll;

            _mouse = value;

            if (_mouse != null)
                _mouse.Scroll += OnMouseScroll;
        }
    }
    private static void OnMouseScroll(IMouse mouse, ScrollWheel scroll)
        => _scrollDelta += scroll.Y;

    // --- MOUSE ---

    public static float GetScrollDelta()
    {
        float delta = _scrollDelta;
        _scrollDelta = 0f;
        return delta;
    }

    public static Vector2 GetMousePosition()
        => Mouse?.Position ?? Vector2.Zero;

    public static bool IsMouseButtonPressed(NetEngine.MouseButton button)
        => Mouse?.IsButtonPressed((Silk.NET.Input.MouseButton)button) ?? false;

    // --- KEYBOARD ---

    public static bool IsKeyPressed(NetEngine.Key key)
        => Keyboard?.IsKeyPressed((Silk.NET.Input.Key)key) ?? false;
}
