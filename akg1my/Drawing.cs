using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public static unsafe void DrawLine(int x0, int y0, int x1, int y1, byte* data, int stride)
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
            int var1, var2;

            for (int x = x0; x <= x1; x++)
            {
                if (step)
                {
                    var1 = x;
                    var2 = y;
                }
                else
                {
                    var1 = y;
                    var2 = x;
                }
                byte* pixelPtr = data + var1 * stride + var2 * 3;
                *(pixelPtr++) = 255;
                *(pixelPtr++) = 255;
                *(pixelPtr) = 255;

                error -= dy;
                if (error < 0)
                {
                    y += ystep;
                    error += dx;
                }
            }
        }
    }
}
