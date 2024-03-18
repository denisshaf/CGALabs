using System.Drawing;
using System.Numerics;

namespace akg1my
{
    internal class LambertsLight : Light
    {
        public LambertsLight(Vector3 position, float intensity) 
        {
            Position = position;
            Intensity = intensity;
        }

        public override Vector3 CalculateLight(Vector3 point, Vector3 normal)
        {
            Vector3 dir = Position - point;

            float angleCos = float.Max(0, Vector3.Dot(dir, normal) / (dir.Length() * normal.Length()));
            return Intensity * angleCos * new Vector3(Color.R, Color.G, Color.B) / 255f;
        }
    }
}
