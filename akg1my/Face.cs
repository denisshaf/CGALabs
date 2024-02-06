using System.Numerics;

namespace akg1my
{
    internal class Face(IEnumerable<Vector4> vertices, IEnumerable<Vector3> textures, IEnumerable<Vector3> normals)
    {
        public IEnumerable<Vector4> Vertices = vertices;
        public IEnumerable<Vector3> Normals = normals;
        public IEnumerable<Vector3> Textures = textures;
    }
}
