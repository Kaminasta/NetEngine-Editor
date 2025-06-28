using Newtonsoft.Json;
using Silk.NET.OpenGL;

namespace NetEngine;

public class Mesh : IDisposable
{
    private readonly GL gl;

    private class GLMesh
    {
        public uint VAO;
        public uint VBOVertices;
        public uint VBOUTexCoords;
        public uint VBONormals;
        public uint EBO;
        public int IndicesCount;
        public int MaterialId;
    }

    private readonly List<GLMesh> glMeshes = new();

    [JsonProperty]
    private List<MeshData> meshData;

    public unsafe Mesh(List<MeshData> meshData)
    {
        gl = OpenGL.GL;
        this.meshData = meshData;

        foreach (var md in meshData)
        {
            // Позиции
            float[] vertexData = new float[md.Vertices.Count * 3];
            for (int i = 0; i < md.Vertices.Count; i++)
            {
                vertexData[i * 3 + 0] = md.Vertices[i].X;
                vertexData[i * 3 + 1] = md.Vertices[i].Y;
                vertexData[i * 3 + 2] = md.Vertices[i].Z;
            }

            // UV
            float[] uvData = new float[md.UVs.Count * 2];
            for (int i = 0; i < md.UVs.Count; i++)
            {
                uvData[i * 2 + 0] = md.UVs[i].X;
                uvData[i * 2 + 1] = md.UVs[i].Y;
            }

            // Нормали
            float[] normalData = new float[md.Normals.Count * 3];
            for (int i = 0; i < md.Normals.Count; i++)
            {
                normalData[i * 3 + 0] = md.Normals[i].X;
                normalData[i * 3 + 1] = md.Normals[i].Y;
                normalData[i * 3 + 2] = md.Normals[i].Z;
            }

            uint vao = gl.GenVertexArray();
            gl.BindVertexArray(vao);

            // VBO - позиции
            uint vboVertices = gl.GenBuffer();
            gl.BindBuffer(BufferTargetARB.ArrayBuffer, vboVertices);
            fixed (float* vertexPtr = &vertexData[0])
            {
                gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(vertexData.Length * sizeof(float)), vertexPtr, BufferUsageARB.StaticDraw);
            }
            gl.EnableVertexAttribArray(0);
            gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), (void*)0);

            // VBO - UV
            uint vboUVs = gl.GenBuffer();
            gl.BindBuffer(BufferTargetARB.ArrayBuffer, vboUVs);
            fixed (float* uvPtr = &uvData[0])
            {
                gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(uvData.Length * sizeof(float)), uvPtr, BufferUsageARB.StaticDraw);
            }
            gl.EnableVertexAttribArray(1);
            gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 2 * sizeof(float), (void*)0);

            // VBO - нормали
            uint vboNormals = gl.GenBuffer();
            gl.BindBuffer(BufferTargetARB.ArrayBuffer, vboNormals);
            fixed (float* normalPtr = &normalData[0])
            {
                gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(normalData.Length * sizeof(float)), normalPtr, BufferUsageARB.StaticDraw);
            }
            gl.EnableVertexAttribArray(2);
            gl.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), (void*)0);

            // EBO - индексы
            uint ebo = gl.GenBuffer();
            gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, ebo);
            fixed (uint* indexPtr = md.Indices.ToArray())
            {
                gl.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint)(md.Indices.Count * sizeof(uint)), indexPtr, BufferUsageARB.StaticDraw);
            }

            gl.BindVertexArray(0);

            glMeshes.Add(new GLMesh
            {
                VAO = vao,
                VBOVertices = vboVertices,
                VBOUTexCoords = vboUVs,
                VBONormals = vboNormals,
                EBO = ebo,
                IndicesCount = md.Indices.Count,
                MaterialId = md.MaterialId
            });
        }
    }

    public unsafe void Render()
    {
        foreach (var mesh in glMeshes)
        {
            gl.BindVertexArray(mesh.VAO);
            gl.DrawElements(PrimitiveType.Triangles, (uint)mesh.IndicesCount, DrawElementsType.UnsignedInt, null);
        }
    }

    public unsafe void RenderByMaterialId(int materialId)
    {
        var mesh = glMeshes.Find(m => m.MaterialId == materialId);
        if (mesh == null)
            return;

        gl.BindVertexArray(mesh.VAO);
        gl.DrawElements(PrimitiveType.Triangles, (uint)mesh.IndicesCount, DrawElementsType.UnsignedInt, null);
    }

    public void Dispose()
    {
        foreach (var mesh in glMeshes)
        {
            gl.DeleteBuffer(mesh.VBOVertices);
            gl.DeleteBuffer(mesh.VBOUTexCoords);
            gl.DeleteBuffer(mesh.VBONormals);
            gl.DeleteBuffer(mesh.EBO);
            gl.DeleteVertexArray(mesh.VAO);
        }
        glMeshes.Clear();
    }
}
