using JitterDemo.Renderer.OpenGL.Native;

namespace JitterDemo.Renderer.OpenGL;

public sealed class Framebuffer
{
    public uint Handle { get; }

    public Framebuffer()
    {
        Handle = GL.GenFramebuffer();
    }

    private Framebuffer(uint handle) { Handle = handle; }

    public static readonly Framebuffer Default = new(0);

    public void Bind() => GL.BindFramebuffer(GLC.FRAMEBUFFER, Handle);

    public void AttachDepthTexture(Texture2D texture)
    {
        Bind();
        GL.FramebufferTexture2D(GLC.FRAMEBUFFER, GLC.DEPTH_ATTACHMENT, GLC.TEXTURE_2D, texture.Handle, 0);
        GL.DrawBuffer(GLC.NONE);
    }
}
