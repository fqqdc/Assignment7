using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vector3f = System.Numerics.Vector3;
using Vector2f = System.Numerics.Vector2;
using System.Runtime.CompilerServices;
using ILGPU.Algorithms.Random;

namespace Structs
{
    public struct Triangle
    {
        public Vector3f V0, V1, V2; // vertices A, B ,C , counter-clockwise order
        public Vector3f E1, E2;     // 2 edges v1-v0, v2-v0;
        //private Vector3f t0, t1, t2; // texture coords
        public Vector3f Normal;
        public float Area;
        public int Material;

        public Triangle(Vector3f _v0, Vector3f _v1, Vector3f _v2, int material = -1)
        {
            V0 = _v0; V1 = _v1; V2 = _v2; Material = material;
            E1 = V1 - V0;
            E2 = V2 - V0;
            Normal = Vector3f.Normalize(Vector3f.Cross(E1, E2));
            Area = Vector3f.Cross(E1, E2).Length() * 0.5f;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Bounds3 GetBounds() => Bounds3.Union(new(V0, V1), V2);

        public readonly Vector3f EvalDiffuseColor(Vector2f st)
        {
            return new(0.5f, 0.5f, 0.5f);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Intersection GetIntersection(in Ray ray)
        {
            Intersection inter = new();

            if (Vector3f.Dot(ray.Direction, Normal) > 0)
                return inter;
            double u, v, t_tmp = 0;
            Vector3f pvec = Vector3f.Cross(ray.Direction, E2); // s1
            double det = Vector3f.Dot(E1, pvec); // e1.dot(s1)
            if (Math.Abs(det) < Const.EPSILON) //分母特别小将导致t特别大，相当于很远很远，看不见
                return inter;

            double det_inv = 1.0 / det; // 1/(e1 s1)
            Vector3f tvec = ray.Origin - V0; // s
            u = Vector3f.Dot(tvec, pvec) * det_inv; // s s1 /b1
            if (u < 0 || u > 1)
                return inter;
            Vector3f qvec = Vector3f.Cross(tvec, E1); // s2
            v = Vector3f.Dot(ray.Direction, qvec) * det_inv; // b2: dir s2 / (dir s1)
            if (v < 0 || u + v > 1)
                return inter;
            t_tmp = Vector3f.Dot(E2, qvec) * det_inv; // t

            // TODO find ray triangle intersection
            if (t_tmp < 0) return inter;

            inter.Distance = t_tmp;
            inter.Happened = true;
            //inter.Material = Material;
            //inter.Object = Object;
            inter.Normal = Normal;
            inter.Coords = ray.GetPosition(t_tmp);

            return inter;
        }

        public readonly void Sample(ref Structs.Random rng, out Intersection pos, out float pdf)
        {
            pos = new();
            float x = MathF.Sqrt(rng.NextFloat()), y = rng.NextFloat();
            pos.Coords = V0 * (1.0f - x) + V1 * (x * (1.0f - y)) + V2 * (x * y);
            pos.Normal = Normal;
            //pos.Material = Material;
            pdf = 1.0f / Area;
        }

        private readonly bool rayTriangleIntersect(in Vector3f v0, in Vector3f v1,
                          in Vector3f v2, in Vector3f orig,
                          in Vector3f dir, out float tnear, out float u, out float v)
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
