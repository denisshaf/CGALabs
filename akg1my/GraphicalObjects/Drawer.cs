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
            ZBuffer = Enumerable.Repeat(float.MaxValue, width * height).ToArray();
        }

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
            if (i0 == i1)
                return [d0];

            var values = new List<float>();
            float slope = (d1 - d0) / (i1 - i0);
            float d = d0;

            for (int i = (int)float.Round(i0); i <= (int)float.Round(i1); i++)
            {
                values.Add(d);
                d += slope;
            }
            return values;
        }

        protected List<Vector3> Interpolate(float i0, Vector3 d0, float i1, Vector3 d1)
        {
            if (i0 == i1)
                return [d0];

            var values = new List<Vector3>();
            Vector3 slope = (d1 - d0) / (i1 - i0);
            Vector3 d = d0;

            for (int i = (int)float.Round(i0); i <= (int)float.Round(i1); i++)
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
            float z0 = v0.Z, z1 = v1.Z, z2 = v2.Z;

            var (x012, x02) = InterpolateSides(y0, y1, y2, x0, x1, x2);
            var (z012, z02) = InterpolateSides(y0, y1, y2, z0, z1, z2);

            int middle = x012.Count / 2;
            List<float> xLeft, xRight, zLeft, zRight;
            if (x02[middle] < x012[middle])
            {
                (xLeft, xRight) = (x02, x012);
                (zLeft, zRight) = (z02, z012);
            }
            else
            {
                (xLeft, xRight) = (x012, x02);
                (zLeft, zRight) = (z012, z02);
            }

            int oldY0 = y0;
            y0 = int.Max(0, y0);
            y2 = int.Min(Height - 1, y2);

            Vector3 lightVector;

            for (int y = y0; y <= y2; y++)
            {
                int index = y - oldY0;

                var zSegment = Interpolate(xLeft[index], zLeft[index], xRight[index], zRight[index]);

                x0 = int.Max(0, (int)float.Round(xLeft[index]));
                x2 = int.Min(Width - 1, (int)float.Round(xRight[index]));

                for (int x = x0; x <= x2; x++)
                {
                    if (zSegment[x - x0] < ZBuffer[y * Width + x])
                    {
                        ZBuffer[y * Width + x] = zSegment[x - x0];

                        lightVector = calculateLight?.Invoke(faceCenter, faceNormal) ?? Vector3.One;

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
            float z0 = v0.Z, z1 = v1.Z, z2 = v2.Z;

            var (x012, x02) = InterpolateSides(y0, y1, y2, x0, x1, x2);
            var (z012, z02) = InterpolateSides(y0, y1, y2, z0, z1, z2);
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
                (zLeft, zRight) = (z02, z012);
                (nLeft, nRight) = (n02, n012);
                (wLeft, wRight) = (w02, w012);
            }
            else
            {
                (xLeft, xRight) = (x012, x02);
                (zLeft, zRight) = (z012, z02);
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
                    if (zSegment[x - x0] < ZBuffer[y * Width + x])
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
    }
}
