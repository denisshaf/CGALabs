using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace akg1my
{
    internal class ObjParser
    {
        public List<Vertex> Vertices = new List<Vertex>();
        public List<VertexTexture> VertexTextures = new List<VertexTexture>();
        public List<VertexNormal> VertexNormals = new List<VertexNormal>();
        public List<Face> Faces = new List<Face>();

        private readonly Dictionary<string, Action<string[]>> _actions;

        public ObjParser(string filename)
        {
            _actions = new Dictionary<string, Action<string[]>>()
            {
                ["v"] = AddVertex,
                ["vt"] = AddVertexTexture,
                ["vn"] = AddVertexNormal,
                ["f"] = AddFace
            };

            using var reader = new StreamReader(filename);

            string? line;
            while (line = reader.ReadLine())
            {
                var data = System.Text.RegularExpressions.Regex.Split(line, @"\s+"); ;

                if (_actions.TryGetValue(data[0], out Action<string[]>? value))
                {
                    value.Invoke(data);
                }
            }
        }
    }
}
