using Assignment7;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Structs
{
    public class MeshTriangleParameter
    {
        public required Triangle[] Triangles { get; init; }
        public required Bounds3 BoundingBox { get; init; }
        public required int Material { get; init; }

    }

    public class MeshTriangleParameterBuilder
    {
        public MeshTriangleParameter[] Build(Assignment7.MeshTriangle[] meshTriangles, out List<Material> lstMaterial)
        {
            Dictionary<Assignment7.Material, int> dictM2Id = new();
            lstMaterial = new();
            MeshTriangleParameter[] parameters = new MeshTriangleParameter[meshTriangles.Length];

            int idMaterial = -1;
            for (int iMesh = 0; iMesh < meshTriangles.Length; iMesh++)
            {
                var meshTriangle = meshTriangles[iMesh];
                var triangles = meshTriangle.Triangles;
                Triangle[] arrTriangle = new Triangle[triangles.Count];

                Bounds3 bounds = new();
                for (int i = 0; i < arrTriangle.Length; i++)
                {
                    var triangle = triangles[i];
                    var arrV = triangle.V;
                    var arrE = triangle.E;

                    arrTriangle[i] = new()
                    {
                        Area = triangle.Area,
                        V0 = arrV[0],
                        V1 = arrV[1],
                        V2 = arrV[2],
                        E1 = arrE[0],
                        E2 = arrE[1],
                        Normal = triangle.Normal,
                    };

                    idMaterial = -1;
                    if (triangle.Material != null
                        && !dictM2Id.TryGetValue(triangle.Material, out idMaterial))
                    {
                        idMaterial = dictM2Id.Count;
                        dictM2Id.Add(triangle.Material, idMaterial);
                    }
                    arrTriangle[i].Material = idMaterial;

                    bounds = Bounds3.Union(bounds, arrTriangle[i].GetBounds());
                }

                idMaterial = -1;
                if (meshTriangle.Material != null
                    && !dictM2Id.TryGetValue(meshTriangle.Material, out idMaterial))
                {
                    idMaterial = dictM2Id.Count;
                    dictM2Id.Add(meshTriangle.Material, idMaterial);
                }
                parameters[iMesh] = new()
                {
                    Triangles = arrTriangle,
                    BoundingBox = bounds,
                    Material = idMaterial,
                };
            }

            foreach (var kpItem in dictM2Id.OrderBy(kp => kp.Value))
            {
                Debug.Assert(kpItem.Value == lstMaterial.Count);
                var i = kpItem.Value;
                var obj = kpItem.Key;
                Material material = new()
                {
                    Emission = obj.Emission,
                    Kd = obj.Kd,
                    Ks = obj.Ks,
                };
                lstMaterial.Add(material);
            }

            return parameters;
        }
    }
}
