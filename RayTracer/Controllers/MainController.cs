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
    /// <summary>
    /// Основной контроллер
    /// </summary>
    public class MainController
    {
        private MainWindow _view;
        private Scene _scene;
        private RayTracerEngine _engine;
        private bool _isUpdatingUI = false;

        /// <summary>
        /// Сцена геометрических приметивов и источников света
        /// </summary>
        public Scene Scene { get { return _scene; } }

        public MainController(MainWindow view)
        {
            _view = view;
            _engine = new RayTracerEngine();
            _scene = new Scene();

            InitializeTestScene();

            _view.BtnRender.Click += OnRenderClicked;
        }

        public void InitializeTestScene()
        {
            _scene.MainCamera = new Camera("Камера", new Vector3(0, 0, 1.5f), new Vector3(0, 0, -1), new Vector3(0, 1, 0), 60f);

            var floorMaterial = new Material(new Vector3(0.5f, 0.5f, 0.5f), 0.1f);
            _scene.Objects.Add(new Sphere("Пол", new Vector3(0, -100.5f, -1), 100f, floorMaterial));

            var redMat = new Material(new Vector3(0.8f, 0.2f, 0.2f), 0.0f);
            _scene.Objects.Add(new Sphere("Красная сфера", new Vector3(0, 0, -1), 0.5f, redMat));

            _scene.Lights.Add(new DirectionalLight("Солнце", new Vector3(1, 1, 1), 1.0f, new Vector3(-1, -1, -1)));

            UpdateHierarchyView();
        }

        /// <summary>
        /// Синхронизирует список объектов Модели (Scene) с интерфейсом (ListBox)
        /// </summary>
        public void UpdateHierarchyView()
        {
            _view.SceneObjectsList.Items.Clear();

            _view.SceneObjectsList.Items.Add(_scene.MainCamera);

            foreach (var light in _scene.Lights)
                _view.SceneObjectsList.Items.Add(light);

            foreach (var obj in _scene.Objects)
                _view.SceneObjectsList.Items.Add(obj);
        }

        public void AddSphere()
        {
            string name = GetUniqueName("Новая сфера", isLight: false);
            _scene.Objects.Add(new Sphere(name, new Vector3(0, 0, -2), 0.5f, new Material(new Vector3(0.8f, 0.8f, 0.8f))));
            UpdateHierarchyView();
        }

        public void AddDirectionalLight()
        {
            string name = GetUniqueName("Новый свет", isLight: true);
            _scene.Lights.Add(new DirectionalLight(name, new Vector3(1, 1, 1), 1.0f, new Vector3(0, -1, 0)));
            UpdateHierarchyView();
        }

        public void CloneSelected()
        {
            var selected = _view.SceneObjectsList.SelectedItem;
            if (selected == null || selected is Camera) return;

            if (selected is SceneObject obj)
            {
                var clone = obj.Clone();
                clone.Name = GetUniqueName(obj.Name, isLight: false);
                _scene.Objects.Add(clone);
            }
            else if (selected is Light light)
            {
                var clone = light.Clone();
                clone.Name = GetUniqueName(light.Name, isLight: true);
                _scene.Lights.Add(clone);
            }

            UpdateHierarchyView();
        }

        public void DeleteSelected()
        {
            var selected = _view.SceneObjectsList.SelectedItem;
            if (selected == null || selected is Camera) return;

            if (selected is SceneObject obj)
                _scene.Objects.Remove(obj);
            else if (selected is Light light)
                _scene.Lights.Remove(light);

            UpdateHierarchyView();
        }

        /// <summary>
        /// Обработка клика по объекту в списке
        /// </summary>
        /// <param name="selectedItem">Выделенный объект</param>
        public void OnObjectSelected(object selectedItem)
        {
            if (selectedItem == null) return;

            _isUpdatingUI = true;

            var culture = System.Globalization.CultureInfo.InvariantCulture;

            if (selectedItem is Camera cam)
            {
                _view.PosX.Text = cam.Position.X.ToString("0.00", culture);
                _view.PosY.Text = cam.Position.Y.ToString("0.00", culture);
                _view.PosZ.Text = cam.Position.Z.ToString("0.00", culture);

                _view.SliderReflectivity.IsEnabled = false;
                _view.SliderLightIntensity.IsEnabled = false;
            }
            else if (selectedItem is Sphere sphere)
            {
                _view.PosX.Text = sphere.Center.X.ToString("0.00", culture);
                _view.PosY.Text = sphere.Center.Y.ToString("0.00", culture);
                _view.PosZ.Text = sphere.Center.Z.ToString("0.00", culture);

                _view.SliderReflectivity.IsEnabled = true;
                _view.SliderReflectivity.Value = sphere.Material.Reflectivity;
                _view.SliderLightIntensity.IsEnabled = false;
            }
            else if (selectedItem is DirectionalLight light)
            {
                _view.PosX.Text = light.Direction.X.ToString("0.00", culture);
                _view.PosY.Text = light.Direction.Y.ToString("0.00", culture);
                _view.PosZ.Text = light.Direction.Z.ToString("0.00", culture);

                _view.SliderReflectivity.IsEnabled = false;
                _view.SliderLightIntensity.IsEnabled = true;
                _view.SliderLightIntensity.Value = light.Intensity;
            }

            _isUpdatingUI = false;
        }

        /// <summary>
        /// Обработка изменения свойств объекта в интерфейсе
        /// </summary>
        public void OnPropertiesChanged()
        {
            if (_isUpdatingUI) return;

            var selectedItem = _view.SceneObjectsList.SelectedItem;
            if (selectedItem == null) return;

            var style = System.Globalization.NumberStyles.Any;
            var culture = System.Globalization.CultureInfo.InvariantCulture;

            float.TryParse(_view.PosX.Text.Replace(',', '.'), style, culture, out float x);
            float.TryParse(_view.PosY.Text.Replace(',', '.'), style, culture, out float y);
            float.TryParse(_view.PosZ.Text.Replace(',', '.'), style, culture, out float z);

            Vector3 newVector = new Vector3(x, y, z);

            if (selectedItem is Camera cam)
            {
                cam.Position = newVector;
            }
            else if (selectedItem is Sphere sphere)
            {
                sphere.Center = newVector;
                sphere.Material.Reflectivity = (float)_view.SliderReflectivity.Value;
            }
            else if (selectedItem is DirectionalLight light)
            {
                if (newVector.Length == 0) newVector = new Vector3(0, -1, 0);

                light.Direction = newVector;
                light.Intensity = (float)_view.SliderLightIntensity.Value;
            }
        }

        /// <summary>
        /// Генератор имени копий
        /// </summary>
        /// <returns>Уникальное имя</returns>
        private string GetUniqueName(string originalName, bool isLight)
        {
            string baseName = originalName;
            int lastSpace = originalName.LastIndexOf(" (");
            if (lastSpace > 0 && originalName.EndsWith(")"))
                baseName = originalName.Substring(0, lastSpace);

            int counter = 1;
            string newName = $"{baseName} ({counter})";

            bool NameExists(string n) => isLight ? _scene.Lights.Exists(l => l.Name == n) : _scene.Objects.Exists(o => o.Name == n);

            while (NameExists(newName))
            {
                counter++;
                newName = $"{baseName} ({counter})";
            }
            return newName;
        }

        /// <summary>
        /// Запуск рендера световой карты
        /// </summary>
        private async void OnRenderClicked(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(_view.ResX.Text, out int width) || !int.TryParse(_view.ResY.Text, out int height)) return;

            int traceDepth = (int)_view.SliderTraceDepth.Value;

            _view.BtnRender.IsEnabled = false;
            _view.StatusText.Text = $"Рендеринг ({width}x{height})... Пожалуйста, подождите.";

            _scene.MainCamera.CalculateVectors((float)width / height);

            Stopwatch sw = Stopwatch.StartNew();

            byte[] pixelData = await Task.Run(() => _engine.RenderView(_scene, _scene.MainCamera, width, height, traceDepth));

            sw.Stop();
            DisplayImage(pixelData, width, height);

            _view.BtnRender.IsEnabled = true;
            _view.StatusText.Text = "Готов к работе";
            _view.RenderTimeText.Text = $"Время рендеринга: {sw.ElapsedMilliseconds / 1000.0:F2} с";
        }

        /// <summary>
        /// Отображение световой карты
        /// </summary>
        /// <param name="pixelData">Массив пикселей</param>
        /// <param name="width">Ширина изображения</param>
        /// <param name="height">Высота изображения</param>
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
