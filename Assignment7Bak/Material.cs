using Assignment6;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using Vector3f = System.Numerics.Vector3;

namespace Assignment7
{
    public enum MaterialType { DIFFUSE };
    public class Material
    {
        // Compute reflection direction
        private Vector3f reflect(Vector3f I, Vector3f N)
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
            if (cosi < 0) { cosi = -cosi; } else { Global.Swap(ref etai, ref etat); n = -N; }
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
            if (cosi > 0) { Global.Swap(ref etai, ref etat); }
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


        private Vector3f toWorld(Vector3f a, Vector3f N)
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


        public MaterialType Type { get; }
        //Vector3f m_color;
        public Vector3f Emission { get; }
        public float Ior { get; }
        public Vector3f Kd { get; init; }
        public Vector3f Ks { get; }
        public float SpecularExponent { get; }
        //Texture tex;

        public Material(MaterialType t, Vector3f e)
        {
            Type = t;
            //m_color = c;
            Emission = e;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Material() : this(MaterialType.DIFFUSE, new(0, 0, 0)) { }

        public bool HasEmission()
        {
            if (Emission.Length() > Renderer.EPSILON) return true;
            else return false;
        }

        public Vector3f GetColorAt(double u, double v)
        {
            return new();
        }

        // sample a ray by Material properties
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3f Sample(Vector3f wi, Vector3f N)
        {
            switch (Type)
            {
                case MaterialType.DIFFUSE:
                    {
                        // uniform sample on the hemisphere
                        float x_1 = Global.GetRandomFloat(), x_2 = Global.GetRandomFloat();
                        float z = MathF.Abs(1.0f - 2.0f * x_1);
                        float r = MathF.Sqrt(1.0f - z * z), phi = 2 * MathF.PI * x_2;
                        Vector3f localRay = new(r * MathF.Cos(phi), r * MathF.Sin(phi), z);
                        return toWorld(localRay, N);
                    }
            }

            return Vector3f.Zero;
        }

        // given a ray, calculate the PdF of this ray
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Pdf(Vector3f wi, Vector3f wo, Vector3f N)
        {
            switch (Type)
            {
                case MaterialType.DIFFUSE:
                    {
                        // uniform sample probability 1 / (2 * PI)
                        if (Vector3f.Dot(wo, N) > 0.0f)
                            return 0.5f / MathF.PI;
                        else
                            return 0.0f;
                    }
            }
            return float.NaN;
        }

        // given a ray, calculate the contribution of this ray
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3f Eval(Vector3f wi, Vector3f wo, Vector3f N)
        {
            switch (Type)
            {
                case MaterialType.DIFFUSE:
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
            return Vector3f.Zero;
        }

    }
}
