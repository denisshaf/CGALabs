using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace akg1my.Light
{
    internal class PhongLight
    {
        public Vector3 Position {  get; set; }
        public float DiffusedIntensity { get; set; } = 1;
        public Color DiffusedColor { get; set; }
        public float BackgroundIntensity { get; set; } = 0.1f;
        public Color BackgroundColor { get; set; }
        public float MirrorIntensity { get; set; } = 1;
        public Color MirrorColor { get; set; }
        public PhongLight(Vector3 position) : this(position, Color.White, Color.White, Color.White) { }
        public PhongLight(Vector3 position, Color diffusedColor, Color backgroundColor, Color mirrorColor)
        {
            Position = position;
            DiffusedColor = diffusedColor;
            BackgroundColor = backgroundColor;
            MirrorColor = mirrorColor;
        }

        public Vector3 CalculateLight(Vector3 point, Vector3 normal, Vector3 eye, float shine)
        {
            Vector3 dir = Position - point;
            Vector3 look = eye - point;
            float intensity = 0.5f;

            float diffusedAngleCos = Vector3.Dot(dir, normal) / (dir.Length() * normal.Length());
            Vector3 diffusedLight = DiffusedIntensity * intensity * float.Max(0, diffusedAngleCos) * new Vector3(DiffusedColor.R, DiffusedColor.G, DiffusedColor.B) / 255f;

            Vector3 backgroundLight = BackgroundIntensity * intensity * new Vector3(BackgroundColor.R, BackgroundColor.G, BackgroundColor.B) / 255f;

            Vector3 reflectionVector = dir - 2 * diffusedAngleCos * normal;
            float mirrorAngleCos = float.Max(0, Vector3.Dot(reflectionVector, look) / (reflectionVector.Length() * look.Length()));
            Vector3 mirrorLight = MirrorIntensity * intensity * float.Pow(mirrorAngleCos, shine) * new Vector3(MirrorColor.R, MirrorColor.G, MirrorColor.B) / 255f;

            Vector3 result = diffusedLight + backgroundLight + mirrorLight;
            if(result.X >= 1)
                result.X = 1;
            if (result.Y >= 1)
                result.Y = 1;
            if (result.Z >= 1)
                result.Z = 1;

            return result;
        }

        public Vector3 CalculateLightWithSpecular(Vector3 point, Vector3 normal, Vector3 eye)
        {
            Vector3 l = Position - point;
            float s = 0.5f;
            Vector3 lightResult = new(0, 0, 0);
            lightResult = new Vector3(DiffusedColor.R, DiffusedColor.G, DiffusedColor.B) / 255f * BackgroundIntensity;
            float angle = Vector3.Dot(normal, l);

            if (angle > 0)
            {
                lightResult += DiffusedIntensity * new Vector3(DiffusedColor.R, DiffusedColor.G, DiffusedColor.B) / 255f * DiffusedIntensity * angle / (l.Length() * normal.Length());
            }
            Vector3 R = 2 * normal * angle - l;
            Vector3 V = eye - point;
            float r_dot_v = Vector3.Dot(R, V);
            if (r_dot_v > 0)
            {
                lightResult += MirrorIntensity * new Vector3(MirrorColor.R, MirrorColor.G, MirrorColor.B) / 255f * DiffusedIntensity * float.Pow(r_dot_v / (R.Length() * V.Length()), s);
            }



            return lightResult;
        }
    }
}
