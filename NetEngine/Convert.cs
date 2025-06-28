using Silk.NET.Maths;
using System.Globalization;
using System.Numerics;
using System.Text.RegularExpressions;

namespace NetEngine;

public static class Convert
{
    public const float PI = 3.14159265358979323846f;

    public static float ToRadians(float degrees)
    {
        return degrees * (PI / 180f);
    }

    public static float ToDegrees(float radians)
    {
        return radians * (180f / PI);
    }
    public static Vector3 TransformVector(Matrix4x4 m, Vector3 v)
    {
        float x = m.M11 * v.X + m.M21 * v.Y + m.M31 * v.Z;
        float y = m.M12 * v.X + m.M22 * v.Y + m.M32 * v.Z;
        float z = m.M13 * v.X + m.M23 * v.Y + m.M33 * v.Z;
        return new(x, y, z);
    }

    public static float[] MatrixToArray(Matrix4x4 m)
    {
        return [
            m.M11, m.M12, m.M13, m.M14,
            m.M21, m.M22, m.M23, m.M24,
            m.M31, m.M32, m.M33, m.M34,
            m.M41, m.M42, m.M43, m.M44
        ];
    }

    public static Quaternion VectorToQuaternion(Vector3 eulerAnglesDegrees)
    {
        // Конвертируем градусы в радианы
        float roll = ToRadians(eulerAnglesDegrees.X);  // X
        float pitch = ToRadians(eulerAnglesDegrees.Y); // Y
        float yaw = ToRadians(eulerAnglesDegrees.Z);   // Z

        float cy = MathF.Cos(yaw * 0.5f);
        float sy = MathF.Sin(yaw * 0.5f);
        float cp = MathF.Cos(pitch * 0.5f);
        float sp = MathF.Sin(pitch * 0.5f);
        float cr = MathF.Cos(roll * 0.5f);
        float sr = MathF.Sin(roll * 0.5f);

        Quaternion q = new Quaternion();
        q.W = cr * cp * cy + sr * sp * sy;
        q.X = sr * cp * cy - cr * sp * sy;
        q.Y = cr * sp * cy + sr * cp * sy;
        q.Z = cr * cp * sy - sr * sp * cy;

        return q;
    }

    public static Vector3 QuaternionToVector3(Quaternion q)
    {
        q = Quaternion.Normalize(q);

        // roll (X-axis rotation)
        float sinr_cosp = 2 * (q.W * q.X + q.Y * q.Z);
        float cosr_cosp = 1 - 2 * (q.X * q.X + q.Y * q.Y);
        float roll = MathF.Atan2(sinr_cosp, cosr_cosp);

        // pitch (Y-axis rotation)
        float sinp = 2 * (q.W * q.Y - q.Z * q.X);
        float pitch;
        if (MathF.Abs(sinp) >= 1)
            pitch = MathF.CopySign(MathF.PI / 2, sinp); // use 90 degrees if out of range
        else
            pitch = MathF.Asin(sinp);

        // yaw (Z-axis rotation)
        float siny_cosp = 2 * (q.W * q.Z + q.X * q.Y);
        float cosy_cosp = 1 - 2 * (q.Y * q.Y + q.Z * q.Z);
        float yaw = MathF.Atan2(siny_cosp, cosy_cosp);

        // Возвращаем в градусах, порядок X (roll), Y (pitch), Z (yaw)
        return new Vector3(
            ToDegrees(roll),
            ToDegrees(pitch),
            ToDegrees(yaw)
        );
    }

    public static Quaternion ToSystemQuaternion(Assimp.Quaternion assimpQuat)
    {
        return new(assimpQuat.X, assimpQuat.Y, assimpQuat.Z, assimpQuat.W);
    }

    public static string ToWords(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        string withSpaces = Regex.Replace(input, "(?<!^)([A-Z])", " $1");

        string result = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(withSpaces.ToLower());
        return result;
    }

    public static Vector3 ToNumerics(Vector3D<float> v) => new(v.X, v.Y, v.Z);
    public static Vector3D<float> ToSilk(Vector3 v) => new(v.X, v.Y, v.Z);
}