using Newtonsoft.Json;
using System.Numerics;

namespace NetEngine.Components;

public class Transform : Component
{
    [ShowInInspector]
    public Vector3 Position = Vector3.Zero;
    [ShowInInspector]
    public Quaternion Rotation = Quaternion.Identity;
    [ShowInInspector]
    public Vector3 Scale = Vector3.One;

    public Vector3 Front => Vector3.Normalize(Vector3.Transform(-Vector3.UnitZ, Rotation));
    public Vector3 Right => Vector3.Normalize(Vector3.Cross(Front, WorldUp));
    public Vector3 Up => Vector3.Normalize(Vector3.Cross(Right, Front));
    public Vector3 WorldUp => Vector3.UnitY;

    public Transform Parent;

    [JsonIgnore]
    public Vector3 RotationEuler
    {
        get => Convert.QuaternionToVector3(Rotation);
        set => Rotation = Convert.VectorToQuaternion(value);
    }

    public Transform(GameObject gameObject)
    {
        GameObject = gameObject;
    }

    public Matrix4x4 GetModelMatrix()
    {
        var S = Matrix4x4.CreateScale(Scale);
        var R = Matrix4x4.CreateFromQuaternion(Rotation);
        var T = Matrix4x4.CreateTranslation(Position);

        return S * R * T;
    }

    public Matrix4x4 GetRotationMatrix()
    {
        var R = Matrix4x4.CreateFromQuaternion(Rotation);
        var T = Matrix4x4.CreateTranslation(Position);
        return R * T;
    }

}
