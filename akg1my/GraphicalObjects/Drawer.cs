using akg1my.Parser;
using System.Drawing;
using System.Numerics;
using System.Runtime.CompilerServices;
using static System.Formats.Asn1.AsnWriter;

namespace akg1my.GraphicalObjects
{
    internal class Drawer
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public int Stride { get; set; }

        public unsafe byte* Data { get; set; }
        public float[] ZBuffer { get; set; }

        public unsafe Drawer(int width, int height)
        {
            Width = width;
            Height = height;
            ZBuffer = Enumerable.Repeat(1f, width * height).ToArray();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool PointInWindow(int x, int y)
        {
            /*if (x > 0 && y > 0 && x < width && y < height)
                Console.WriteLine($"{x}, {y}, {width}, {height}, {x > 0 && y > 0 && x < width && y < height}");*/
            return x > 0 && y > 0 && x < Width && y < Height;
        }

        public unsafe void DrawLine(int x0, int y0, int x1, int y1, Color color)
        {
            /*Console.WriteLine($"draw line from ({x0}, {y0}) to ({x1}, {y1})");*/

            bool step = Math.Abs(y1 - y0) > Math.Abs(x1 - x0);

            if (step)
            {
                (x0, y0) = (y0, x0);
                (x1, y1) = (y1, x1);
            }

            if (x0 > x1)
            {
                (x0, x1) = (x1, x0);
                (y0, y1) = (y1, y0);
            }

            int dx = x1 - x0;
            int dy = Math.Abs(y1 - y0);
            int error = dx / 2;
            int ystep = y0 < y1 ? 1 : -1;
            int y = y0;
            int row, col;

            for (int x = x0; x <= x1; x++)
            {
                if (step)
                {
                    row = x;
                    col = y;
                }
                else
                {
                    row = y;
                    col = x;
                }

                byte* pixelPtr = Data + row * Stride + col * 3;
                *pixelPtr++ = color.B;
                *pixelPtr++ = color.G;
                *pixelPtr = color.R;

                error -= dy;
                if (error < 0)
                {
                    y += ystep;
                    error += dx;
                }
            }
        }

        // i - independent value, d - dependent value
        protected List<float> Interpolate(float i0, float d0, float i1, float d1)
        {
            int roundedI0 = (int)float.Round(i0);
            int roundedI1 = (int)float.Round(i1);
            if (roundedI0 == roundedI1)
                return [d0];

            var values = new List<float>();
            float slope = (d1 - d0) / (roundedI1 - roundedI0);
            float d = d0;

            for (int i = roundedI0; i <= roundedI1; i++)
            {
                values.Add(d);
                d += slope;
            }
            return values;
        }

        protected List<Vector3> Interpolate(float i0, Vector3 d0, float i1, Vector3 d1)
        {
            int roundedI0 = (int)float.Round(i0);
            int roundedI1 = (int)float.Round(i1);
            if (roundedI0 == roundedI1)
                return [d0];

            var values = new List<Vector3>();
            Vector3 slope = (d1 - d0) / (roundedI1 - roundedI0);
            Vector3 d = d0;

            for (int i = roundedI0; i <= roundedI1; i++)
            {
                values.Add(d);
                d += slope;
            }
            return values;
        }

        private (List<float>, List<float>) InterpolateSides(float i0, float i1, float i2, float d0, float d1, float d2)
        {
            var d01 = Interpolate(i0, d0, i1, d1);
            var d12 = Interpolate(i1, d1, i2, d2);
            var d02 = Interpolate(i0, d0, i2, d2);

            d01.RemoveAt(d01.Count - 1);
            var d012 = new List<float>(d01.Count + d12.Count);
            d012.AddRange(d01);
            d012.AddRange(d12);

            return (d02, d012);
        }

        private (List<Vector3>, List<Vector3>) InterpolateSides(float i0, float i1, float i2, Vector3 d0, Vector3 d1, Vector3 d2)
        {
            var d01 = Interpolate(i0, d0, i1, d1);
            var d12 = Interpolate(i1, d1, i2, d2);
            var d02 = Interpolate(i0, d0, i2, d2);

            d01.RemoveAt(d01.Count - 1);
            var d012 = new List<Vector3>(d01.Count + d12.Count);
            d012.AddRange(d01);
            d012.AddRange(d12);

            return (d02, d012);
        }

        public unsafe void RasterizeTriangleFlat(Triangle triangle, Color baseColor, Vector3 faceNormal, Vector3 faceCenter, CalculateLightDelegate? calculateLight)
        {
            Vector3 v0 = triangle.v0;
            Vector3 v1 = triangle.v1;
            Vector3 v2 = triangle.v2;

            if (v1.Y < v0.Y) (v1, v0) = (v0, v1);
            if (v2.Y < v0.Y) (v2, v0) = (v0, v2);
            if (v2.Y < v1.Y) (v2, v1) = (v1, v2);

            int x0 = (int)float.Round(v0.X), x1 = (int)float.Round(v1.X), x2 = (int)float.Round(v2.X);
            int y0 = (int)float.Round(v0.Y), y1 = (int)float.Round(v1.Y), y2 = (int)float.Round(v2.Y);
            // float z0 = v0.Z, z1 = v1.Z, z2 = v2.Z;
            float invZ0 = 1 / v0.Z, invZ1 = 1 / v1.Z, invZ2 = 1 / v2.Z;

            var (x012, x02) = InterpolateSides(y0, y1, y2, x0, x1, x2);
            var (invZ012, invZ02) = InterpolateSides(y0, y1, y2, invZ0, invZ1, invZ2);

            int middle = x012.Count / 2;
            List<float> xLeft, xRight, zLeft, zRight;
            if (x02[middle] < x012[middle])
            {
                (xLeft, xRight) = (x02, x012);
                (zLeft, zRight) = (invZ02, invZ012);
            }
            else
            {
                (xLeft, xRight) = (x012, x02);
                (zLeft, zRight) = (invZ012, invZ02);
            }

            int oldY0 = y0;
            y0 = int.Max(0, y0);
            y2 = int.Min(Height - 1, y2);

            Vector3 lightVector = calculateLight?.Invoke(faceCenter, faceNormal) ?? Vector3.One;

            for (int y = y0; y <= y2; y++)
            {
                int index = y - oldY0;

                var zSegment = Interpolate(xLeft[index], zLeft[index], xRight[index], zRight[index]);

                x0 = int.Max(0, (int)float.Round(xLeft[index]));
                x2 = int.Min(Width - 1, (int)float.Round(xRight[index]));

                for (int x = x0; x <= x2; x++)
                {
                    if (zSegment[x - x0] > ZBuffer[y * Width + x])
                    {
                        ZBuffer[y * Width + x] = zSegment[x - x0];

                        byte* pixelPtr = Data + y * Stride + x * 3;

                        *pixelPtr++ = (byte)(baseColor.B * lightVector.X);
                        *pixelPtr++ = (byte)(baseColor.G * lightVector.Y);
                        *pixelPtr = (byte)(baseColor.R * lightVector.Z);
                    }
                }
            }
        }

        public unsafe void RasterizeTrianglePhong(Triangle triangle, Color baseColor, CalculateLightDelegate? calculateLight)
        {
            Vector3 v0 = triangle.v0, n0 = triangle.n0, w0 = triangle.w0;
            Vector3 v1 = triangle.v1, n1 = triangle.n1, w1 = triangle.w1;
            Vector3 v2 = triangle.v2, n2 = triangle.n2, w2 = triangle.w2;

            if (v1.Y < v0.Y)
            {
                (v1, v0) = (v0, v1);
                (n1, n0) = (n0, n1);
                (w1, w0) = (w0, w1);
            }
            if (v2.Y < v0.Y)
            {
                (v2, v0) = (v0, v2);
                (n2, n0) = (n0, n2);
                (w2, w0) = (w0, w2);
            }
            if (v2.Y < v1.Y)
            {
                (v2, v1) = (v1, v2);
                (n2, n1) = (n1, n2);
                (w2, w1) = (w1, w2);
            }

            int x0 = (int)float.Round(v0.X), x1 = (int)float.Round(v1.X), x2 = (int)float.Round(v2.X);
            int y0 = (int)float.Round(v0.Y), y1 = (int)float.Round(v1.Y), y2 = (int)float.Round(v2.Y);
            // float z0 = v0.Z, z1 = v1.Z, z2 = v2.Z;
            float invZ0 = 1 / v0.Z, invZ1 = 1 / v1.Z, invZ2 = 1 / v2.Z;

            var (x012, x02) = InterpolateSides(y0, y1, y2, x0, x1, x2);
            var (invZ012, invZ02) = InterpolateSides(y0, y1, y2, invZ0, invZ1, invZ2);
            var (n012, n02) = InterpolateSides(y0, y1, y2, n0, n1, n2);
            var (w012, w02) = InterpolateSides(y0, y1, y2, w0, w1, w2);

            int middle = x012.Count / 2;
            List<float> xLeft, xRight;
            List<float> zLeft, zRight;
            List<Vector3> nLeft, nRight;
            List<Vector3> wLeft, wRight;
            if (x02[middle] < x012[middle])
            {
                (xLeft, xRight) = (x02, x012);
                (zLeft, zRight) = (invZ02, invZ012);
                (nLeft, nRight) = (n02, n012);
                (wLeft, wRight) = (w02, w012);
            }
            else
            {
                (xLeft, xRight) = (x012, x02);
                (zLeft, zRight) = (invZ012, invZ02);
                (nLeft, nRight) = (n012, n02);
                (wLeft, wRight) = (w012, w02);
            }

            int oldY0 = y0;
            y0 = int.Max(0, y0);
            y2 = int.Min(Height - 1, y2);

            Vector3 lightVector;

            for (int y = y0; y <= y2; y++)
            {
                int index = y - oldY0;

                var zSegment = Interpolate(xLeft[index], zLeft[index], xRight[index], zRight[index]);
                var nSegment = Interpolate(xLeft[index], nLeft[index], xRight[index], nRight[index]);
                var wSegment = Interpolate(xLeft[index], wLeft[index], xRight[index], wRight[index]);

                x0 = int.Max(0, (int)float.Round(xLeft[index]));
                x2 = int.Min(Width - 1, (int)float.Round(xRight[index]));

                for (int x = x0; x <= x2; x++)
                {
                    if (zSegment[x - x0] > ZBuffer[y * Width + x])
                    {
                        ZBuffer[y * Width + x] = zSegment[x - x0];

                        lightVector = calculateLight?.Invoke(wSegment[x - x0], nSegment[x - x0]) ?? Vector3.One;

                        byte* pixelPtr = Data + y * Stride + x * 3;

                        *pixelPtr++ = (byte)(baseColor.B * lightVector.X);
                        *pixelPtr++ = (byte)(baseColor.G * lightVector.Y);
                        *pixelPtr = (byte)(baseColor.R * lightVector.Z);
                    }
                }
            }
        }
        public unsafe void RasterizeTriangleTexture(Triangle triangle, ImageData diffuseMap, ImageData? normalsMap, CalculateLightDelegate? calculateLight)
        {
            Vector3 v0 = triangle.v0, v1 = triangle.v1, v2 = triangle.v2;
            Vector3 n0 = triangle.n0, n1 = triangle.n1, n2 = triangle.n2;
            Vector3 w0 = triangle.w0, w1 = triangle.w1, w2 = triangle.w2;
            Vector3 t0 = triangle.t0, t1 = triangle.t1, t2 = triangle.t2;
            Vector4 p0 = triangle.p0, p1 = triangle.p1, p2 = triangle.p2;

            if (v1.Y < v0.Y)
            {
                (v1, v0) = (v0, v1);
                (n1, n0) = (n0, n1);
                (w1, w0) = (w0, w1);
                (t1, t0) = (t0, t1);
                (p1, p0) = (p0, p1);
            }
            if (v2.Y < v0.Y)
            {
                (v2, v0) = (v0, v2);
                (n2, n0) = (n0, n2);
                (w2, w0) = (w0, w2);
                (t2, t0) = (t0, t2);
                (p2, p0) = (p0, p2);
            }
            if (v2.Y < v1.Y)
            {
                (v2, v1) = (v1, v2);
                (n2, n1) = (n1, n2);
                (w2, w1) = (w1, w2);
                (t2, t1) = (t1, t2);
                (p2, p1) = (p1, p2);
            }

            int x0 = (int)float.Round(v0.X), x1 = (int)float.Round(v1.X), x2 = (int)float.Round(v2.X);
            int y0 = (int)float.Round(v0.Y), y1 = (int)float.Round(v1.Y), y2 = (int)float.Round(v2.Y);
            // float z0 = v0.Z, z1 = v1.Z, z2 = v2.Z;
            float invZ0 = 1 / v0.Z, invZ1 = 1 / v1.Z, invZ2 = 1 / v2.Z;
            float invZ0Proj = 1 / p0.Z, invZ1Proj = 1 / p1.Z, invZ2Proj = 1 / p2.Z;

            var (x012, x02) = InterpolateSides(y0, y1, y2, x0, x1, x2);
            var (invZ012, invZ02) = InterpolateSides(y0, y1, y2, invZ0, invZ1, invZ2);
            var (invZ012proj, invZ02proj) = InterpolateSides(y0, y1, y2, invZ0Proj, invZ1Proj, invZ2Proj);
            var (n012, n02) = InterpolateSides(y0, y1, y2, n0, n1, n2);
            var (w012, w02) = InterpolateSides(y0, y1, y2, w0, w1, w2);
            var (t012, t02) = InterpolateSides(y0, y1, y2, t0 * invZ0Proj, t1 * invZ1Proj, t2 * invZ2Proj);

            int middle = x012.Count / 2;
            List<float> xLeft, xRight;
            List<float> zLeft, zRight;
            List<float> zLeftProj, zRightProj;
            List<Vector3> nLeft, nRight;
            List<Vector3> wLeft, wRight;
            List<Vector3> tLeft, tRight;
            if (x02[middle] < x012[middle])
            {
                (xLeft, xRight) = (x02, x012);
                (zLeft, zRight) = (invZ02, invZ012);
                (zLeftProj, zRightProj) = (invZ02proj, invZ012proj);
                (nLeft, nRight) = (n02, n012);
                (wLeft, wRight) = (w02, w012);
                (tLeft, tRight) = (t02, t012);
            }
            else
            {
                (xLeft, xRight) = (x012, x02);
                (zLeft, zRight) = (invZ012, invZ02);
                (zLeftProj, zRightProj) = (invZ012proj, invZ02proj);
                (nLeft, nRight) = (n012, n02);
                (wLeft, wRight) = (w012, w02);
                (tLeft, tRight) = (t012, t02);
            }

            int oldY0 = y0;
            y0 = int.Max(0, y0);
            y2 = int.Min(Height - 1, y2);

            Vector3 lightVector;

            for (int y = y0; y <= y2; y++)
            {
                int index = y - oldY0;

                /*float xl = 848.5f, xr = 848.6f;
                Vector3 tl = new(0.4039695f, 0.9932775f, 0), tr = new(0.40411997f, 0.9940798f, 0);
                var i = Interpolate(xl, tl, xr, tr);*/

                var zSegment = Interpolate(xLeft[index], zLeft[index], xRight[index], zRight[index]);
                var zSegmentProj = Interpolate(xLeft[index], zLeftProj[index], xRight[index], zRightProj[index]);
                var nSegment = Interpolate(xLeft[index], nLeft[index], xRight[index], nRight[index]);
                var wSegment = Interpolate(xLeft[index], wLeft[index], xRight[index], wRight[index]);
                var tSegment = Interpolate(xLeft[index], tLeft[index], xRight[index], tRight[index]);

                x0 = int.Max(0, (int)float.Round(xLeft[index]));
                x2 = int.Min(Width - 1, (int)float.Round(xRight[index]));

                for (int x = x0; x <= x2; x++)
                {
                    if (zSegment[x - x0] > ZBuffer[y * Width + x])
                    {
                        ZBuffer[y * Width + x] = zSegment[x - x0];

                        if (normalsMap != null)
                        {
                            int normXInd = (int)float.Abs(tSegment[x - x0].X / zSegmentProj[x - x0] * (diffuseMap.Width - 1)) % diffuseMap.Width;
                            int normYInd = (int)float.Abs((1 - tSegment[x - x0].Y / zSegmentProj[x - x0]) * (diffuseMap.Height - 1)) % diffuseMap.Height;
                            int textureByteNorm = (int)((1 - tSegment[x - x0].Y / zSegmentProj[x - x0]) * normalsMap.Height) * normalsMap.Stride + (int)(tSegment[x - x0].X / zSegmentProj[x - x0] * normalsMap.Width) * normalsMap.ColorSize / 8;
                            Vector3 normal = new Vector3((normalsMap.MapData[textureByteNorm + 2] / 255.0f) * 2 - 1, (normalsMap.MapData[textureByteNorm + 1] / 255.0f) * 2 - 1, (normalsMap.MapData[textureByteNorm + 0] / 255.0f) * 2 - 1);
                            lightVector = calculateLight?.Invoke(wSegment[x - x0], normal) ?? Vector3.One;
                        }
                        else
                        {
                            lightVector = calculateLight?.Invoke(wSegment[x - x0], nSegment[x - x0]) ?? Vector3.One;
                        }

                        byte* pixelPtr = Data + y * Stride + x * 3;

                        int texXInd = (int)float.Abs(tSegment[x - x0].X / zSegmentProj[x - x0] * (diffuseMap.Width - 1)) % diffuseMap.Width;
                        int texYInd = (int)float.Abs((1 - tSegment[x - x0].Y / zSegmentProj[x - x0]) * (diffuseMap.Height - 1)) % diffuseMap.Height;
                        int texByteInd = texYInd * diffuseMap.Stride + texXInd * diffuseMap.ColorSize / 8;
                        Vector3 color = new(diffuseMap.MapData[texByteInd], diffuseMap.MapData[texByteInd + 1], diffuseMap.MapData[texByteInd + 2]);

                        *pixelPtr++ = (byte)(color.X * lightVector.X);
                        *pixelPtr++ = (byte)(color.Y * lightVector.Y);
                        *pixelPtr = (byte)(color.Z * lightVector.Z);
                    }
                }
            }
        }
    }
}
