using akg1my.Parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace akg1my.GraphicalObjects
{
    internal class WorldObject
    {
        public Vector3 PositionInParentSpace { get; set; }
        public Vector3 RotationInParentSpace { get; set; }
        public Color Color { get; set; }
        public bool IsAlwaysVisible { get; set; }

        public List<Vector4> Vertices => _vertices.ToList();
        public List<Face> Faces => _faces.ToList();
        public List<Vector3>? VertexNormals => _vertexNormals?.ToList();
        public List<Vector3>? VertexTextures => _vertexTextures?.ToList();
        public ImageData? DiffuseMap => _diffuseMap;
        public ImageData? NormalsMap => _normalsMap;
        public ImageData? SpecularMap => _specularMap;

        private readonly List<Vector4> _vertices;
        private readonly List<Face> _faces;
        private readonly List<Vector3>? _vertexTextures;
        private readonly List<Vector3>? _vertexNormals;

        private readonly ImageData? _diffuseMap;
        private readonly ImageData? _normalsMap;
        private readonly ImageData? _specularMap;

        public WorldObject(List<Vector4> vertices, List<Face> faces, List<Vector3>? vertexTextures = null, List<Vector3>? vertexNormals = null,
            ImageData? diffuseMap = null, ImageData? normalsMap = null, ImageData? specularMap = null)
        {
            _vertices = vertices;
            _faces = faces;
            _vertexTextures = vertexTextures;
            _vertexNormals = vertexNormals;
            _diffuseMap = diffuseMap;
            _normalsMap = normalsMap;
            _specularMap = specularMap;

            PositionInParentSpace = Vector3.Zero;
            RotationInParentSpace = Vector3.Zero;
            Color = Color.White;
            IsAlwaysVisible = false;
        }

        public Vector3 PositionInWorldSpace { get; set; }
        public Vector3 RotationInWorldSpace { get; set; }
        public Vector3 ScaleInWorldSpace { get; set; }
    }
}
