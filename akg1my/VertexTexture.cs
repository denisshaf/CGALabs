using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;

namespace akg1my
{
    internal struct VertexTexture(double u, double v = 0, double w = 0)
    {
        public double U = u; public double V = v; public double W = w;
    }
}
