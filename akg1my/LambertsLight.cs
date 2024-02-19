using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace akg1my
{
    internal class LambertsLight : ILight
    {
        public float Intensity { get; set; }
        public Vector3 Position { get; set; }

        public LambertsLight(Vector3 position, float intensity) 
        {
            Position = position;
            Intensity = intensity;
        }

        public float CalculateLight(Vector3 point, Vector3 normal)
        {
            float angleCos = float.Max(0, Vector3.Dot(point, normal) / (point.Length() * normal.Length()));
            return Intensity * angleCos;
        }
    }
}
