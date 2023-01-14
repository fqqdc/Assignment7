using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vector3f = System.Numerics.Vector3;

namespace Structs
{
    public struct Intersection
    {
        public bool Happened;
        public Vector3f Coords;
        public Vector3f Normal;
        public Vector3f Emit;
        public double Distance;
        public int Object;
        public int Material;

        public Intersection()
        {
            Happened = false;
            Distance = double.MaxValue;
            Object = -1;
            Material = -1;
        }
    }
}
