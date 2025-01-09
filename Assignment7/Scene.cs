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
using Random = System.Random;
using ILGPU;

namespace Assignment7
{
    public class Scene
    {
        // setting up options
        public int Width { get; } = 1280;
        public int Height { get; } = 960;
        public double Fov { get; init; } = 40;
        public Vector3f BackgroundColor { get; init; } = Vector3f.Zero;
        public int MaxDepth { get; init; } = 1;

        public float RussianRoulette { get; init; } = 0.8f;

        /// <summary>
        /// 光线反射次数期望
        /// </summary>
        public float ExpectedTime
        {
            get
            {
                return RussianRoulette / (1 - RussianRoulette);
            }
            init
            {
                if (value < 0) throw new ArgumentOutOfRangeException("value");
                RussianRoulette = value / (1 + value);
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

        public void SampleMirror(in Intersection pInter, out Intersection sInter, out float sPdf)
        {
            List<Triangle> triangles = [];

            sInter = new(); sPdf = 1f;

            float emit_area_sum = 0;
            for (int k = 0; k < objects.Count; ++k)
            {
                if (objects[k] is MeshTriangle mesh 
                    && mesh.Material.Type == MaterialType.Mirror)
                {
                    foreach (var t in mesh.Triangles)
                    {
                        if (Vector3f.Dot(t.Normal, -pInter.Normal) > Const.EPSILON)
                        {
                            triangles.Add(t);
                            emit_area_sum += t.Area;
                        }
                    }                    
                }
            }
            float fullArea = emit_area_sum;
            float p = Global.GetRandomFloat() * emit_area_sum;
            emit_area_sum = 0;
            foreach (var t in triangles)
            {
                emit_area_sum += t.Area;
                if (p <= emit_area_sum)
                {
                    t.Sample(out sInter, out sPdf);
                    sPdf *= t.Area / fullArea;
                    break;
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
        public Vector3f CastRay(Ray ray, int depth = 0)
        {
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
                var wi = Vector3f.Normalize(inter.Material.Sample(wo, n)); //(随机)生成出射角
                var ray_i = new Ray(p, wi); // 生成出射光线（间接光照光线）

                var hitInter = Intersect(ray_i); // 出射光线与其他物体交点（间接光照发光点）
                //ReplaceWithMirror(ref hitInter, ref ray_i);
                if (!hitInter.Happened)
                    break; // 计算间接光照，出射光线未命中物体，结束

                if (hitInter.Material.HasEmission()
                    && inter.Material.Type != MaterialType.Mirror)
                {
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

            // 对场景光源进行采样，interLight光源位置；pdfLight光源密度函数
            SampleLight(out var interLight, out var pdfLight);

            var p = inter.Coords;
            var n = Vector3f.Normalize(inter.Normal);

            var pLight = interLight.Coords;
            var nLight = interLight.Normal;
            var emitLight = interLight.Emit;

            var distanceP2L = (pLight - p);
            var dirP2L = Vector3f.Normalize(distanceP2L);

            // Shoot a ray from p to x(Light)            
            Ray rayP2L = new(p, dirP2L);
            Intersection interP2L = Intersect(rayP2L); ;
            // 如果关于没被遮挡
            if ((interP2L.Coords - pLight).LengthSquared() < Const.EPSILON)
            //if (interP2L.Distance - distanceP2L.Length() > -Const.EPSILON)
            {
                //GAMES101_Lecture_16.pdf P41
                //Lo = ∫A Li(x,ωi) fr(x,ωi,ωo) cosθ cosθ' /(pLight-p)^2 dA

                directLight = emitLight * inter.Material.Eval(wo, rayP2L.direction, n)
                    * Vector3f.Dot(dirP2L, n) // cosθ p点法线与光源的夹角
                    * Vector3f.Dot(-dirP2L, nLight) // cosθ' 光源法线与光线的夹角
                    / distanceP2L.LengthSquared() // 光源到p点距离的平方
                    / pdfLight; // 1/dA
            }

            return directLight;
        }

        public virtual Vector3f CastRayRecursive(Ray ray, int depth = 0)
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

        #region CastRayNew

        public Vector3f CastRay2(Ray ray, bool includeEmission = true)
        {
            // 从像素发出的光线与物体的交点
            Intersection inter = Intersect(ray);
            if (!inter.Happened)
                return Vector3f.Zero;

            // 自发光
            var Le = Vector3f.Zero;
            if(includeEmission)
                Le = Emission(inter, ray.direction);

            // 直接光照
            var Ld = DirectLight2(inter, ray.direction);

            // 间接光照
            var Li = Vector3f.Zero;

            // 按照概率计算间接光照
            if (Random.Shared.NextSingle() < RussianRoulette)
            {
                //随机出射角
                var wo = Vector3f.Normalize(inter.Material!.Sample(ray.direction, inter.Normal));
                //生成出射光线（间接光照光线）
                var ray_i = new Ray(inter.Coords, wo);
                var pdf = inter.Material.Pdf(ray.direction, wo, inter.Normal);

                if (pdf > 0)
                {
                    Li = CastRay2(ray_i, false)
                                * Vector3f.Dot(wo, inter.Normal)
                                * inter.Material.Eval(ray.direction, wo, inter.Normal)
                                / pdf
                                / RussianRoulette;
                }

            }

            return Le + Ld + Li;
        }
        private Vector3f Emission(in Intersection inter, in Vector3f iw)
        {
            Debug.Assert(inter.Material != null);
            return inter.Material.Emission;
        }

        private Vector3f DirectLight2(in Intersection inter, in Vector3f iw)
        {
            Debug.Assert(inter.Material != null);

            // 镜面材质
            if (inter.Material.Type == MaterialType.Mirror)
            {
                ////计算反射光线
                //var pMirror = inter.Coords; // 光照的位置
                //var nMirror = inter.Normal; // 光照的位置法线
                //var reflect = Vector3f.Reflect(iw, nMirror); // 生成反射角
                ////判断反射光线是否与光源相交
                //var rayMirror = new Ray(pMirror, reflect); // 生成出射光线（间接光照光线）
                //var hitInter = Intersect(rayMirror);
                //if (hitInter.Happened)
                //{
                //    // 如果与光源相交，返回光源的颜色
                //    if (hitInter.Material.HasEmission())
                //    {
                //        return hitInter.Material.Emission;
                //    }
                //    // 如果没有相交，返回零向量
                //    return Vector3f.Zero;
                //}
                ////如果不相交，返回零向量
                return Vector3f.Zero;
            }

            // 非镜面材质
            var Ll = DoSampleLight(inter, iw);

            var Lm = DoSampleMirror(inter);

            return Ll + Lm;
        }

        private Vector3f DoSampleMirror(Intersection inter)
        {
            // 对场景中的镜面进行采样，interMirror镜面位置；pdfMirror镜面密度函数
            SampleMirror(inter, out var interMirror, out var pdfMirror);

            // 交点法线nP
            var nP = inter.Normal;
            // 镜面采样点法线mL
            var nM = interMirror.Normal;

            var pMirror = interMirror.Coords;
            var p = inter.Coords;
            var distanceP2M = (pMirror - p);
            // 交点到镜面的方向的单位向量 dirP2L
            var dirP2M = Vector3f.Normalize(distanceP2M);

            // 计算交点法线与光源方向的点积
            var cosTheta = Vector3f.Dot(nP, dirP2M);
            if (cosTheta <= Const.EPSILON)
            {
                // 如果点积小于0，说明光源在交点的背面，返回零向量
                return Vector3f.Zero;
            }
            

            // 判断采样点是否被遮挡
            if (IsShadow(inter, interMirror))
            {
                return Vector3f.Zero;
            }

            var reflect = Vector3f.Reflect(dirP2M, nM); // 生成反射角
            var rayReflect = new Ray(pMirror, reflect); // 生成出射光线（间接光照光线）
                                                        //反射光线与其他物体相交
                                                        //是否命中光源
            var hitInter = Intersect(rayReflect);
            // 如果与光源相交，返回光源的颜色
            if (hitInter.Happened && hitInter.Material!.HasEmission())
            {
                var pLight = hitInter.Coords;
                var nLight = hitInter.Normal;

                // 计算光源法线与光源方向的点积
                var cosThetaL = Vector3f.Dot(nLight, -rayReflect.direction);
                if (cosThetaL <= Const.EPSILON)
                {
                    // 如果点积小于0，说明光源在交点的背面，返回零向量
                    return Vector3f.Zero;
                }
                var distanceM2L = (pLight - pMirror);
                var disranceP2L = distanceP2M.Length() + distanceM2L.Length();

                return hitInter.Material.Emission * cosTheta * cosThetaL / (disranceP2L * disranceP2L) / pdfMirror;
            }
            return Vector3f.Zero;
        }

        private Vector3f DoSampleLight(Intersection inter, Vector3f iw)
        {
            // 对场景光源进行采样，interLight光源位置；pdfLight光源密度函数
            SampleLight(out var interLight, out var pdfLight);

            // 交点法线nP
            var nP = inter.Normal;
            // 光源采样点法线nL
            var nL = interLight.Normal;

            var pLight = interLight.Coords;
            var p = inter.Coords;
            var distanceP2L = (pLight - p);
            // 交点到光源的方向的单位向量 dirP2L
            var dirP2L = Vector3f.Normalize(distanceP2L);

            // 计算交点法线与光源方向的点积
            var cosTheta = Vector3f.Dot(nP, dirP2L);
            if (cosTheta <= 0)
            {
                // 如果点积小于0，说明光源在交点的背面，返回零向量
                return Vector3f.Zero;
            }
            // 计算光源法线与光源方向的点积
            var cosThetaL = Vector3f.Dot(nL, -dirP2L);
            if (cosThetaL <= 0)
            {
                // 如果点积小于0，说明光源在交点的背面，返回零向量
                return Vector3f.Zero;
            }

            // 判断采样点是否被遮挡
            if (IsShadow(inter, interLight))
            {
                return Vector3f.Zero;
            }
            // 如果没有被遮挡，计算直接光照

            var emitLight = interLight.Emit;
            var BRDF = inter.Material!.Eval(iw, dirP2L, nP);

            // 光照强度 = 光源强度 * BRDF * dot(nP,dirP2L) * dot(nL,-dirP2L) / 交点到光源距离的平方 / pdfLight
            // 返回直接光照
            return emitLight * BRDF * cosTheta * cosThetaL / distanceP2L.LengthSquared() / pdfLight;
        }

        private bool IsShadow(Intersection inter, Intersection interLight)
        {
            // 创建从交点到光源的光线
            var p = inter.Coords;
            var pLight = interLight.Coords;
            var dir = Vector3f.Normalize(pLight - p);
            var ray = new Ray(p, dir);
            // 判断光线是否与其他物体相交
            var hitInter = Intersect(ray);
            // 相交距离小于到光源的距离，返回true
            return (hitInter.Coords - pLight).LengthSquared() > Const.EPSILON;
        }

        #endregion CastRayNew
    }
}
