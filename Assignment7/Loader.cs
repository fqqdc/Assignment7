using Assignment;
using System.Collections.Generic;
using System.IO;
using System.Numerics;

namespace ObjLoader
{
    internal class Loader
    {
        public bool LoadFile(string path)
        {
            // If the file is not an .obj file return false
            if (path.Substring(path.Length - 4, 4) != ".obj")
                return false;

            using var file = File.OpenText(path);

            LoadedMeshes.Clear();
            LoadedVertices.Clear();
            LoadedIndices.Clear();

            List<Vector3> Positions = new();
            List<Vector2> TCoords = new();
            List<Vector3> Normals = new();

            List<Vertex> Vertices = new();
            List<uint> Indices = new();

            List<string> MeshMatNames = new();

            bool listening = false;
            string meshname = string.Empty;

            Mesh tempMesh;

            string? curline;
            while ((curline = file.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(curline))
                    continue;

                // Generate a Mesh Object or Prepare for an object to be created
                if (Algorithm.FirstToken(curline) == "o" || Algorithm.FirstToken(curline) == "g" || curline[0] == 'g')
                {
                    if (!listening)
                    {
                        listening = true;

                        if (Algorithm.FirstToken(curline) == "o" || Algorithm.FirstToken(curline) == "g")
                        {
                            meshname = Algorithm.Tail(curline);
                        }
                        else
                        {
                            meshname = "unnamed";
                        }
                    }
                    else
                    {
                        // Generate the mesh to put into the array

                        if (!Indices.Empty() && !Vertices.Empty())
                        {
                            // Create Mesh
                            tempMesh = new Mesh(Vertices, Indices);
                            tempMesh.MeshName = meshname;

                            // Insert Mesh
                            LoadedMeshes.Add(tempMesh);

                            // Cleanup
                            Vertices.Clear();
                            Indices.Clear();
                            meshname = string.Empty;

                            meshname = Algorithm.Tail(curline);
                        }
                        else
                        {
                            if (Algorithm.FirstToken(curline) == "o" || Algorithm.FirstToken(curline) == "g")
                            {
                                meshname = Algorithm.Tail(curline);
                            }
                            else
                            {
                                meshname = "unnamed";
                            }
                        }
                    }
                }

                // Generate a Vertex Position
                if (Algorithm.FirstToken(curline) == "v")
                {
                    List<string> spos;
                    Vector3 vpos;
                    Algorithm.Split(Algorithm.Tail(curline), out spos, " ");

                    vpos.X = float.Parse(spos[0]);
                    vpos.Y = float.Parse(spos[1]);
                    vpos.Z = float.Parse(spos[2]);

                    Positions.Add(vpos);
                }
                // Generate a Vertex Texture Coordinate
                if (Algorithm.FirstToken(curline) == "vt")
                {
                    List<string> stex;
                    Vector2 vtex;
                    Algorithm.Split(Algorithm.Tail(curline), out stex, " ");

                    vtex.X = float.Parse(stex[0]);
                    vtex.Y = float.Parse(stex[1]);

                    TCoords.Add(vtex);
                }
                // Generate a Vertex Normal;
                if (Algorithm.FirstToken(curline) == "vn")
                {
                    List<string> snor;
                    Vector3 vnor;
                    Algorithm.Split(Algorithm.Tail(curline), out snor, " ");

                    vnor.X = float.Parse(snor[0]);
                    vnor.Y = float.Parse(snor[1]);
                    vnor.Z = float.Parse(snor[2]);

                    Normals.Add(vnor);
                }
                // Generate a Face (vertices & indices)
                if (Algorithm.FirstToken(curline) == "f")
                {
                    // Generate the vertices
                    List<Vertex> vVerts;
                    GenVerticesFromRawOBJ(out vVerts, Positions, TCoords, Normals, curline);

                    // Add Vertices
                    for (int i = 0; i < vVerts.Count; i++)
                    {
                        Vertices.Add(vVerts[i]);

                        LoadedVertices.Add(vVerts[i]);
                    }

                    List<uint> iIndices;

                    VertexTriangluation(out iIndices, vVerts);

                    // Add Indices
                    for (int i = 0; i < iIndices.Count; i++)
                    {
                        uint indnum = (uint)((Vertices.Count) - vVerts.Count) + iIndices[i];
                        Indices.Add(indnum);

                        indnum = (uint)((LoadedVertices.Count) - vVerts.Count) + iIndices[i];
                        LoadedIndices.Add(indnum);

                    }
                }
                // Get Mesh Material Name
                if (Algorithm.FirstToken(curline) == "usemtl")
                {
                    MeshMatNames.Add(Algorithm.Tail(curline));

                    // Create new Mesh, if Material changes within a group
                    if (!Indices.Empty() && !Vertices.Empty())
                    {
                        // Create Mesh
                        tempMesh = new Mesh(Vertices, Indices);
                        tempMesh.MeshName = meshname;
                        int i = 2;
                        while (true)
                        {
                            tempMesh.MeshName = meshname + "_" + i;

                            foreach (var m in LoadedMeshes)
                                if (m.MeshName == tempMesh.MeshName)
                                    continue;
                            break;
                        }

                        // Insert Mesh
                        LoadedMeshes.Add(tempMesh);

                        // Cleanup
                        Vertices.Clear();
                        Indices.Clear();
                    }
                }
                // Load Materials
                if (Algorithm.FirstToken(curline) == "mtllib")
                {
                    // Generate LoadedMaterial

                    // Generate a path to the material file
                    List<string> temp;
                    Algorithm.Split(path, out temp, "/");

                    string pathtomat = "";

                    if (temp.Count != 1)
                    {
                        for (int i = 0; i < temp.Count - 1; i++)
                        {
                            pathtomat += temp[i] + "/";
                        }
                    }


                    pathtomat += Algorithm.Tail(curline);

                    // Load Materials
                    LoadMaterials(pathtomat);
                }

            }

            // Deal with last mesh

            if (!Indices.Empty() && !Vertices.Empty())
            {
                // Create Mesh
                tempMesh = new Mesh(Vertices, Indices);
                tempMesh.MeshName = meshname;

                // Insert Mesh
                LoadedMeshes.Add(tempMesh);
            }

            file.Dispose();

            // Set Materials for each Mesh
            for (int i = 0; i < MeshMatNames.Count; i++)
            {
                string matname = MeshMatNames[i];

                // Find corresponding material name in loaded materials
                // when found copy material variables into mesh material
                for (int j = 0; j < LoadedMaterials.Count; j++)
                {
                    if (LoadedMaterials[j].name == matname)
                    {
                        LoadedMeshes[i] = LoadedMeshes[i] with { MeshMaterial = LoadedMaterials[j] };
                        break;
                    }
                }
            }

            if (LoadedMeshes.Empty() && LoadedVertices.Empty() && LoadedIndices.Empty())
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        // Generate vertices from a list of positions,
        //	tcoords, normals and a face line
        private void GenVerticesFromRawOBJ(out List<Vertex> oVerts,
                                   List<Vector3> iPositions,
                                   List<Vector2> iTCoords,
                                   List<Vector3> iNormals,
                                   string icurline)
        {
            List<string> sface, svert;
            Vertex vVert = new();
            Algorithm.Split(Algorithm.Tail(icurline), out sface, " ");

            bool noNormal = false;
            oVerts = new();

            // For every given vertex do this
            for (int i = 0; i < sface.Count; i++)
            {
                // See What type the vertex is.
                int vtype = 0;

                Algorithm.Split(sface[i], out svert, "/");

                // Check for just position - v1
                if (svert.Count == 1)
                {
                    // Only position
                    vtype = 1;
                }

                // Check for position & texture - v1/vt1
                if (svert.Count == 2)
                {
                    // Position & Texture
                    vtype = 2;
                }

                // Check for Position, Texture and Normal - v1/vt1/vn1
                // or if Position and Normal - v1//vn1
                if (svert.Count == 3)
                {
                    if (svert[1] != "")
                    {
                        // Position, Texture, and Normal
                        vtype = 4;
                    }
                    else
                    {
                        // Position & Normal
                        vtype = 3;
                    }
                }

                // Calculate and store the vertex
                switch (vtype)
                {
                    case 1: // P
                        {
                            vVert.Position = Algorithm.GetElement(iPositions, svert[0]);
                            vVert.TextureCoordinate = new Vector2(0, 0);
                            noNormal = true;
                            oVerts.Add(vVert);
                            break;
                        }
                    case 2: // P/T
                        {
                            vVert.Position = Algorithm.GetElement(iPositions, svert[0]);
                            vVert.TextureCoordinate = Algorithm.GetElement(iTCoords, svert[1]);
                            noNormal = true;
                            oVerts.Add(vVert);
                            break;
                        }
                    case 3: // P//N
                        {
                            vVert.Position = Algorithm.GetElement(iPositions, svert[0]);
                            vVert.TextureCoordinate = new Vector2(0, 0);
                            vVert.Normal = Algorithm.GetElement(iNormals, svert[2]);
                            oVerts.Add(vVert);
                            break;
                        }
                    case 4: // P/T/N
                        {
                            vVert.Position = Algorithm.GetElement(iPositions, svert[0]);
                            vVert.TextureCoordinate = Algorithm.GetElement(iTCoords, svert[1]);
                            vVert.Normal = Algorithm.GetElement(iNormals, svert[2]);
                            oVerts.Add(vVert);
                            break;
                        }
                    default:
                        {
                            break;
                        }
                }
            }

            // take care of missing normals
            // these may not be truly acurate but it is the
            // best they get for not compiling a mesh with normals
            if (noNormal)
            {
                Vector3 A = oVerts[0].Position - oVerts[1].Position;
                Vector3 B = oVerts[2].Position - oVerts[1].Position;

                Vector3 normal = Vector3.Cross(A, B);

                for (int i = 0; i < oVerts.Count; i++)
                {
                    oVerts[i] = oVerts[i] with { Normal = normal };
                }
            }
        }

        // Triangulate a list of vertices into a face by printing
        //	inducies corresponding with triangles within it
        private void VertexTriangluation(out List<uint> oIndices,
                                 List<Vertex> iVerts)
        {
            oIndices = new();

            // If there are 2 or less verts,
            // no triangle can be created,
            // so exit
            if (iVerts.Count < 3)
            {
                return;
            }

            // If it is a triangle no need to calculate it
            if (iVerts.Count == 3)
            {
                oIndices.Add(0);
                oIndices.Add(1);
                oIndices.Add(2);
                return;
            }

            // Create a list of vertices
            List<Vertex> tVerts = iVerts;

            while (true)
            {
                // For every vertex
                for (int i = 0; i < tVerts.Count; i++)
                {
                    // pPrev = the previous vertex in the list
                    Vertex pPrev;
                    if (i == 0)
                    {
                        pPrev = tVerts[tVerts.Count - 1];
                    }
                    else
                    {
                        pPrev = tVerts[i - 1];
                    }

                    // pCur = the current vertex;
                    Vertex pCur = tVerts[i];

                    // pNext = the next vertex in the list
                    Vertex pNext;
                    if (i == tVerts.Count - 1)
                    {
                        pNext = tVerts[0];
                    }
                    else
                    {
                        pNext = tVerts[i + 1];
                    }

                    // Check to see if there are only 3 verts left
                    // if so this is the last triangle
                    if (tVerts.Count == 3)
                    {
                        // Create a triangle from pCur, pPrev, pNext
                        for (int j = 0; j < tVerts.Count; j++)
                        {
                            if (iVerts[j].Position == pCur.Position)
                                oIndices.Add((uint)j);
                            if (iVerts[j].Position == pPrev.Position)
                                oIndices.Add((uint)j);
                            if (iVerts[j].Position == pNext.Position)
                                oIndices.Add((uint)j);
                        }

                        tVerts.Clear();
                        break;
                    }
                    if (tVerts.Count == 4)
                    {
                        // Create a triangle from pCur, pPrev, pNext
                        for (int j = 0; j < iVerts.Count; j++)
                        {
                            if (iVerts[j].Position == pCur.Position)
                                oIndices.Add((uint)j);
                            if (iVerts[j].Position == pPrev.Position)
                                oIndices.Add((uint)j);
                            if (iVerts[j].Position == pNext.Position)
                                oIndices.Add((uint)j);
                        }

                        Vector3 tempVec = new();
                        for (int j = 0; j < tVerts.Count; j++)
                        {
                            if (tVerts[j].Position != pCur.Position
                                && tVerts[j].Position != pPrev.Position
                                && tVerts[j].Position != pNext.Position)
                            {
                                tempVec = tVerts[j].Position;
                                break;
                            }
                        }

                        // Create a triangle from pCur, pPrev, pNext
                        for (int j = 0; j < iVerts.Count; j++)
                        {
                            if (iVerts[j].Position == pPrev.Position)
                                oIndices.Add((uint)j);
                            if (iVerts[j].Position == pNext.Position)
                                oIndices.Add((uint)j);
                            if (iVerts[j].Position == tempVec)
                                oIndices.Add((uint)j);
                        }

                        tVerts.Clear();
                        break;
                    }

                    // If Vertex is not an interior vertex
                    float angle = MathV3.AngleBetween(pPrev.Position - pCur.Position, pNext.Position - pCur.Position) * (180f / 3.14159265359f);
                    if (angle <= 0 && angle >= 180)
                        continue;

                    // If any vertices are within this triangle
                    bool inTri = false;
                    for (int j = 0; j < iVerts.Count; j++)
                    {
                        if (Algorithm.InTriangle(iVerts[j].Position, pPrev.Position, pCur.Position, pNext.Position)
                            && iVerts[j].Position != pPrev.Position
                            && iVerts[j].Position != pCur.Position
                            && iVerts[j].Position != pNext.Position)
                        {
                            inTri = true;
                            break;
                        }
                    }
                    if (inTri)
                        continue;

                    // Create a triangle from pCur, pPrev, pNext
                    for (int j = 0; j < iVerts.Count; j++)
                    {
                        if (iVerts[j].Position == pCur.Position)
                            oIndices.Add((uint)j);
                        if (iVerts[j].Position == pPrev.Position)
                            oIndices.Add((uint)j);
                        if (iVerts[j].Position == pNext.Position)
                            oIndices.Add((uint)j);
                    }

                    // Delete pCur from the list
                    for (int j = 0; j < tVerts.Count; j++)
                    {
                        if (tVerts[j].Position == pCur.Position)
                        {
                            tVerts.RemoveAt(j);
                            break;
                        }
                    }

                    // reset i to the start
                    // -1 since loop will add 1 to it
                    i = -1;
                }

                // if no triangles were created
                if (oIndices.Count == 0)
                    break;

                // if no more vertices
                if (tVerts.Count == 0)
                    break;
            }
        }

        // Load Materials from .mtl file
        bool LoadMaterials(string path)
        {
            // If the file is not a material file return false
            if (path.Substring(path.Length - 4, 4) != ".mtl")
                return false;

            using var file = File.OpenText(path);

            Material tempMaterial = new();

            bool listening = false;

            // Go through each line looking for material variables
            string? curline;
            while ((curline = file.ReadLine()) != null)
            {
                // new material and material name
                if (Algorithm.FirstToken(curline) == "newmtl")
                {
                    if (!listening)
                    {
                        listening = true;

                        if (curline.Length > 7)
                        {
                            tempMaterial.name = Algorithm.Tail(curline);
                        }
                        else
                        {
                            tempMaterial.name = "none";
                        }
                    }
                    else
                    {
                        // Generate the material

                        // Push Back loaded Material
                        LoadedMaterials.Add(tempMaterial);

                        // Clear Loaded Material
                        tempMaterial = new Material();

                        if (curline.Length > 7)
                        {
                            tempMaterial.name = Algorithm.Tail(curline);
                        }
                        else
                        {
                            tempMaterial.name = "none";
                        }
                    }
                }
                // Ambient Color
                if (Algorithm.FirstToken(curline) == "Ka")
                {
                    List<string> temp;
                    Algorithm.Split(Algorithm.Tail(curline), out temp, " ");

                    if (temp.Count != 3)
                        continue;

                    tempMaterial.Ka.X = float.Parse(temp[0]);
                    tempMaterial.Ka.Y = float.Parse(temp[1]);
                    tempMaterial.Ka.Z = float.Parse(temp[2]);
                }
                // Diffuse Color
                if (Algorithm.FirstToken(curline) == "Kd")
                {
                    List<string> temp;
                    Algorithm.Split(Algorithm.Tail(curline), out temp, " ");

                    if (temp.Count != 3)
                        continue;

                    tempMaterial.Kd.X = float.Parse(temp[0]);
                    tempMaterial.Kd.Y = float.Parse(temp[1]);
                    tempMaterial.Kd.Z = float.Parse(temp[2]);
                }
                // Specular Color
                if (Algorithm.FirstToken(curline) == "Ks")
                {
                    List<string> temp;
                    Algorithm.Split(Algorithm.Tail(curline), out temp, " ");

                    if (temp.Count != 3)
                        continue;

                    tempMaterial.Ks.X = float.Parse(temp[0]);
                    tempMaterial.Ks.Y = float.Parse(temp[1]);
                    tempMaterial.Ks.Z = float.Parse(temp[2]);
                }
                // Specular Exponent
                if (Algorithm.FirstToken(curline) == "Ns")
                {
                    tempMaterial.Ns = float.Parse(Algorithm.Tail(curline));
                }
                // Optical Density
                if (Algorithm.FirstToken(curline) == "Ni")
                {
                    tempMaterial.Ni = float.Parse(Algorithm.Tail(curline));
                }
                // Dissolve
                if (Algorithm.FirstToken(curline) == "d")
                {
                    tempMaterial.d = float.Parse(Algorithm.Tail(curline));
                }
                // Illumination
                if (Algorithm.FirstToken(curline) == "illum")
                {
                    tempMaterial.illum = int.Parse(Algorithm.Tail(curline));
                }
                // Ambient Texture Map
                if (Algorithm.FirstToken(curline) == "map_Ka")
                {
                    tempMaterial.map_Ka = Algorithm.Tail(curline);
                }
                // Diffuse Texture Map
                if (Algorithm.FirstToken(curline) == "map_Kd")
                {
                    tempMaterial.map_Kd = Algorithm.Tail(curline);
                }
                // Specular Texture Map
                if (Algorithm.FirstToken(curline) == "map_Ks")
                {
                    tempMaterial.map_Ks = Algorithm.Tail(curline);
                }
                // Specular Hightlight Map
                if (Algorithm.FirstToken(curline) == "map_Ns")
                {
                    tempMaterial.map_Ns = Algorithm.Tail(curline);
                }
                // Alpha Texture Map
                if (Algorithm.FirstToken(curline) == "map_d")
                {
                    tempMaterial.map_d = Algorithm.Tail(curline);
                }
                // Bump Map
                if (Algorithm.FirstToken(curline) == "map_Bump" || Algorithm.FirstToken(curline) == "map_bump" || Algorithm.FirstToken(curline) == "bump")
                {
                    tempMaterial.map_bump = Algorithm.Tail(curline);
                }
            }

            // Deal with last material

            // Push Back loaded Material
            LoadedMaterials.Add(tempMaterial);

            // Test to see if anything was loaded
            // If not return false
            if (LoadedMaterials.Empty())
                return false;
            // If so return true
            else
                return true;
        }

        // Loaded Mesh Objects
        public List<Mesh> LoadedMeshes { get; } = new();
        // Loaded Vertex Objects
        public List<Vertex> LoadedVertices { get; } = new();
        // Loaded Index Positions
        public List<uint> LoadedIndices { get; } = new();
        // Loaded Material Objects
        public List<Material> LoadedMaterials { get; } = new();
    }
}
