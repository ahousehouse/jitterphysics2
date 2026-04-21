using JitterDemo.Renderer.OpenGL;

namespace JitterDemo.Renderer;

/// Drawable wrapping a mesh loaded from an OBJ file (plain or zipped).
public class TriangleMeshDrawable : InstancedDrawable
{
    public TriangleMeshDrawable(string filename, float scale = 1f, bool reverseWinding = false)
        : base(Mesh.LoadObj(filename, scale, reverseWinding))
    {
    }

    public TriangleMeshDrawable(Mesh mesh) : base(mesh) { }
}

/// Drawable with runtime-mutable vertex data (used for soft bodies, heightmaps).
/// Call <see cref="SetTriangles"/> once to establish the index buffer, then mutate
/// <see cref="Mesh"/>.Vertices and call <see cref="RefreshGeometry"/> each frame.
public class MutableMeshDrawable : InstancedDrawable
{
    public MutableMeshDrawable() : base(new Mesh(new Vertex[4], new TriangleVertexIndex[0]))
    {
        // Seed with a placeholder so construction works; SetTriangles supplies real data.
        TwoSided = true;
        ShadowsDoubleSided = true;
    }

    public void SetTriangles(TriangleVertexIndex[] triangles)
    {
        Mesh.Indices = triangles;
        // Same reason as in CreateBuffers: element-buffer binding is VAO state.
        Vao.Bind();
        Ibo.Upload<TriangleVertexIndex>(triangles);

        uint largest = 0;
        for (int i = 0; i < triangles.Length; i++)
        {
            if (triangles[i].T1 > largest) largest = triangles[i].T1;
            if (triangles[i].T2 > largest) largest = triangles[i].T2;
            if (triangles[i].T3 > largest) largest = triangles[i].T3;
        }

        Mesh.Vertices = new Vertex[largest + 1];
    }

    /// Recomputes per-vertex normals from triangles and uploads the vertex data.
    public void RefreshGeometry()
    {
        var verts = Mesh.Vertices;
        var inds = Mesh.Indices;

        for (int i = 0; i < verts.Length; i++) verts[i].Normal = Vector3.Zero;

        for (int i = 0; i < inds.Length; i++)
        {
            ref var v1 = ref verts[inds[i].T1];
            ref var v2 = ref verts[inds[i].T2];
            ref var v3 = ref verts[inds[i].T3];

            Vector3 n = Vector3.Cross(v2.Position - v1.Position, v3.Position - v1.Position);
            v1.Normal += n;
            v2.Normal += n;
            v3.Normal += n;
        }

        Vbo.Stream<Vertex>(verts, verts.Length);
    }
}
