using System.Numerics;
using System.Drawing;

namespace akg1my
{
    internal abstract class Light
    {
        public float Intensity { get; set; }
        public Vector3 Position { get; set; }
        public Color Color { get; set; }
        public abstract Vector3 CalculateLight(Vector3 point, Vector3 normal);
    }
}
