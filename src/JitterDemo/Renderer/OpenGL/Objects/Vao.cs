using JitterDemo.Renderer.OpenGL.Native;

namespace JitterDemo.Renderer.OpenGL;

public enum AttribType : uint
{
    Byte = GLC.BYTE,
    UnsignedByte = GLC.UNSIGNED_BYTE,
    Short = GLC.SHORT,
    Int = GLC.INT,
    UnsignedInt = GLC.UNSIGNED_INT,
    HalfFloat = GLC.HALF_FLOAT,
    Float = GLC.FLOAT,
    Double = GLC.DOUBLE,
}

public sealed class Vao
{
    public uint Handle { get; }

    public Vao()
    {
        Handle = GL.GenVertexArray();
    }

    public void Bind() => GL.BindVertexArray(Handle);

    public void Attrib(uint index, GpuBuffer buffer, int components, AttribType type,
                       int stride, int offset, bool normalized = false, uint divisor = 0)
    {
        Bind();
        buffer.Bind();
        GL.EnableVertexAttribArray(index);
        GL.VertexAttribPointer(index, components, (uint)type, normalized, stride, offset);
        if (divisor != 0) GL.VertexAttribDivisor(index, divisor);
    }

    public void AttachIndexBuffer(GpuBuffer buffer)
    {
        Bind();
        buffer.Bind();
    }
}
