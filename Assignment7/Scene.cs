using Assignment6;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vector3f = System.Numerics.Vector3;
using Vector2f = System.Numerics.Vector2;
using System.Reflection;
using System.Security.Permissions;
using System.Diagnostics.Metrics;
using Structs;

namespace Assignment7
{
    public class Scene
    {
        // setting up options
        public int Width { get; } = 1280;
        public int Height { get; } = 960;
        public double Fov { get; init; } = 40;
        public Vector3f BackgroundColor { get; init; } = new(0.235294f, 0.67451f, 0.843137f);
        public int MaxDepth { get; init; } = 1;

        public float RussianRoulette { get; init; } = 0.8f;

        /// <summary>
        /// 光线反射次数期望
        /// </summary>
        public float ExpectedTime
        {
            get
            {
                return 1 / (1 - RussianRoulette);
            }
            init
            {
                if (value < 1) throw new ArgumentOutOfRangeException("value");
                RussianRoulette = (value - 1) / value;
            }
        }

        // creating the scene (adding objects and lights)
        List<GeometryObject> objects = new();
        public GeometryObject[] Objects { get => objects.ToArray(); }

        List<Light> lights = new();

        public Scene(int w, int h)
        {
            Width = w;
            Height = h;
        }

        public void Add(GeometryObject @object) { objects.Add(@object); }
        public void Add(Light light) { lights.Add(light); }

        // Compute reflection direction
        public Vector3f reflect(Vector3f I, Vector3f N)
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
        public Vector3f refract(Vector3f I, Vector3f N, float ior)
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
        public void fresnel(Vector3f I, Vector3f N, float ior, out float kr)
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

        BVHAccel? bvh;
        public void BuildBVH()
        {
            Debug.Write(" - Generating BVH...\n\n");
            bvh = new BVHAccel(objects, 1, BVHAccel.SplitMethod.BVH);
        }

        public Intersection Intersect(in Ray ray)
        {
            if (bvh == null) throw new NullReferenceException();

            return bvh.Intersect(ray);
        }

        public void SampleLight(out Intersection pos, out float pdf)
        {
            pos = new(); pdf = 1f;

            float emit_area_sum = 0;
            for (int k = 0; k < objects.Count; ++k)
            {
                if (objects[k].HasEmit())
                {
                    emit_area_sum += objects[k].Area;
                }
            }
            float p = Global.GetRandomFloat() * emit_area_sum;
            emit_area_sum = 0;
            for (int k = 0; k < objects.Count; ++k)
            {
                if (objects[k].HasEmit())
                {
                    emit_area_sum += objects[k].Area;
                    if (p <= emit_area_sum)
                    {
                        objects[k].Sample(out pos, out pdf);
                        break;
                    }
                }
            }
        }

        public bool Trace(Ray ray, List<GeometryObject> objects,
            out float tNear, out int index, out GeometryObject? hitObject)
        {
            tNear = float.MaxValue;
            index = -1;
            hitObject = null;

            for (int k = 0; k < objects.Count; ++k)
            {
                if (objects[k].Intersect(ray, out var tNearK, out var indexK) && tNearK < tNear)
                {
                    hitObject = objects[k];
                    tNear = tNearK;
                    index = indexK;
                }
            }

            return (hitObject != null);
        }

        // Implementation of Path Tracing
        public virtual Vector3f CastRay(in Ray ray, int depth)
        {
            // TODO Implement Path Tracing Algorithm here

            // 从像素发出的光线与物体的交点
            Intersection inter = Intersect(ray);
            if (!inter.Happened)
                return BackgroundColor;

            // 直接光照
            var Ld = directLight(inter, ray.direction, depth);

            // 间接光照
            var Fd = Vector3f.One; // 间接光照系数
            var wo = Vector3f.Normalize(ray.direction); //光线入射角
            var Li = Vector3f.Zero; // 间接光照
            // 按照概率计算间接光照
            while (Global.GetRandomFloat() < RussianRoulette)
            {
                depth += 1;

                var p = inter.Coords; // 待计算间接光照的位置
                var n = Vector3f.Normalize(inter.Normal); // 待计算间接光照的位置法线

                Debug.Assert(inter.Material != null);
                var wi = Vector3f.Normalize(inter.Material.Sample(wo, n)); //随机出射角
                var ray_i = new Ray(p, wi); // 生成出射光线（间接光照光线）

                var hitInter = Intersect(ray_i); // 出射光线与其他物体交点（间接光照发光点）
                if (!hitInter.Happened)
                    break; // 计算间接光照，出射光线未命中物体，结束

                Debug.Assert(hitInter.Material != null);

                if (hitInter.Material.HasEmission())
                {
                    if (inter.Material.Type == MaterialType.Mirror)
                    {
                        Li += hitInter.Material.Emission * Fd;
                    }

                    break; // 计算间接光照，出射光线命中光源，结束
                }

                // 间接光线的直接光照部分
                var Li_d = directLight(hitInter, ray_i.direction, depth);

                if (inter.Material.Type != MaterialType.Mirror)
                {
                    var f_r = inter.Material.Eval(wo, wi, n); // 材质光照系数
                    var pdf = inter.Material.Pdf(wo, wi, n); // 密度函数
                    //更新间接光照系数
                    Fd *= f_r * Vector3f.Dot(wi, n) / pdf / RussianRoulette;
                }

                Li += Li_d * Fd;

                // 下个循环计算：间接光线的间接光照部分
                inter = hitInter;
                wo = wi;
            }

            return Ld + Li;
        }

        private Vector3f directLight(in Intersection inter, in Vector3f wo, int depth)
        {
            var directLight = Vector3f.Zero;

            Debug.Assert(inter.Material != null);
            if (inter.Material.HasEmission())
            {
                return inter.Material.Emission;
            }

            if (inter.Material.Type == MaterialType.Mirror) return new(0);

            // 对场景光源进行采样，interLight光源位置；pdfLight光源密度函数
            SampleLight(out var interLight, out var pdfLight);

            var p = inter.Coords;
            var n = Vector3f.Normalize(inter.Normal);

            var pLight = interLight.Coords;
            var nLight = interLight.Normal;
            var emitLight = interLight.Emit;

            var disP2L = (pLight - p);
            var dirP2L = Vector3f.Normalize(disP2L);

            // Shoot a ray from p to x(Light)            
            Ray rayP2L = new(p, dirP2L);
            Intersection interP2L = Intersect(rayP2L); ;
            // 如果关于没被遮挡
            //if ((interP2L.coords - pLight).LengthSquared() < Renderer.EPSILON)
            if (interP2L.Distance - disP2L.Length() > -Renderer.EPSILON)
            {
                directLight = emitLight * inter.Material.Eval(wo, rayP2L.direction, n)
                    * Vector3f.Dot(dirP2L, n)
                    * Vector3f.Dot(-dirP2L, nLight)
                    / disP2L.LengthSquared()
                    / pdfLight;
            }

            return directLight;
        }

        public virtual Vector3f CastRayRecursive(Ray ray, int depth)
        {
            // TODO Implement Path Tracing Algorithm here

            // 从像素发出的光线与物体的交点
            Intersection inter = Intersect(ray);
            if (!inter.Happened)
                return BackgroundColor;

            // 直接光照
            var Ld = directLight(inter, ray.direction, depth);

            // 间接光照
            var wo = Vector3f.Normalize(ray.direction); //光线入射角
            var Li = indirectLightRecursive(inter, wo, depth);

            return Ld + Li;
        }

        private Vector3f indirectLightRecursive(in Intersection inter, in Vector3f wo, int depth)
        {
            // 按照概率计算间接光照
            if (Global.GetRandomFloat() > RussianRoulette)
                return Vector3f.Zero;

            var p = inter.Coords; // 待计算间接光照的位置
            var n = Vector3f.Normalize(inter.Normal); // 待计算间接光照的位置法线

            Debug.Assert(inter.Material != null);
            var wi = Vector3f.Normalize(inter.Material.Sample(wo, n)); //随机出射角
            var ray_i = new Ray(p, wi); // 随机随机生成出射光线（间接光照光线）

            var hitInter = Intersect(ray_i); // 出射光线与其他物体交点（间接光照发光点）
            if (!hitInter.Happened)
                return Vector3f.Zero; // 计算间接光照，出射光线未命中物体，结束

            Debug.Assert(hitInter.Material != null);
            if (hitInter.Material.HasEmission())
                return Vector3f.Zero; // 计算间接光照，出射光线命中光源，结束

            // 间接光线的直接光照部分
            var Ld = directLight(hitInter, ray_i.direction, depth + 1);
            var Li = indirectLightRecursive(hitInter, wi, depth + 1);

            var f_r = inter.Material.Eval(wo, wi, n); // 材质光照系数
            var pdf = inter.Material.Pdf(wo, wi, n); // 密度函数
            var factor = f_r * Vector3f.Dot(wi, n) / pdf / RussianRoulette; // 光照系数

            return (Ld + Li) * factor;
        }

    }
}
