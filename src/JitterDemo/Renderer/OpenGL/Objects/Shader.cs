using System;
using System.Collections.Generic;
using System.Diagnostics;
using JitterDemo.Renderer.OpenGL.Native;

namespace JitterDemo.Renderer.OpenGL;

public enum ShaderStage : uint
{
    Vertex = GLC.VERTEX_SHADER,
    Fragment = GLC.FRAGMENT_SHADER,
    Geometry = GLC.GEOMETRY_SHADER
}

public class ShaderException : Exception
{
    public ShaderException(string msg) : base(msg) { }
}

public sealed class Shader
{
    public uint Handle { get; }

    private readonly Dictionary<string, int> locations = new();

    public Shader(string vertex, string fragment)
    {
        uint vs = CompileStage(ShaderStage.Vertex, vertex);
        uint fs = CompileStage(ShaderStage.Fragment, fragment);

        Handle = GL.CreateProgram();
        GL.AttachShader(Handle, vs);
        GL.AttachShader(Handle, fs);
        GL.LinkProgram(Handle);
        GL.GetProgramiv(Handle, GLC.LINK_STATUS, out int success);

        if (success == GLC.FALSE)
        {
            GL.GetProgramInfoLog(Handle, 1024, out _, out string log);
            Debug.Fail(log);
            throw new ShaderException($"Shader link failed:{Environment.NewLine}{log}");
        }

        GL.DeleteShader(vs);
        GL.DeleteShader(fs);
    }

    private static uint CompileStage(ShaderStage stage, string source)
    {
        uint id = GL.CreateShader((uint)stage);
        GL.ShaderSource(id, 1, new[] { source }, null);
        GL.CompileShader(id);
        GL.GetShaderiv(id, GLC.COMPILE_STATUS, out int success);
        if (success == GLC.FALSE)
        {
            GL.GetShaderInfoLog(id, 2048, out _, out string log);
            Debug.Fail(log);
            throw new ShaderException($"{stage} shader compile failed:{Environment.NewLine}{log}");
        }
        return id;
    }

    public void Use() => GL.UseProgram(Handle);

    private int Loc(string name)
    {
        if (locations.TryGetValue(name, out int loc)) return loc;
        loc = GL.GetUniformLocation(Handle, name);
        locations[name] = loc;
        return loc;
    }

    public bool Has(string name) => Loc(name) >= 0;

    public void Set(string name, int value) => GL.Uniform1i(Loc(name), value);
    public void Set(string name, uint value) => GL.Uniform1ui(Loc(name), value);
    public void Set(string name, bool value) => GL.Uniform1i(Loc(name), value ? 1 : 0);
    public void Set(string name, float value) => GL.Uniform1f(Loc(name), value);

    public void Set(string name, float x, float y) => GL.Uniform2f(Loc(name), x, y);
    public void Set(string name, float x, float y, float z) => GL.Uniform3f(Loc(name), x, y, z);
    public void Set(string name, float x, float y, float z, float w) => GL.Uniform4f(Loc(name), x, y, z, w);

    public void Set(string name, in Vector2 v) => GL.Uniform2f(Loc(name), v.X, v.Y);
    public void Set(string name, in Vector3 v) => GL.Uniform3f(Loc(name), v.X, v.Y, v.Z);
    public void Set(string name, in Vector4 v) => GL.Uniform4f(Loc(name), v.X, v.Y, v.Z, v.W);

    public void Set(string name, in Matrix4 m)
    {
        unsafe
        {
            fixed (float* p = &m.M11) GL.UniformMatrix4fv(Loc(name), 1, false, p);
        }
    }

    public void Set(string name, Matrix4[] m)
    {
        unsafe
        {
            fixed (float* p = &m[0].M11) GL.UniformMatrix4fv(Loc(name), m.Length, false, p);
        }
    }

    public void Set(string name, float[] values)
    {
        unsafe
        {
            fixed (float* p = values) GL.Uniform1fv(Loc(name), values.Length, p);
        }
    }
}
