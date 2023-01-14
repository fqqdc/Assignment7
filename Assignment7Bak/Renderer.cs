using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Vector2f = System.Numerics.Vector2;
using Vector3f = System.Numerics.Vector3;

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
        public const float EPSILON = 0.001f;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float deg2rad(float deg) => deg * MathF.PI / 180.0f;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static (float subX, float subY) CalcSubXY(int x, int y, int subIndex, int ssLevel)
        {
            float levelInc = 1f / ssLevel;
            float baseValue = levelInc * 0.5f;

            return (x + baseValue + levelInc * (subIndex % ssLevel), y + baseValue + levelInc * (subIndex / ssLevel));
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

                colorDate[index * 3 + 2] = (byte)(255 * Math.Clamp(v.X, 0, 1)); //R
                colorDate[index * 3 + 1] = (byte)(255 * Math.Clamp(v.Y, 0, 1)); //G
                colorDate[index * 3 + 0] = (byte)(255 * Math.Clamp(v.Z, 0, 1)); //B
            }

            Global.UpdateProgress(1);
            return colorDate;
        }

        public byte[] RenderParallel(Scene scene, int ssLevel = 1)
        {
            if (ssLevel < 1)
                throw new ArgumentOutOfRangeException("ssLevel", "超采样等级范围：1 <= ssLevel");

            byte[] colorDate = new byte[scene.Width * scene.Height * 3];
            Vector3f[] rowbuffer = new Vector3f[scene.Width * ssLevel * ssLevel];

            float scale = MathF.Tan(deg2rad((float)scene.Fov * 0.5f));
            float imageAspectRatio = scene.Width / (float)scene.Height;

            // Use this variable as the eye position to start your rays.
            Vector3f eye_pos = new(278, 273, -800);

            var part = Partitioner.Create(0, scene.Width, scene.Width / Environment.ProcessorCount);
            var partSS = Partitioner.Create(0, ssLevel * ssLevel * scene.Width, ssLevel * ssLevel * scene.Width / Environment.ProcessorCount);
            for (int j = 0; j < scene.Height; j++)
            {
                var plr = Parallel.ForEach(partSS, range =>
                {
                    for (int m = range.Item1; m < range.Item2; m++)
                    {
                        int i = m / (ssLevel * ssLevel);
                        int subIndex = m % (ssLevel * ssLevel);

                        var (x, y) = CalcSubXY(i, j, subIndex, ssLevel);
                        //var (x, y) = (i + 0.5f, j + 0.5f);

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

                        int index = i * ssLevel * ssLevel + subIndex;
                        rowbuffer[index] = scene.CastRay(new(eye_pos, dir), 0);
                    }
                });

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
                        }
                        v /= _v.Length;

                        rowDate[index * 3 + 2] = (byte)(255 * v.X); //R
                        rowDate[index * 3 + 1] = (byte)(255 * v.Y); //G
                        rowDate[index * 3 + 0] = (byte)(255 * v.Z); //B
                    }
                });

                Global.UpdateProgress((j + 1) / (float)scene.Height);
            }

            Global.UpdateProgress(1);
            return colorDate;
        }
    }
}