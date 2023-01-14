using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vector3f = System.Numerics.Vector3;

namespace Structs
{
    public struct Ray
    {
        public Vector3f Origin;
        public Vector3f Direction, Direction_inv;
        public double Time;//transportation time,
        public double MinTime, MaxTime;

        public Ray(in Vector3f ori, in Vector3f dir, double _t = 0.0)
        {
            Origin = ori;
            Direction = dir;
            Time = _t;

            Direction_inv = new(1 / Direction.X, 1 / Direction.Y, 1 / Direction.Z);
            MinTime = 0.0;
            MaxTime = double.MaxValue;
        }

        public readonly Vector3f GetPosition(double time) { return Origin + Direction * (float)time; }

        public override string ToString()
        {
            return $"[origin:={Origin}, direction={Direction}, time={Time}]";
        }
    }
}
