using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using JitterDemo.Renderer.DearImGui;
using JitterDemo.Renderer.OpenGL;
using JitterDemo.Renderer.OpenGL.Native;

namespace JitterDemo.Renderer;

/// OpenGL backend for Dear ImGui. Forwards mouse/keyboard events from the host
/// window, uploads ImGui's vertex/index streams each frame, and renders draw commands.
public sealed class ImGuiRenderer
{
    private GpuBuffer vbo = null!;
    private GpuBuffer ebo = null!;
    private Vao vao = null!;
    private Shader shader = null!;

    private readonly List<Texture2D> textures = new();

    public bool WantsCaptureMouse { get; private set; }
    public bool WantsCaptureKeyboard { get; private set; }

    public unsafe void Load(int framebufferWidth, int framebufferHeight)
    {
        shader = new Shader(Vs, Fs);
        shader.Use();
        shader.Set("uFontTexture", 0);

        ImFontAtlas* shared = null;
        IntPtr ctx = ImGuiNative.igCreateContext(shared);
        ImGuiNative.igSetCurrentContext(ctx);

        vao = new Vao();
        vbo = GpuBuffer.Vertex();
        ebo = GpuBuffer.Index();

        int stride = Unsafe.SizeOf<ImDrawVert>();
        vao.Attrib(0, vbo, 2, AttribType.Float, stride, 0);
        vao.Attrib(1, vbo, 2, AttribType.Float, stride, 2 * sizeof(float));
        vao.Attrib(2, vbo, 4, AttribType.UnsignedByte, stride, 4 * sizeof(float), normalized: true);
        vao.AttachIndexBuffer(ebo);

        RebuildFontAtlas();

        ImGuiIO* io = ImGuiNative.igGetIO();
        io->DeltaTime = 0;
        io->DisplaySize = new Vector2(framebufferWidth, framebufferHeight);

        ImGui.DisableIni();
    }

    private unsafe void RebuildFontAtlas()
    {
        ImGuiIO* io = ImGuiNative.igGetIO();
        byte* pixel;
        int width, height, bpp;
        ImGuiNative.ImFontAtlas_GetTexDataAsRGBA32(io->Fonts, &pixel, &width, &height, &bpp);

        var texture = new Texture2D();
        texture.LoadImage((IntPtr)pixel, width, height, generateMipmap: false);
        textures.Add(texture);

        ImGuiNative.ImFontAtlas_SetTexID(io->Fonts, textures.Count - 1);
        ImGuiNative.ImFontAtlas_ClearTexData(io->Fonts);
    }

    public unsafe void Draw(float deltaTime, int logicalWidth, int logicalHeight,
                            int framebufferWidth, int framebufferHeight,
                            Keyboard keyboard, Mouse mouse)
    {
        ImGuiIO* io = ImGuiNative.igGetIO();
        io->DeltaTime = deltaTime;

        float scaleX = (float)framebufferWidth / logicalWidth;
        float scaleY = (float)framebufferHeight / logicalHeight;

        ImGuiNative.ImGuiIO_AddMousePosEvent(io, (float)mouse.Position.X * scaleX, (float)mouse.Position.Y * scaleY);
        ImGuiNative.ImGuiIO_AddMouseButtonEvent(io, 0, Convert.ToByte(mouse.IsButtonDown(Mouse.Button.Left)));
        ImGuiNative.ImGuiIO_AddMouseButtonEvent(io, 1, Convert.ToByte(mouse.IsButtonDown(Mouse.Button.Right)));
        ImGuiNative.ImGuiIO_AddMouseButtonEvent(io, 2, Convert.ToByte(mouse.IsButtonDown(Mouse.Button.Middle)));
        ImGuiNative.ImGuiIO_AddMouseWheelEvent(io, (float)mouse.ScrollWheel.X, (float)mouse.ScrollWheel.Y);

        foreach (uint codepoint in keyboard.CharInput)
        {
            ImGuiNative.ImGuiIO_AddInputCharacter(io, codepoint);
        }

        ForwardKey(io, ImGuiKey.LeftArrow, keyboard, Keyboard.Key.Left);
        ForwardKey(io, ImGuiKey.RightArrow, keyboard, Keyboard.Key.Right);
        ForwardKey(io, ImGuiKey.UpArrow, keyboard, Keyboard.Key.Up);
        ForwardKey(io, ImGuiKey.DownArrow, keyboard, Keyboard.Key.Down);
        ForwardKey(io, ImGuiKey.PageDown, keyboard, Keyboard.Key.PageDown);
        ForwardKey(io, ImGuiKey.PageUp, keyboard, Keyboard.Key.PageUp);
        ForwardKey(io, ImGuiKey.LeftCtrl, keyboard, Keyboard.Key.LeftControl);
        ForwardKey(io, ImGuiKey.LeftAlt, keyboard, Keyboard.Key.LeftAlt);
        ForwardKey(io, ImGuiKey.ModAlt, keyboard, Keyboard.Key.RightAlt);
        ForwardKey(io, ImGuiKey.Backspace, keyboard, Keyboard.Key.Backspace);
        ForwardKey(io, ImGuiKey.Tab, keyboard, Keyboard.Key.Tab);
        ForwardKey(io, ImGuiKey.LeftShift, keyboard, Keyboard.Key.LeftShift);
        ForwardKey(io, ImGuiKey.RightShift, keyboard, Keyboard.Key.RightShift);
        ForwardKey(io, ImGuiKey.Delete, keyboard, Keyboard.Key.Delete);
        ForwardKey(io, ImGuiKey.End, keyboard, Keyboard.Key.End);
        ForwardKey(io, ImGuiKey.Home, keyboard, Keyboard.Key.Home);
        ForwardKey(io, ImGuiKey.Escape, keyboard, Keyboard.Key.Escape);

        WantsCaptureKeyboard = Convert.ToBoolean(io->WantCaptureKeyboard);
        WantsCaptureMouse = Convert.ToBoolean(io->WantCaptureMouse);

        io->DisplaySize = new Vector2(framebufferWidth, framebufferHeight);
        io->DisplayFramebufferScale = new Vector2(1, 1);

        vao.Bind();
        shader.Use();

        Matrix4 projection = MatrixHelper.CreateOrthographicOffCenter(
            0, framebufferWidth, framebufferHeight, 0, -1f, +1f);
        shader.Set("uProjection", projection);

        GLDevice.Enable(Capability.Blend);
        GLDevice.Enable(Capability.ScissorTest);
        GLDevice.Disable(Capability.DepthTest);
        GLDevice.Disable(Capability.CullFace);

        ImDrawData* drawData = ImGuiNative.igGetDrawData();
        ImGuiNative.ImDrawData_ScaleClipRects(drawData, io->DisplayFramebufferScale);

        for (int i = 0; i < drawData->CmdListsCount; i++)
        {
            ImDrawList* cmdList = (ImDrawList*)drawData->CmdLists.Ref<IntPtr>(i);

            vbo.Stream(cmdList->VtxBuffer.Data, cmdList->VtxBuffer.Size * Unsafe.SizeOf<ImDrawVert>());
            ebo.Stream(cmdList->IdxBuffer.Data, cmdList->IdxBuffer.Size * sizeof(ushort));

            for (int c = 0; c < cmdList->CmdBuffer.Size; c++)
            {
                ref ImDrawCmd cmd = ref cmdList->CmdBuffer.Ref<ImDrawCmd>(c);

                textures[(int)cmd.TextureId].Bind(0);
                var clip = cmd.ClipRect;
                GLDevice.Scissor((int)clip.X, framebufferHeight - (int)clip.W,
                                 (int)(clip.Z - clip.X), (int)(clip.W - clip.Y));
                GLDevice.DrawElementsBaseVertex(DrawMode.Triangles, (int)cmd.ElemCount,
                    IndexType.UnsignedShort, (int)cmd.IdxOffset * sizeof(ushort),
                    (int)cmd.VtxOffset);
            }
        }

        GLDevice.Disable(Capability.Blend);
        GLDevice.Enable(Capability.DepthTest);
        GLDevice.Enable(Capability.CullFace);
        GLDevice.Disable(Capability.ScissorTest);
    }

    private static unsafe void ForwardKey(ImGuiIO* io, ImGuiKey key, Keyboard keyboard, Keyboard.Key k)
        => ImGuiNative.ImGuiIO_AddKeyEvent(io, key, Convert.ToByte(keyboard.IsKeyDown(k)));

    private const string Vs = @"
#version 330 core

uniform mat4 uProjection;

layout(location = 0) in vec2 aPos;
layout(location = 1) in vec2 aUV;
layout(location = 2) in vec4 aColor;

out vec4 vColor;
out vec2 vUV;

void main()
{
    gl_Position = uProjection * vec4(aPos, 0, 1);
    vColor = aColor;
    vUV = aUV;
}
";

    private const string Fs = @"
#version 330 core

uniform sampler2D uFontTexture;

in vec4 vColor;
in vec2 vUV;

out vec4 FragColor;

void main()
{
    FragColor = vColor * texture(uFontTexture, vUV);
}
";
}
