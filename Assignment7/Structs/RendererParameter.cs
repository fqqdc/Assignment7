using Vector3f = System.Numerics.Vector3;

namespace Structs
{
    public struct RendererParameter
    {
        public int ssLevel = 1;
        public int j;
        public SceneContext scene;
        public Vector3f eye_pos = new(278, 273, -800);

        public RendererParameter() { }
    }
}
