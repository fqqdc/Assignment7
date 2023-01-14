using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vector3f = System.Numerics.Vector3;
using Vector2f = System.Numerics.Vector2;

namespace Assignment7
{
    public abstract class GeometryObject
    {
        public abstract bool Intersect(Ray ray);
        public abstract bool Intersect(Ray ray, out float tnear, out int index);
        public abstract Intersection GetIntersection(Ray ray);
        public abstract void GetSurfaceProperties(Vector3f P, Vector3f I,
                        uint index, Vector2f uv,
                        out Vector3f N, out Vector2f st);
        public abstract Vector3f EvalDiffuseColor(Vector2f st);
        public abstract Bounds3 GetBounds();
        public abstract float Area { get; }
        public abstract void Sample(out Intersection pos, out float pdf);
        public abstract bool HasEmit();
    }
}
