using Silk.NET.OpenGL;
using System.Numerics;

namespace NetEngine;

public unsafe static class GridMesh
{
    private static GL gl => OpenGL.GL;
    private static Material Material => Editor.gizmosMaterial;
    private static uint VAO;
    private static uint VBO;
    private static int LineCount;
    private static int Size = 100;

    public static void Init()
    {
        List<float> vertices = new();

        for (int i = -Size; i <= Size; i++)
        {
            // Линии вдоль Z (по оси X меняется)
            if (i == 0)
            {
                // Центральная ось Z (ось X = 0) — красная
                vertices.AddRange([
                    -Size, 0, i, 1f, 0f, 0f,
                    Size, 0, i, 1f, 0f, 0f
                ]);
            }
            //else
            //{
            //    // Обычные линии — серые
            //    vertices.AddRange([
            //        -Size, 0, i, 0.5f, 0.5f, 0.5f,
            //        Size, 0, i, 0.5f, 0.5f, 0.5f
            //    ]);
            //}

            // Линии вдоль X (по оси Z меняется)
            if (i == 0)
            {
                // Центральная ось X (ось Z = 0) — синяя
                vertices.AddRange([
                    i, 0, -Size, 0f, 0f, 1f,
                    i, 0,  Size, 0f, 0f, 1f
                ]);
            }
            //else
            //{
            //    // Обычные линии — серые
            //    vertices.AddRange([
            //        i, 0, -Size, 0.5f, 0.5f, 0.5f,
            //        i, 0,  Size, 0.5f, 0.5f, 0.5f,
            //    ]);
            //}
        }

        LineCount = (vertices.Count / 3) / 2;

        VAO = gl.GenVertexArray();
        VBO = gl.GenBuffer();

        gl.BindVertexArray(VAO);
        gl.BindBuffer(GLEnum.ArrayBuffer, VBO);

        fixed (float* v = vertices.ToArray())
            gl.BufferData(GLEnum.ArrayBuffer, (nuint)(vertices.Count * sizeof(float)), v, GLEnum.StaticDraw);

        gl.VertexAttribPointer(0, 3, GLEnum.Float, false, 6 * sizeof(float), (void*)0);
        gl.EnableVertexAttribArray(0);

        gl.VertexAttribPointer(1, 3, GLEnum.Float, false, 6 * sizeof(float), (void*)(3 * sizeof(float)));
        gl.EnableVertexAttribArray(1);


        gl.BindVertexArray(0);
    }

    public unsafe static void Render(Matrix4x4 view, Matrix4x4 projection)
    {
        Matrix4x4 model = Matrix4x4.Identity;

        Material.Use();

        Material["model"] = model;
        Material["view"] = view;
        Material["projection"] = projection;

        // Отрисовка линий
        gl.BindVertexArray(VAO);
        gl.DrawArrays(GLEnum.Lines, 0, (uint)(LineCount * 2));
        gl.BindVertexArray(0);
    }

    public static void Dispose()
    {
        if (VBO != 0)
        {
            gl.DeleteBuffer(VBO);
            VBO = 0;
        }

        if (VAO != 0)
        {
            gl.DeleteVertexArray(VAO);
            VAO = 0;
        }

        LineCount = 0;
    }

}
