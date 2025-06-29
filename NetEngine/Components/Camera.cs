using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using System.Numerics;

namespace NetEngine;

public class Camera : Behaviour
{
    [ShowInInspector]
    [Range(1, 179)]
    public float FieldOfView = 75f;

    [ShowInInspector]
    public float NearPlane = 0.1f;

    [ShowInInspector]
    public float FarPlane = 1000f;

    [ShowInInspector]
    public float Depth = 0;

    public float AspectRatio = 16f / 9f;

    public Matrix4x4 GetViewMatrix()
    {
        return Matrix4x4.CreateLookAt(
            GameObject.Transform.Position,
            GameObject.Transform.Position + GameObject.Transform.Front,
            GameObject.Transform.Up);
    }

    public Matrix4x4 GetProjectionMatrix()
    {
        return Matrix4x4.CreatePerspectiveFieldOfView(
            Convert.ToRadians(FieldOfView),
            AspectRatio,
            NearPlane,
            FarPlane);
    }

    public void RenderGizmos(Material material, Matrix4x4 view, Matrix4x4 projection)
    {
        var gl = OpenGL.GL;

        var pos = GameObject.Transform.Position;
        var forward = Vector3.Normalize(GameObject.Transform.Front);
        var up = Vector3.Normalize(GameObject.Transform.Up);
        var right = Vector3.Normalize(Vector3.Cross(forward, up));

        float nearHeight = 2 * MathF.Tan(Convert.ToRadians(FieldOfView) / 2) * NearPlane;
        float nearWidth = nearHeight * AspectRatio;
        float farHeight = 2 * MathF.Tan(Convert.ToRadians(FieldOfView) / 2) * FarPlane;
        float farWidth = farHeight * AspectRatio;

        Vector3 nearCenter = pos + forward * NearPlane;
        Vector3 farCenter = pos + forward * FarPlane;

        // Near plane corners
        Vector3 nearTopLeft = nearCenter + (up * (nearHeight / 2)) - (right * (nearWidth / 2));
        Vector3 nearTopRight = nearCenter + (up * (nearHeight / 2)) + (right * (nearWidth / 2));
        Vector3 nearBottomLeft = nearCenter - (up * (nearHeight / 2)) - (right * (nearWidth / 2));
        Vector3 nearBottomRight = nearCenter - (up * (nearHeight / 2)) + (right * (nearWidth / 2));

        // Far plane corners
        Vector3 farTopLeft = farCenter + (up * (farHeight / 2)) - (right * (farWidth / 2));
        Vector3 farTopRight = farCenter + (up * (farHeight / 2)) + (right * (farWidth / 2));
        Vector3 farBottomLeft = farCenter - (up * (farHeight / 2)) - (right * (farWidth / 2));
        Vector3 farBottomRight = farCenter - (up * (farHeight / 2)) + (right * (farWidth / 2));

        var lines = new[]
        {
            // Near plane
            nearTopLeft, nearTopRight, 
            nearTopRight, nearBottomRight,
            nearBottomRight, nearBottomLeft,
            nearBottomLeft, nearTopLeft,

            // Far plane
            farTopLeft, farTopRight,
            farTopRight, farBottomRight,
            farBottomRight, farBottomLeft,
            farBottomLeft, farTopLeft,

            // Connections
            nearTopLeft, farTopLeft,
            nearTopRight, farTopRight,
            nearBottomLeft, farBottomLeft,
            nearBottomRight, farBottomRight,
        };

        DrawLines(material, lines, view, projection);
    }

    private unsafe void DrawLines(Material material, Vector3[] lines, Matrix4x4 view, Matrix4x4 projection)
    {
        var gl = OpenGL.GL;

        uint vao = gl.GenVertexArray();
        gl.BindVertexArray(vao);

        uint vbo = gl.GenBuffer();
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);

        // Каждая вершина: позиция (3) + цвет (3)
        float[] lineVertices = new float[lines.Length * 6];
        for (int i = 0; i < lines.Length; i++)
        {
            lineVertices[i * 6 + 0] = lines[i].X;
            lineVertices[i * 6 + 1] = lines[i].Y;
            lineVertices[i * 6 + 2] = lines[i].Z;

            // Белый цвет (RGB)
            lineVertices[i * 6 + 3] = 1f;
            lineVertices[i * 6 + 4] = 1f;
            lineVertices[i * 6 + 5] = 1f;
        }

        fixed (float* vertexPtr = lineVertices)
        {
            gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(lineVertices.Length * sizeof(float)), vertexPtr, BufferUsageARB.StaticDraw);
        }

        material.Use();

        material["view"] = view;
        material["projection"] = projection;

        int stride = 6 * sizeof(float);

        gl.EnableVertexAttribArray(0); // позиция
        gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, (uint)stride, (void*)0);

        gl.EnableVertexAttribArray(1); // цвет
        gl.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, (uint)stride, (void*)(3 * sizeof(float)));

        gl.DrawArrays(PrimitiveType.Lines, 0, (uint)lines.Length);

        gl.DisableVertexAttribArray(0);
        gl.DisableVertexAttribArray(1);

        gl.DeleteBuffer(vbo);
        gl.DeleteVertexArray(vao);
    }

}
