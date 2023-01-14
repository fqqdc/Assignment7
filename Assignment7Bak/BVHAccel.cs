using Assignment6;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Intrinsics.X86;

namespace Assignment7
{
    public class BVHBuildNode
    {
        public Bounds3 Bounds { get; set; }

        public BVHBuildNode? Left { get; set; }
        public BVHBuildNode? Right { get; set; }
        public GeometryObject? Object { get; set; }
        public float Area { get; set; }

        //int splitAxis = 0;
        //int firstPrimOffset = 0;
        //int nPrimitives = 0;

        // BVHBuildNode Public Methods
        public BVHBuildNode()
        {
            Bounds = new();
        }
    };




    public class BVHAccel
    {
        // BVHAccel Public Types
        public enum SplitMethod { BVH, SAH };

        BVHBuildNode? root;

        // BVHAccel Private Data
        private readonly int maxPrimsInNode;
        private readonly SplitMethod splitMethod;
        GeometryObject[] primitives;

        public BVHAccel(IEnumerable<GeometryObject> p, int maxPrimsInNode,
                   SplitMethod splitMethod)
        {
            this.maxPrimsInNode = int.Min(255, maxPrimsInNode);
            this.splitMethod = splitMethod;
            primitives = p.ToArray();

            Stopwatch stopwatch = new();
            if (!primitives.Any())
                return;

            root = splitMethod switch
            {
                SplitMethod.BVH => recursiveBuildByBVH(primitives),
                SplitMethod.SAH => throw new NotImplementedException(),
                _ => throw new NotImplementedException(),
            };

            var diff = stopwatch.Elapsed;

            Console.Write(
                $"\r{splitMethod} Generation complete: \nTime Taken: {diff.Hours} hrs, {diff.Minutes} mins, {diff.Seconds} secs, {diff.Milliseconds} ms\n\n");
        }
        public BVHAccel(IEnumerable<GeometryObject> p)
            : this(p, 1, SplitMethod.BVH) { }

        public BVHBuildNode recursiveBuildByBVH(Span<GeometryObject> objects)
        {
            BVHBuildNode node = new();

            // Compute bounds of all primitives in BVH node
            Bounds3 bounds = new();
            for (int i = 0; i < objects.Length; ++i)
                bounds = Bounds3.Union(bounds, objects[i].GetBounds());
            if (objects.Length == 1)
            {
                // Create leaf _BVHBuildNode_
                node.Bounds = objects[0].GetBounds();
                node.Object = objects[0];
                node.Area = objects[0].Area;
                return node;
            }
            else if (objects.Length == 2)
            {
                node.Left = recursiveBuildByBVH(objects[0..1]);
                node.Right = recursiveBuildByBVH(objects[1..2]);

                node.Bounds = Bounds3.Union(node.Left.Bounds, node.Right.Bounds);
                node.Area = node.Left.Area + node.Right.Area;
                return node;
            }
            else
            {
                Bounds3 centroidBounds = new();
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

                int middling = objects.Length / 2;

                var r = 0..1;
                var s = objects[r];

                var leftshapes = objects[0..middling];
                var rightshapes = objects[middling..];

                Debug.Assert(objects.Length == (leftshapes.Length + rightshapes.Length));

                node.Left = recursiveBuildByBVH(leftshapes);
                node.Right = recursiveBuildByBVH(rightshapes);

                node.Bounds = Bounds3.Union(node.Left.Bounds, node.Right.Bounds);
                node.Area = node.Left.Area + node.Right.Area;
            }

            return node;
        }

        public Intersection Intersect(Ray ray)
        {
            Intersection isect = new();
            if (root == null)
                return isect;
            isect = GetIntersection(root, ray);
            return isect;
        }

        public Intersection GetIntersection(BVHBuildNode? node, Ray ray)
        {
            if (node == null) throw new NullReferenceException();

            // TODO Traverse the BVH to find intersection
            bool[] dirIsNeg = new bool[3];
            for (int i = 0; i < dirIsNeg.Length; i++)
                dirIsNeg[i] = ray.direction[i] < 0;

            if (!node.Bounds.IntersectP(ray, ray.direction_inv, dirIsNeg))
                return new();

            if (node.Object != null)
                return node.Object.GetIntersection(ray);

            Intersection hit1 = GetIntersection(node.Left, ray);
            Intersection hit2 = GetIntersection(node.Right, ray);

            return hit1.distance < hit2.distance ? hit1 : hit2;
        }

        public void GetSample(BVHBuildNode node, float p, out Intersection pos, out float pdf)
        {
            if (node.Object != null)
            {
                node.Object.Sample(out pos, out pdf);
                pdf *= node.Area;
                return;
            }

            Debug.Assert(node.Left != null && node.Right != null);
            if (p < node.Left.Area) GetSample(node.Left, p, out pos, out pdf);
            else GetSample(node.Right, p - node.Left.Area, out pos, out pdf);
        }

        public void Sample(out Intersection pos, out float pdf)
        {
            Debug.Assert(root != null);

            float p = MathF.Sqrt(Global.GetRandomFloat()) * root.Area;
            GetSample(root, p, out pos, out pdf);
            pdf /= root.Area;
        }
    }
}