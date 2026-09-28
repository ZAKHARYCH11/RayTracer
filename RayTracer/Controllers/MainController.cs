using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using OpenTK.Mathematics;
using RayTracer.Models;
using RayTracer.Views;

namespace RayTracer.Controllers
{
    public class MainController
    {
        private MainWindow _view;
        private Scene _scene;
        private RayTracerEngine _engine;

        public MainController(MainWindow view)
        {
            _view = view;
            _engine = new RayTracerEngine();
            _scene = new Scene();

            InitializeTestScene();

            _view.BtnRender.Click += OnRenderClicked;
        }

        private void InitializeTestScene()
        {
            var floorMaterial = new Material(new Vector3(0.5f, 0.5f, 0.5f), 0.1f);
            _scene.Objects.Add(new Sphere(new Vector3(0, -100.5f, -1), 100f, floorMaterial));

            var redMat = new Material(new Vector3(0.8f, 0.2f, 0.2f), 0.0f);
            _scene.Objects.Add(new Sphere(new Vector3(0, 0, -1), 0.5f, redMat));

            var mirrorMat = new Material(new Vector3(0.9f, 0.9f, 0.9f), 0.8f);
            _scene.Objects.Add(new Sphere(new Vector3(-1.0f, 0, -1), 0.5f, mirrorMat));

            var blueMat = new Material(new Vector3(0.2f, 0.2f, 0.8f), 0.3f);
            _scene.Objects.Add(new Sphere(new Vector3(1.0f, 0, -1), 0.5f, blueMat));

            _scene.Lights.Add(new DirectionalLight(new Vector3(1, 1, 1), 1.0f, new Vector3(-1, -1, -1)));
        }

        private async void OnRenderClicked(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(_view.ResX.Text, out int width) || !int.TryParse(_view.ResY.Text, out int height))
            {
                MessageBox.Show("Неверный формат разрешения!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            int traceDepth = (int)_view.SliderTraceDepth.Value;

            _view.BtnRender.IsEnabled = false;
            _view.StatusText.Text = $"Рендеринг ({width}x{height})... Пожалуйста, подождите.";

            Camera camera = new Camera(
                position: new Vector3(0, 0, 1.5f),
                lookAt: new Vector3(0, 0, -1),
                up: new Vector3(0, 1, 0),
                fovDegrees: 60f,
                aspectRatio: (float)width / height);

            Stopwatch sw = Stopwatch.StartNew();

            byte[] pixelData = await Task.Run(() => _engine.RenderView(_scene, camera, width, height, traceDepth));

            sw.Stop();

            DisplayImage(pixelData, width, height);

            _view.BtnRender.IsEnabled = true;
            _view.StatusText.Text = "Готов к работе";
            _view.RenderTimeText.Text = $"Время рендеринга: {sw.ElapsedMilliseconds / 1000.0:F2} с";
        }

        private void DisplayImage(byte[] pixelData, int width, int height)
        {
            int stride = width * 3;
            BitmapSource bitmap = BitmapSource.Create(
                width, height, 96, 96,
                PixelFormats.Rgb24, null, pixelData, stride);

            _view.LightmapPreviewImage.Source = bitmap;
        }
    }
}
