using NetEngine.Components;
using Silk.NET.OpenGL;
using System.Numerics;

namespace NetEngine;

public class GizmoRenderer
{
    private GL gl;
    private Material material;

    private uint vao, vbo;
    private (uint vao, uint vbo, int count) coneX, coneY, coneZ;
    private (uint vao, uint vbo, int count) cubeX, cubeY, cubeZ;
    private (uint vao, uint vbo, int count) ringX, ringY, ringZ;


    public GizmoRenderer(Material material)
    {
        gl = OpenGL.GL;
        this.material = material;

        coneX = GenerateCone(0.05f, 0.2f, 16, new Vector3(1f, 0f, 0f)); // красный
        coneY = GenerateCone(0.05f, 0.2f, 16, new Vector3(0f, 1f, 0f)); // зелёный
        coneZ = GenerateCone(0.05f, 0.2f, 16, new Vector3(0f, 0f, 1f)); // синий

        cubeX = GenerateCube(new Vector3(1f, 0f, 0f));
        cubeY = GenerateCube(new Vector3(0f, 1f, 0f));
        cubeZ = GenerateCube(new Vector3(0f, 0f, 1f));

        ringX = GenerateCircle(0.6f, 32, new Vector3(1f, 0f, 0f));
        ringY = GenerateCircle(0.6f, 32, new Vector3(0f, 1f, 0f));
        ringZ = GenerateCircle(0.6f, 32, new Vector3(0f, 0f, 1f));

    }

    private unsafe (uint vao, uint vbo, int vertexCount) GenerateCone(float radius, float height, int segments, Vector3 color)
    {
        List<float> vertices = new();

        for (int i = 0; i <= segments; i++)
        {
            float angle = (float)(i * 2.0 * Math.PI / segments);
            float x = radius * MathF.Cos(angle);
            float z = radius * MathF.Sin(angle);

            // Боковая грань (нарисуем как треугольники от основания к вершине)
            vertices.AddRange([
                x, 0f, z,       color.X, color.Y, color.Z,
            0f, height, 0f, color.X, color.Y, color.Z
            ]);
        }

        uint vao = gl.GenVertexArray();
        uint vbo = gl.GenBuffer();

        gl.BindVertexArray(vao);
        gl.BindBuffer(GLEnum.ArrayBuffer, vbo);

        fixed (float* v = vertices.ToArray())
        {
            gl.BufferData(GLEnum.ArrayBuffer, (nuint)(vertices.Count * sizeof(float)), v, GLEnum.StaticDraw);
        }

        gl.VertexAttribPointer(0, 3, GLEnum.Float, false, 6 * sizeof(float), (void*)0);
        gl.EnableVertexAttribArray(0);
        gl.VertexAttribPointer(1, 3, GLEnum.Float, false, 6 * sizeof(float), (void*)(3 * sizeof(float)));
        gl.EnableVertexAttribArray(1);

        gl.BindVertexArray(0);

        return (vao, vbo, vertices.Count / 6);
    }

    private unsafe (uint vao, uint vbo, int count) GenerateCube(Vector3 color)
    {
        float s = 0.05f;
        float[] vertices = {
        // позиция        // цвет
        -s,-s,-s, color.X, color.Y, color.Z,
         s,-s,-s, color.X, color.Y, color.Z,
         s, s,-s, color.X, color.Y, color.Z,
        -s, s,-s, color.X, color.Y, color.Z,
        -s,-s, s, color.X, color.Y, color.Z,
         s,-s, s, color.X, color.Y, color.Z,
         s, s, s, color.X, color.Y, color.Z,
        -s, s, s, color.X, color.Y, color.Z,
    };

        uint[] indices = {
        0,1,2, 2,3,0,
        4,5,6, 6,7,4,
        0,1,5, 5,4,0,
        2,3,7, 7,6,2,
        0,3,7, 7,4,0,
        1,2,6, 6,5,1,
    };

        uint vao = gl.GenVertexArray();
        uint vbo = gl.GenBuffer();
        uint ebo = gl.GenBuffer();

        gl.BindVertexArray(vao);

        fixed (float* v = vertices)
        {
            gl.BindBuffer(GLEnum.ArrayBuffer, vbo);
            gl.BufferData(GLEnum.ArrayBuffer, (nuint)(vertices.Length * sizeof(float)), v, GLEnum.StaticDraw);
        }

        fixed (uint* i = indices)
        {
            gl.BindBuffer(GLEnum.ElementArrayBuffer, ebo);
            gl.BufferData(GLEnum.ElementArrayBuffer, (nuint)(indices.Length * sizeof(uint)), i, GLEnum.StaticDraw);
        }

        gl.VertexAttribPointer(0, 3, GLEnum.Float, false, 6 * sizeof(float), (void*)0);
        gl.EnableVertexAttribArray(0);
        gl.VertexAttribPointer(1, 3, GLEnum.Float, false, 6 * sizeof(float), (void*)(3 * sizeof(float)));
        gl.EnableVertexAttribArray(1);

        gl.BindVertexArray(0);
        return (vao, vbo, indices.Length);
    }

    private unsafe (uint vao, uint vbo, int count) GenerateCircle(float radius, int segments, Vector3 color)
    {
        List<float> verts = new();
        for (int i = 0; i <= segments; i++)
        {
            float angle = i * 2.0f * MathF.PI / segments;
            float x = MathF.Cos(angle) * radius;
            float z = MathF.Sin(angle) * radius;

            verts.AddRange([x, 0f, z, color.X, color.Y, color.Z]);
        }

        uint vao = gl.GenVertexArray();
        uint vbo = gl.GenBuffer();

        gl.BindVertexArray(vao);
        gl.BindBuffer(GLEnum.ArrayBuffer, vbo);

        fixed (float* ptr = verts.ToArray())
        {
            gl.BufferData(GLEnum.ArrayBuffer, (nuint)(verts.Count * sizeof(float)), ptr, GLEnum.StaticDraw);
        }

        gl.VertexAttribPointer(0, 3, GLEnum.Float, false, 6 * sizeof(float), (void*)0);
        gl.EnableVertexAttribArray(0);
        gl.VertexAttribPointer(1, 3, GLEnum.Float, false, 6 * sizeof(float), (void*)(3 * sizeof(float)));
        gl.EnableVertexAttribArray(1);

        gl.BindVertexArray(0);

        return (vao, vbo, verts.Count / 6);
    }

    public enum GizmoType
    { 
        Position,
        Rotation,
        Scale
    }

    public unsafe void RenderGizmo(GizmoType gizmoType, Transform transform, Matrix4x4 view, Matrix4x4 projection, Vector3 cameraPosition)
    {
        material.Use();

        float distance = Vector3.Distance(cameraPosition, transform.Position);
        float scale = distance * 0.5f;

        material["view"] = view;
        material["projection"] = projection;

        switch (gizmoType)
        {
            case GizmoType.Position:
                RenderPositionGizmo(transform, scale);
                break;
            case GizmoType.Scale:
                
                break;
            case GizmoType.Rotation:
                RenderRotationGizmo(transform, scale);
                break;
        }
    }

    private unsafe void RenderPositionGizmo(Transform transform, float scale)
    {
        float[] lineVerts = new float[(3 * 2) * 6];
        Vector3[] dirs = { Vector3.UnitX, Vector3.UnitY, Vector3.UnitZ };
        Vector3[] cols = { new(1, 0, 0), new(0, 1, 0), new(0, 0, 1) };
        int idx = 0;

        for (int i = 0; i < 3; i++)
        {
            var dir = dirs[i];
            var col = cols[i];
            var p0 = transform.Position;
            var worldOffset = Vector3.Transform(dir * 0.5f * scale, transform.Rotation);
            var p1 = p0 + worldOffset;

            lineVerts[idx++] = p0.X; lineVerts[idx++] = p0.Y; lineVerts[idx++] = p0.Z;
            lineVerts[idx++] = col.X; lineVerts[idx++] = col.Y; lineVerts[idx++] = col.Z;

            lineVerts[idx++] = p1.X; lineVerts[idx++] = p1.Y; lineVerts[idx++] = p1.Z;
            lineVerts[idx++] = col.X; lineVerts[idx++] = col.Y; lineVerts[idx++] = col.Z;
        }

        uint tmpVao = gl.GenVertexArray();
        uint tmpVbo = gl.GenBuffer();
        gl.BindVertexArray(tmpVao);
        gl.BindBuffer(GLEnum.ArrayBuffer, tmpVbo);

        fixed (float* v = lineVerts)
            gl.BufferData(GLEnum.ArrayBuffer, (nuint)(lineVerts.Length * sizeof(float)), v, GLEnum.DynamicDraw);

        gl.VertexAttribPointer(0, 3, GLEnum.Float, false, 6 * sizeof(float), (void*)0);
        gl.EnableVertexAttribArray(0);
        gl.VertexAttribPointer(1, 3, GLEnum.Float, false, 6 * sizeof(float), (void*)(3 * sizeof(float)));
        gl.EnableVertexAttribArray(1);

        gl.DrawArrays(GLEnum.Lines, 0, 6);

        gl.DeleteBuffer(tmpVbo);
        gl.DeleteVertexArray(tmpVao);

        DrawCone(transform, Vector3.UnitX, coneX, scale);
        DrawCone(transform, Vector3.UnitY, coneY, scale);
        DrawCone(transform, Vector3.UnitZ, coneZ, scale);
    }

    private unsafe void RenderRotationGizmo(Transform transform, float scale)
    {
        gl.LineWidth(3.0f);  // Толще линии для колец
        DrawCircle(transform, Vector3.UnitX, ringX, scale);
        DrawCircle(transform, Vector3.UnitY, ringY, scale);
        DrawCircle(transform, Vector3.UnitZ, ringZ, scale);
        gl.LineWidth(1.0f);  // Вернуть толщину обратно
    }


    private unsafe void DrawCone(Transform transform, Vector3 direction, (uint vao, uint vbo, int count) cone, float scale)
    {
        Vector3 endPos = transform.Position + Vector3.Transform(direction * 0.5f * scale, transform.Rotation);

        Quaternion rot = Quaternion.Identity;

        if (direction == Vector3.UnitX)
            rot = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, -MathF.PI / 2);
        else if (direction == Vector3.UnitZ)
            rot = Quaternion.CreateFromAxisAngle(Vector3.UnitX, MathF.PI / 2);
        else if (direction == Vector3.UnitY)
            rot = Quaternion.Identity;

        rot = transform.Rotation * rot;

        Matrix4x4 model =
            Matrix4x4.CreateScale(0.5f * scale, 0.5f * scale, 0.5f * scale) *
            Matrix4x4.CreateFromQuaternion(rot) *
            Matrix4x4.CreateTranslation(endPos);

        float[] modelArray = Convert.MatrixToArray(model);

        material["model"] = model;

        gl.BindVertexArray(cone.vao);
        gl.DrawArrays(GLEnum.TriangleStrip, 0, (uint)cone.count);
        gl.BindVertexArray(0);
    }

    private unsafe void DrawCircle(Transform transform, Vector3 axis, (uint vao, uint vbo, int count) circle, float scale)
    {
        Quaternion rot = Quaternion.Identity;
        if (axis == Vector3.UnitX)
            rot = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathF.PI / 2);
        else if (axis == Vector3.UnitZ)
            rot = Quaternion.CreateFromAxisAngle(Vector3.UnitX, -MathF.PI / 2);
        else
            rot = Quaternion.Identity;

        rot = transform.Rotation * rot;

        Matrix4x4 model = Matrix4x4.CreateScale(scale) *
                          Matrix4x4.CreateFromQuaternion(rot) *
                          Matrix4x4.CreateTranslation(transform.Position);

        material["model"] = model;

        gl.BindVertexArray(circle.vao);
        gl.DrawArrays(GLEnum.LineStrip, 0, (uint)circle.count);
        gl.BindVertexArray(0);
    }
}
