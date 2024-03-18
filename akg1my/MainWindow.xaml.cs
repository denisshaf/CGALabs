﻿using akg1my.Parser;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Color = System.Drawing.Color;
using DoublePoint = System.Windows.Point;

namespace akg1my
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private int _windowWidth;
        private int _windowHeight;
        private World _world;
        private Drawer _drawer;

        private DispatcherTimer _timer;
        private TextBlock _textBlock;
        private int _frameCount;
        private DoublePoint _lastMousePosition;
        private bool _rasterizationOn, _backFacesOn, _lightOn;
        private int _superSamplingCoef = 2;

        public MainWindow()
        {
            InitializeComponent();
            var parser = new ObjParser(@"D:\Study\АКГ\akg1my\objects\knight\knight.obj");
            var model = new WorldObject(parser.Vertices, parser.Faces, parser.VertexTextures, parser.VertexNormals);
            parser = new ObjParser(@"D:\Study\АКГ\akg1my\objects\sphere\sphere.obj");
            var sphere = new WorldObject(parser.Vertices, parser.Faces, parser.VertexTextures, parser.VertexNormals);
            parser = new ObjParser(@"D:\Study\АКГ\akg1my\objects\XAxis.obj");
            var xAxis = new WorldObject(parser.Vertices, parser.Faces, parser.VertexTextures, parser.VertexNormals);
            parser = new ObjParser(@"D:\Study\АКГ\akg1my\objects\YAxis.obj");
            var yAxis = new WorldObject(parser.Vertices, parser.Faces, parser.VertexTextures, parser.VertexNormals);
            parser = new ObjParser(@"D:\Study\АКГ\akg1my\objects\ZAxis.obj");
            var zAxis = new WorldObject(parser.Vertices, parser.Faces, parser.VertexTextures, parser.VertexNormals);

            xAxis.Color = Color.Red;
            yAxis.Color = Color.Green;
            zAxis.Color = Color.Blue;
            xAxis.IsAlwaysVisible = true;
            yAxis.IsAlwaysVisible = true;
            zAxis.IsAlwaysVisible = true;

            model.RotationInWorldSpace = Vector3.Zero;
            model.PositionInWorldSpace = new Vector3(0, 0, 0);
            model.ScaleInWorldSpace = new Vector3(1, 1, 1);
            model.Color = Color.White;

            sphere.PositionInWorldSpace = new Vector3(0, 0, -6);
            sphere.ScaleInWorldSpace = new Vector3(0.01f, 0.01f, 0.01f);

            InitializeWindowComponents();

            _world = new World(_windowWidth, _windowHeight);
            _world.AddWorldObject(model);
            _world.AddWorldObject(xAxis);
            _world.AddWorldObject(yAxis);
            _world.AddWorldObject(zAxis);
            _world.AddWorldObject(sphere);
            _drawer = new Drawer(_windowWidth, _windowHeight);
            _rasterizationOn = false;
            _backFacesOn = false;
            _lightOn = false;

            DrawFrame();
        }

        private void InitializeWindowComponents()
        {
            SizeChanged += MainWindow_Resize;
            PreviewMouseWheel += MainWindow_PreviewMouseWheel;
            MouseMove += MainWindow_MouseMove;
            MouseLeftButtonDown += MainWindow_MouseLeftButtonDown;
            PreviewKeyDown += MainWindow_PreviewKeyDown;

            _windowHeight = (int)Height;
            _windowWidth = (int)Width;
            Image = new Image();
            Image.Width = Width;
            Image.Height = Height;

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += Timer_Tick;

            _timer.Start();
            var grid = new Grid();
            Image.Stretch = Stretch.Fill;

            _textBlock = new TextBlock();
            _textBlock.HorizontalAlignment = HorizontalAlignment.Left;
            _textBlock.VerticalAlignment = VerticalAlignment.Bottom;
            _textBlock.FontSize = 12;
            _textBlock.Foreground = Brushes.White;

            grid.Children.Add(Image);
            grid.Children.Add(_textBlock);

            Grid.SetRow(Image, 0);
            Grid.SetColumn(Image, 0);
            Grid.SetZIndex(Image, 0);

            Grid.SetRow(_textBlock, 0);
            Grid.SetColumn(_textBlock, 0);
            Grid.SetZIndex(_textBlock, 1);

            Content = grid;

        }

        private void Resize(object sender, SizeChangedEventArgs e)
        {
            Image.Width = (int)e.NewSize.Width;
            Image.Height = (int)e.NewSize.Height;

            _windowHeight = (int)e.NewSize.Height;
            _windowWidth = (int)e.NewSize.Width;
        }


        private async void DrawFrame()
        {
            Vector4 vi, vj;

            while (true)
            {
                int dpi = 96;
                WriteableBitmap writableBitmap = new WriteableBitmap(_windowWidth, _windowHeight, dpi, dpi, PixelFormats.Bgr24, null);
                writableBitmap.Lock();

                Int32Rect rect = new Int32Rect(0, 0, _windowWidth, _windowHeight);
                IntPtr buffer = writableBitmap.BackBuffer;
                int stride = writableBitmap.BackBufferStride;
                Array.Fill(_drawer.ZBuffer, float.MaxValue);

                unsafe
                {
                    foreach (var obj in _world.WorldObjects)
                    {
                        var viewportVerteces = _world.TransformObjectsVerteces(obj);
                        var worldVerteces = _world.TransformVertecesToWorld(obj);
                        var normals = obj.VertexNormals;
                        var faces = obj.Faces;
                        byte* pixels = (byte*)buffer.ToPointer();
                        Console.Out.WriteLineAsync($"{buffer}");
                        var edgeColor = obj.Color;

                        _drawer.Data = pixels;
                        _drawer.Stride = stride;

                        // var colors = new List<Color>() { Color.Red, Color.Red, Color.Blue, Color.Blue, Color.Green, Color.Green, Color.Purple, Color.Purple, Color.Yellow, Color.Yellow, Color.LightBlue, Color.LightBlue }.GetEnumerator();
                        // var colors = new List<Color>() { Color.Red, Color.Blue }.GetEnumerator();

                        foreach (var face in faces)
                        /*Parallel.ForEach(faces, face =>*/
                        {
                            var vertexIds = face.VertexIds.ToList();

                            Vector3 faceNormal = CalculateFaceNormal(face, normals, worldVerteces);
                            Vector3 faceCenter = CalculateFaceCenter(face, worldVerteces);

                            bool coordsInWindow;

                            // colors.MoveNext();
                            if (_rasterizationOn)
                            {
                                if (obj.IsAlwaysVisible || _backFacesOn ? true : _world.IsVisible(faceCenter, faceNormal))
                                {
                                    Vector3 p1, p2, p3;
                                    Color faceColor = obj.Color;
                                    Vector3 light;

                                    if (_lightOn && !(float.IsNaN(faceNormal.X) ||
                                            float.IsNaN(faceNormal.Y) ||
                                            float.IsNaN(faceNormal.Z)))
                                    {
                                        light = _world.CalculateLight(faceCenter, faceNormal);
                                        faceColor = Color.FromArgb(1, 
                                            (int)float.Round(faceColor.R * light.X), 
                                            (int)float.Round(faceColor.G * light.Y), 
                                            (int)float.Round(faceColor.B * light.Z));
                                    }

                                    p1 = new Vector3(viewportVerteces[vertexIds[0] - 1].X, 
                                        viewportVerteces[vertexIds[0] - 1].Y,
                                        viewportVerteces[vertexIds[0] - 1].Z);
                                    for (int i = 1; i < vertexIds.Count - 1; i++)
                                    {
                                        p2 = new Vector3(viewportVerteces[vertexIds[i] - 1].X,
                                            viewportVerteces[vertexIds[i] - 1].Y,
                                            viewportVerteces[vertexIds[i] - 1].Z);
                                        p3 = new Vector3(viewportVerteces[vertexIds[i + 1] - 1].X,
                                            viewportVerteces[vertexIds[i + 1] - 1].Y,
                                            viewportVerteces[vertexIds[i + 1] - 1].Z);

                                        if (p1 != p2 && p1 != p3 && p2 != p3)
                                        {
                                            _drawer.RasterizeTriangle(p1, p2, p3, faceColor);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (obj.IsAlwaysVisible || _backFacesOn ? true : _world.IsVisible(faceCenter, faceNormal))
                                {
                                    vi = viewportVerteces[vertexIds.Last() - 1];
                                    vj = viewportVerteces[vertexIds[0] - 1];

                                    /*Console.WriteLine($"draw line from ({(int)vi.X}, {(int)vi.Y}) to ({(int)vj.X}, {(int)vj.Y})");*/
                                    /*Console.Out.WriteLineAsync($"{_windowHeight}, {_windowWidth}");*/

                                    coordsInWindow = _drawer.PointInWindow((int)float.Round(vi.X), (int)float.Round(vi.Y)) &&
                                        _drawer.PointInWindow((int)float.Round(vj.X), (int)float.Round(vj.Y));
                                    if (coordsInWindow)
                                        _drawer.DrawLine((int)float.Round(vi.X), (int)float.Round(vi.Y), (int)float.Round(vj.X), (int)float.Round(vj.Y), edgeColor);
                                    for (int i = 0; i < vertexIds.Count - 1; i++)
                                    {
                                        vi = viewportVerteces[vertexIds[i] - 1];
                                        vj = viewportVerteces[vertexIds[i + 1] - 1];
                                        coordsInWindow = _drawer.PointInWindow((int)float.Round(vi.X), (int)float.Round(vi.Y)) &&
                                            _drawer.PointInWindow((int)float.Round(vj.X), (int)float.Round(vj.Y));
                                        if (coordsInWindow)
                                            _drawer.DrawLine((int)float.Round(vi.X), (int)float.Round(vi.Y), (int)float.Round(vj.X), (int)float.Round(vj.Y), edgeColor);

                                        /*Console.WriteLine($"draw line from ({(int)vi.X}, {(int)vi.Y}) to ({(int)vj.X}, {(int)vj.Y})");*/
                                    }
                                }
                            }
                        }/*);*/
                    }
                }
                writableBitmap.AddDirtyRect(rect);
                writableBitmap.Unlock();
                
                Image.Source = writableBitmap;
                _frameCount++;

                
                await Task.Delay(1);

                /* break;*/
            }
        }

        private Vector3 CalculateFaceNormal(Face face, List<Vector3> normals, List<Vector4> verteces)
        {
            List<int> vertexIds = face.VertexIds.ToList();
            Vector3 faceNormal = Vector3.Zero;

            if (face.NormalIds.Count() > 0)
            {
                foreach (int index in face.NormalIds)
                {
                    faceNormal += normals[index - 1];
                }
                faceNormal = Vector3.Normalize(faceNormal);
            }
            else
            {
                var a = new Vector3(verteces[vertexIds[0] - 1].X,
                    verteces[vertexIds[0] - 1].Y,
                    verteces[vertexIds[0] - 1].Z);
                var b = new Vector3(verteces[vertexIds[1] - 1].X,
                    verteces[vertexIds[1] - 1].Y,
                    verteces[vertexIds[1] - 1].Z);
                var c = new Vector3(verteces[vertexIds[^1] - 1].X,
                    verteces[vertexIds[^1] - 1].Y,
                    verteces[vertexIds[^1] - 1].Z);
                faceNormal = Vector3.Normalize(Vector3.Cross(b - a, c - a));
            }
            return faceNormal;
        }
        private Vector3 CalculateFaceCenter(Face face, List<Vector4> verteces)
        {
            Vector4 faceCenter = Vector4.Zero;
            List<int> vertexIds = face.VertexIds.ToList();

            for (int i = 0; i < vertexIds.Count; i++)
            {
                faceCenter += verteces[vertexIds[i] - 1];
            }
            faceCenter /= vertexIds.Count;
            var faceCenter3D = new Vector3(faceCenter.X, faceCenter.Y, faceCenter.Z);

            return faceCenter3D;
        }
        private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.R:
                    _rasterizationOn = !_rasterizationOn;
                    break;
                case Key.B:
                    _backFacesOn = !_backFacesOn;
                    break;
                case Key.L:
                    _lightOn = !_lightOn;
                    break;
                default:
                    break;
            }
        }

        private void MainWindow_Resize(object sender, SizeChangedEventArgs e)
        {
            Image.Width = (int)e.NewSize.Width;
            Image.Height = (int)e.NewSize.Height;
            _windowWidth = (int)Width * _superSamplingCoef;
            _windowHeight = (int)Height * _superSamplingCoef;
            _drawer.Width = _windowWidth;
            _drawer.Height = _windowHeight;
            _drawer.ZBuffer = Enumerable.Repeat(float.MaxValue, _windowWidth * _windowHeight).ToArray(); ;
            _world.Resize(_windowWidth, _windowHeight);
        }

        private void MainWindow_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            _world.Zoom(-e.Delta / 100.0f);

            e.Handled = true;
        }

        private void MainWindow_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var currentPosition = e.GetPosition(this);
                float xOffset = (float)(currentPosition.X - _lastMousePosition.X);
                float yOffset = (float)(_lastMousePosition.Y - currentPosition.Y);

                _world.Rotate(yOffset * 0.005f, xOffset * 0.005f);
                _lastMousePosition = currentPosition;
            }
        }
        private void MainWindow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) =>
            _lastMousePosition = e.GetPosition(this);

        private void Timer_Tick(object? sender, EventArgs e)
        {
            _textBlock.Text = $"{_frameCount} fps";
            _frameCount = 0;
        }
    }
}