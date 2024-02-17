using akg1my.Parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace akg1my
{
    internal class WorldObject
    {
        public Vector3 PositionInParentSpace { get; set; }
        public Vector3 RotationInParentSpace { get; set; }

        public List<Vector4> Vertices { get { return _vertices.ToList(); } }
        public List<Face> Faces { get { return _faces.ToList(); } }

        private readonly List<Vector4> _vertices;
        private readonly List<Face> _faces;
        private readonly List<Vector3>? _vertexTextures;
        private readonly List<Vector3>? _vertexNormals;

        public WorldObject(List<Vector4> vertices, List<Face> faces, List<Vector3>? vertexTextures = null, List<Vector3>? vertexNormals = null)
        {
            _vertices = vertices;
            _faces = faces;
            _vertexTextures = vertexTextures;
            _vertexNormals = vertexNormals;

            PositionInParentSpace = Vector3.Zero;
            RotationInParentSpace = Vector3.Zero;
        }

        public Vector3 PositionInWorldSpace{ get; set; }
        public Vector3 RotationInWorldSpace { get; set; }
    }
}
