using System;
using Vector3f = System.Numerics.Vector3;
using Vector2f = System.Numerics.Vector2;

namespace Assignment7
{
    public class Sphere : GeometryObject
    {
        Vector3f center;
        float radius, radius2;
        Material m;
        float area;

        public Sphere(Vector3f c, float r, Material mt)
        {
            center = c;
            radius = r;
            radius2 = r * r;
            m = mt;
            area = 4 * MathF.PI * r * r;
        }

        public Sphere(Vector3f c, float r) : this(c, r, new()) { }

        public override float Area => area;

        public override Vector3f EvalDiffuseColor(Vector2f st) => Vector3f.Zero;

        public override Bounds3 GetBounds()
        {
            return new(new(center.X - radius, center.Y - radius, center.Z - radius),
                       new(center.X + radius, center.Y + radius, center.Z + radius));
        }

        public override Intersection GetIntersection(in Ray ray)
        {
            Intersection result = new() { Happened = false };
            Vector3f L = ray.origin - center;
            float a = Vector3f.Dot(ray.direction, ray.direction);
            float b = 2 * Vector3f.Dot(ray.direction, L);
            float c = Vector3f.Dot(L, L) - radius2;
            float t0, t1;
            if (!Global.SolveQuadratic(a, b, c, out t0, out t1)) return result;
            if (t0 < 0) t0 = t1;
            if (t0 < 0) return result;

            // 相交判定修改
            result.Happened = true;

            result.Coords = ray.origin + ray.direction * t0;
            result.Normal = Vector3f.Normalize(result.Coords - center);
            result.Material = this.m;
            result.Object = this;
            result.Distance = t0;
            return result;
        }

        public override void GetSurfaceProperties(Vector3f P, Vector3f I, uint index, Vector2f uv, out Vector3f N, out Vector2f st)
        {
            st = Vector2f.Zero;

            N = Vector3f.Normalize(P - center);
        }

        public override bool HasEmit() => m.HasEmission();

        public override bool Intersect(Ray ray)
        {
            // analytic solution
            Vector3f L = ray.origin - center;
            float a = Vector3f.Dot(ray.direction, ray.direction);
            float b = 2 * Vector3f.Dot(ray.direction, L);
            float c = Vector3f.Dot(L, L) - radius2;
            float t0, t1;
            float area = 4 * MathF.PI * radius2;
            if (!Global.SolveQuadratic(a, b, c, out t0, out t1)) return false;
            if (t0 < 0) t0 = t1;
            if (t0 < 0) return false;
            return true;
        }

        public override bool Intersect(Ray ray, out float tnear, out int index)
        {
            index = -1; tnear = float.MaxValue;
            // analytic solution
            Vector3f L = ray.origin - center;
            float a = Vector3f.Dot(ray.direction, ray.direction);
            float b = 2 * Vector3f.Dot(ray.direction, L);
            float c = Vector3f.Dot(L, L) - radius2;
            float t0, t1;
            if (!Global.SolveQuadratic(a, b, c, out t0, out t1)) return false;
            if (t0 < 0) t0 = t1;
            if (t0 < 0) return false;
            tnear = t0;

            return true;
        }

        public override void Sample(out Intersection pos, out float pdf)
        {
            float theta = 2.0f * MathF.PI * Global.GetRandomFloat(), phi = MathF.PI * Global.GetRandomFloat();
            Vector3f dir = new(MathF.Cos(phi), MathF.Sin(phi) * MathF.Cos(theta), MathF.Sin(phi) * MathF.Sin(theta));
            pos = new()
            {
                Coords = center + radius * dir,
                Normal = dir,
                Emit = m.Emission,
            };
            pdf = 1.0f / area;
        }
    }
}
