using System.Globalization;
using System.IO;
using System.Numerics;

namespace akg1my
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
            var vs = new List<Vector4>();
            var vns = new List<Vector3>();
            var vts = new List<Vector3>();

            for (int i = 1; i < data.Length && data[i] != string.Empty; i++)
            {
                var elem = data[i].Split('/');

                int vId = int.Parse(elem[0]);
                if (vId != -1)
                {
                    vs.Add(Vertices[vId - 1]);
                }
                else
                {
                    vs.Add(Vertices[^1]);
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
                            vts.Add(VertexTextures[vtId - 1]);
                        }
                        else
                        {
                            vts.Add(VertexTextures[^1]);
                        }
                    }
                    else
                    {
                        vnId = int.Parse(elem[2]);
                        if (vnId != -1)
                        {
                            vns.Add(VertexNormals[vnId - 1]);
                        }
                        else
                        {
                            vns.Add(VertexNormals[^1]);
                        }
                    }
                }
                if (elem.Length > 2)
                {
                    vnId = int.Parse(elem[2]);
                    if (vnId != -1)
                    {
                        vns.Add(VertexNormals[vnId - 1]);
                    }
                    else
                    {
                        vns.Add(VertexNormals[^1]);
                    }
                }

                Faces.Add(new(vs, vts, vns));
            }
        }
    }
}
