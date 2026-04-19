using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using JitterDemo.Renderer.OpenGL;

namespace JitterDemo.Renderer;

public struct MeshGroup
{
    public string Name;
    public int FromInclusive;
    public int ToExclusive;
}

public class Mesh
{
    public Vertex[] Vertices;
    public TriangleVertexIndex[] Indices;
    public MeshGroup[] Groups;

    public Mesh(Vertex[] vertices, TriangleVertexIndex[] indices, MeshGroup[]? groups = null)
    {
        Vertices = vertices;
        Indices = indices;
        Groups = groups ?? Array.Empty<MeshGroup>();
    }

    public void Transform(in Matrix4 matrix)
    {
        for (int i = 0; i < Vertices.Length; i++)
        {
            Vertices[i].Position = Vector3.Transform(Vertices[i].Position, matrix);
        }
    }

    public static Mesh Cube()
    {
        Vertex[] vertices = new Vertex[24];
        TriangleVertexIndex[] indices = new TriangleVertexIndex[12];

        Vector3[] normals =
        {
            new(0, 0, 1), new(0, 0, -1),
            new(1, 0, 0), new(-1, 0, 0),
            new(0, 1, 0), new(0, -1, 0)
        };

        for (int i = 0; i < 6; i++)
        {
            Vector3 n = normals[i];
            Vector3 s1 = new(n.Y, n.Z, n.X);
            Vector3 s2 = Vector3.Cross(n, s1);

            vertices[4 * i + 0] = new Vertex((n - s1 - s2) * 0.5f, n);
            vertices[4 * i + 1] = new Vertex((n - s1 + s2) * 0.5f, n);
            vertices[4 * i + 2] = new Vertex((n + s1 + s2) * 0.5f, n);
            vertices[4 * i + 3] = new Vertex((n + s1 - s2) * 0.5f, n);

            indices[2 * i + 0] = new TriangleVertexIndex(4 * i + 1, 4 * i + 0, 4 * i + 2);
            indices[2 * i + 1] = new TriangleVertexIndex(4 * i + 2, 4 * i + 0, 4 * i + 3);
        }

        return new Mesh(vertices, indices);
    }

    public static Mesh Sphere(int tesselation = 20)
    {
        int t = tesselation;
        var vertices = new List<Vertex>();
        var indices = new List<TriangleVertexIndex>();

        for (int e = 0; e < t; e++)
        {
            for (int i = 0; i < t; i++)
            {
                double alpha = 2.0 * Math.PI / t * i;
                double beta = Math.PI / (t + 1) * (e + 1);

                Vector3 v;
                v.X = (float)(Math.Cos(alpha) * Math.Sin(beta));
                v.Y = (float)Math.Cos(beta);
                v.Z = (float)(Math.Sin(alpha) * Math.Sin(beta));

                vertices.Add(new Vertex(v * 0.5f, v));
            }
        }

        vertices.Add(new Vertex(-Vector3.UnitY * 0.5f, -Vector3.UnitY));
        vertices.Add(new Vertex(+Vector3.UnitY * 0.5f, +Vector3.UnitY));

        for (int e = 0; e < t - 1; e++)
        {
            for (int i = 0; i < t; i++)
            {
                indices.Add(new TriangleVertexIndex(e * t + i, e * t + (i + 1) % t, (e + 1) * t + i));
                indices.Add(new TriangleVertexIndex(e * t + (i + 1) % t, (e + 1) * t + (i + 1) % t, (e + 1) * t + i));
            }
        }

        for (int i = 0; i < t; i++)
        {
            int e = t - 1;
            indices.Add(new TriangleVertexIndex(e * t + i, e * t + (i + 1) % t, t * t + 0));
            indices.Add(new TriangleVertexIndex(0 * t + (i + 1) % t, 0 * t + i, t * t + 1));
        }

        return new Mesh(vertices.ToArray(), indices.ToArray());
    }

    public static Mesh HalfSphere(int tesselation = 20)
    {
        int t = tesselation;
        int halfT = t / 2;

        var vertices = new List<Vertex>();
        var indices = new List<TriangleVertexIndex>();

        for (int e = 0; e < halfT; e++)
        {
            for (int i = 0; i < t; i++)
            {
                double alpha = 2.0 * Math.PI / t * i;
                double beta = Math.PI / t * e;

                Vector3 v;
                v.X = (float)(Math.Cos(alpha) * Math.Cos(beta));
                v.Y = (float)Math.Sin(beta);
                v.Z = (float)(Math.Sin(alpha) * Math.Cos(beta));

                vertices.Add(new Vertex(v * 0.5f, v));
            }
        }

        vertices.Add(new Vertex(Vector3.UnitY * 0.5f, Vector3.UnitY));

        for (int e = 0; e < halfT - 1; e++)
        {
            for (int i = 0; i < t; i++)
            {
                indices.Add(new TriangleVertexIndex(e * t + (i + 1) % t, e * t + i, (e + 1) * t + i));
                indices.Add(new TriangleVertexIndex((e + 1) * t + (i + 1) % t, e * t + (i + 1) % t, (e + 1) * t + i));
            }
        }

        for (int i = 0; i < t; i++)
        {
            int e = halfT - 1;
            indices.Add(new TriangleVertexIndex(e * t + (i + 1) % t, e * t + i, halfT * t));
        }

        return new Mesh(vertices.ToArray(), indices.ToArray());
    }

    public static Mesh Cylinder(int tesselation = 20)
    {
        int t = tesselation;
        var vertices = new List<Vertex>
        {
            new(-0.5f * Vector3.UnitY, -Vector3.UnitY),
            new(+0.5f * Vector3.UnitY, +Vector3.UnitY)
        };
        var indices = new List<TriangleVertexIndex>();

        for (int i = 0; i < t; i++)
        {
            double alpha = 2.0 * Math.PI / t * i;

            Vector3 v = new()
            {
                X = (float)Math.Cos(alpha),
                Z = (float)Math.Sin(alpha)
            };

            vertices.Add(new Vertex(v - 0.5f * Vector3.UnitY, -Vector3.UnitY));
            vertices.Add(new Vertex(v + 0.5f * Vector3.UnitY, +Vector3.UnitY));
            vertices.Add(new Vertex(v - 0.5f * Vector3.UnitY, v));
            vertices.Add(new Vertex(v + 0.5f * Vector3.UnitY, v));
        }

        int t4 = 4 * t;
        for (int i = 0; i < t; i++)
        {
            indices.Add(new TriangleVertexIndex(0, 2 + 4 * i, 2 + (4 * i + 4) % t4));
            indices.Add(new TriangleVertexIndex(3 + 4 * i, 1, 3 + (4 * i + 4) % t4));
            indices.Add(new TriangleVertexIndex(4 + 4 * i, 5 + 4 * i, 4 + (4 * i + 4) % t4));
            indices.Add(new TriangleVertexIndex(5 + 4 * i, 5 + (4 * i + 4) % t4, 4 + (4 * i + 4) % t4));
        }

        return new Mesh(vertices.ToArray(), indices.ToArray());
    }

    public static Mesh Cone(int tesselation = 20)
    {
        int t = tesselation;
        var vertices = new List<Vertex>();
        var indices = new List<TriangleVertexIndex>();

        const float bottomY = -1f / 4f;
        const float topY = 3f / 4f;

        vertices.Add(new Vertex(new Vector3(0, bottomY, 0), -Vector3.UnitY));

        for (int i = 0; i < t; i++)
        {
            double alpha1 = 2.0 * Math.PI / t * i;
            double alpha2 = alpha1 + Math.PI / t;
            float beta = (float)(2.0 / Math.Sqrt(5.0));

            Vector3 v1 = new()
            {
                X = 0.5f * (float)Math.Cos(alpha1),
                Z = 0.5f * (float)Math.Sin(alpha1)
            };

            Vector3 n2 = Spherical(alpha1, beta);
            Vector3 n3 = Spherical(alpha2, beta);

            vertices.Add(new Vertex(new Vector3(v1.X, bottomY, v1.Z), -Vector3.UnitY));
            vertices.Add(new Vertex(new Vector3(v1.X, bottomY, v1.Z), n2));
            vertices.Add(new Vertex(new Vector3(0, topY, 0), n3));
        }

        int t3 = 3 * t;
        for (int i = 0; i < t; i++)
        {
            indices.Add(new TriangleVertexIndex(0, 1 + 3 * i, 1 + (3 * i + 3) % t3));
            indices.Add(new TriangleVertexIndex(2 + 3 * i, 3 + 3 * i, 2 + (3 * i + 3) % t3));
        }

        return new Mesh(vertices.ToArray(), indices.ToArray());

        static Vector3 Spherical(double alpha, double beta)
        {
            return new Vector3(
                (float)(Math.Cos(alpha) * Math.Sin(beta)),
                (float)Math.Sin(beta),
                (float)(Math.Sin(alpha) * Math.Sin(beta)));
        }
    }

    public static Mesh Quad(float halfSize, float uvScale = 1f)
    {
        Vertex[] vertices = new Vertex[4];
        vertices[0] = new Vertex(new Vector3(-halfSize, 0, -halfSize), Vector3.UnitY, new Vector2(0, 0));
        vertices[1] = new Vertex(new Vector3(-halfSize, 0, +halfSize), Vector3.UnitY, new Vector2(0, uvScale));
        vertices[2] = new Vertex(new Vector3(+halfSize, 0, -halfSize), Vector3.UnitY, new Vector2(uvScale, 0));
        vertices[3] = new Vertex(new Vector3(+halfSize, 0, +halfSize), Vector3.UnitY, new Vector2(uvScale, uvScale));

        TriangleVertexIndex[] indices =
        {
            new(0, 1, 2),
            new(2, 1, 3)
        };

        return new Mesh(vertices, indices);
    }

    public static Mesh LoadObj(string filename, float scale = 1f, bool reverseWinding = false)
    {
        string path = Path.IsPathRooted(filename) ? filename : Path.Combine("assets", filename);

        var format = new NumberFormatInfo { NumberDecimalSeparator = "." };
        float ParseFloat(string s) => float.Parse(s, format);
        int ParseIndex(string s) => int.Parse(s) - 1;

        string[] contents = path.EndsWith(".zip") ? ReadZip(path).ToArray() : File.ReadAllLines(path);
        var lines = contents.Select(s => s.Trim()).Where(s => s.Length > 0).ToArray();

        List<Vector3> positions = new();
        List<Vector3> normals = new();
        List<Vector2> uvs = new();

        foreach (string line in lines)
        {
            var s = line.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            switch (s[0])
            {
                case "v":
                    positions.Add(new Vector3(ParseFloat(s[1]), ParseFloat(s[2]), ParseFloat(s[3])));
                    break;
                case "vn":
                    normals.Add(new Vector3(ParseFloat(s[1]), ParseFloat(s[2]), ParseFloat(s[3])));
                    break;
                case "vt":
                    uvs.Add(new Vector2(ParseFloat(s[1]), ParseFloat(s[2])));
                    break;
            }
        }

        bool hasUvs = uvs.Count > 0;
        bool hasNormals = normals.Count > 0;

        Dictionary<string, int> vertexCache = new();
        List<Vertex> vertices = new();
        List<TriangleVertexIndex> indices = new();
        List<MeshGroup> groups = new();
        MeshGroup current = default;
        bool groupOpen = false;

        int AddVertex(string s)
        {
            var a = s.Split('/');
            Vector3 p = positions[ParseIndex(a[0])];
            Vector3 n = hasNormals ? normals[ParseIndex(a[2])] : Vector3.UnitX;
            Vector2 uv = hasUvs ? uvs[ParseIndex(a[1])] : Vector2.Zero;
            vertices.Add(new Vertex(p, n, uv));
            return vertices.Count - 1;
        }

        foreach (string line in lines)
        {
            var s = line.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

            switch (s[0])
            {
                case "o":
                    if (groupOpen)
                    {
                        current.ToExclusive = indices.Count;
                        groups.Add(current);
                    }
                    groupOpen = true;
                    current = new MeshGroup
                    {
                        FromInclusive = indices.Count,
                        Name = s.Length > 1 ? s[1] : string.Empty
                    };
                    break;

                case "f":
                    int i0 = GetOrAdd(s[1]);
                    int i1 = GetOrAdd(s[2]);
                    int i2 = GetOrAdd(s[3]);
                    indices.Add(reverseWinding
                        ? new TriangleVertexIndex(i1, i0, i2)
                        : new TriangleVertexIndex(i0, i1, i2));
                    break;
            }
        }

        if (groupOpen)
        {
            current.ToExclusive = indices.Count;
            groups.Add(current);
        }

        Mesh mesh = new(vertices.ToArray(), indices.ToArray(), groups.ToArray());
        if (scale != 1f) mesh.Transform(MatrixHelper.CreateScale(scale));
        return mesh;

        int GetOrAdd(string token)
        {
            if (!vertexCache.TryGetValue(token, out int index))
            {
                index = AddVertex(token);
                vertexCache.Add(token, index);
            }
            return index;
        }
    }

    private static IEnumerable<string> ReadZip(string filename)
    {
        using var zip = new ZipArchive(File.OpenRead(filename));
        if (zip.Entries.Count != 1)
            throw new InvalidOperationException("Invalid zip file. Expected exactly one entry.");

        using var sr = new StreamReader(zip.Entries[0].Open(), Encoding.UTF8);
        while (!sr.EndOfStream) yield return sr.ReadLine()!;
    }
}
