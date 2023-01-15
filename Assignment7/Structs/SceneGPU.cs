using Assignment7;
using ILGPU;
using ILGPU.Algorithms.Random;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vector3f = System.Numerics.Vector3;

namespace Structs
{
    public struct SceneContext
    {
        public Vector3f BackgroundColor { get; init; } = new(0.235294f, 0.67451f, 0.843137f);
        public float RussianRoulette { get; init; } = 0.8f;
        public int Width { get; } = 1280;
        public int Height { get; } = 960;
        public double Fov { get; init; } = 40;

        public SceneContext(int w, int h)
        {
            Width = w;
            Height = h;
        }

        public static Vector3f CastRay(in Ray ray, in BVHAccel bvh, in SceneContext scene, ref Structs.Random rng)
        {
            // TODO Implement Path Tracing Algorithm here

            // 从像素发出的光线与物体的交点
            Intersection inter = Intersect(ray, bvh);
            if (!inter.Happened)
                return scene.BackgroundColor;

            // 直接光照
            var Ld = directLight(inter, ray.Direction, bvh, ref rng);

            // 间接光照
            var Fd = Vector3f.One; // 间接光照系数
            var wo = Vector3f.Normalize(ray.Direction); //光线入射角
            var Li = Vector3f.Zero; // 间接光照
            // 按照概率计算间接光照
            while (rng.NextFloat() < scene.RussianRoulette)
            {
                var p = inter.Coords; // 待计算间接光照的位置
                var n = Vector3f.Normalize(inter.Normal); // 待计算间接光照的位置法线

                Debug.Assert(inter.Material != -1);
                var wi = Vector3f.Normalize(bvh.Materials[inter.Material].Sample(wo, n, ref rng)); //随机出射角
                var ray_i = new Ray(p, wi); // 随机随机生成出射光线（间接光照光线）

                var hitInter = Intersect(ray_i, bvh); // 出射光线与其他物体交点（间接光照发光点）
                if (!hitInter.Happened)
                    break; // 计算间接光照，出射光线未命中物体，结束

                Debug.Assert(hitInter.Material != -1);
                if (bvh.Materials[hitInter.Material].HasEmission())
                    break; // 计算间接光照，出射光线命中光源，结束

                // 间接光线的直接光照部分
                var Li_d = directLight(hitInter, ray_i.Direction, bvh, ref rng);

                var f_r = bvh.Materials[inter.Material].Eval(wo, wi, n); // 材质光照系数
                var pdf = bvh.Materials[inter.Material].Pdf(wo, wi, n); // 密度函数
                //更新间接光照系数
                Fd *= f_r * Vector3f.Dot(wi, n) / pdf / scene.RussianRoulette;

                Li += Li_d * Fd;

                // 下个循环计算：间接光线的间接光照部分
                inter = hitInter;
                wo = wi;
            }

            return Ld + Li;
        }

        private static Vector3f directLight(in Intersection inter, in Vector3f wo, in BVHAccel bvh, ref Random rng)
        {
            var directLight = Vector3f.Zero;

            Debug.Assert(inter.Material != -1);
            if (bvh.Materials[inter.Material].HasEmission())
            {
                return bvh.Materials[inter.Material].Emission;
            }

            // 对场景光源进行采样，interLight光源位置；pdfLight光源密度函数
            SampleLight(out var interLight, out var pdfLight, bvh, ref rng);

            var p = inter.Coords;
            var n = Vector3f.Normalize(inter.Normal);

            var pLight = interLight.Coords;
            var nLight = interLight.Normal;            
            var emitLight = interLight.Emit;

            var disP2L = (pLight - p);
            var dirP2L = Vector3f.Normalize(disP2L);

            // Shoot a ray from p to x(Light)            
            Ray rayP2L = new(p, dirP2L);
            Intersection interP2L = Intersect(rayP2L, bvh); ;
            // 如果关于没被遮挡
            //if ((interP2L.coords - pLight).LengthSquared() < Renderer.EPSILON)
            if (interP2L.Distance - disP2L.Length() > -Const.EPSILON)
            {
                directLight = emitLight * bvh.Materials[inter.Material].Eval(wo, rayP2L.Direction, n)
                    * Vector3f.Dot(dirP2L, n)
                    * Vector3f.Dot(-dirP2L, nLight)
                    / disP2L.LengthSquared()
                    / pdfLight;
            }

            return directLight;
        }

        public static Intersection Intersect(in Ray ray, in BVHAccel bvh)
        {
            return bvh.Intersect(ray);
        }

        public static void SampleLight(out Intersection pos, out float pdf, in BVHAccel bvh, ref Random rng)
        {
            var nodes = bvh.Nodes;
            var meshIdxes = bvh.MeshIndexes;
            pos = new(); pdf = 1f;

            float emit_area_sum = 0;
            for (int i = 0; i < meshIdxes.IntLength; ++i)
            {
                var meshIdx = meshIdxes[i];
                var idxMaterial = nodes[meshIdx].Material;
                if (idxMaterial != -1 && bvh.Materials[idxMaterial].HasEmission())
                {
                    emit_area_sum += nodes[meshIdx].Area;
                }
            }
            float p = rng.NextFloat() * emit_area_sum;
            emit_area_sum = 0;
            for (int i = 0; i < meshIdxes.IntLength; ++i)
            {
                var meshIdx = meshIdxes[i];
                var idxMaterial = nodes[meshIdx].Material;
                if (idxMaterial != -1 && bvh.Materials[idxMaterial].HasEmission())
                {
                    emit_area_sum += nodes[meshIdx].Area;
                    if (p <= emit_area_sum)
                    {
                        bvh.Sample(ref rng, meshIdx, out pos, out pdf);
                        if (nodes[meshIdx].Material != -1)
                            pos.Emit = bvh.Materials[nodes[meshIdx].Material].Emission;
                        break;
                    }
                }
            }
        }
    }
}
