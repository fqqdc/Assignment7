using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Assignment7
{
    internal static class Global
    {
        internal static Func<Random>? RandomGenerator;

        internal static float GetRandomFloat()
        {
            Debug.Assert(RandomGenerator != null);
            return RandomGenerator().NextSingle();
        }

        internal static bool SolveQuadratic(float a, float b, float c, out float x0, out float x1)
        {
            x0 = float.NaN; x1 = float.NaN;

            float discr = b * b - 4 * a * c;
            if (discr < 0)
                return false;
            else if (discr == 0)
                x0 = x1 = -0.5f * b / a;
            else
            {
                float q = (b > 0) ? -0.5f * (b + MathF.Sqrt(discr)) : -0.5f * (b - MathF.Sqrt(discr));
                x0 = q / a;
                x1 = c / q;
            }
            if (x0 > x1)
                Swap(ref x0, ref x1);
            return true;
        }

        internal static Action<float> UpdateProgress = DefaultUpdateProgress;

        internal static void DefaultUpdateProgress(float progress)
        {
            int barWidth = 70;

            Debug.Write("[");
            int pos = (int)(barWidth * progress);
            for (int i = 0; i < barWidth; ++i)
            {
                if (i < pos)
                    Debug.Write("=");
                else if (i == pos)
                    Debug.Write(">");
                else
                    Debug.Write(" ");
            }
            Debug.WriteLine($"] {(int)(progress * 100.0)} %");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Swap<T>(ref T a, ref T b)
        {
            T temp = a;
            a = b;
            b = temp;
        }
    }
}
