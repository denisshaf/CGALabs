using System.Drawing;
using System.Numerics;
using System.Security.Cryptography.Pkcs;

namespace akg1my
{
    internal static class Drawing
    {
        public static bool PointInWindow(int x, int y, int width, int height)
        {
            /*if (x > 0 && y > 0 && x < width && y < height)
                Console.WriteLine($"{x}, {y}, {width}, {height}, {x > 0 && y > 0 && x < width && y < height}");*/
            return x > 0 && y > 0 && x < width && y < height;
        }

        public static unsafe void DrawLine(int x0, int y0, int x1, int y1, byte* data, int stride, Color color)
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
                byte* pixelPtr = data + row * stride + col * 3;
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
        public static List<float> Interpolate(float i0, float d0, float i1, float d1)
        {
            var values = new List<float>();
            float slope = (d1 - d0) / (i1 - i0);
            var d = d0;

            for (float i = i0; i <= i1; i += slope)
            {
                values.Add(d);
                d += slope;
            }
            return values;
        }

        public static void RasterizeTriangle(Vector2 v0, Vector2 v1, Vector2 v2, Color color)
        {
            if (v1.Y < v0.Y) (v1, v0) = (v0, v1);
            if (v2.Y < v0.Y) (v2, v0) = (v0, v2);
            if (v2.Y < v1.Y) (v2, v1) = (v1, v2);

            var x01 = Interpolate(v0.Y, v0.X, v1.Y, v1.X);
            var x12 = Interpolate(v1.Y, v1.X, v2.Y, v2.X);
            var x02 = Interpolate(v0.Y, v0.X, v2.Y, v2.X);

            x01.RemoveAt(x01.Count - 1);
            var x012 = new List<float>(x01.Count + x12.Count);
            x012.AddRange(x01);
            x012.AddRange(x12);

            int middle = x012.Count / 2;
            List<float> x_left, x_right;
            if (x02[middle] < x012[middle])
            {
                x_left = x02;
                x_right = x012;
            }
            else
            {
                x_left = x012;
                x_right = x02;
            }

            for (int y = (int)v0.Y; y <= v2.Y; y++)
            {
                for (int x = x_left[y - (int)v0.Y])
            }
        }
    }
}
