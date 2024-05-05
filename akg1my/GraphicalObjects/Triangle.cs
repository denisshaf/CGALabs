using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace akg1my.GraphicalObjects
{
    internal struct Triangle
    {
        public Vector4 p0, p1, p2;
        // viewport coordinates
        public Vector3 v0, v1, v2;

        // world coordinates
        public Vector3 w0, w1, w2;

        // vertex normals
        public Vector3 n0, n1, n2;

        // texels
        public Vector3 t0, t1, t2;
        public Triangle(Vector3 v0, Vector3 v1, Vector3 v2)
        {
            this.v0 = v0; this.v1 = v1; this.v2 = v2;
        }

        public Triangle(Vector3 v0, Vector3 v1, Vector3 v2, Vector3 w0, Vector3 w1, Vector3 w2, Vector3 n0, Vector3 n1, Vector3 n2) : this(v0, v1, v2)
        {
            this.w0 = w0; this.w1 = w1; this.w2 = w2;
            this.n0 = n0; this.n1 = n1; this.n2 = n2;
        }
        public Triangle(Vector3 v0, Vector3 v1, Vector3 v2, Vector3 w0, Vector3 w1, Vector3 w2, Vector3 n0, Vector3 n1, Vector3 n2, Vector3 t0, Vector3 t1, Vector3 t2, Vector4 p0, Vector4 p1, Vector4 p2) : this(v0, v1, v2, w0, w1, w2, n0, n1, n2)
        {
            this.t0 = t0; this.t1 = t1; this.t2 = t2;
            this.p0 = p0; this.p1 = p1; this.p2 = p2;
        }
    }
}
