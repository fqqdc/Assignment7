using ILGPU.Runtime;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Structs
{
    public struct BVHNode
    {
        public Bounds3 Bounds;
        public int Left;
        public int Right;
        public int Object;
        public float Area;
        public int Material;

        // BVHBuildNode Public Methods
        public BVHNode()
        {
            Bounds = new();
            Left = -1;
            Right = -1;
            Object = -1;
            Area = 0;
            Material = -1;
        }
    }
}
