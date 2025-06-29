using Silk.NET.Input;
using System.Numerics;

namespace NetEngine;

public static class EditorCamera
{
    private static GameObject editorCamera;
    public static GameObject Camera => editorCamera;

    private static Vector3 cameraRotation;
    private static bool isCameraRotating = false;
    private static bool isCameraMoving = false;
    private static float acceleration = 1f;
    private static float accelerationTimer = 0f;

    public static float moveSpeed = 5f;
    public static float accelerationDuration = 15f;

    public static void Init()
    {
        editorCamera = new();
        var cameraComp = editorCamera.AddComponent<Camera>();
        cameraComp.FarPlane = 10000;
    }

    public static void HandleSceneCameraMovement()
    {
        if (editorCamera == null || !EditorGui.SceneViewportOpened) return;

        Vector2 viewportPos = EditorGui.SceneViewportPosition;
        Vector2 viewportSize = EditorGui.SceneViewportSize;
        Vector2 viewportCenter = new Vector2(
            (float)Math.Round(viewportPos.X + viewportSize.X * 0.5f),
            (float)Math.Round(viewportPos.Y + viewportSize.Y * 0.5f)
        );

        Vector2 mousePos = Input.GetMousePosition();
        bool isMouseInViewport = mousePos.X >= viewportPos.X && mousePos.X <= viewportPos.X + viewportSize.X &&
                                 mousePos.Y >= viewportPos.Y && mousePos.Y <= viewportPos.Y + viewportSize.Y;
        bool isRightMouseDown = Input.IsMouseButtonPressed(MouseButton.Right);

        if (isRightMouseDown && isMouseInViewport)
        {
            if (!isCameraRotating)
            {
                isCameraRotating = true;
                Input.Mouse.Cursor.CursorMode = CursorMode.Hidden;
                Input.Mouse.Position = viewportCenter;
                return;
            }

            Vector3 direction = Vector3.Zero;
            if (Input.IsKeyPressed(Key.W)) direction += editorCamera.Transform.Front;
            if (Input.IsKeyPressed(Key.S)) direction -= editorCamera.Transform.Front;
            if (Input.IsKeyPressed(Key.A)) direction -= editorCamera.Transform.Right;
            if (Input.IsKeyPressed(Key.D)) direction += editorCamera.Transform.Right;
            if (Input.IsKeyPressed(Key.E)) direction += editorCamera.Transform.Up;
            if (Input.IsKeyPressed(Key.Q)) direction -= editorCamera.Transform.Up;

            float scrollDelta = Input.GetScrollDelta();

            if (scrollDelta > 0 || scrollDelta < 0)
            {
                moveSpeed += scrollDelta > 0 ? 0.1f : -0.1f;
                moveSpeed = float.Clamp(moveSpeed, 0.5f, 10f);
                Console.EditorLog("Viewport camera move speed: " + moveSpeed);
            }

            if (direction != Vector3.Zero)
            {
                float baseSpeed = Input.IsKeyPressed(Key.ShiftLeft) ? moveSpeed * 2 : moveSpeed;

                if (!isCameraMoving)
                {
                    isCameraMoving = true;
                    accelerationTimer = 0f;
                    acceleration = 1f;
                }
                else
                {
                    accelerationTimer = Math.Min(accelerationTimer + Time.DeltaTime, accelerationDuration);
                    float t = accelerationTimer / accelerationDuration;
                    acceleration = 1f + t * (moveSpeed * 3 - 1f);
                }

                editorCamera.Transform.Position += Vector3.Normalize(direction) * baseSpeed * acceleration * Time.DeltaTime;
            }
            else
            {
                isCameraMoving = false;
                acceleration = 1f;
                accelerationTimer = 0f;
            }

            Vector2 delta = Input.GetMousePosition() * Time.DeltaTime - viewportCenter * Time.DeltaTime;
            if (delta != Vector2.Zero)
            {
                cameraRotation.Y -= delta.X * EditorGui.SceneViewportSensitivity;
                cameraRotation.X = Math.Clamp(cameraRotation.X - delta.Y * EditorGui.SceneViewportSensitivity, -89.99f, 89.99f);

                editorCamera.Transform.Rotation = Convert.VectorToQuaternion(new(cameraRotation.X, cameraRotation.Y, 0));
            }

            Input.Mouse.Position = viewportCenter;
        }
        else if (isCameraRotating)
        {
            isCameraRotating = false;
            Input.Mouse.Cursor.CursorMode = CursorMode.Normal;
        }
    }
}
