using ILGPU;
using ILGPU.Runtime;
using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Vector2f = System.Numerics.Vector2;
using Vector3f = System.Numerics.Vector3;
using System.Linq;
using Structs;
using ILGPU.Algorithms;
using System.Diagnostics;
using System.Threading;
using ILGPU.Runtime.CPU;

namespace Assignment7
{
    public struct hit_payload
    {
        public float tNear;
        public uint index;
        public Vector2f uv;
        public GeometryObject? hit_obj;
    };
    public class Renderer
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float deg2rad(float deg) => deg * MathF.PI / 180.0f;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static (float subX, float subY) CalcSubXY(int x, int y, int subIndex, int ssLevel)
        {
            float levelInc = 1f / ssLevel;
            float baseValue = levelInc * 0.5f;

            return (x + baseValue + levelInc * (subIndex % ssLevel), y + baseValue + levelInc * (subIndex / ssLevel));
        }

        static (float subX, float subY) CalcSubXY(int x, int y)
        {
            return (x + Global.GetRandomFloat(), y + Global.GetRandomFloat());
        }

        public byte[] Render(Scene scene, int ssLevel = 1)
        {
            if (ssLevel < 1)
                throw new ArgumentOutOfRangeException("ssLevel", "超采样等级范围：1 <= ssLevel");

            Vector3f[] framebuffer = new Vector3f[scene.Width * scene.Height * ssLevel * ssLevel];

            float scale = MathF.Tan(deg2rad((float)(scene.Fov) * 0.5f));
            float imageAspectRatio = scene.Width / (float)scene.Height;

            Vector3f eye_pos = new(278, 273, -800);
            for (int j = 0, index = 0; j < scene.Height; ++j)
            {
                for (int i = 0; i < scene.Width; ++i)
                {
                    for (int subIndex = 0; subIndex < ssLevel * ssLevel; subIndex++)
                    {
                        var (x, y) = CalcSubXY(i, j, subIndex, ssLevel);

                        // generate primary ray direction
                        x = x * 2f / scene.Width - 1;
                        x *= scale * imageAspectRatio;
                        y = y * -2f / scene.Height + 1;
                        y *= scale;
                        // TODO: Find the x and y positions of the current pixel to get the direction
                        // vector that passes through it.
                        // Also, don't forget to multiply both of them with the variable *scale*, and
                        // x (horizontal) variable with the *imageAspectRatio*            

                        // Don't forget to normalize this direction!
                        Vector3f dir = Vector3f.Normalize(new(-x, y, 1));
                        framebuffer[index] = scene.CastRay(new(eye_pos, dir), 0);
                        index += 1;
                    }
                }
                Global.UpdateProgress(j / (float)scene.Height);
            }

            byte[] colorDate = new byte[scene.Width * scene.Height * 3];
            for (int index = 0; index < scene.Width * scene.Height; index++)
            {
                var _v = framebuffer[(index * ssLevel * ssLevel)..(index * ssLevel * ssLevel + ssLevel * ssLevel)];
                Vector3f v = Vector3f.Zero;
                for (int i = 0; i < _v.Length; i++)
                {
                    v += _v[i];
                }
                v /= _v.Length;

                colorDate[index * 3 + 2] = (byte)(255 * Math.Pow(Math.Clamp(v.X, 0, 1), 0.6f)); //R
                colorDate[index * 3 + 1] = (byte)(255 * Math.Pow(Math.Clamp(v.Y, 0, 1), 0.6f)); //G
                colorDate[index * 3 + 0] = (byte)(255 * Math.Pow(Math.Clamp(v.Z, 0, 1), 0.6f)); //B
            }

            Global.UpdateProgress(1);
            return colorDate;
        }

        public byte[] RenderSingleStepByFrame(Scene scene, Vector3f[] framebuffer, int nStep)
        {
            Debug.Assert(framebuffer.Length == scene.Width * scene.Height);

            float scale = MathF.Tan(deg2rad((float)scene.Fov * 0.5f));
            float imageAspectRatio = scene.Width / (float)scene.Height;

            // Use this variable as the eye position to start your rays.
            Vector3f eye_pos = new(278, 273, -800);

            var part = Partitioner.Create(0, scene.Width * scene.Height, scene.Width * scene.Height / Environment.ProcessorCount);
            var plr = Parallel.ForEach(part, range =>
            {
                for (int m = range.Item1; m < range.Item2; m++)
                {
                    int i = m % scene.Width;
                    int j = m / scene.Width;

                    int subIndex = nStep;

                    var (x, y) = CalcSubXY(i, j);

                    // generate primary ray direction
                    x = x * 2f / scene.Width - 1;
                    x *= scale * imageAspectRatio;
                    y = y * -2f / scene.Height + 1;
                    y *= scale;
                    // TODO: Find the x and y positions of the current pixel to get the direction
                    // vector that passes through it.
                    // Also, don't forget to multiply both of them with the variable *scale*, and
                    // x (horizontal) variable with the *imageAspectRatio*            

                    // Don't forget to normalize this direction!
                    Vector3f dir = Vector3f.Normalize(new(-x, y, 1));

                    var _v = scene.CastRay(new(eye_pos, dir), 0);
                    framebuffer[m] += Vector3f.Clamp(_v, Vector3f.Zero, Vector3f.One);
                }
            });

            byte[] colorDate = new byte[scene.Width * scene.Height * 3];
            Parallel.ForEach(part, range =>
            {

                for (int index = range.Item1; index < range.Item2; index++)
                {
                    var _v = framebuffer[index];
                    var v = _v / (nStep + 1);

                    colorDate[index * 3 + 2] = (byte)(255 * MathF.Pow(v.X, 0.6f)); //R
                    colorDate[index * 3 + 1] = (byte)(255 * MathF.Pow(v.Y, 0.6f)); //G
                    colorDate[index * 3 + 0] = (byte)(255 * MathF.Pow(v.Z, 0.6f)); //B
                }
            });

            return colorDate;
        }

        public byte[] RenderParallel(Scene scene, int ssLevel = 1)
        {
            if (ssLevel < 1)
                throw new ArgumentOutOfRangeException("ssLevel", "超采样等级范围：1 <= ssLevel");

            //samples per pixel
            int spp = ssLevel * ssLevel;

            byte[] colorDate = new byte[scene.Width * scene.Height * 3];
            Vector3f[] rowbuffer = new Vector3f[scene.Width * spp];

            float scale = MathF.Tan(deg2rad((float)scene.Fov * 0.5f));
            float imageAspectRatio = scene.Width / (float)scene.Height;

            // Use this variable as the eye position to start your rays.
            Vector3f eye_pos = new(278, 273, -800);

            var part = Partitioner.Create(0, scene.Width, scene.Width / Environment.ProcessorCount);
            var partSS = Partitioner.Create(0, spp * scene.Width, spp * scene.Width / Environment.ProcessorCount);
            for (int j = 0; j < scene.Height; j++)
            {
                var plr = Parallel.ForEach(partSS, range =>
                {
                    for (int m = range.Item1; m < range.Item2; m++)
                    {
                        int i = m / spp;
                        int subIndex = m % spp;

                        //var (x, y) = CalcSubXY(i, j, subIndex, ssLevel);
                        var (x, y) = CalcSubXY(i, j);

                        // generate primary ray direction
                        x = x * 2f / scene.Width - 1;
                        x *= scale * imageAspectRatio;
                        y = y * -2f / scene.Height + 1;
                        y *= scale;
                        // TODO: Find the x and y positions of the current pixel to get the direction
                        // vector that passes through it.
                        // Also, don't forget to multiply both of them with the variable *scale*, and
                        // x (horizontal) variable with the *imageAspectRatio*            

                        // Don't forget to normalize this direction!
                        Vector3f dir = Vector3f.Normalize(new(-x, y, 1));

                        int index = i * spp + subIndex;
                        rowbuffer[index] = scene.CastRay(new(eye_pos, dir), 0);
                    }
                });

                Parallel.ForEach(part, range =>
                {

                    for (int index = range.Item1; index < range.Item2; index++)
                    {
                        var rowDate = colorDate.AsSpan(j * scene.Width * 3, scene.Width * 3);
                        var _v = rowbuffer.AsSpan((index * spp)..(index * spp + spp));
                        Vector3f v = Vector3f.Zero;
                        for (int i = 0; i < _v.Length; i++)
                        {
                            v += Vector3f.Clamp(_v[i], Vector3f.Zero, Vector3f.One);
                            //v += _v[i]; // TEST
                        }
                        v /= _v.Length;
                        //v = Vector3f.Clamp(v, Vector3f.Zero, Vector3f.One); // TEST

                        rowDate[index * 3 + 2] = (byte)(255 * MathF.Pow(v.X, 0.6f)); //R
                        rowDate[index * 3 + 1] = (byte)(255 * MathF.Pow(v.Y, 0.6f)); //G
                        rowDate[index * 3 + 0] = (byte)(255 * MathF.Pow(v.Z, 0.6f)); //B
                    }
                });

                Global.UpdateProgress((j + 1) / (float)scene.Height);
            }

            Global.UpdateProgress(1);
            return colorDate;
        }

        #region GPU
        public byte[] RenderGPU(Scene scene, int ssLevel = 1, bool preferCPU = false)
        {
            if (ssLevel < 1)
                throw new ArgumentOutOfRangeException("ssLevel", "超采样等级范围：1 <= ssLevel");

            byte[] colorDate = new byte[scene.Width * scene.Height * 3];
            Vector3f[] rowbuffer = new Vector3f[scene.Width * ssLevel * ssLevel];

            var part = Partitioner.Create(0, scene.Width, scene.Width / Environment.ProcessorCount);

            using Context context = Context.Create(builder => builder.Default().EnableAlgorithms());

            Device device = context.GetCPUDevice(0);
            if(!preferCPU)
                device = context.Devices.OrderByDescending(d => d.MaxNumThreads).First();

            using Accelerator accelerator = device.CreateAccelerator(context);

            // load / precompile the kernel
            var actionKernel = RenderKernel;
            var loadedKernel =
                accelerator.LoadAutoGroupedStreamKernel(actionKernel);

            using var random = accelerator.Allocate1D<Structs.Random>(CreateRandoms(scene.Width * ssLevel * ssLevel));
            using var output = accelerator.Allocate1D<Vector3f>(scene.Width * ssLevel * ssLevel);

            Structs.RendererParameter rendererParameter = new()
            {
                ssLevel = ssLevel,
                scene = new(scene.Width, scene.Height)
                {
                    BackgroundColor = scene.BackgroundColor,
                    RussianRoulette = scene.RussianRoulette,
                    Fov = scene.Fov,
                },
            };

            PreBVHAccel(accelerator, scene, out var bvh);

            for (int j = 0; j < scene.Height; j++)
            {
                rendererParameter.j = j;
                loadedKernel(scene.Width * ssLevel * ssLevel, rendererParameter, random.View, bvh, output.View);
                accelerator.Synchronize();
                output.CopyToCPU(rowbuffer);

                Parallel.ForEach(part, range =>
                {
                    for (int index = range.Item1; index < range.Item2; index++)
                    {
                        var rowDate = colorDate.AsSpan(j * scene.Width * 3, scene.Width * 3);
                        var _v = rowbuffer.AsSpan((index * ssLevel * ssLevel)..(index * ssLevel * ssLevel + ssLevel * ssLevel));
                        Vector3f v = Vector3f.Zero;
                        for (int i = 0; i < _v.Length; i++)
                        {
                            v += Vector3f.Clamp(_v[i], Vector3f.Zero, Vector3f.One);
                            //v += _v[i]; // TEST
                        }
                        v /= _v.Length;
                        //v = Vector3f.Clamp(v, Vector3f.Zero, Vector3f.One); // TEST

                        rowDate[index * 3 + 2] = (byte)(255 * MathF.Pow(v.X, 0.6f)); //R
                        rowDate[index * 3 + 1] = (byte)(255 * MathF.Pow(v.Y, 0.6f)); //G
                        rowDate[index * 3 + 0] = (byte)(255 * MathF.Pow(v.Z, 0.6f)); //B
                    }
                });

                Global.UpdateProgress((j + 1) / (float)scene.Height);
            }

            Global.UpdateProgress(1);
            return colorDate;
        }

        private void RenderKernel(Index1D m, Structs.RendererParameter parameter, ArrayView<Structs.Random> rngView, Structs.BVHAccel bvh, ArrayView<Vector3f> output)
        {
            var (scene, ssLevel, eye_pos, j) = (parameter.scene, parameter.ssLevel, parameter.eye_pos, parameter.j);
            ref Structs.Random rng = ref rngView[m];

            float scale = MathF.Tan(deg2rad((float)scene.Fov * 0.5f));
            float imageAspectRatio = scene.Width / (float)scene.Height;

            int i = m / (ssLevel * ssLevel);
            int subIndex = m % (ssLevel * ssLevel);

            var (x, y) = CalcSubXY(i, j, subIndex, ssLevel);

            x = x * 2f / scene.Width - 1;
            x *= scale * imageAspectRatio;
            y = y * -2f / scene.Height + 1;
            y *= scale;

            Vector3f dir = Vector3f.Normalize(new(-x, y, 1));
            output[m] = Structs.SceneContext.CastRay(new(eye_pos, dir), bvh, scene, ref rng);
        }

        private void PreBVHAccel(in Accelerator accelerator, in Scene scene, out Structs.BVHAccel bvh)
        {
            Structs.BVHAccelBulder bVHAccelBulder = new();
            Structs.MeshTriangleParameterBuilder parameterBuilder = new();
            var meshTriangles = parameterBuilder.Build(scene.Objects.Cast<MeshTriangle>().ToArray(), out var lstMateral);
            bvh = bVHAccelBulder.Build(meshTriangles, lstMateral, accelerator);
        }

        private Structs.Random[] CreateRandoms(int number)
        {
            System.Random random = new();
            var objsRandom = new Structs.Random[number];
            for (int i = 0; i < number; i++)
            {
                objsRandom[i] = new(((uint)random.Next()) | ((ulong)random.Next() << 32));
            }
            return objsRandom;
        }

        #endregion
    }
}