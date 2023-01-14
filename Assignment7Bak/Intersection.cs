using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vector3f = System.Numerics.Vector3;

namespace Assignment7
{
    

    public struct Intersection
    {
        public bool happened;
        public Vector3f coords;
        //public Vector3f tcoords;
        public Vector3f normal;
        public Vector3f emit;
        public double distance;
        public GeometryObject? obj;
        public Material? m;

        public Intersection()
        {
            happened = false;
            distance = double.MaxValue;
        }
    }
}
