using Assignment6;
using Assignment7;
using ILGPU.Algorithms.Random;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using Vector3f = System.Numerics.Vector3;

namespace Structs
{
    public struct Material
    {
        // Compute reflection direction
        private readonly Vector3f reflect(Vector3f I, Vector3f N)
        {
            return I - 2 * Vector3f.Dot(I, N) * N;
        }

        // Compute refraction direction using Snell's law
        //
        // We need to handle with care the two possible situations:
        //
        //    - When the ray is inside the object
        //
        //    - When the ray is outside.
        //
        // If the ray is outside, you need to make cosi positive cosi = -N.I
        //
        // If the ray is inside, you need to invert the refractive indices and negate the normal N
        private Vector3f refract(Vector3f I, Vector3f N, float ior)
        {
            float cosi = float.Clamp(Vector3f.Dot(I, N), -1, 1);
            float etai = 1, etat = ior;
            Vector3f n = N;
            if (cosi < 0) { cosi = -cosi; } else { (etai, etat) = (etat, etai); n = -N; }
            float eta = etai / etat;
            float k = 1 - eta * eta * (1 - cosi * cosi);
            return k < 0 ? Vector3f.Zero : eta * I + (eta * cosi - MathF.Sqrt(k)) * n;
        }

        // Compute Fresnel equation
        //
        // \param I is the incident view direction
        //
        // \param N is the normal at the intersection point
        //
        // \param ior is the material refractive index
        //
        // \param[out] kr is the amount of light reflected
        private void fresnel(Vector3f I, Vector3f N, float ior, out float kr)
        {
            float cosi = float.Clamp(Vector3f.Dot(I, N), -1, 1);
            float etai = 1, etat = ior;
            if (cosi > 0) { (etai, etat) = (etat, etai); }
            // Compute sini using Snell's law
            float sint = etai / etat * MathF.Abs(MathF.Max(0f, 1 - cosi * cosi));
            // Total internal reflection
            if (sint >= 1)
            {
                kr = 1;
            }
            else
            {
                float cost = MathF.Sqrt(MathF.Max(0f, 1 - sint * sint));
                cosi = MathF.Abs(cosi);
                float Rs = ((etat * cosi) - (etai * cost)) / ((etat * cosi) + (etai * cost));
                float Rp = ((etai * cosi) - (etat * cost)) / ((etai * cosi) + (etat * cost));
                kr = (Rs * Rs + Rp * Rp) / 2;
            }
            // As a consequence of the conservation of energy, transmittance is given by:
            // kt = 1 - kr;
        }


        private readonly Vector3f toWorld(Vector3f a, Vector3f N)
        {
            Vector3f B, C;
            if (MathF.Abs(N.X) > MathF.Abs(N.Y))
            {
                float invLen = 1.0f / MathF.Sqrt(N.X * N.X + N.Z * N.Z);
                C = new(N.Z * invLen, 0.0f, -N.X * invLen);
            }
            else
            {
                float invLen = 1.0f / MathF.Sqrt(N.Y * N.Y + N.Z * N.Z);
                C = new(0.0f, N.Z * invLen, -N.Y * invLen);
            }
            B = Vector3f.Cross(C, N);
            return a.X * B + a.Y * C + a.Z * N;
        }


        //Vector3f m_color;
        public required Vector3f Emission { get; init; }
        public float Ior { get; }
        public required Vector3f Kd { get; init; }
        public required Vector3f Ks { get; init; }
        public float SpecularExponent { get; }

        public MaterialType Type { get; init; }

        public readonly bool HasEmission()
        {
            if (Emission.Length() > Const.EPSILON) return true;
            else return false;
        }

        public Vector3f GetColorAt(double u, double v)
        {
            return new();
        }

        // sample a ray by Material properties
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Vector3f Sample(in Vector3f wi, in Vector3f N, ref Structs.Random rng)
        {
            if(Type == MaterialType.Mirror)
                return reflect(wi, N);

            // uniform sample on the hemisphere在半球上均匀采样
            float x_1 = rng.NextFloat(), x_2 = rng.NextFloat();
            //z∈[0,1]，是随机半球方向的z轴向量
            float z = MathF.Abs(1.0f - 2.0f * x_1);
            //r是半球半径随机向量以法线为旋转轴的半径
            //phi是r沿法线旋转轴的旋转角度
            float r = MathF.Sqrt(1.0f - z * z), phi = 2 * MathF.PI * x_2;//phi∈[0,2*pi]
            Vector3f localRay = new(r * MathF.Cos(phi), r * MathF.Sin(phi), z);//半球面上随机的光线的弹射方向
            return toWorld(localRay, N);//转换到世界坐标

        }

        // given a ray, calculate the PdF of this ray
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly float Pdf(in Vector3f wi, in Vector3f wo, in Vector3f N)
        {
            // uniform sample probability 1 / (2 * PI)
            if (Vector3f.Dot(wo, N) > 0.0f)
                return 0.5f / MathF.PI;
            else
                return 0.0f;
        }

        // given a ray, calculate the contribution of this ray
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Vector3f Eval(in Vector3f wi, in Vector3f wo, in Vector3f N)
        {
            // calculate the contribution of diffuse   model
            float cosalpha = Vector3f.Dot(N, wo);
            if (cosalpha > 0.0f)
            {
                Vector3f diffuse = Kd / MathF.PI;
                return diffuse;
            }
            else
                return new(0.0f);
        }
    }
}
