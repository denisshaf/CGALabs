using System.Drawing;
using System.Numerics;

namespace akg1my
{
    internal class World
    {
        public List<WorldObject> WorldObjects { get { return _worldObjects; } }

        private Camera _camera;
        private Light _light;
        private List<WorldObject> _worldObjects = new List<WorldObject>();

        public World(int windowWidth, int windowHeight)
        {

            var eye = new Vector3(0, 0, -5);
            var target = new Vector3(0, 0, 0);
            var up = new Vector3(0, 1, 0);

            _camera = new Camera(eye, target, up, 0.1f, 10000f, windowWidth, windowHeight, 70);
            _camera.Projection = Camera.ProjectionType.Perspective;

            var lightPosition = new Vector3(0, 0, -5);
            float lightIntensity = 1;
            _light = new LambertsLight(lightPosition, lightIntensity);
            _light.Color = Color.Red;
        }

        public Vector3 CalculateLight(Vector3 point, Vector3 normal)
        {
            return _light.CalculateLight(point, normal);
        }

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

        public List<Vector4> TransformVertecesToWorld(WorldObject worldObject)
        {
            var verteces = worldObject.Vertices;

            for (var i = 0; i < verteces.Count; i++)
            {
                verteces[i] = Vector4.Transform(verteces[i], GetWorldMatrix(worldObject));
            }

            return verteces;
        }
        public List<Vector4> TransformVertecesToView(WorldObject worldObject)
        {
            var verteces = worldObject.Vertices;

            for (var i = 0; i < verteces.Count; i++)
            {
                verteces[i] = Vector4.Transform(verteces[i], GetWorldMatrix(worldObject));
                verteces[i] = Vector4.Transform(verteces[i], Matrix4x4.Transpose(_camera.ViewMatrix));
            }

            return verteces;
        }

        public List<Vector4> TransformObjectsVerteces(WorldObject worldObject)
        {
            var verteces = worldObject.Vertices;

            for (var i = 0; i < verteces.Count; i++)
            {
                verteces[i] = Vector4.Transform(verteces[i], GetWorldMatrix(worldObject));
                verteces[i] = Vector4.Transform(verteces[i], Matrix4x4.Transpose(_camera.ViewMatrix));
                verteces[i] = Vector4.Transform(verteces[i], Matrix4x4.Transpose(_camera.ProjectionMatrix));
                verteces[i] = Vector4.Divide(verteces[i], verteces[i].W);
                verteces[i] = Vector4.Transform(verteces[i], Matrix4x4.Transpose(_camera.ViewportMatrix));
            }

            return verteces;
        }

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
