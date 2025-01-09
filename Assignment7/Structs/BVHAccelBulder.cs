using Assignment7;
using ILGPU.Runtime;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Structs
{
    public class BVHAccelBulder
    {
        private List<BVHNode> lstNode = new();
        private List<int> lstMeshIndex = new();
        private List<Triangle> lstTriangle = new();
        private Dictionary<Assignment7.Material, int> dictMaterial2Id = new();

        public BVHAccelGpu Build(MeshTriangleParameter[] meshTriangles, in List<Material> lstMaterial, in Accelerator accelerator)
        {
            var rootIndex = recursiveBuildFromMeshTriangle(meshTriangles);

            var nodes = accelerator.Allocate1D(lstNode.ToArray()).View;
            var meshIndexes = accelerator.Allocate1D(lstMeshIndex.ToArray()).View;
            var triangles = accelerator.Allocate1D(lstTriangle.ToArray()).View;
            var materials = accelerator.Allocate1D(lstMaterial.ToArray()).View;

            return new()
            {
                RootIndex = rootIndex,
                Nodes = nodes,
                MeshIndexes = meshIndexes,
                Triangles = triangles,
                Materials = materials,
            };
        }

        private int recursiveBuildFromTriangles(Span<Triangle> triangles)
        {
            BVHNode node = new();

            // Compute bounds of all primitives in BVH node
            Bounds3 bounds = new();
            for (int i = 0; i < triangles.Length; ++i)
                bounds = Bounds3.Union(bounds, triangles[i].GetBounds());
            if (triangles.Length == 1)
            {
                // Create leaf _BVHBuildNode_
                node.Bounds = triangles[0].GetBounds();

                var indexObjcet = lstTriangle.Count;
                lstTriangle.Add(triangles[0]);

                node.Object = indexObjcet;
                node.Area = triangles[0].Area;
            }
            else if (triangles.Length == 2)
            {
                node.Left = recursiveBuildFromTriangles(triangles[0..1]);
                node.Right = recursiveBuildFromTriangles(triangles[1..2]);

                node.Bounds = Bounds3.Union(lstNode[node.Left].Bounds, lstNode[node.Right].Bounds);
                node.Area = lstNode[node.Left].Area + lstNode[node.Right].Area;
            }
            else
            {
                Bounds3 centroidBounds = new();
                for (int i = 0; i < triangles.Length; ++i)
                    centroidBounds =
                        Bounds3.Union(centroidBounds, triangles[i].GetBounds().Centroid());
                int dim = centroidBounds.MaxExtent();

                switch (dim)
                {
                    case 0:
                        triangles.Sort((f1, f2) => f1.GetBounds().Centroid().X < f2.GetBounds().Centroid().X ? -1 : 1);
                        break;
                    case 1:
                        triangles.Sort((f1, f2) => f1.GetBounds().Centroid().Y < f2.GetBounds().Centroid().Y ? -1 : 1);
                        break;
                    case 2:
                        triangles.Sort((f1, f2) => f1.GetBounds().Centroid().Z < f2.GetBounds().Centroid().Z ? -1 : 1);
                        break;
                }

                int middling = triangles.Length / 2;

                var r = 0..1;
                var s = triangles[r];

                var leftshapes = triangles[0..middling];
                var rightshapes = triangles[middling..];

                Debug.Assert(triangles.Length == (leftshapes.Length + rightshapes.Length));

                node.Left = recursiveBuildFromTriangles(leftshapes);
                node.Right = recursiveBuildFromTriangles(rightshapes);

                node.Bounds = Bounds3.Union(lstNode[node.Left].Bounds, lstNode[node.Right].Bounds);
                node.Area = lstNode[node.Left].Area + lstNode[node.Right].Area;
            }

            var indexNode = lstNode.Count;
            lstNode.Add(node);
            return indexNode;
        }

        private int recursiveBuildFromMeshTriangle(Span<MeshTriangleParameter> meshTriangles)
        {

            BVHNode node = new();
            if (meshTriangles.Length == 1)
            {
                var idxMesh = recursiveBuildFromTriangles(meshTriangles[0].Triangles);
                lstMeshIndex.Add(idxMesh);
                lstNode[idxMesh] = lstNode[idxMesh] with { Material = meshTriangles[0].Material };
                return idxMesh;
            }
            else if (meshTriangles.Length == 2)
            {
                node.Left = recursiveBuildFromMeshTriangle(meshTriangles[0..1]);
                node.Right = recursiveBuildFromMeshTriangle(meshTriangles[1..2]);

                node.Bounds = Bounds3.Union(lstNode[node.Left].Bounds, lstNode[node.Right].Bounds);
                node.Area = lstNode[node.Left].Area + lstNode[node.Right].Area;
            }
            else
            {
                Bounds3 centroidBounds = new();
                for (int i = 0; i < meshTriangles.Length; ++i)
                    centroidBounds =
                        Bounds3.Union(centroidBounds, meshTriangles[i].BoundingBox.Centroid());
                int dim = centroidBounds.MaxExtent();

                switch (dim)
                {
                    case 0:
                        meshTriangles.Sort((f1, f2) => f1.BoundingBox.Centroid().X < f2.BoundingBox.Centroid().X ? -1 : 1);
                        break;
                    case 1:
                        meshTriangles.Sort((f1, f2) => f1.BoundingBox.Centroid().Y < f2.BoundingBox.Centroid().Y ? -1 : 1);
                        break;
                    case 2:
                        meshTriangles.Sort((f1, f2) => f1.BoundingBox.Centroid().Z < f2.BoundingBox.Centroid().Z ? -1 : 1);
                        break;
                }

                int middling = meshTriangles.Length / 2;

                var r = 0..1;
                var s = meshTriangles[r];

                var leftshapes = meshTriangles[0..middling];
                var rightshapes = meshTriangles[middling..];

                Debug.Assert(meshTriangles.Length == (leftshapes.Length + rightshapes.Length));

                node.Left = recursiveBuildFromMeshTriangle(leftshapes);
                node.Right = recursiveBuildFromMeshTriangle(rightshapes);

                node.Bounds = Bounds3.Union(lstNode[node.Left].Bounds, lstNode[node.Right].Bounds);
                node.Area = lstNode[node.Left].Area + lstNode[node.Right].Area;
            }

            var indexNode = lstNode.Count;
            lstNode.Add(node);
            return indexNode;
        }
    }
}
