using System.Globalization;
using System.IO;
using System.Numerics;

namespace akg1my.Parser
{
    internal class ObjParser
    {
        public List<Vector4> Vertices = new List<Vector4>();
        public List<Vector3> VertexTextures = new List<Vector3>();
        public List<Vector3> VertexNormals = new List<Vector3>();
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
            while ((line = reader.ReadLine()) is not null)
            {
                var data = System.Text.RegularExpressions.Regex.Split(line, @"\s+"); ;

                if (_actions.TryGetValue(data[0], out Action<string[]>? value))
                {
                    value?.Invoke(data);
                }
            }
        }

        private void AddVertex(string[] data)
        {
            Vertices.Add(new Vector4(
                float.Parse(data[1], CultureInfo.InvariantCulture),
                float.Parse(data[2], CultureInfo.InvariantCulture),
                float.Parse(data[3], CultureInfo.InvariantCulture),
                data.Length == 5 ? float.Parse(data[4], CultureInfo.InvariantCulture) : 1.0f
                ));
        }

        private void AddVertexTexture(string[] data)
        {
            VertexTextures.Add(new Vector3(
                float.Parse(data[1], CultureInfo.InvariantCulture),
                data.Length > 2 ? float.Parse(data[2], CultureInfo.InvariantCulture) : 0.0f,
                data.Length > 3 ? float.Parse(data[3], CultureInfo.InvariantCulture) : 0.0f
                ));
        }

        private void AddVertexNormal(string[] data)
        {
            VertexNormals.Add(new Vector3(
                float.Parse(data[1], CultureInfo.InvariantCulture),
                float.Parse(data[2], CultureInfo.InvariantCulture),
                float.Parse(data[3], CultureInfo.InvariantCulture)
                ));
        }

        private void AddFace(string[] data)
        {
            List<int> vs = [];
            List<int> vns = [];
            List<int> vts = [];

            var coords = data[1..].Where(d => d != string.Empty).ToArray();

            for (int i = 0; i < coords.Length; i++)
            {
                var elem = coords[i].Split('/');

                int vId = int.Parse(elem[0]);
                if (vId != -1)
                {
                    vs.Add(vId);
                }
                else
                {
                    vs.Add(Vertices.Count);
                }

                int vtId;
                int vnId;

                if (elem.Length > 1)
                {
                    if (elem[1] != string.Empty)
                    {
                        vtId = int.Parse(elem[1]);

                        if (vtId != -1)
                        {
                            vts.Add(vtId);
                        }
                        else
                        {
                            vts.Add(VertexTextures.Count);
                        }
                    }
                    else
                    {
                        vnId = int.Parse(elem[2]);

                        if (vnId != -1)
                        {
                            vns.Add(vnId);
                        }
                        else
                        {
                            vns.Add(VertexNormals.Count);
                        }
                    }
                }
                if (elem.Length > 2)
                {
                    vnId = int.Parse(elem[2]);

                    if (vnId != -1)
                    {
                        vns.Add(vnId);
                    }
                    else
                    {
                        vns.Add(VertexNormals.Count);
                    }
                }
            }

            Faces.Add(new(vs, vts, vns));
        }
    }
}
