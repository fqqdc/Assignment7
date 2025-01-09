using ILGPU;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Printing;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;

namespace Assignment7
{
    public struct BVHNode
    {
        public Bounds3 Bounds;
        public int LeftIndex;
        public int RightIndex;
        public float Area;
        public GeometryObject? Object;

        // BVHBuildNode Public Methods
        public BVHNode()
        {
            Bounds = new();
        }
    };




    public class BVHAccel
    {
        // BVHAccel Public Types
        public enum SplitMethod { BVH, SAH };

        // BVHAccel Private Data
        private readonly int maxPrimsInNode;
        private readonly SplitMethod splitMethod;
        Memory<GeometryObject> primitives = Memory<GeometryObject>.Empty;

        public readonly int RootIndex = -1;
        public readonly List<BVHNode> NodeList = [];

        public BVHAccel(IEnumerable<GeometryObject> p, int maxPrimsInNode,
                   SplitMethod splitMethod)
        {
            this.maxPrimsInNode = int.Min(255, maxPrimsInNode);
            this.splitMethod = splitMethod;
            primitives = p.ToArray();

            Stopwatch stopwatch = new();
            if (primitives.Length == 0)
                return;

            RootIndex = splitMethod switch
            {
                SplitMethod.BVH => RecursiveBuildByBVH(primitives.Span),
                SplitMethod.SAH => throw new NotImplementedException(),
                _ => throw new NotImplementedException(),
            };

            var diff = stopwatch.Elapsed;

            Console.Write(
                $"\r{splitMethod} Generation complete: \nTime Taken: {diff.Hours} hrs, {diff.Minutes} mins, {diff.Seconds} secs, {diff.Milliseconds} ms\n\n");
        }
        public BVHAccel(IEnumerable<GeometryObject> p)
            : this(p, 1, SplitMethod.BVH) { }

        public int RecursiveBuildByBVH(Span<GeometryObject> objects)
        {
            NodeList.Add(new BVHNode());
            int nodeIndex = NodeList.Count - 1;
            var nodes = CollectionsMarshal.AsSpan(NodeList);


            // Compute bounds of all primitives in BVH node
            Bounds3 bounds = default;
            for (int i = 0; i < objects.Length; ++i)
                bounds = Bounds3.Union(bounds, objects[i].GetBounds());
            if (objects.Length == 1)
            {
                ref var node = ref nodes[nodeIndex];
                // Create leaf _BVHBuildNode_
                node.Bounds = objects[0].GetBounds();
                node.Object = objects[0];
                node.Area = objects[0].Area;
            }
            else if (objects.Length == 2)
            {
                var leftIndex = RecursiveBuildByBVH(objects.Slice(0, 1));
                var rightIndex = RecursiveBuildByBVH(objects.Slice(1, 1));

                nodes = CollectionsMarshal.AsSpan(NodeList);
                ref var node = ref nodes[nodeIndex];
                node.LeftIndex = leftIndex;
                node.RightIndex = rightIndex;
                node.Bounds = Bounds3.Union(nodes[node.LeftIndex].Bounds, nodes[node.RightIndex].Bounds);
                node.Area = nodes[node.LeftIndex].Area + nodes[node.RightIndex].Area;
            }
            else
            {
                Bounds3 centroidBounds = default;
                for (int i = 0; i < objects.Length; ++i)
                    centroidBounds =
                        Bounds3.Union(centroidBounds, objects[i].GetBounds().Centroid());
                int dim = centroidBounds.MaxExtent();

                switch (dim)
                {
                    case 0:
                        objects.Sort((f1, f2) => f1.GetBounds().Centroid().X < f2.GetBounds().Centroid().X ? -1 : 1);
                        break;
                    case 1:
                        objects.Sort((f1, f2) => f1.GetBounds().Centroid().Y < f2.GetBounds().Centroid().Y ? -1 : 1);
                        break;
                    case 2:
                        objects.Sort((f1, f2) => f1.GetBounds().Centroid().Z < f2.GetBounds().Centroid().Z ? -1 : 1);
                        break;
                }

                int mid = objects.Length / 2;

                var leftshapes = objects[0..mid];
                var rightshapes = objects[mid..];

                Debug.Assert(objects.Length == (leftshapes.Length + rightshapes.Length));

                var leftIndex = RecursiveBuildByBVH(leftshapes);
                var rightIndex = RecursiveBuildByBVH(rightshapes);

                nodes = CollectionsMarshal.AsSpan(NodeList);
                ref var node = ref nodes[nodeIndex];
                node.LeftIndex = leftIndex;
                node.RightIndex = rightIndex;
                node.Bounds = Bounds3.Union(nodes[node.LeftIndex].Bounds, nodes[node.RightIndex].Bounds);
                node.Area = nodes[node.LeftIndex].Area + nodes[node.RightIndex].Area;
            }

            return nodeIndex;
        }

        public Intersection Intersect(in Ray ray)
        {
            Intersection isect = new();
            if (RootIndex == -1)
                return isect;
            isect = GetIntersection(RootIndex, ray);
            return isect;
        }

        public Intersection GetIntersection(int nodeIndex, in Ray ray)
        {
            Debug.Assert(nodeIndex >= 0 && nodeIndex < NodeList.Count);

            var nodes = CollectionsMarshal.AsSpan(NodeList);
            ref readonly var node = ref nodes[nodeIndex];

            // TODO Traverse the BVH to find intersection
            bool[] dirIsNeg = new bool[3];
            for (int i = 0; i < dirIsNeg.Length; i++)
                dirIsNeg[i] = ray.direction[i] < 0;

            if (!node.Bounds.IntersectP(ray, ray.direction_inv, dirIsNeg))
                return new();

            if (node.Object != null)
                return node.Object.GetIntersection(ray);

            Intersection hit1 = GetIntersection(node.LeftIndex, ray);
            Intersection hit2 = GetIntersection(node.RightIndex, ray);

            return hit1.Distance < hit2.Distance ? hit1 : hit2;
        }

        public void GetSample(int nodeIndex, float p, out Intersection pos, out float pdf)
        {
            var nodes = CollectionsMarshal.AsSpan(NodeList);
            ref readonly var node = ref nodes[nodeIndex];

            if (node.Object != null)
            {
                node.Object.Sample(out pos, out pdf);
                pdf *= node.Area;
                return;
            }

            Debug.Assert(node.LeftIndex != -1 && node.RightIndex != -1);
            if (p < nodes[node.LeftIndex].Area) GetSample(node.LeftIndex, p, out pos, out pdf);
            else GetSample(node.RightIndex, p - nodes[node.LeftIndex].Area, out pos, out pdf);
        }

        public void Sample(out Intersection pos, out float pdf)
        {
            Debug.Assert(RootIndex != -1);

            var nodes = CollectionsMarshal.AsSpan(NodeList);
            ref readonly var root = ref nodes[RootIndex];

            float p = MathF.Sqrt(Global.GetRandomFloat()) * root.Area;
            GetSample(RootIndex, p, out pos, out pdf);
            pdf /= root.Area;
        }
    }
}