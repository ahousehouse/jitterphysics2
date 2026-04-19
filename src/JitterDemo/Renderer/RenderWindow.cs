using System;
using System.Collections.Generic;
using JitterDemo.Renderer.OpenGL;

namespace JitterDemo.Renderer;

/// Scene-level host for the engine. Owns the shadow caster, lit shader, debug
/// renderer, ImGui backend, skybox, camera, and the registry of instanced drawables.
public class RenderWindow : GLFWWindow
{
    public static RenderWindow Instance { get; private set; } = null!;

    public Camera Camera { get; set; }
    public ShadowCaster Shadow { get; private set; } = null!;
    public DebugRenderer DebugRenderer { get; private set; } = null!;
    public ImGuiRenderer GuiRenderer { get; private set; } = null!;

    public bool ShowShadowDebug { get; set; }

    private Skybox skybox = null!;
    private Shader litShader = null!;
    private TexturedQuad? shadowDebug;

    private readonly Dictionary<Type, InstancedDrawable> drawables = new();

    private double lastTime;

    public RenderWindow()
    {
        Instance = this;
        Camera = new FreeCamera();
    }

    /// Registers a drawable the first time it is requested, caching it by its runtime type.
    public T GetDrawable<T>() where T : InstancedDrawable, new()
    {
        if (!drawables.TryGetValue(typeof(T), out var d))
        {
            d = new T();
            drawables.Add(typeof(T), d);
        }
        return (T)d;
    }

    /// Registers an externally-constructed drawable. Useful when construction needs arguments.
    public T RegisterDrawable<T>(T drawable) where T : InstancedDrawable
    {
        drawables[typeof(T)] = drawable;
        return drawable;
    }

    public IReadOnlyCollection<InstancedDrawable> Drawables => drawables.Values;

    public override void Load()
    {
        Shadow = new ShadowCaster();
        DebugRenderer = new DebugRenderer();
        GuiRenderer = new ImGuiRenderer();

        skybox = new Skybox();
        skybox.Load();

        litShader = LitShader.Create();

        var fb = FramebufferSize;
        GuiRenderer.Load(fb.Width, fb.Height);
        DebugRenderer.Load();

        VerticalSync = true;

        Camera.Position = new Vector3(0, 4, 8);
        Camera.Update(Keyboard, Mouse, Width > 0 ? (float)Width / Math.Max(1, Height) : 1f);

        lastTime = Time;
    }

    public override void Draw()
    {
        float dt = (float)(Time - lastTime);
        lastTime = Time;

        // --- Render setup -------------------------------------------------
        GLDevice.Enable(Capability.DepthTest);
        GLDevice.Enable(Capability.Blend);
        GLDevice.Blend(BlendFunc.SrcAlpha, BlendFunc.OneMinusSrcAlpha);

        GLDevice.ClearColor(73f / 255f, 76f / 255f, 92f / 255f, 1f);
        GLDevice.Clear(ClearFlags.ColorAndDepth);

        skybox.Draw(Camera);

        // --- Upload all drawable instance buffers once --------------------
        foreach (var d in drawables.Values) d.UploadInstances();

        // --- Shadow pass --------------------------------------------------
        Shadow.Render(Camera, Width, Math.Max(1, Height), _ =>
        {
            foreach (var d in drawables.Values) d.DrawShadow();
        });

        // --- Lit pass -----------------------------------------------------
        (int fbw, int fbh) = FramebufferSize;
        GLDevice.Viewport(0, 0, fbw, fbh);

        litShader.Use();
        litShader.Set("uProjection", Camera.ProjectionMatrix);
        litShader.Set("uView", Camera.ViewMatrix);
        litShader.Set("uViewPos", Camera.Position);
        Shadow.BindToLitShader(litShader);

        foreach (var d in drawables.Values) d.DrawLit(litShader);

        // --- Debug lines --------------------------------------------------
        DebugRenderer.Draw(Camera);

        if (ShowShadowDebug)
        {
            shadowDebug ??= new TexturedQuad { Texture = Shadow.DepthMaps[0], Size = new Vector2(200, 200) };
            shadowDebug.Position = new Vector2(10, 10);
            shadowDebug.Draw(fbw, fbh);
        }

        if (Keyboard.IsKeyDown(Keyboard.Key.Escape)) Close();

        // --- ImGui overlay ------------------------------------------------
        GuiRenderer.Draw(dt, Width, Math.Max(1, Height), fbw, fbh, Keyboard, Mouse);

        // --- Clear per-frame instance data after we've used it ------------
        foreach (var d in drawables.Values) d.Clear();

        // --- Camera update ------------------------------------------------
        Camera.IgnoreKeyboardInput = GuiRenderer.WantsCaptureKeyboard;
        Camera.IgnoreMouseInput = GuiRenderer.WantsCaptureMouse;
        float aspect = Width > 0 ? (float)Width / Math.Max(1, Height) : 1f;
        Camera.Update(Keyboard, Mouse, aspect);
    }
}
