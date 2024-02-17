using akg1my.Parser;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Point = System.Windows.Point;
using Color = System.Drawing.Color;

namespace akg1my
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private int _windowWidth;
        private int _windowHeight;
        private ObjParser _objParser;
        private World _world;

        private DispatcherTimer _timer;
        private TextBlock _textBlock;
        private int _frameCount;
        private Point _lastMousePosition;

        public MainWindow()
        {
            InitializeComponent();

            var parser = new ObjParser(@"D:\Study\АКГ\akg1my\objects\Shrek\Shrek.obj");
            var model = new WorldObject(parser.Vertices, parser.Faces, parser.VertexTextures, parser.VertexNormals);

            model.RotationInWorldSpace = Vector3.Zero;
            model.PositionInWorldSpace = Vector3.Zero;

            InitializeWindowComponents();

            _world = new World(_windowWidth, _windowHeight);
            _world.AddWorldObject(model);

            DrawFrame();
        }

        private void InitializeWindowComponents()
        {
            Application.Current.MainWindow.SizeChanged += MainWindow_Resize;
            PreviewMouseWheel += MainWindow_PreviewMouseWheel;
            MouseMove += MainWindow_MouseMove;
            MouseLeftButtonDown += MainWindow_MouseLeftButtonDown;


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


        async private void DrawFrame()
        {
            Vector4 vi, vj;

            await Console.Out.WriteLineAsync($"{_windowHeight}, {_windowWidth}");

            while (true)
            {
                WriteableBitmap writableBitmap = new WriteableBitmap(_windowWidth, _windowHeight, 96, 96, PixelFormats.Bgr24, null);
                Int32Rect rect = new Int32Rect(0, 0, _windowWidth, _windowHeight);
                IntPtr buffer = writableBitmap.BackBuffer;
                int stride = writableBitmap.BackBufferStride;
                writableBitmap.Lock();

                unsafe
                {
                    foreach (var obj in _world.WorldObjects)
                    {
                        var verteces = _world.TransformObjectsVerteces(obj);
                        var faces = obj.Faces;
                        byte* pixels = (byte*)buffer.ToPointer();
                        var edgeColor = Color.White;

                        foreach (var face in faces)
                        {
                            var vertexIds = face.VertexIds.ToList();

                            vi = verteces[vertexIds.Last() - 1];
                            vj = verteces[vertexIds[0] - 1];

                            /*Console.WriteLine($"draw line from ({(int)vi.X}, {(int)vi.Y}) to ({(int)vj.X}, {(int)vj.Y})");*/
                            /*Console.Out.WriteLineAsync($"{_windowHeight}, {_windowWidth}");*/

                            bool coordsInWindow = Drawing.PointInWindow((int)vi.X, (int)vi.Y, _windowWidth, _windowHeight) &&
                                Drawing.PointInWindow((int)vj.X, (int)vj.Y, _windowWidth, _windowHeight);
                            if (((int)vi.X > 0 && (int)vj.X > 0 &&
                                    (int)vi.Y > 0 && (int)vj.Y > 0 &&
                                    (int)vi.X < _windowWidth && (int)vj.X < _windowWidth &&
                                    (int)vi.Y < _windowHeight && (int)vj.Y < _windowHeight))
                                Drawing.DrawLine((int)vi.X, (int)vi.Y, (int)vj.X, (int)vj.Y, pixels, stride, edgeColor);
                            for (int i = 0; i < vertexIds.Count - 1; i++)
                            {
                                vi = verteces[vertexIds[i] - 1];
                                vj = verteces[vertexIds[i + 1] - 1];
                                coordsInWindow = Drawing.PointInWindow((int)vi.X, (int)vi.Y, _windowWidth, _windowHeight) &&
                                    Drawing.PointInWindow((int)vj.X, (int)vj.Y, _windowWidth, _windowHeight);
                                if (((int)vi.X > 0 && (int)vj.X > 0 &&
                                    (int)vi.Y > 0 && (int)vj.Y > 0 &&
                                    (int)vi.X < _windowWidth && (int)vj.X < _windowWidth &&
                                    (int)vi.Y < _windowHeight && (int)vj.Y < _windowHeight))
                                    Drawing.DrawLine((int)vi.X, (int)vi.Y, (int)vj.X, (int)vj.Y, pixels, stride, edgeColor);

                                /*Console.WriteLine($"draw line from ({(int)vi.X}, {(int)vi.Y}) to ({(int)vj.X}, {(int)vj.Y})");*/
                            }
                        }
                    }
                }
                writableBitmap.AddDirtyRect(rect);
                writableBitmap.Unlock();
                Image.Source = writableBitmap;
                _frameCount++;
                await Task.Delay(0);

               /* break;*/
            }
        }

        private void MainWindow_Resize(object sender, SizeChangedEventArgs e)
        {
            Image.Width = (int)e.NewSize.Width;
            Image.Height = (int)e.NewSize.Height;
            _windowWidth = (int)Width;
            _windowHeight = (int)Height;
            _world.Resize(_windowWidth, _windowHeight);
        }

        private void MainWindow_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            _world.Zoom(-e.Delta / 1000.0f);

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