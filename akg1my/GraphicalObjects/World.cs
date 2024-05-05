using System.Numerics;
using System.Runtime.CompilerServices;

namespace akg1my.GraphicalObjects
{
    public delegate Vector3 CalculateLightDelegate(Vector3 point, Vector3 normal);
    internal sealed class World
    {
        public List<WorldObject> WorldObjects { get { return _worldObjects; } }

        private Camera _camera;
        private List<Light.LambertsLight> _lightsLambert = new List<Light.LambertsLight>();
        private List<Light.PhongLight> _lightsPhong = new List<Light.PhongLight>();
        private List<WorldObject> _worldObjects = new List<WorldObject>();

        public World(int windowWidth, int windowHeight)
        {
            var eye = new Vector3(5, 5, 12);
            var target = new Vector3(0, 0, 0);
            var up = new Vector3(0, 1, 0);

            _camera = new Camera(eye, target, up, 0.1f, 10000f, windowWidth, windowHeight, 70);
            _camera.Projection = Camera.ProjectionType.Perspective;

            Light.PhongLight light1 = new Light.PhongLight(new(5, 3, 4));

            light1.DiffusedIntensity = 1f;
            light1.BackgroundIntensity = 0.3f;
            light1.MirrorIntensity = 1.0f;
            _lightsPhong.Add(light1);

            Light.LambertsLight light2 = new Light.LambertsLight(new(5, 2, -5));
            // _lightsLambert.Add(light2);
        }

        public Vector3 CalculateLight(Vector3 point, Vector3 normal)
        {
            Vector3 resultLight = Vector3.Zero;

            foreach (var light in _lightsLambert)
            {
                resultLight = ClipSum(resultLight, light.CalculateLight(point, normal), 1);
            }
            foreach (var light in _lightsPhong)
            {
                resultLight = ClipSum(resultLight, light.CalculateLightWithSpecular(point, normal, _camera.Eye), 1);
            }
            return resultLight;
        }

        private static Vector3 ClipSum(Vector3 v0, Vector3 v1, float clip)
        {
            Vector3 result = v0 + v1;
            if (result.X >= clip)
                result.X = clip;
            if (result.Y >= clip)
                result.Y = clip;
            if (result.Z >= clip)
                result.Z = clip;
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsVisible(Vector3 point, Vector3 normal)
        {
            Vector3 direction = _camera.Eye - point;
            float dotProd = Vector3.Dot(direction, normal);
            return dotProd >= 0;
        }

        public void AddWorldObject(WorldObject worldObject)
        {
            _worldObjects.Add(worldObject);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public List<Vector4> TransformModelToWorld(WorldObject worldObject)
        {
            var vertices = worldObject.Vertices;

            for (var i = 0; i < vertices.Count; i++)
            {
                vertices[i] = Vector4.Transform(vertices[i], GetWorldMatrix(worldObject));
            }

            return vertices;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public List<Vector4> TransformWorldToView(List<Vector4> vertices)
        {
            for (var i = 0; i < vertices.Count; i++)
            {
                vertices[i] = Vector4.Transform(vertices[i], Matrix4x4.Transpose(_camera.ViewMatrix));
            }

            return vertices;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public (List<Vector4>, List<bool>) TransformViewToClip(List<Vector4> vertices)
        {
            var isOut = new List<bool>(new bool[vertices.Count]);

            for (var i = 0; i < vertices.Count; i++)
            {
                vertices[i] = Vector4.Transform(vertices[i], Matrix4x4.Transpose(_camera.ProjectionMatrix));
                if (vertices[i].W < 0.05)
                {
                    isOut[i] = true;
                    continue;
                }
            }

            return (vertices, isOut);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public List<Vector4> TransformClipToProjection(List<Vector4> vertices, List<bool> isOut)
        {
            for (var i = 0; i < vertices.Count; i++)
            {
                if (!isOut[i])
                    vertices[i] = Vector4.Divide(vertices[i], vertices[i].W);
            }

            return vertices;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public List<Vector4> TransformProjectionToViewport(List<Vector4> vertices, List<bool> isOut)
        {
            for (var i = 0; i < vertices.Count; i++)
            {
                if (!isOut[i])
                    vertices[i] = Vector4.Transform(vertices[i], Matrix4x4.Transpose(_camera.ViewportMatrix));
            }

            return vertices;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Matrix4x4 GetWorldMatrix(WorldObject worldObject)
        {
            Matrix4x4 fromObjToWorldMatrix =
                    Matrix4x4.CreateScale(worldObject.ScaleInWorldSpace) *
                    Matrix4x4.CreateTranslation(worldObject.PositionInWorldSpace) *
                    Matrix4x4.CreateRotationX(worldObject.RotationInWorldSpace.X) *
                    Matrix4x4.CreateRotationY(worldObject.RotationInWorldSpace.Y) *
                    Matrix4x4.CreateRotationZ(worldObject.RotationInWorldSpace.Z);
            return fromObjToWorldMatrix;
        }

        public void Resize(int width, int height)
        {
            _camera.Width = width;
            _camera.Height = height;
        }
        public void Zoom(float delta)
        {
            _camera.RadialDistance += delta;

            if (_camera.RadialDistance <= 0)
                _camera.RadialDistance = 0.05f;
        }
        public void Rotate(float polarAngleDelta, float azimuthalAngleDelta)
        {
            if (_camera.Eye.Length() != 0)
            {
                _camera.PolarAngle += polarAngleDelta;
                _camera.AzimuthalAngle += azimuthalAngleDelta;
            }

            if (_camera.PolarAngle >= float.Pi)
                _camera.PolarAngle = float.Pi - 0.001f;
            if (_camera.PolarAngle <= 0)
                _camera.PolarAngle = 0.001f;
        }

        private void PrintMatrix(Matrix4x4 matrix)
        {
            for (int row = 0; row < 4; row++)
            {
                Console.WriteLine($"{matrix[row, 0]}, {matrix[row, 1]}, {matrix[row, 2]}, {matrix[row, 3]}");
            }
            Console.WriteLine();
        }
    }
}
