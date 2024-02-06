using System.Numerics;

namespace akg1my
{
    internal struct Vertex(double x, double y, double z, double w = 1)
    {
        public double X = x, Y = y, Z = z, W = w;
    }
}
