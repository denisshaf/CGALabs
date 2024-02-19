using System.Drawing;
using System.Numerics;
using System.Security.Cryptography.Pkcs;

namespace akg1my
{
    internal class Drawer
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public int Stride { get; set; }

        public unsafe byte* Data {  get; set; }
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
            int ystep = (y0 < y1) ? 1 : -1;
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
                *(pixelPtr++) = color.B;
                *(pixelPtr++) = color.G;
                *(pixelPtr) = color.R;

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

            for (int i = (int)i0; i <= (int)i1; i++)
            {
                values.Add(d);
                d += slope;
            }
            return values;
        }

        public unsafe void RasterizeTriangle(Point v0, Point v1, Point v2, Vector3 z, Color color)
        {
            if (v1.Y < v0.Y) (v1, v0) = (v0, v1);
            if (v2.Y < v0.Y) (v2, v0) = (v0, v2);
            if (v2.Y < v1.Y) (v2, v1) = (v1, v2);

            int x0 = v0.X, x1 = v1.X, x2 = v2.X;
            int y0 = v0.Y, y1 = v1.Y, y2 = v2.Y;
            float z0 = z.X, z1 = z.Y, z2 = z.Z;

            /*if (y0 <= 0 || y2 >= Height || x0 <= 0 || x2 >= Width)
                return;*/

            var x01 = Interpolate(y0, x0, y1, x1);
            var x12 = Interpolate(y1, x1, y2, x2);
            var x02 = Interpolate(y0, x0, y2, x2);

            var z01 = Interpolate(y0, z0, y1, z1);
            var z12 = Interpolate(y1, z1, y2, z2);
            var z02 = Interpolate(y0, z0, y2, z2);

            x01.RemoveAt(x01.Count - 1);
            var x012 = new List<float>(x01.Count + x12.Count);
            x012.AddRange(x01);
            x012.AddRange(x12);

            z01.RemoveAt(z01.Count - 1);
            var z012 = new List<float>(z01.Count + z12.Count);
            z012.AddRange(x01);
            z012.AddRange(x12);

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

            for (int y = y0; y <= y2; y++)
            {
                int index = y - y0;

                var zSegment = Interpolate(xLeft[index], zLeft[index], xRight[index], zRight[index]);

                for (int x = (int)xLeft[index]; x <= (int)xRight[index]; x++)
                {
                    if (zSegment[x - (int)xLeft[index]] < ZBuffer[y * Width + x])
                    {
                        ZBuffer[y * Width + x] = zSegment[x - (int)xLeft[index]];

                        byte* pixelPtr = Data + y * Stride + x * 3;

                        *(pixelPtr++) = color.B;
                        *(pixelPtr++) = color.G;
                        *(pixelPtr) = color.R;
                    }
                }
            }
        }
    }
}
