using System.Numerics;

namespace NetEngine.Components;

public class MeshRenderer : Behaviour
{
    [ShowInInspector]
    public List<Material> materials = new();

    public MeshRenderer()
    {

    }

    Vector3 GetCameraPosition(Matrix4x4 viewMatrix)
    {
        Matrix4x4.Invert(viewMatrix, out Matrix4x4 invertedView);
        return invertedView.Translation;
    }

    public unsafe void Render(Matrix4x4 view, Matrix4x4 projection)
    {
        var gl = OpenGL.GL;

        var meshFilter = GameObject.GetComponent<MeshFilter>();
        if (meshFilter != null)
        {
            for (int i = 0; i < materials.Count; i++)
            {
                {
                    var material = materials[i];
                    material.Use();

                    var model = GameObject.Transform.GetModelMatrix();

                    material["model"] = model;
                    material["view"] = view;
                    material["projection"] = projection;
                    material["_cameraPos"] = GetCameraPosition(view);

                    if (meshFilter.mesh != null)
                        meshFilter.mesh.RenderByMaterialId(i);
                }


            }
        }

    }
}