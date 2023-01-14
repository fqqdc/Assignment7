using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Assignment
{
    internal static class MathV3
    {
        // Vector3 Magnitude Calculation
        public static float Magnitude(Vector3 vector3)
        {
            //return (sqrtf(powf(@in.X, 2) + powf(@in.Y, 2) + powf(@in.Z, 2)));
            return (MathF.Sqrt(MathF.Pow(vector3.X, 2) + MathF.Pow(vector3.Y, 2) + MathF.Pow(vector3.Z, 2)));
        }

        // Projection Calculation of a onto b
        public static Vector3 Projection(Vector3 a, Vector3 b)
        {
            Vector3 bn = b / Magnitude(b);
            return bn * Vector3.Dot(a, bn);
        }

        // Angle between 2 Vector3 Objects
        public static float AngleBetween(Vector3 a, Vector3 b)
        {
            float angle = Vector3.Dot(a, b);
            angle /= (Magnitude(a) * Magnitude(b));
            return angle = MathF.Acos(angle);
        }
    }

    internal static class Algorithm
    {
        // A test to see if P1 is on the same side as P2 of a line segment ab
        public static bool SameSide(Vector3 p1, Vector3 p2, Vector3 a, Vector3 b)
        {
            Vector3 cp1 = Vector3.Cross(b - a, p1 - a);
            Vector3 cp2 = Vector3.Cross(b - a, p2 - a);

            if (Vector3.Dot(cp1, cp2) >= 0)
                return true;
            else
                return false;
        }

        // Generate a cross produect normal for a triangle
        public static Vector3 GenTriNormal(Vector3 t1, Vector3 t2, Vector3 t3)
        {
            Vector3 u = t2 - t1;
            Vector3 v = t3 - t1;

            Vector3 normal = Vector3.Cross(u, v);

            return normal;
        }

        // Check to see if a Vector3 Point is within a 3 Vector3 Triangle
        public static bool InTriangle(Vector3 point, Vector3 tri1, Vector3 tri2, Vector3 tri3)
        {
            // Test to see if it is within an infinite prism that the triangle outlines.
            bool within_tri_prisim = SameSide(point, tri1, tri2, tri3) && SameSide(point, tri2, tri1, tri3)
                                     && SameSide(point, tri3, tri1, tri2);

            // If it isn't it will never be on the triangle
            if (!within_tri_prisim)
                return false;

            // Calulate Triangle's Normal
            Vector3 n = GenTriNormal(tri1, tri2, tri3);

            // Project the point onto this normal
            Vector3 proj = MathV3.Projection(point, n);

            // If the distance from the triangle to the point is 0
            //	it lies on the triangle
            if (MathV3.Magnitude(proj) == 0)
                return true;
            else
                return false;
        }

        // Split a String into a string array at a given token
        public static void Split(string @in,
                          out List<string> @out,
                          string? token)
        {
            @out = @in.Split(token).ToList();
        }

        // Get tail of string after first token and possibly following spaces
        public static string Tail(string @in)
        {
            //int token_start = in.find_first_not_of(" \t");
            int token_start = @in.IndexOfAny(" \t");
            //int space_start = in.find_first_of(" \t", token_start);
            int space_start = @in.IndexOfAny(" \t", token_start);
            //int tail_start = in.find_first_not_of(" \t", space_start);
            int tail_start = @in.IndexOfAnyExcept(" \t", space_start);
            //int tail_end = in.find_last_not_of(" \t");
            int tail_end = @in.LastIndexOfAnyExcept(" \t");
            if (tail_start != -1 && tail_end != -1)
            {
                //return in.substr(tail_start, tail_end - tail_start + 1);
                return @in.Substring(tail_start, tail_end - tail_start + 1);
            }
            else if (tail_start != -1)
            {
                return @in.Substring(tail_start);
            }
            return "";
        }

        // Get first token of string
        public static string FirstToken(string @in)
        {
            if (!string.IsNullOrWhiteSpace(@in))
            {
                int token_start = @in.IndexOfAnyExcept(" \t");
                int token_end = @in.IndexOfAny(" \t", token_start);
                if (token_start != -1 && token_end != -1)
                {
                    return @in.Substring(token_start, token_end - token_start);
                }
                else if (token_start != -1)
                {
                    return @in.Substring(token_start);
                }
            }
            return "";
        }

        // Get element at given index position
        public static T GetElement<T>(List<T> elements, string index)
        {
            int idx = int.Parse(index);
            if (idx < 0)
                idx = elements.Count + idx;
            else
                idx--;
            return elements[idx];
        }

        public static int IndexOfAny(this string? strIn, ReadOnlySpan<char> values, int start = 0)
        {
            if (start < 0)
                return strIn.AsSpan().IndexOfAny(values);

            return strIn.AsSpan(start).IndexOfAny(values) + start;
        }

        public static int IndexOfAnyExcept(this string? strIn, ReadOnlySpan<char> values, int start = 0)
        {
            if (start < 0)
                return strIn.AsSpan().IndexOfAnyExcept(values);

            return strIn.AsSpan(start).IndexOfAnyExcept(values) + start;
        }

        public static int LastIndexOfAny(this string? strIn, ReadOnlySpan<char> values, int index = 0)
        {
            if (index < 0)
                return strIn.AsSpan().LastIndexOfAny(values);

            return strIn.AsSpan(index).LastIndexOfAny(values) + index;
        }

        public static int LastIndexOfAnyExcept(this string? strIn, ReadOnlySpan<char> values, int index = 0)
        {
            if (index < 0)
                return strIn.AsSpan().LastIndexOfAnyExcept(values);

            return strIn.AsSpan(index).LastIndexOfAnyExcept(values) + index;
        }
    }
}
