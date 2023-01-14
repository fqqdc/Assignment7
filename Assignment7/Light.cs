using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vector3f = System.Numerics.Vector3;

namespace Assignment6
{
    public class Light
    {
        public Light(Vector3f p, Vector3f i)
        {
            Position = p;
            Intensity = i;
        }

        public Vector3f Position { get; }
        public Vector3f Intensity { get; }
    };
}
