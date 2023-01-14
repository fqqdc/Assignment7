using Assignment6;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;
using Vector3f = System.Numerics.Vector3;

namespace Assignment7
{
    public class Bounds3
    {
        // two points to specify the bounding box
        public Vector3f MinPoint { get; private set; }
        public Vector3f MaxPoint { get; private set; }
        public Bounds3()
        {
            float minNum = float.MinValue;
            float maxNum = float.MaxValue;
            MaxPoint = new(minNum, minNum, minNum);
            MinPoint = new(maxNum, maxNum, maxNum);
        }
        public Bounds3(Vector3f p) { MinPoint = p; MaxPoint = p; }
        public Bounds3(Vector3f p1, Vector3f p2)
        {
            MinPoint = Vector3f.Min(p1, p2);
            MaxPoint = Vector3f.Max(p1, p2);
        }

        /// <summary>
        /// 对角线
        /// </summary>
        public Vector3f Diagonal() { return MaxPoint - MinPoint; }

        /// <summary>
        /// 最长的轴
        /// </summary>
        public int MaxExtent()
        {
            Vector3f d = Diagonal();
            if (d.X > d.Y && d.X > d.Z)
                return 0;
            else if (d.Y > d.Z)
                return 1;
            else
                return 2;
        }

        /// <summary>
        /// 表面积
        /// </summary>
        public float SurfaceArea()
        {
            Vector3f d = Diagonal();
            return 2 * (d.X * d.Y + d.X * d.Z + d.Y * d.Z);
        }

        /// <summary>
        /// 重心
        /// </summary>
        public Vector3f Centroid()
        {
            return 0.5f * MinPoint + 0.5f * MaxPoint;
        }

        public Bounds3 Intersect(Bounds3 b)
        {
            return new(new(MathF.Max(MinPoint.X, b.MinPoint.X), MathF.Max(MinPoint.Y, b.MinPoint.Y),
                                    MathF.Max(MinPoint.Z, b.MinPoint.Z)),
                           new(MathF.Min(MaxPoint.X, b.MaxPoint.X), MathF.Min(MaxPoint.Y, b.MaxPoint.Y),
                                    MathF.Min(MaxPoint.Z, b.MaxPoint.Z)));
        }

        public Vector3f Offset(Vector3f p)
        {
            Vector3f o = p - MinPoint;
            if (MaxPoint.Z > MinPoint.Z)
                o.Z /= MaxPoint.Z - MinPoint.Z;
            if (MaxPoint.Y > MinPoint.Y)
                o.Y /= MaxPoint.Y - MinPoint.Y;
            if (MaxPoint.Z > MinPoint.Z)
                o.Z /= MaxPoint.Z - MinPoint.Z;
            return o;
        }

        public bool Overlaps(Bounds3 b1, Bounds3 b2)
        {
            bool x = (b1.MaxPoint.X >= b2.MinPoint.X) && (b1.MinPoint.X <= b2.MaxPoint.X);
            bool y = (b1.MaxPoint.Y >= b2.MinPoint.Y) && (b1.MinPoint.Y <= b2.MaxPoint.Y);
            bool z = (b1.MaxPoint.Z >= b2.MinPoint.Z) && (b1.MinPoint.Z <= b2.MaxPoint.Z);
            return (x && y && z);
        }

        public bool Inside(Vector3f p, Bounds3 b)
        {
            return (p.X >= b.MinPoint.X && p.X <= b.MaxPoint.X && p.Y >= b.MinPoint.Y &&
                    p.Y <= b.MaxPoint.Y && p.Z >= b.MinPoint.Z && p.Z <= b.MaxPoint.Z);
        }

        public Vector3f this[int i]
        {
            get => (i == 0) ? MinPoint : MinPoint;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IntersectP(Ray ray, Vector3f invDir,
                                bool[] dirIsNeg)
        {
            // invDir: ray direction(x,y,z), invDir=(1.0/x,1.0/y,1.0/z), use this because Multiply is faster that Division
            // dirIsNeg: ray direction(x,y,z), dirIsNeg=[int(x>0),int(y>0),int(z>0)], use this to simplify your logic
            // TODO test if ray bound intersects

            //invDir = 1 / D; t = (Px - Ox) / dx
            var tMinX = (MinPoint.X - ray.origin.X) * invDir.X;
            var tMinY = (MinPoint.Y - ray.origin.Y) * invDir.Y;
            var tMinZ = (MinPoint.Z - ray.origin.Z) * invDir.Z;
            var tMaxX = (MaxPoint.X - ray.origin.X) * invDir.X;
            var tMaxY = (MaxPoint.Y - ray.origin.Y) * invDir.Y;
            var tMaxZ = (MaxPoint.Z - ray.origin.Z) * invDir.Z;

            //如果发现射线的方向是反的，调换t_min和t_max的位置。
            if (dirIsNeg[0])
                Global.Swap(ref tMinX, ref tMaxX);
            if (dirIsNeg[1])
                Global.Swap(ref tMinY, ref tMaxY);
            if (dirIsNeg[2])
                Global.Swap(ref tMinZ, ref tMaxZ);

            var tEnter = new float[] { tMinX, tMinY, tMinZ }.Max();
            var tExit = new float[] { tMaxX, tMaxY, tMaxZ }.Min();

            return tEnter <= tExit && tExit >= 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Bounds3 Union(Bounds3 b1, Bounds3 b2)
        {
            return new()
            {
                MinPoint = Vector3f.Min(b1.MinPoint, b2.MinPoint),
                MaxPoint = Vector3f.Max(b1.MaxPoint, b2.MaxPoint),
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Bounds3 Union(Bounds3 b, Vector3f p)
        {
            return new()
            {
                MinPoint = Vector3f.Min(b.MinPoint, p),
                MaxPoint = Vector3f.Max(b.MaxPoint, p),
            };
        }
    }
}
