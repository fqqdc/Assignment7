using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vector3f = System.Numerics.Vector3;
using Vector2f = System.Numerics.Vector2;
using System.Runtime.CompilerServices;

namespace Assignment7
{
    class Triangle : GeometryObject
    {
        private Vector3f v0, v1, v2; // vertices A, B ,C , counter-clockwise order
        private Vector3f e1, e2;     // 2 edges v1-v0, v2-v0;
        //private Vector3f t0, t1, t2; // texture coords
        private Vector3f normal;
        private float area;
        private Material? m;

        public Vector3f[] V { get => new[] { v0, v1, v2 }; }

        public Triangle(Vector3f _v0, Vector3f _v1, Vector3f _v2, Material? _m = null)
        {
            v0 = _v0; v1 = _v1; v2 = _v2; m = _m;
            e1 = v1 - v0;
            e2 = v2 - v0;
            normal = Vector3f.Normalize(Vector3f.Cross(e1, e2));
            area = Vector3f.Cross(e1, e2).Length() * 0.5f;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override Bounds3 GetBounds() => Bounds3.Union(new(v0, v1), v2);

        public override float Area => area;

        public override Vector3f EvalDiffuseColor(Vector2f _vector2)
        {
            return new(0.5f, 0.5f, 0.5f);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override Intersection GetIntersection(Ray ray)
        {
            Intersection inter = new();

            if (Vector3f.Dot(ray.direction, normal) > 0)
                return inter;
            double u, v, t_tmp = 0;
            Vector3f pvec = Vector3f.Cross(ray.direction, e2); // s1
            double det = Vector3f.Dot(e1, pvec); // e1.dot(s1)
            if (Math.Abs(det) < Renderer.EPSILON) //分母特别小将导致t特别大，相当于很远很远，看不见
                return inter;

            double det_inv = 1.0 / det; // 1/(e1 s1)
            Vector3f tvec = ray.origin - v0; // s
            u = Vector3f.Dot(tvec, pvec) * det_inv; // s s1 /b1
            if (u < 0 || u > 1)
                return inter;
            Vector3f qvec = Vector3f.Cross(tvec, e1); // s2
            v = Vector3f.Dot(ray.direction, qvec) * det_inv; // b2: dir s2 / (dir s1)
            if (v < 0 || u + v > 1)
                return inter;
            t_tmp = Vector3f.Dot(e2, qvec) * det_inv; // t

            // TODO find ray triangle intersection
            if (t_tmp < 0) return inter;

            inter.distance = t_tmp;
            inter.happened = true;
            inter.m = m;
            inter.obj = this;
            inter.normal = normal;
            inter.coords = ray.GetPosition(t_tmp);

            return inter;
        }

        public override void GetSurfaceProperties(Vector3f P, Vector3f I,
                              uint index, Vector2f uv,
                              out Vector3f N, out Vector2f st)
        {
            st = Vector2f.Zero;

            N = normal;
        }

        public override bool HasEmit() => m != null && m.HasEmission();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Intersect(Ray ray)
        {
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Intersect(Ray ray, out float tnear, out int index)
        {
            tnear = float.NaN;
            index = -1;

            return true;

            //if (rayTriangleIntersect(v0, v1, v2, ray.origin, ray.direction,
            //         out var t, out var u, out var v))
            //{
            //    tnear = t;
            //    return true;
            //}

            //return false;
        }

        public override void Sample(out Intersection pos, out float pdf)
        {
            pos = new();
            float x = MathF.Sqrt(Global.GetRandomFloat()), y = Global.GetRandomFloat();
            pos.coords = v0 * (1.0f - x) + v1 * (x * (1.0f - y)) + v2 * (x * y);
            pos.normal = normal;
            pdf = 1.0f / area;
        }

        private bool rayTriangleIntersect(Vector3f v0, Vector3f v1,
                          Vector3f v2, Vector3f orig,
                          Vector3f dir, out float tnear, out float u, out float v)
        {
            tnear = float.PositiveInfinity;
            u = float.NaN; v = float.NaN;

            Vector3f edge1 = v1 - v0;
            Vector3f edge2 = v2 - v0;
            Vector3f pvec = Vector3f.Cross(dir, edge2);
            float det = Vector3f.Dot(edge1, pvec);
            if (det == 0 || det < 0)
                return false;

            Vector3f tvec = orig - v0;
            u = Vector3f.Dot(tvec, pvec);
            if (u < 0 || u > det)
                return false;

            Vector3f qvec = Vector3f.Cross(tvec, edge1);
            v = Vector3f.Dot(dir, qvec);
            if (v < 0 || u + v > det)
                return false;

            float invDet = 1 / det;

            tnear = Vector3f.Dot(edge2, qvec) * invDet;
            u *= invDet;
            v *= invDet;

            return true;
        }
    }
}
