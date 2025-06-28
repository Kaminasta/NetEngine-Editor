using Assimp;
using System.Numerics;

namespace NetEngine;

public class SceneData
{
    public List<ModelData> Models { get; set; } = new List<ModelData>();
    public List<MaterialData> Materials { get; set; } = new List<MaterialData>();
}

// Описание одного объекта/узла
public class ModelData
{
    public string Name { get; set; }
    public Vector3 Position { get; set; }
    public System.Numerics.Quaternion Rotation { get; set; }
    public Vector3 Scale { get; set; }
    public List<MeshData> Meshes { get; set; } = new List<MeshData>();
}

// Данные одного меша
public class MeshData
{
    public List<Vector3> Vertices { get; set; } = new List<Vector3>();
    public List<Vector3> Normals { get; set; } = new List<Vector3>();
    public List<Vector2> UVs { get; set; } = new List<Vector2>();
    public List<uint> Indices { get; set; } = new List<uint>();
    public int MaterialId { get; set; }
}

// Данные материала (пример)
public class MaterialData
{
    public string Name { get; set; }
    public Vector4? DiffuseColor { get; set; }
    public float Smooth { get; set; } = 0.5f;
    public float Metalic { get; set; } = 0f;
}

public static class FBXImporter
{
    public static SceneData Load(string filePath)
    {
        var importer = new AssimpContext();
        var aiScene = importer.ImportFile(filePath, PostProcessSteps.Triangulate | PostProcessSteps.GenerateNormals);   

        var sceneData = new SceneData();

        // Материалы
        for (int i = 0; i < aiScene.MaterialCount; i++)
        {
            var aiMat = aiScene.Materials[i];
            var matData = new MaterialData
            {
                Name = aiMat.Name,
            };
            if (aiMat.HasColorDiffuse)
            {
                var c = aiMat.ColorDiffuse;
                matData.DiffuseColor = new Vector4(c.R, c.G, c.B, c.A);
            }

            float GetMaterialFloatProperty(Assimp.Material mat, string key, float defaultValue = 0f)
            {
                try
                {
                    var prop = mat.GetProperty(key, TextureType.None, 0);
                    if (prop != null && prop.HasRawData && prop.RawData.Length >= 4)
                        return BitConverter.ToSingle(prop.RawData, 0);
                }
                catch { }
                return defaultValue;
            }

            matData.Metalic = GetMaterialFloatProperty(aiMat, "metalness", 0f);
            if (matData.Metalic == 0f)
                matData.Metalic = GetMaterialFloatProperty(aiMat, "metallic", 0f);

            float roughness = GetMaterialFloatProperty(aiMat, "roughness", 0.5f);

            matData.Smooth = 1.0f - roughness;

            matData.Smooth = Math.Clamp(matData.Smooth, 0f, 1f);
            matData.Metalic = Math.Clamp(matData.Metalic, 0f, 1f);

            sceneData.Materials.Add(matData);
        }

        // Рекурсивный обход узлов
        void ProcessNode(Node node, Assimp.Matrix4x4 parentTransform)
        {
            var globalTransform = parentTransform * node.Transform;

            globalTransform.Decompose(out Vector3D scaleA, out Assimp.Quaternion rotA, out Vector3D transA);
            var position = new Vector3(transA.X, transA.Y, transA.Z);
            var scale = new Vector3(scaleA.X, scaleA.Y, scaleA.Z);
            var rotation = Convert.ToSystemQuaternion(rotA);

            var model = new ModelData
            {
                Name = node.Name,
                Position = position,
                Rotation = rotation,
                Scale = scale
            };

            // Все меши, привязанные к этому узлу
            foreach (var meshIndex in node.MeshIndices)
            {
                var aiMesh = aiScene.Meshes[meshIndex];
                var mesh = new MeshData { MaterialId = aiMesh.MaterialIndex };

                // Вершины
                foreach (var v in aiMesh.Vertices)
                    mesh.Vertices.Add(new Vector3(v.X, v.Y, v.Z));

                // Нормали
                if (aiMesh.HasNormals)
                {
                    foreach (var n in aiMesh.Normals)
                        mesh.Normals.Add(new Vector3(n.X, n.Y, n.Z));
                }
                else
                {
                    // Нет нормалей? Заполняем нулями (или можно позже сгенерировать)
                    for (int i = 0; i < aiMesh.VertexCount; i++)
                        mesh.Normals.Add(Vector3.Zero);
                }

                // UV координаты (только первый канал)
                if (aiMesh.HasTextureCoords(0))
                {
                    foreach (var uv in aiMesh.TextureCoordinateChannels[0])
                        mesh.UVs.Add(new Vector2(uv.X, uv.Y));
                }
                else
                {
                    // Заполняем пустыми UV, чтобы длина совпадала с вершинами
                    for (int i = 0; i < aiMesh.VertexCount; i++)
                        mesh.UVs.Add(Vector2.Zero);
                }

                // Индексы
                foreach (var face in aiMesh.Faces)
                {
                    foreach (var idx in face.Indices)
                        mesh.Indices.Add((uint)idx);
                }


                GenerateSmoothNormals(mesh);

                model.Meshes.Add(mesh);
            }

            sceneData.Models.Add(model);

            // Обработка дочерних узлов
            foreach (var child in node.Children)
                ProcessNode(child, globalTransform);
        }

        ProcessNode(aiScene.RootNode, Assimp.Matrix4x4.Identity);

        return sceneData;
    }

    public static void GenerateSmoothNormals(MeshData mesh)
    {
        // Инициализация нормалей на 0
        mesh.Normals = new List<Vector3>(new Vector3[mesh.Vertices.Count]);

        // Суммируем нормали всех треугольников на вершины
        for (int i = 0; i < mesh.Indices.Count; i += 3)
        {
            int i0 = (int)mesh.Indices[i];
            int i1 = (int)mesh.Indices[i + 1];
            int i2 = (int)mesh.Indices[i + 2];

            var v0 = mesh.Vertices[i0];
            var v1 = mesh.Vertices[i1];
            var v2 = mesh.Vertices[i2];

            var edge1 = v1 - v0;
            var edge2 = v2 - v0;

            var normal = Vector3.Cross(edge1, edge2);
            if (normal.LengthSquared() > 0)
                normal = Vector3.Normalize(normal);

            mesh.Normals[i0] += normal;
            mesh.Normals[i1] += normal;
            mesh.Normals[i2] += normal;
        }

        // Нормализуем сумму
        for (int i = 0; i < mesh.Normals.Count; i++)
        {
            if (mesh.Normals[i].LengthSquared() > 0)
                mesh.Normals[i] = Vector3.Normalize(mesh.Normals[i]);
            else
                mesh.Normals[i] = Vector3.UnitY; // на всякий случай
        }
    }

}
