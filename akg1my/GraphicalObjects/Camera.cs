using System.Numerics;

namespace akg1my.GraphicalObjects
{
    internal class Camera
    {
        public enum ProjectionType
        {
            Orthographic,
            Perspective
        }
        public Vector3 Eye
        {
            get
            {
                return _eye;
            }
            protected set
            {
                _eye = value;

                _radialDistance = _eye.Length();
                _polarAngle = float.Acos(_eye.Y / _radialDistance);
                _azimuthalAngle = float.Atan(_eye.Z / _eye.X); ;

                _viewMatrix = Matrix4x4.Transpose(Matrix4x4.CreateLookAt(_eye, _target, _up));
            }
        }
        public Vector3 Target
        {
            get
            {
                return _target;
            }
            set
            {
                _target = value;
                _viewMatrix = Matrix4x4.Transpose(Matrix4x4.CreateLookAt(_eye, _target, _up));
            }
        }
        public Vector3 Up
        {
            get
            {
                return _up;
            }
            set
            {
                _up = value;
                _viewMatrix = Matrix4x4.Transpose(Matrix4x4.CreateLookAt(_eye, _target, _up));
            }
        }
        public bool Moved { get; set; }
        public float ZFar
        {
            get
            {
                return _zFar;
            }
            set
            {
                _zFar = value;

                _orthographicProjectionMatrix.M33 = 1 / (_zNear - _zFar);
                _orthographicProjectionMatrix.M34 = _orthographicProjectionMatrix.M33 * _zNear;

                _perspectiveProjectionMatrix.M33 = _orthographicProjectionMatrix.M33 * _zFar;
                _perspectiveProjectionMatrix.M33 = _orthographicProjectionMatrix.M34 * _zFar;
            }
        }
        public float ZNear
        {
            get
            {
                return _zNear;
            }
            set
            {
                _zNear = value;

                _orthographicProjectionMatrix.M33 = 1 / (_zNear - _zFar);
                _orthographicProjectionMatrix.M34 = _orthographicProjectionMatrix.M33 * _zNear;

                _perspectiveProjectionMatrix.M11 = 2 * _zNear / _width;
                _perspectiveProjectionMatrix.M22 = _perspectiveProjectionMatrix.M11;
                _perspectiveProjectionMatrix.M33 = _orthographicProjectionMatrix.M33 * _zFar;
                _perspectiveProjectionMatrix.M33 = _orthographicProjectionMatrix.M34 * _zFar;
            }
        }
        public int Width
        {
            get
            {
                return _width;
            }
            set
            {
                _width = value;

                _orthographicProjectionMatrix.M11 = 2 / _width;

                _perspectiveProjectionMatrix.M11 = 1.0f / (AspectRatio * float.Tan(_fov / 2));

                _viewportMatrix.M11 = _width >> 1;
                _viewportMatrix.M14 = _width >> 1;
            }
        }
        public int Height
        {
            get
            {
                return _height;
            }
            set
            {
                _height = value;

                _orthographicProjectionMatrix.M22 = 2 / _height;

                _perspectiveProjectionMatrix.M11 = 1.0f / (AspectRatio * float.Tan(_fov / 2));

                _viewportMatrix.M22 = -_height >> 1;
                _viewportMatrix.M24 = _height >> 1;
            }
        }
        public float AspectRatio
        {
            get
            {
                return (float)_width / _height;
            }
        }

        public float FOV
        {
            get
            {
                return _fov * (180 / float.Pi);
            }
            set
            {
                _fov = value * float.Pi / 180;

                _perspectiveProjectionMatrix.M11 = 1.0f / (AspectRatio * float.Tan(_fov / 2));
                _perspectiveProjectionMatrix.M22 = 1.0f / float.Tan(_fov / 2);
            }
        }


        public ProjectionType Projection { get; set; }

        public Matrix4x4 ProjectionMatrix
        {
            get
            {
                return Projection == ProjectionType.Orthographic ?
                    _orthographicProjectionMatrix :
                    _perspectiveProjectionMatrix;
            }
        }

        public Matrix4x4 ViewMatrix
        {
            get
            {
                /*return _viewMatrix;*/

                Vector3 ZAxis = Vector3.Normalize(Vector3.Subtract(_eye, _target));
                Vector3 XAxis = Vector3.Normalize(Vector3.Cross(_up, ZAxis));
                Vector3 YAxis = Vector3.Normalize(Vector3.Cross(ZAxis, XAxis));


                return new Matrix4x4(XAxis.X, XAxis.Y, XAxis.Z, -Vector3.Dot(XAxis, _eye),
                                     YAxis.X, YAxis.Y, YAxis.Z, -Vector3.Dot(YAxis, _eye),
                                     ZAxis.X, ZAxis.Y, ZAxis.Z, -Vector3.Dot(ZAxis, _eye),
                                     0, 0, 0, 1.0f);
            }
        }

        public Matrix4x4 ViewportMatrix
        {
            get
            {
                return _viewportMatrix;
            }
        }

        public float RadialDistance
        {
            get
            {
                return _radialDistance;
            }
            set
            {
                _radialDistance = value;

                MoveEye();
                _viewMatrix = Matrix4x4.Transpose(Matrix4x4.CreateLookAt(_eye, _target, _up));
            }
        }
        private void MoveEye()
        {
            float sinPolarAngle = float.Sin(_polarAngle);
            float cosPolarAngle = float.Cos(_polarAngle);
            float sinAzimuthalAngle = float.Sin(_azimuthalAngle);
            float cosAzimuthalAngle = float.Cos(_azimuthalAngle);
            _eye = new Vector3(_radialDistance * sinPolarAngle * cosAzimuthalAngle,
                               _radialDistance * cosPolarAngle,
                               _radialDistance * sinPolarAngle * sinAzimuthalAngle);

        }

        public float PolarAngle
        {
            get
            {
                return _polarAngle;
            }
            set
            {
                _polarAngle = value;

                MoveEye();
                _viewMatrix = Matrix4x4.Transpose(Matrix4x4.CreateLookAt(_eye, _target, _up));
            }
        }
        public float AzimuthalAngle
        {
            get
            {
                return _azimuthalAngle;
            }
            set
            {
                _azimuthalAngle = value;

                MoveEye();
                _viewMatrix = Matrix4x4.Transpose(Matrix4x4.CreateLookAt(_eye, _target, _up));
            }
        }

        private Matrix4x4 _orthographicProjectionMatrix;
        private Matrix4x4 _perspectiveProjectionMatrix;
        private Matrix4x4 _viewMatrix, _viewportMatrix;
        private int _width, _height;
        private float _zFar, _zNear, _fov;
        private Vector3 _eye, _target, _up;
        private float _radialDistance, _polarAngle, _azimuthalAngle;

        public Camera(Vector3 position, Vector3 target, Vector3 up, float zNear, float zFar, int width, int height, float fov)
        {
            _eye = position;
            _target = target;
            _up = up;
            _zFar = zFar;
            _zNear = zNear;
            _width = width;
            _height = height;
            FOV = fov;

            if (_eye.Length() == 0)
            {
                _radialDistance = 0;
                _polarAngle = float.Pi / 2;
                _azimuthalAngle = float.Pi / 2;
            }
            else
            {
                _radialDistance = _eye.Length();
                _polarAngle = float.Acos(_eye.Y / _radialDistance);
                _azimuthalAngle = float.Atan2(_eye.Z, _eye.X);
            }

            _perspectiveProjectionMatrix = Matrix4x4.Transpose(Matrix4x4.CreatePerspectiveFieldOfView(_fov, AspectRatio, _zNear, _zFar));
            _orthographicProjectionMatrix = Matrix4x4.Transpose(Matrix4x4.CreateOrthographic(_width, _height, _zNear, _zFar));
            _viewMatrix = Matrix4x4.Transpose(Matrix4x4.CreateLookAt(_eye, _target, _up));
            _viewportMatrix = Matrix4x4.Transpose(Matrix4x4.CreateViewport(0, 0, _width, _height, 0, -1));

            Moved = true;
        }
    }
}
