using Newtonsoft.Json;
using Silk.NET.OpenGL;
using System.Numerics;

namespace NetEngine;

public class Shader : Object, IDisposable
{
    private GL _gl;
    public uint ProgramId { get; }

    [JsonProperty]
    private string VertexSource;
    [JsonProperty]
    private string FragmentSource;

    public Shader(string vertexSource, string fragmentSource)
    {
        _gl = OpenGL.GL;

        this.VertexSource = vertexSource;
        this.FragmentSource = fragmentSource;

        ProgramId = CreateShaderProgram();
    }

    private uint CreateShaderProgram()
    {
        uint vertex = CompileShader(ShaderType.VertexShader, VertexSource);
        uint fragment = CompileShader(ShaderType.FragmentShader, FragmentSource);

        uint program = _gl.CreateProgram();
        _gl.AttachShader(program, vertex);
        _gl.AttachShader(program, fragment);
        _gl.LinkProgram(program);

        _gl.GetProgram(program, GLEnum.LinkStatus, out int status);
        if (status == 0)
        {
            string infoLog = _gl.GetProgramInfoLog(program);
            throw new Exception("Ошибка линковки шейдера: " + infoLog);
        }

        _gl.DeleteShader(vertex);
        _gl.DeleteShader(fragment);

        return program;
    }

    private uint CompileShader(ShaderType type, string source)
    {
        uint shader = _gl.CreateShader(type);
        _gl.ShaderSource(shader, source);
        _gl.CompileShader(shader);

        _gl.GetShader(shader, ShaderParameterName.CompileStatus, out int status);
        if (status == 0)
        {
            string infoLog = _gl.GetShaderInfoLog(shader);
            throw new Exception($"Ошибка компиляции {type}: {infoLog}");
        }

        return shader;
    }

    public void Use()
    {
        _gl.UseProgram(ProgramId);
    }

    public int GetUniformLocation(string name)
    {
        return _gl.GetUniformLocation(ProgramId, name);
    }

    public void SetUniform(string name, int value)
    {
        int location = GetUniformLocation(name);
        if (location == -1) return;
        _gl.Uniform1(location, value);
    }

    public void SetUniform(string name, float value)
    {
        int location = GetUniformLocation(name);
        if (location == -1) return;
        _gl.Uniform1(location, value);
    }

    public void SetUniform(string name, Vector3 value)
    {
        int location = GetUniformLocation(name);
        if (location == -1) return;
        _gl.Uniform3(location, value.X, value.Y, value.Z);
    }

    public void SetUniform(string name, Vector4 value)
    {
        int location = GetUniformLocation(name);
        if (location == -1) return;
        _gl.Uniform4(location, value.X, value.Y, value.Z, value.W);
    }

    public unsafe void SetMatrix4x4(string name, Matrix4x4 matrix)
    {
        int location = GetUniformLocation(name);
        if (location == -1) return;

        fixed (float* ptr = Convert.MatrixToArray(matrix))
        {
            _gl.UniformMatrix4(location, 1, false, ptr);
        }
    }


    public void Dispose()
    {
        _gl.DeleteProgram(ProgramId);
    }
}