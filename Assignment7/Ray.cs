using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vector3f = System.Numerics.Vector3;

namespace Assignment7
{
    public struct Ray
    {
        public Vector3f origin;
        public Vector3f direction, direction_inv;
        public double t;//transportation time,
        public double t_min, t_max;

        public Ray(in Vector3f ori, in Vector3f dir, double _t = 0.0)
        {
            origin = ori;
            direction = dir;
            t = _t;

            direction_inv = new(1 / direction.X, 1 / direction.Y, 1 / direction.Z);
            t_min = 0.0;
            t_max = double.MaxValue;
        }

        public Vector3f GetPosition(double t) { return origin + direction * (float)t; }

        public override string ToString()
        {
            return $"[origin:={origin}, direction={direction}, time={t}]";
        }
    }
}
