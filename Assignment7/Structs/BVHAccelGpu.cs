using ILGPU;
using System;
using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;

namespace Structs
{
    public struct BVHAccelGpu
    {
        public required int RootIndex;
        public required ArrayView<BVHNode> Nodes;
        public required ArrayView<int> MeshIndexes;
        public required ArrayView<Triangle> Triangles;
        public required ArrayView<Material> Materials;

        public readonly Intersection Intersect(in Ray ray)
        {
            Intersection isect = new();
            if (RootIndex == -1)
                return isect;
            isect = GetIntersection(RootIndex, ray);
            return isect;
        }

        public readonly Intersection GetIntersection(int nodeIndex, in Ray ray)
        { 
            // TODO Traverse the BVH to find intersection
            bool[] dirIsNeg = new bool[3];
            dirIsNeg[0] = ray.Direction.X < 0;
            dirIsNeg[1] = ray.Direction.Y < 0;
            dirIsNeg[2] = ray.Direction.Z < 0;

            IntStack intStack = new(1024);
            intStack.Push(nodeIndex);

            Intersection[] inters = new Intersection[2];
            int idxInter = 0;

            while (intStack.Count > 0)
            {
                if(idxInter == 2)
                {
                    inters[0]= inters[0].Distance < inters[1].Distance ? inters[0] : inters[1];
                    idxInter = 1;
                }

                var index = intStack.Pop();
                var node = Nodes[index];

                if (!node.Bounds.IntersectP(ray, ray.Direction_inv, dirIsNeg))
                {
                    inters[idxInter] = new();
                    idxInter++;
                    continue;
                }

                if (node.Object != -1)
                {
                    ref var t = ref Triangles[node.Object];
                    inters[idxInter] = t.GetIntersection(ray);
                    inters[idxInter].Material = t.Material;
                    inters[idxInter].Object = node.Object;
                    idxInter++;
                    continue;
                }

                intStack.Push(node.Right);
                intStack.Push(node.Left);
            }

            if (idxInter == 2)
            {
                inters[0] = inters[0].Distance < inters[1].Distance ? inters[0] : inters[1];
            }

            return inters[0];
        }

        public readonly void GetSample(ref Structs.Random rng, in int nodeIndex, float p, out Intersection pos, out float pdf)
        {
            int idxNode = nodeIndex;
            while (true)
            {
                var node = Nodes[idxNode];
                if (node.Object != -1)
                {
                    Triangles[node.Object].Sample(ref rng, out pos, out pdf);
                    pdf *= node.Area;
                    return;
                }

                Debug.Assert(node.Left != -1 && node.Right != -1);
                if (p < Nodes[node.Left].Area) 
                    idxNode = node.Left;
                else idxNode = node.Right;
            }
        }

        public readonly void Sample(ref Structs.Random rng, int nodeIndex, out Intersection pos, out float pdf)
        {
            Debug.Assert(nodeIndex != -1);

            float p = MathF.Sqrt(rng.NextSingle()) * Nodes[nodeIndex].Area;
            GetSample(ref rng, nodeIndex, p, out pos, out pdf);
            pdf /= Nodes[nodeIndex].Area;
        }
    }
}
