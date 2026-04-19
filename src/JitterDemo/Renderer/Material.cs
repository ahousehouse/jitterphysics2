using JitterDemo.Renderer.OpenGL;

namespace JitterDemo.Renderer;

/// Describes how a surface is shaded by the lit shader. Each field is independent:
/// the final fragment color mixes vertex-color, a flat tint, and optional texture
/// according to the two weight scalars.
public struct Material
{
    /// Uniform color added when VertexColorWeight &lt; 1.
    public Vector3 Tint;

    /// Specular reflection color.
    public Vector3 Specular;

    /// Specular exponent.
    public float Shininess;

    /// Alpha multiplier applied to the final color.
    public float Alpha;

    /// Ambient = VertexColorWeight * vertexInstanceColor + (1 - VertexColorWeight) * Tint.
    /// 0 = use Tint only, 1 = use per-instance color only.
    public float VertexColorWeight;

    /// Diffuse = (1 - TextureWeight) * flatGrey + TextureWeight * sampleTexture.
    /// 0 = flat, 1 = fully textured.
    public float TextureWeight;

    /// If true the vertex normal is flipped before lighting (used for two-sided draws).
    public bool FlipNormals;

    /// Sampled when TextureWeight &gt; 0. May be null only if TextureWeight = 0.
    public Texture2D? Texture;

    public static Material Default => new()
    {
        Tint = Vector3.Zero,
        Specular = new Vector3(0.1f, 0.1f, 0.1f),
        Shininess = 128f,
        Alpha = 1f,
        VertexColorWeight = 1f,
        TextureWeight = 0f,
        FlipNormals = false,
        Texture = null
    };
}
