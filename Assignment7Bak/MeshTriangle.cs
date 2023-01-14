using System.Collections.Generic;
using System.Diagnostics;
using System;
using System.IO;
using Vector3f = System.Numerics.Vector3;
using Vector2f = System.Numerics.Vector2;
using System.Windows.Controls.Primitives;
using System.Windows;

namespace Assignment7
{
    public class MeshTriangle : GeometryObject
    {
        BVHAccel? bvh;
        float area;
        Material m;
        List<Triangle> triangles = new();
        Bounds3 bounding_box;
        public override float Area => area;

        public MeshTriangle(string filename, Material mt, Vector3f trans, Vector3f scale)
        {
            FileInfo fileInfo = new FileInfo(filename);
            if (!fileInfo.Exists) {
                MessageBox.Show(fileInfo.FullName, "缺少文件");
                throw new FileNotFoundException(); 
            }

            ObjLoader.Loader loader = new();
            loader.LoadFile(filename);
            area = 0;
            m = mt;
            Debug.Assert(loader.LoadedMeshes.Count == 1);
            var mesh = loader.LoadedMeshes[0];

            Vector3f min_vert = new(float.PositiveInfinity,
                float.PositiveInfinity,
                float.PositiveInfinity);

            Vector3f max_vert = new(float.NegativeInfinity,
                    float.NegativeInfinity,
                    float.NegativeInfinity);

            for (int i = 0; i < mesh.Vertices.Count; i += 3)
            {
                var face_vertices = new Vector3f[3];
                for (int j = 0; j < 3; j++)
                {
                    var vert = new Vector3f(
                        mesh.Vertices[i + j].Position.X,
                        mesh.Vertices[i + j].Position.Y,
                        mesh.Vertices[i + j].Position.Z);

                    vert = scale * vert + trans;
                    face_vertices[j] = vert;

                    min_vert = new(
                        Math.Min(min_vert.X, vert.X),
                        Math.Min(min_vert.Y, vert.Y),
                        Math.Min(min_vert.Z, vert.Z));
                    max_vert = new(
                        Math.Max(max_vert.X, vert.X),
                        Math.Max(max_vert.Y, vert.Y),
                        Math.Max(max_vert.Z, vert.Z));
                }

                triangles.Add(new(face_vertices[0], face_vertices[1],
                                       face_vertices[2], mt));
            }

            bounding_box = new(min_vert, max_vert);

            List<GeometryObject> ptrs = new();
            foreach (var tri in triangles)
            {
                ptrs.Add(tri);
                area += tri.Area;
            }

            bvh = new BVHAccel(ptrs);
        }
        public MeshTriangle(string filename, Material mt) : this(filename, mt, new(0), new(1)) { }
        public MeshTriangle(string filename) : this(filename, new()) { }

        public override bool Intersect(Ray ray)
        {
            return true;
        }

        public override bool Intersect(Ray ray, out float tnear, out int index)
        {
            tnear = float.PositiveInfinity;
            index = -1;

            bool intersect = false;
            foreach (var triangle in triangles)
            {
                if (triangle.Intersect(ray, out var tnearK, out var indexK)
                    && tnearK < tnear)
                {
                    tnear = tnearK;
                    index = indexK;
                    intersect |= true;
                }
            }
            return intersect;


            //tnear = float.PositiveInfinity;
            //index = uint.MaxValue;

            //bool intersect = false;
            //for (uint k = 0; k < numTriangles; ++k)
            //{
            //    Vector3f v0 = vertices[vertexIndex[k * 3]];
            //    Vector3f v1 = vertices[vertexIndex[k * 3 + 1]];
            //    Vector3f v2 = vertices[vertexIndex[k * 3 + 2]];

            //    if (rayTriangleIntersect(v0, v1, v2, ray.origin, ray.direction,
            //         out var t, out var u, out var v)
            //        && t < tnear)
            //    {
            //        tnear = t;
            //        index = k;
            //        intersect |= true;
            //    }
            //}

            //return intersect;
        }

        public override Bounds3 GetBounds() => bounding_box;

        public override Vector3f EvalDiffuseColor(Vector2f st)
        {
            float scale = 5;
            float pattern =
                (st.X * scale % 1 > 0.5) ^ (st.Y * scale % 1 > 0.5) ? 1f : 0f;
            return Vector3f.Lerp(
                new(0.815f, 0.235f, 0.031f),
                new(0.937f, 0.937f, 0.231f), pattern);
        }

        public override Intersection GetIntersection(Ray ray)
        {
            Intersection intersec = new();

            if (bvh != null)
            {
                intersec = bvh.Intersect(ray);
            }

            return intersec;
        }

        public override void GetSurfaceProperties(Vector3f P, Vector3f I, uint index, Vector2f uv, out Vector3f N, out Vector2f st)
        {
            triangles[(int)index].GetSurfaceProperties(P, I, index, uv, out N, out st);

            //Vector3f v0 = vertices[vertexIndex[index * 3]];
            //Vector3f v1 = vertices[vertexIndex[index * 3 + 1]];
            //Vector3f v2 = vertices[vertexIndex[index * 3 + 2]];
            //Vector3f e0 = Vector3f.Normalize(v1 - v0);
            //Vector3f e1 = Vector3f.Normalize(v2 - v1);
            //N = Vector3f.Normalize(Vector3f.Cross(e0, e1));
            //Vector2f st0 = stCoordinates[vertexIndex[index * 3]];
            //Vector2f st1 = stCoordinates[vertexIndex[index * 3 + 1]];
            //Vector2f st2 = stCoordinates[vertexIndex[index * 3 + 2]];
            //st = st0 * (1 - uv.X - uv.Y) + st1 * uv.X + st2 * uv.Y;
        }


        public override void Sample(out Intersection pos, out float pdf)
        {
            if (bvh == null) throw new NullReferenceException();

            bvh.Sample(out pos, out pdf);
            pos.emit = m.Emission;
        }

        public override bool HasEmit()
        {
            return m.HasEmission();
        }
    }
}