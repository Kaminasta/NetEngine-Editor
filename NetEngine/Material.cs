using Silk.NET.OpenGL;
using System.Numerics;

namespace NetEngine;

public class Material
{
    public Shader Shader { get; private set; }

    private readonly Dictionary<string, object> _uniformValues = new();

    private GL gl;

    [ShowInInspector]
    public string test;

    public Material(Shader shader)
    {
        Shader = shader ?? throw new ArgumentNullException(nameof(shader));
        gl = OpenGL.GetGL();
    }

    public void Use()
    {
        gl.UseProgram(Shader.ProgramId);
    }

    public void SetUniform(string name, object value)
    {
        _uniformValues[name] = value;
        ApplyUniform(name, value);
    }

    private void ApplyUniform(string name, object value)
    {
        if (value is int intVal)
        {
            Shader.SetUniform(name, intVal);
        }
        else if (value is float floatVal)
        {
            Shader.SetUniform(name, floatVal);
        }
        else if (value is Vector3 vec3Val)
        {
            Shader.SetUniform(name, vec3Val);
        }
        else if (value is Vector4 vec4Val)
        {
            Shader.SetUniform(name, vec4Val);
        }
        else if (value is Matrix4x4 matVal)
        {
            Shader.SetMatrix4x4(name, matVal);
        }
        else
        {
            throw new ArgumentException($"Тип uniform `{value.GetType()}` не поддерживается");
        }
    }

    // Можно добавить индексатор для удобства
    public object this[string uniformName]
    {
        get => _uniformValues.TryGetValue(uniformName, out var val) ? val : null;
        set => SetUniform(uniformName, value);
    }

    // Обновление всех uniform, например после вызова Shader.Use()
    public void ApplyAllUniforms()
    {
        foreach (var kvp in _uniformValues)
        {
            ApplyUniform(kvp.Key, kvp.Value);
        }
    }
}
