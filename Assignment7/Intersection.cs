using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vector3f = System.Numerics.Vector3;

namespace Assignment7
{
    public struct Intersection
    {
        [MemberNotNull(nameof(Material))]
        public bool Happened { get; set; }
        public Vector3f Coords;
        //public Vector3f tcoords;
        public Vector3f Normal;
        public Vector3f Emit;
        public double Distance;
        public GeometryObject? Object;
        public Material? Material;

        public Intersection()
        {
            Happened = false;
            Distance = double.MaxValue;
        }
    }
}
