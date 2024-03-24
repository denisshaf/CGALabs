using System.Drawing;
using System.Numerics;

namespace akg1my.Light
{
    internal class LambertsLight
    {
        public Color Color { get; set; }
        public float Intensity {  get; set; } = 1.0f;
        public Vector3 Position { get; set; }
        public LambertsLight(Vector3 position) : this(position, Color.White) { }

        public LambertsLight(Vector3 position, Color color)
        {
            Position = position;
            Color = color;
        }

        public Vector3 CalculateLight(Vector3 point, Vector3 normal)
        {
            Vector3 dir = Position - point;

            float angleCos = float.Max(0, Vector3.Dot(dir, normal) / (dir.Length() * normal.Length()));
            return Intensity * angleCos * new Vector3(Color.R, Color.G, Color.B) / 255f;
        }
    }
}
