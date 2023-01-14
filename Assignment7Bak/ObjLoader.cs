using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace ObjLoader
{
    // Structure: Vertex
    //
    // Description: Model Vertex object that holds
    //	a Position, Normal, and Texture Coordinate
    struct Vertex
    {
        // Position Vector
        public Vector3 Position;

        // Normal Vector
        public Vector3 Normal;

        // Texture Coordinate Vector
        public Vector2 TextureCoordinate;
    }

    struct Material
    {
        public Material()
        {
            name = "";
            Ns = 0.0f;
            Ni = 0.0f;
            d = 0.0f;
            illum = 0;
        }

        // Material Name
        public string name;
        // Ambient Color
        public Vector3 Ka;
        // Diffuse Color
        public Vector3 Kd;
        // Specular Color
        public Vector3 Ks;
        // Specular Exponent
        public float Ns;
        // Optical Density
        public float Ni;
        // Dissolve
        public float d;
        // Illumination
        public int illum;
        // Ambient Texture Map
        public string? map_Ka;
        // Diffuse Texture Map
        public string? map_Kd;
        // Specular Texture Map
        public string? map_Ks;
        // Specular Hightlight Map
        public string? map_Ns;
        // Alpha Texture Map
        public string? map_d;
        // Bump Map
        public string? map_bump;
    }

    // Structure: Mesh
    //
    // Description: A Simple Mesh Object that holds
    //	a name, a vertex list, and an index list
    struct Mesh
    {
        // Default Constructor
        public Mesh()
        {
            Vertices = new();
            Indices = new();
        }
        // Variable Set Constructor
        public Mesh(List<Vertex> _Vertices, List<uint> _Indices)
        {
            Vertices = _Vertices;
            Indices = _Indices;
            MeshMaterial = null;
        }
        // Mesh Name
        public string? MeshName;
        // Vertex List
        public List<Vertex> Vertices { get; init; }
        // Index List
        public List<uint> Indices { get; init; }

        // Material
        public Material? MeshMaterial;
    };
}

