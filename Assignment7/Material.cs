using Assignment6;
using Structs;
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
    public enum MaterialType { DIFFUSE, Microfacet, Mirror };
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
        public Vector3f Ks { get; init; }
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
            if (Emission.Length() > Const.EPSILON) return true;
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
                case MaterialType.Mirror:
                    {
                        return reflect(wi, N);
                    }
                case MaterialType.DIFFUSE:
                case MaterialType.Microfacet:
                    {
                        // uniform sample on the hemisphere在半球上均匀采样
                        float x_1 = Global.GetRandomFloat(), x_2 = Global.GetRandomFloat();
                        //z∈[0,1]，是随机半球方向的z轴向量
                        float z = MathF.Abs(1.0f - 2.0f * x_1);
                        //r是半球半径随机向量以法线为旋转轴的半径
                        //phi是r沿法线旋转轴的旋转角度
                        float r = MathF.Sqrt(1.0f - z * z), phi = 2 * MathF.PI * x_2;//phi∈[0,2*pi]
                        Vector3f localRay = new(r * MathF.Cos(phi), r * MathF.Sin(phi), z);//半球面上随机的光线的弹射方向
                        return toWorld(localRay, N);//转换到世界坐标
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
                case MaterialType.Mirror:
                    {
                        if (wo == reflect(wi, N))
                            return 1f;
                        return 0f;
                    }
                case MaterialType.DIFFUSE:
                case MaterialType.Microfacet:
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
        public Vector3f Eval(in Vector3f wi, in Vector3f wo, in Vector3f N)
        {
            switch (Type)
            {
                case MaterialType.Mirror:
                    {
                        if (wo == reflect(wi, N))
                            return Vector3f.One;
                        return Vector3f.Zero;
                    }
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
                            return Vector3f.Zero;
                    }
                case MaterialType.Microfacet: //微表面材质的BRDF
                    {
                        // Disney PBR 方案
                        float cosalpha = Vector3f.Dot(N, wo);
                        if (cosalpha > 0.0f)
                        {
                            float roughness = 0.40f;

                            Vector3f V = -wi;
                            Vector3f L = wo;
                            Vector3f H = Vector3f.Normalize(V + L);

                            // 计算 distribution of normals: D
                            float D = DistributionGGX(N, H, roughness);

                            // 计算 shadowing masking term: G
                            float G = GeometrySmith(N, V, L, roughness);

                            // 计算 fresnel 系数: F
                            float F;
                            float etat = 1.85f;
                            fresnel(wi, N, etat, out F);

                            Vector3f nominator = new(D * G * F);
                            float denominator = 4 * MathF.Max(Vector3f.Dot(N, V), 0.0f) * MathF.Max(Vector3f.Dot(N, L), 0.0f);
                            Vector3f specular = nominator / MathF.Max(denominator, 0.001f);

                            // 能量守恒
                            float ks_ = F;//反射比率
                            float kd_ = 1.0f - ks_;//折射比率

                            Vector3f diffuse = new(1.0f / MathF.PI);

                            // 因为在 specular 项里已经考虑了反射部分的比例：F。所以反射部分不需要再乘以 ks_ 
                            //Ks为镜面反射项，Kd为漫反射项。
                            return Ks * specular + kd_ * Kd * diffuse;
                        }
                        else
                            return Vector3f.Zero;
                    }
            }
            return Vector3f.Zero;
        }
        /// <summary>
        /// 法线分布函数 D
        /// 微平面的法线分布函数D(m)描述了微观表面上的表面法线m的统计分布。
        /// 业界较为主流的法线分布函数是GGX（Trowbridge-Reitz），因为具有更好的高光长尾。
        /// D(n,h,a) = a^2 / ((n dot d)^2 (a^2 - 1) + 1)^2
        /// α为粗糙度∈ [ 0 , 1 ] \in[0,1]∈[0,1]。
        /// n为宏观平面法线
        /// h为微观平面法线，即V和I的中间向量。
        /// </summary>
        /// <param name="N"></param>
        /// <param name="H"></param>
        /// <param name="roughness"></param>
        /// <returns></returns>
        float DistributionGGX(Vector3f N, Vector3f H, float roughness)
        {
            float a = roughness * roughness;
            float a2 = a * a;
            float NdotH = MathF.Max(Vector3f.Dot(N, H), 0.0f);
            float NdotH2 = NdotH * NdotH;

            float nom = a2;
            float denom = (NdotH2 * (a2 - 1.0f) + 1.0f);
            denom = MathF.PI * denom * denom;

            return nom / MathF.Max(denom, 0.0000001f); // prevent divide by zero for roughness=0.0 and NdotH=1.0
        }

        float GeometrySchlickGGX(float NdotV, float k)
        {
            float nom = NdotV;
            float denom = NdotV * (1.0f - k) + k;

            return nom / denom;
        }

        /// <summary>
        /// 几何函数 G
        /// 目前较为常用的是其中最为简单的形式，分离遮蔽阴影（Separable Masking and Shadowing Function）。
        /// 该形式将几何项G分为两个独立的部分：光线方向（light） 和 视线方向（view），并对两者用相同的分布函数来描述。
        /// 其中UE4的方案是Schlick-GGX，即基于Schlick近似
        /// </summary>
        /// <param name="N">为宏观表面法线</param>
        /// <param name="V">为光线反射方向（可理解为视口的方向）</param>
        /// <param name="L">为光线进入入的反方向（可理解为光源的方向）</param>
        /// <param name="roughness">为粗糙度 0完全光滑，光线通过率为1.</param>
        /// <returns></returns>
        float GeometrySmith(Vector3f N, Vector3f V, Vector3f L, float roughness)
        {
            float r = (roughness + 1.0f);
            float k = (r * r) / 8.0f;
            float NdotV = Math.Max(Vector3f.Dot(N, V), 0.0f);
            float NdotL = Math.Max(Vector3f.Dot(N, L), 0.0f);
            float ggx2 = GeometrySchlickGGX(NdotV, k);
            float ggx1 = GeometrySchlickGGX(NdotL, k);

            return ggx1 * ggx2;
        }
    }
}
