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

        public void AddCube()
        {
            string name = GetUniqueName("Новый куб", isLight: false);
            _scene.Objects.Add(new Cube(name, new Vector3(0, 0, -2), 1.0f, new Material(new Vector3(0.3f, 0.8f, 0.3f))));
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

        public void SelectColor()
        {
            var selectedItem = _view.SceneObjectsList.SelectedItem;

            if (selectedItem is SceneObject sceneObj)
            {
                var dialog = new System.Windows.Forms.ColorDialog();

                Vector3 currColor = sceneObj.Material.Color;
                dialog.Color = System.Drawing.Color.FromArgb(
                    255,
                    (int)(currColor.X * 255),
                    (int)(currColor.Y * 255),
                    (int)(currColor.Z * 255)
                );

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    sceneObj.Material.Color = new Vector3(
                        dialog.Color.R / 255f,
                        dialog.Color.G / 255f,
                        dialog.Color.B / 255f
                    );

                    UpdateColorButtonUI(sceneObj.Material.Color);
                }
            }
        }

        public void SaveScene()
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Сохранить сцену",
                Filter = "RayTracer Scene (*.json)|*.json",
                DefaultExt = ".json"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    SceneSerializer.SaveScene(_scene, dialog.FileName);
                    System.Windows.MessageBox.Show("Сцена успешно сохранена!", "Сохранение", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Ошибка при сохранении:\n{ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public void LoadScene()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Открыть сцену",
                Filter = "RayTracer Scene (*.json)|*.json"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    Scene loadedScene = SceneSerializer.LoadScene(dialog.FileName);

                    if (loadedScene != null)
                    {
                        _scene = loadedScene;
                        UpdateHierarchyView();
                        _view.SceneObjectsList.SelectedItem = null;

                        System.Windows.MessageBox.Show("Сцена успешно загружена!", "Загрузка", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Ошибка при загрузке:\n{ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
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

                _view.BtnSelectColor.IsEnabled = false;
                _view.BtnSelectColor.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(50, 50, 50));
                _view.BtnSelectColor.Content = "Не применимо";
            }
            else if (selectedItem is SceneObject obj)
            {
                if(obj is Sphere sphere)
                {
                    _view.TxtCubeSide.Visibility = Visibility.Collapsed;
                    _view.Side.Visibility = Visibility.Collapsed;
                    _view.TxtSphereRadius.Visibility = Visibility.Visible;
                    _view.Radius.Visibility = Visibility.Visible; 
                    _view.Radius.Text = sphere.Radius.ToString("0.00", culture);
                }
                else if(obj is Cube cube)
                {
                    _view.TxtSphereRadius.Visibility = Visibility.Collapsed;
                    _view.Radius.Visibility = Visibility.Collapsed;
                    _view.TxtCubeSide.Visibility = Visibility.Visible;
                    _view.Side.Visibility = Visibility.Visible;
                    _view.Side.Text = cube.Side.ToString("0.00", culture);
                }

                _view.PosX.Text = obj.Center.X.ToString("0.00", culture);
                _view.PosY.Text = obj.Center.Y.ToString("0.00", culture);
                _view.PosZ.Text = obj.Center.Z.ToString("0.00", culture);

                _view.SliderReflectivity.IsEnabled = true;
                _view.SliderReflectivity.Value = obj.Material.Reflectivity;
                _view.SliderLightIntensity.IsEnabled = false;

                _view.Reflectivity.Text = obj.Material.Reflectivity.ToString("0.00", culture);

                _view.BtnSelectColor.IsEnabled = true;
                UpdateColorButtonUI(obj.Material.Color);
            }
            else if (selectedItem is DirectionalLight light)
            {
                _view.PosX.Text = light.Direction.X.ToString("0.00", culture);
                _view.PosY.Text = light.Direction.Y.ToString("0.00", culture);
                _view.PosZ.Text = light.Direction.Z.ToString("0.00", culture);

                _view.SliderReflectivity.IsEnabled = false;
                _view.SliderLightIntensity.IsEnabled = true;
                _view.SliderLightIntensity.Value = light.Intensity;

                _view.BtnSelectColor.IsEnabled = false;
                _view.BtnSelectColor.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(50, 50, 50));
                _view.BtnSelectColor.Content = "Не применимо";
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

            float.TryParse(_view.Radius.Text.Replace(',', '.'), style, culture,out float radius);
            float.TryParse(_view.Side.Text.Replace(',', '.'), style, culture, out float side);

            float.TryParse(_view.Reflectivity.Text.Replace(',', '.'), style, culture, out float reflectiviti);
            reflectiviti = Math.Clamp(reflectiviti, 0f, 1f);

            Vector3 newVector = new Vector3(x, y, z);

            if (selectedItem is Camera cam)
            {
                cam.Position = newVector;
            }
            else if (selectedItem is SceneObject obj)
            {
                obj.Center = newVector;
                if(obj is Sphere sphere)
                    sphere.Radius = radius;

                if(obj is Cube cube)
                    cube.Side = side;

                if(reflectiviti != obj.Material.Reflectivity)
                {
                    obj.Material.Reflectivity = reflectiviti;
                    _view.SliderReflectivity.Value = reflectiviti;
                }
                else
                {
                    obj.Material.Reflectivity = (float)_view.SliderReflectivity.Value;
                    _view.Reflectivity.Text = obj.Material.Reflectivity.ToString();
                }
                
            }
            else if (selectedItem is DirectionalLight light)
            {
                if (newVector.Length == 0) newVector = new Vector3(0, -1, 0);

                light.Direction = newVector;
                light.Intensity = (float)_view.SliderLightIntensity.Value;
            }
        }

        /// <summary>
        /// Пытается переименовать объект. Возвращает false, если имя уже занято.
        /// </summary>
        public bool TryRenameObject(object target, string newName)
        {
            if (target == null || target is Camera || string.IsNullOrWhiteSpace(newName)) return false;

            string currentName = target.ToString();
            if (currentName == newName) return true;

            bool isLight = target is Light;
            bool nameExists = isLight
                ? _scene.Lights.Exists(l => l.Name == newName)
                : _scene.Objects.Exists(o => o.Name == newName);

            if (nameExists)
            {
                return false;
            }

            if (target is SceneObject obj)
                obj.Name = newName;
            else if (target is Light light)
                light.Name = newName;

            System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                _view.SceneObjectsList.Items.Refresh();
            });

            return true;
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

        /// <summary>
        /// Обновление цвета и текста кнопки выбора цвета
        /// </summary>
        private void UpdateColorButtonUI(Vector3 color)
        {
            byte r = (byte)(Math.Clamp(color.X, 0f, 1f) * 255);
            byte g = (byte)(Math.Clamp(color.Y, 0f, 1f) * 255);
            byte b = (byte)(Math.Clamp(color.Z, 0f, 1f) * 255);

            _view.BtnSelectColor.Content = $"Выбрать цвет (R:{r} G:{g} B:{b})";

            _view.BtnSelectColor.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(r, g, b));
        }
    }
}
