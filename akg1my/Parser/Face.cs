using System.Numerics;

namespace akg1my.Parser
{
    internal class Face(IEnumerable<int> vertices, IEnumerable<int> textures, IEnumerable<int> normals)
    {
        public readonly IEnumerable<int> VertexIds = vertices.ToList();
        public readonly IEnumerable<int> TextureIds = textures.ToList();
        public readonly IEnumerable<int> NormalIds = normals.ToList();
    }
}
