using System;
using System.Runtime.CompilerServices;
using Vector3f = System.Numerics.Vector3;

namespace Assignment7
{
    public struct Bounds3
    {
        // two points to specify the bounding box
        public readonly Vector3f Min;
        public readonly Vector3f Max;
        public Bounds3()
        {
            float minNum = float.MinValue;
            float maxNum = float.MaxValue;
            Max = new(minNum, minNum, minNum);
            Min = new(maxNum, maxNum, maxNum);
        }
        public Bounds3(Vector3f p) { Min = p; Max = p; }
        public Bounds3(Vector3f p1, Vector3f p2)
        {
            Min = Vector3f.Min(p1, p2);
            Max = Vector3f.Max(p1, p2);
        }

        /// <summary>
        /// 对角线
        /// </summary>
        public Vector3f Diagonal() { return Max - Min; }

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
            return 0.5f * Min + 0.5f * Max;
        }

        public Bounds3 Intersect(Bounds3 b)
        {
            return new(new(MathF.Max(Min.X, b.Min.X), MathF.Max(Min.Y, b.Min.Y),
                                    MathF.Max(Min.Z, b.Min.Z)),
                           new(MathF.Min(Max.X, b.Max.X), MathF.Min(Max.Y, b.Max.Y),
                                    MathF.Min(Max.Z, b.Max.Z)));
        }

        public Vector3f Offset(Vector3f p)
        {
            Vector3f o = p - Min;
            if (Max.Z > Min.Z)
                o.Z /= Max.Z - Min.Z;
            if (Max.Y > Min.Y)
                o.Y /= Max.Y - Min.Y;
            if (Max.Z > Min.Z)
                o.Z /= Max.Z - Min.Z;
            return o;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Overlaps(Bounds3 b1, Bounds3 b2)
        {
            bool x = (b1.Max.X >= b2.Min.X) && (b1.Min.X <= b2.Max.X);
            bool y = (b1.Max.Y >= b2.Min.Y) && (b1.Min.Y <= b2.Max.Y);
            bool z = (b1.Max.Z >= b2.Min.Z) && (b1.Min.Z <= b2.Max.Z);
            return (x && y && z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Inside(Vector3f p, Bounds3 b)
        {
            return (p.X >= b.Min.X && p.X <= b.Max.X && p.Y >= b.Min.Y &&
                    p.Y <= b.Max.Y && p.Z >= b.Min.Z && p.Z <= b.Max.Z);
        }

        public Vector3f this[int i]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (i == 0) ? Min : Max;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IntersectP(Ray ray, Vector3f invDir,
                                ReadOnlySpan<bool> dirIsNeg)
        {
            // invDir: ray direction(x,y,z), invDir=(1.0/x,1.0/y,1.0/z), use this because Multiply is faster that Division
            // dirIsNeg: ray direction(x,y,z), dirIsNeg=[int(x>0),int(y>0),int(z>0)], use this to simplify your logic
            // TODO test if ray bound intersects

            //invDir = 1 / D; t = (Px - Ox) / dx
            var tMinX = (Min.X - ray.origin.X) * invDir.X;
            var tMinY = (Min.Y - ray.origin.Y) * invDir.Y;
            var tMinZ = (Min.Z - ray.origin.Z) * invDir.Z;
            var tMaxX = (Max.X - ray.origin.X) * invDir.X;
            var tMaxY = (Max.Y - ray.origin.Y) * invDir.Y;
            var tMaxZ = (Max.Z - ray.origin.Z) * invDir.Z;

            //如果发现射线的方向是反的，调换t_min和t_max的位置。
            if (dirIsNeg[0])
                Global.Swap(ref tMinX, ref tMaxX);
            if (dirIsNeg[1])
                Global.Swap(ref tMinY, ref tMaxY);
            if (dirIsNeg[2])
                Global.Swap(ref tMinZ, ref tMaxZ);

            var tEnter = MathF.Max(tMinX, MathF.Max(tMinY, tMinZ));
            var tExit = MathF.Min(tMaxX, MathF.Min(tMaxY, tMaxZ));

            return tEnter <= tExit && tExit >= 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Bounds3 Union(Bounds3 b1, Bounds3 b2)
            => new(Vector3f.Min(b1.Min, b2.Min), Vector3f.Max(b1.Max, b2.Max));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Bounds3 Union(Bounds3 b, Vector3f p)
            => new(Vector3f.Min(b.Min, p), Vector3f.Max(b.Max, p));
    }
}
