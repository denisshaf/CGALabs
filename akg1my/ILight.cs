using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace akg1my
{
    internal interface ILight
    {
        float Intensity { get; set; }
        float CalculateLight(Vector3 point, Vector3 normal);
    }
}
