using OpenTK.Graphics.GL;
using OpenTK.Mathematics;
using RayTracer.Models;
using RayTracer.Views;
using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using MessageBox = System.Windows.MessageBox;

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
        private string? _currentFilePath = null;
        private bool _isDirty = false;

        /// <summary>
        /// Сцена геометрических приметивов и источников света
        /// </summary>
        public Scene Scene { get { return _scene; } }

        public MainController(MainWindow view)
        {
            _view = view;
            _engine = new RayTracerEngine();
            _scene = new Scene();

            InitializeStartScene();
            UpdateWindowTitle();

            _view.BtnRender.Click += OnRenderClicked;
        }

        /// <summary>
        /// Инициализация начальной сцены
        /// </summary>
        public void InitializeStartScene()
        {
            _scene.Skybox = new Skybox(new Vector3(0.49f, 0.75f, 1.0f), new Vector3(0.2f, 0.2f, 0.2f));

            _scene.MainCamera = new Camera("Камера", new Vector3(0, 0, 1.5f), new Vector3(0, 0, -1), new Vector3(0, 1, 0), 75f, 2.0f, 0.005f);

            var floorMaterial = new Material(new Vector3(0.5f, 0.5f, 0.5f), 0.1f);
            _scene.Objects.Add(new Cube("Пол", new Vector3(0, -5.5f, 0), 10f, floorMaterial, 4096));

            var redMat = new Material(new Vector3(0.8f, 0.2f, 0.2f), 0.0f);
            _scene.Objects.Add(new Sphere("Красная сфера", new Vector3(0, 0, -1), 0.5f, redMat));

            _scene.Lights.Add(new GlobalLight("Солнце", new Vector3(1, 1, 1), 1.0f, new Vector3(-1, -1, -1)));

            UpdateHierarchyView();
        }

        /// <summary>
        /// Обновление заголовка окна, статус открытого файла
        /// </summary>
        private void UpdateWindowTitle()
        {
            string fileName = string.IsNullOrEmpty(_currentFilePath)
                ? "Безымянный.json"
                : System.IO.Path.GetFileName(_currentFilePath);

            string dirtyMark = _isDirty ? "*" : "";
            _view.Title = $"Трассировщик лучей — {fileName}{dirtyMark}";
        }

        /// <summary>
        /// Пометка сцены как изменённой
        /// </summary>
        private void MarkDirty()
        {
            if (_isDirty) return;
            _isDirty = true;
            UpdateWindowTitle();
        }

        /// <summary>
        /// Синхронизирует список объектов Модели с интерфейсом
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

        /// <summary>
        /// Добавлние новой сферы
        /// </summary>
        public void AddSphere()
        {
            string name = GetUniqueName("Новая сфера");
            _scene.Objects.Add(new Sphere(name, new Vector3(0, 0, 0), 0.5f, new Material(new Vector3(0.8f, 0.8f, 0.8f))));
            UpdateHierarchyView();
            MarkDirty();
        }

        /// <summary>
        /// Добавление нового куба
        /// </summary>
        public void AddCube()
        {
            string name = GetUniqueName("Новый куб");
            _scene.Objects.Add(new Cube(name, new Vector3(0, 0, 0), 1.0f, new Material(new Vector3(0.8f, 0.8f, 0.8f))));
            UpdateHierarchyView();
            MarkDirty();
        }

        /// <summary>
        /// Добавление нового цилиндра
        /// </summary>
        public void AddCylinder()
        {
            string name = GetUniqueName("Новый цилиндр");
            _scene.Objects.Add(new Cylinder(name, new Vector3(0, 0, 0), 0.5f, 1.0f, new Material(new Vector3(0.8f, 0.8f, 0.8f))));
            UpdateHierarchyView();
            MarkDirty();
        }

        /// <summary>
        /// Добавление нового глобавльного света
        /// </summary>
        public void AddGlobalLight()
        {
            string name = GetUniqueName("Новый глобальный свет");
            _scene.Lights.Add(new GlobalLight(name, new Vector3(1, 1, 1), 1.0f, new Vector3(0, -1, 0)));
            UpdateHierarchyView();
            MarkDirty();
        }

        /// <summary>
        /// Добавление нового направленного света
        /// </summary>
        public void AddSpotLight()
        {
            string name = GetUniqueName("Новый направленный свет");
            _scene.Lights.Add(new SpotLight(name, new Vector3(1, 1, 1), 1.0f, new Vector3(0, 0, 0), new Vector3(0, -1, 0), 30f, 10f));
            UpdateHierarchyView();
            MarkDirty();
        }

        /// <summary>
        /// Добавление нового точечного света
        /// </summary>
        public void AddPointLight()
        {
            string name = GetUniqueName("Новый точечный свет");
            _scene.Lights.Add(new PointLight(name, new Vector3(1, 1, 1), 1.0f, new Vector3(0, 0, 0), 10f));
            UpdateHierarchyView();
            MarkDirty();
        }

        /// <summary>
        /// Клонирование выбранного объекта
        /// </summary>
        public void CloneSelected()
        {
            var selected = _view.SceneObjectsList.SelectedItem;
            if (selected == null || selected is Camera) return;

            object? newItem = null;

            if (selected is SceneObject obj)
            {
                var clone = obj.Clone();
                clone.Name = GetUniqueName(obj.Name);
                _scene.Objects.Add(clone);
                newItem = clone;
            }
            else if (selected is Light light)
            {
                var clone = light.Clone();
                clone.Name = GetUniqueName(light.Name);
                _scene.Lights.Add(clone);
                newItem = clone;
            }

            UpdateHierarchyView();

            if (newItem != null)
            {
                _view.SceneObjectsList.SelectedItem = newItem;
                MarkDirty();
            }
        }

        /// <summary>
        /// Удаление выбранного объекта
        /// </summary>
        public void DeleteSelected()
        {
            var selected = _view.SceneObjectsList.SelectedItem;
            if (selected == null || selected is Camera) return;

            string objectName = selected.ToString() ?? "объект";
            var result = MessageBox.Show(
                $"Вы действительно хотите удалить \"{objectName}\"? Это действие нельзя отменить.",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            if (selected is SceneObject obj)
                _scene.Objects.Remove(obj);
            else if (selected is Light light)
                _scene.Lights.Remove(light);

            UpdateHierarchyView();
            OnObjectSelected(null);
            MarkDirty();
        }

        /// <summary>
        /// Выбор цвета
        /// </summary>
        public void SelectColor()
        {
            var selectedItem = _view.SceneObjectsList.SelectedItem;
            if (selectedItem == null) return;

            Vector3 currColor = Vector3.Zero;

            if (selectedItem is SceneObject obj)
                currColor = obj.Material.Color;
            else if (selectedItem is Light light)
                currColor = light.Color;
            else
                return;

            var dialog = new System.Windows.Forms.ColorDialog();
            dialog.Color = System.Drawing.Color.FromArgb(
                255,
                (int)(currColor.X * 255),
                (int)(currColor.Y * 255),
                (int)(currColor.Z * 255)
            );

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                Vector3 newColor = new Vector3(
                    dialog.Color.R / 255f,
                    dialog.Color.G / 255f,
                    dialog.Color.B / 255f
                );

                if (selectedItem is SceneObject o)
                    o.Material.Color = newColor;
                else if (selectedItem is Light l)
                    l.Color = newColor;

                UpdateColorButtonUI(newColor);
                MarkDirty();
            }
        }

        /// <summary>
        /// Проверяет несохранённые изменения перед выходом или открытием другой сцены
        /// </summary>
        /// <returns>true, если можно продолжать действие, false, если пользователь отменил</returns>
        public bool ConfirmUnsavedChanges()
        {
            if (!_isDirty) return true;

            string fileName = string.IsNullOrEmpty(_currentFilePath)
                ? "Безымянный.json"
                : System.IO.Path.GetFileName(_currentFilePath);

            var result = MessageBox.Show(
                $"В сцене \"{fileName}\" есть несохранённые изменения.\nСохранить изменения?",
                "Несохранённые изменения",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                return SaveScene();
            }

            if (result == MessageBoxResult.No)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Обычное сохранение сцены
        /// </summary>
        public bool SaveScene()
        {
            if (string.IsNullOrEmpty(_currentFilePath))
            {
                return SaveSceneAs();
            }

            return WriteSceneToFile(_currentFilePath);
        }

        /// <summary>
        /// Сохранение сцены в конкретный файл
        /// </summary>
        public bool SaveSceneAs()
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Сохранить сцену",
                Filter = "RayTracer Scene (*.json)|*.json",
                DefaultExt = ".json",
                FileName = string.IsNullOrEmpty(_currentFilePath)
                    ? "Безымянный"
                    : System.IO.Path.GetFileNameWithoutExtension(_currentFilePath)
            };

            bool? result;
            _view.SetOpenGLPaused(true);
            try
            {
                result = dialog.ShowDialog(_view);
            }
            finally
            {
                _view.SetOpenGLPaused(false);
            }

            if (result == true)
            {
                return WriteSceneToFile(dialog.FileName);
            }

            return false;
        }

        /// <summary>
        /// Запись сцены по указанному пути
        /// </summary>
        private bool WriteSceneToFile(string filePath)
        {
            try
            {
                SceneSerializer.SaveScene(_scene, filePath);
                _currentFilePath = filePath;
                _isDirty = false;
                UpdateWindowTitle();
                _view.StatusText.Text = $"Сцена сохранена: {System.IO.Path.GetFileName(filePath)}";
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении:\n{ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        /// <summary>
        /// Загрузка сцены
        /// </summary>
        public async void LoadScene()
        {
            if (!ConfirmUnsavedChanges()) return;

            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Открыть сцену",
                Filter = "RayTracer Scene (*.json)|*.json"
            };

            bool? result;
            _view.SetOpenGLPaused(true);
            try
            {
                result = dialog.ShowDialog(_view);
            }
            finally
            {
                _view.SetOpenGLPaused(false);
            }

            if (result == true)
            {
                string fileName = dialog.FileName;
                _view.StatusText.Text = "Загрузка сцены...";

                try
                {
                    Scene? loadedScene = await Task.Run(() => SceneSerializer.LoadScene(fileName));

                    if (loadedScene != null)
                    {
                        _scene = loadedScene;
                        _currentFilePath = fileName;
                        _isDirty = false;
                        UpdateWindowTitle();

                        _view.ClearTextureCache();
                        UpdateHierarchyView();
                        _view.SceneObjectsList.SelectedItem = null;
                        OnObjectSelected(null);

                        _view.StatusText.Text = $"Сцена загружена: {System.IO.Path.GetFileName(fileName)}";
                    }
                }
                catch (Exception ex)
                {
                    _view.StatusText.Text = "Ошибка загрузки сцены";
                    MessageBox.Show($"Ошибка при загрузке:\n{ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// Вращение камеры
        /// </summary>
        /// <param name="dx">Изменение координат мыши по X</param>
        /// <param name="dy">Изменение координат мыши по Y</param>
        public void RotateCamera(float dx, float dy)
        {
            _scene.MainCamera.Rotate(dx, dy);

            RefreshCameraUI();
        }

        /// <summary>
        /// Перемещение камеры
        /// </summary>
        /// <param name="dx">Направление по X</param>
        /// <param name="dy">Направление по Y</param>
        /// <param name="shift">Нажата ли клавиша Shift</param>
        /// <param name="deltaTime">Пройденное время между кадрами</param>
        public void MoveCamera(int dx, int dy, bool shift, float deltaTime)
        {
            _scene.MainCamera.Move(dx, dy, shift, deltaTime);

            if (dx != 0 || dy != 0) RefreshCameraUI();
        }

        /// <summary>
        /// Обновление значение в интерфейсе при перемещении камеры
        /// </summary>
        private void RefreshCameraUI()
        {
            if (_view.SceneObjectsList.SelectedItem == _scene.MainCamera)
            {
                _isUpdatingUI = true;
                var culture = System.Globalization.CultureInfo.InvariantCulture;

                _view.PosX.Text = _scene.MainCamera.Position.X.ToString("0.00", culture);
                _view.PosY.Text = _scene.MainCamera.Position.Y.ToString("0.00", culture);
                _view.PosZ.Text = _scene.MainCamera.Position.Z.ToString("0.00", culture);

                _view.DirX.Text = _scene.MainCamera.Direction.X.ToString("0.00", culture);
                _view.DirY.Text = _scene.MainCamera.Direction.Y.ToString("0.00", culture);
                _view.DirZ.Text = _scene.MainCamera.Direction.Z.ToString("0.00", culture);

                _isUpdatingUI = false;
            }
            MarkDirty();
        }

        /// <summary>
        /// Обработка клика по объекту в списке
        /// </summary>
        /// <param name="selectedItem">Выделенный объект</param>
        public void OnObjectSelected(object selectedItem)
        {
            if (selectedItem == null)
            {
                _view.PropertiesContainer.Visibility = Visibility.Hidden;
                _view.LightmapPreviewImage.Source = null;
                return;
            }

            _view.PropertiesContainer.Visibility = Visibility.Visible;
            HideAllDynamicPanels();

            _isUpdatingUI = true;
            var culture = System.Globalization.CultureInfo.InvariantCulture;

            if (selectedItem is Camera cam)
            {
                _view.TxtObjType.Text = "Тип объекта: Камера";
                _view.PanelCoordinates.Visibility = Visibility.Visible;
                _view.PanelDirection.Visibility = Visibility.Visible;
                _view.PanelCamera.Visibility = Visibility.Visible;
                _view.PosX.Text = cam.Position.X.ToString("0.00", culture);
                _view.PosY.Text = cam.Position.Y.ToString("0.00", culture);
                _view.PosZ.Text = cam.Position.Z.ToString("0.00", culture);
                _view.DirX.Text = cam.Direction.X.ToString("0.00", culture);
                _view.DirY.Text = cam.Direction.Y.ToString("0.00", culture);
                _view.DirZ.Text = cam.Direction.Z.ToString("0.00", culture);
                _view.FOV.Text = cam.FovDegrees.ToString("0.00", culture);
            }
            else if (selectedItem is SceneObject obj)
            {
                _view.PanelCoordinates.Visibility = Visibility.Visible;

                _view.PosX.Text = obj.Center.X.ToString("0.00", culture);
                _view.PosY.Text = obj.Center.Y.ToString("0.00", culture);
                _view.PosZ.Text = obj.Center.Z.ToString("0.00", culture);

                _view.PanelMaterial.Visibility = Visibility.Visible;
                _view.PanelColor.Visibility = Visibility.Visible;

                _view.SliderReflectivity.Value = obj.Material.Reflectivity;
                _view.Reflectivity.Text = obj.Material.Reflectivity.ToString("0.00", culture);

                _view.ObjResolution.Text = obj.LightmapRes.ToString();

                UpdateColorButtonUI(obj.Material.Color);

                if (obj is Sphere sphere)
                {
                    _view.TxtObjType.Text = "Тип объекта: Сфера";
                    _view.PanelSphere.Visibility = Visibility.Visible;
                    _view.Radius.Text = sphere.Radius.ToString("0.00", culture);
                }
                else if (obj is Cube cube)
                {
                    _view.TxtObjType.Text = "Тип объекта: Куб";
                    _view.PanelCube.Visibility = Visibility.Visible;
                    _view.Side.Text = cube.Side.ToString("0.00", culture);
                }
                else if (obj is Cylinder cyl)
                {
                    _view.TxtObjType.Text = "Тип объекта: Цилиндр";
                    _view.PanelCylinder.Visibility = Visibility.Visible;
                    _view.CylRadius.Text = cyl.Radius.ToString("0.00", culture);
                    _view.Height.Text = cyl.Height.ToString("0.00", culture);
                } 
            }
            else if (selectedItem is Light light)
            {
                _view.PanelLight.Visibility = Visibility.Visible;
                _view.PanelColor.Visibility = Visibility.Visible;
                _view.SliderLightIntensity.Value = light.Intensity;
                UpdateColorButtonUI(light.Color);

                if (light is GlobalLight gl)
                {
                    _view.TxtObjType.Text = "Тип объекта: Глобальный источник света";
                    _view.PanelDirection.Visibility = Visibility.Visible;
                    _view.DirX.Text = gl.Direction.X.ToString("0.00", culture);
                    _view.DirY.Text = gl.Direction.Y.ToString("0.00", culture);
                    _view.DirZ.Text = gl.Direction.Z.ToString("0.00", culture);
                }
                else if (light is PointLight pl)
                {
                    _view.TxtObjType.Text = "Тип объекта: Точечный источник света";
                    _view.PanelCoordinates.Visibility = Visibility.Visible;
                    _view.PanelLightRange.Visibility = Visibility.Visible;
                    _view.PosX.Text = pl.Position.X.ToString("0.00", culture);
                    _view.PosY.Text = pl.Position.Y.ToString("0.00", culture);
                    _view.PosZ.Text = pl.Position.Z.ToString("0.00", culture);

                    _view.LightRange.Text = pl.Range.ToString("0.00", culture);
                }
                else if (light is SpotLight sl)
                {
                    _view.TxtObjType.Text = "Тип объекта: Направленный источник света";
                    _view.PanelCoordinates.Visibility = Visibility.Visible;
                    _view.PanelLightRange.Visibility = Visibility.Visible;
                    _view.PosX.Text = sl.Position.X.ToString("0.00", culture);
                    _view.PosY.Text = sl.Position.Y.ToString("0.00", culture);
                    _view.PosZ.Text = sl.Position.Z.ToString("0.00", culture);

                    _view.PanelDirection.Visibility = Visibility.Visible;
                    _view.DirX.Text = sl.Direction.X.ToString("0.00", culture);
                    _view.DirY.Text = sl.Direction.Y.ToString("0.00", culture);
                    _view.DirZ.Text = sl.Direction.Z.ToString("0.00", culture);

                    _view.LightRange.Text = sl.Range.ToString("0.00", culture);

                    _view.PanelSpotAngle.Visibility = Visibility.Visible;
                    _view.SpotAngle.Text = sl.CutOffAngle.ToString("0.00", culture);
                }
            }

            ShowLightmapPreview();

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
            Vector3 newPosiotion = new Vector3(x, y, z);

            float.TryParse(_view.DirX.Text.Replace(',', '.'), style, culture, out float dx);
            float.TryParse(_view.DirY.Text.Replace(',', '.'), style, culture, out float dy);
            float.TryParse(_view.DirZ.Text.Replace(',', '.'), style, culture, out float dz);
            Vector3 newDir = new Vector3(dx, dy, dz);
            if (newDir.Length == 0) newDir = new Vector3(0, -1, 0);

            float.TryParse(_view.SpotAngle.Text.Replace(',', '.'), style, culture, out float angle);

            float.TryParse(_view.Radius.Text.Replace(',', '.'), style, culture,out float radius);
            float.TryParse(_view.Side.Text.Replace(',', '.'), style, culture, out float side);
            float.TryParse(_view.CylRadius.Text.Replace(',', '.'), style, culture, out float cylRadius);
            float.TryParse(_view.Height.Text.Replace(',', '.'), style, culture, out float height);

            float.TryParse(_view.Reflectivity.Text.Replace(',', '.'), style, culture, out float reflectiviti);
            reflectiviti = Math.Clamp(reflectiviti, 0f, 1f);

            int.TryParse(_view.ObjResolution.Text, out int lightmapRes);
            lightmapRes = Math.Clamp(lightmapRes, 16, 8192);

            float.TryParse(_view.FOV.Text.Replace(',', '.'), style, culture, out float fov);
            fov = Math.Clamp(fov, 1f, 179.99f);

            float.TryParse(_view.LightRange.Text.Replace(',', '.'), style, culture, out float lightRange);
            lightRange = Math.Max(0.1f, lightRange);

            if (selectedItem is Camera cam)
            {
                cam.Position = newPosiotion;
                cam.Direction = newDir;
                cam.FovDegrees = fov;
            }
            else if (selectedItem is SceneObject obj)
            {
                obj.LightmapRes = lightmapRes;

                obj.Center = newPosiotion;
                if(obj is Sphere sphere)
                    sphere.Radius = radius;

                if(obj is Cube cube)
                    cube.Side = side;

                if(obj is Cylinder cyl)
                {
                    cyl.Radius = cylRadius;
                    cyl.Height = height;
                }

                if(reflectiviti != obj.Material.Reflectivity)
                {
                    obj.Material.Reflectivity = reflectiviti;
                    _view.SliderReflectivity.Value = reflectiviti;
                }
                else
                {
                    obj.Material.Reflectivity = (float)_view.SliderReflectivity.Value;
                    _view.Reflectivity.Text = obj.Material.Reflectivity.ToString("0.00", culture);
                }
            }
            else if (selectedItem is Light light)
            {
                light.Intensity = (float)_view.SliderLightIntensity.Value;

                if (light is GlobalLight gl)
                {
                    gl.Direction = newDir;
                }
                else if (light is PointLight pl)
                {
                    pl.Position = newPosiotion;
                    pl.Range = lightRange;
                }
                else if (light is SpotLight sl)
                {
                    sl.Position = newPosiotion;
                    sl.Direction = newDir;
                    sl.CutOffAngle = Math.Clamp(angle, 1f, 180f);
                    sl.Range = lightRange;
                }
            }
            MarkDirty();
        }

        /// <summary>
        /// Обработка изменения глубины трассировки через текстовое поле
        /// </summary>
        public void OnTraceDepthChanged()
        {
            if (_isUpdatingUI) return;

            var style = System.Globalization.NumberStyles.Any;
            var culture = System.Globalization.CultureInfo.InvariantCulture;
            int.TryParse(_view.TraceDepth.Text, style, culture, out int traceDepth);
            
            _view.SliderTraceDepth.Value = Math.Clamp(traceDepth, 1, 10);

            if (traceDepth < 1 || traceDepth > 10)
                _view.TraceDepth.Text = Math.Clamp(traceDepth, 1, 10).ToString();
            MarkDirty();
        }

        /// <summary>
        /// Обработка изменения глубины трассировки через слайдер
        /// </summary>
        public void OnSliderTraceDepthChanged()
        {
            if (_isUpdatingUI) return;

            int traceDepth = (int)_view.SliderTraceDepth.Value;
            _view.TraceDepth.Text = traceDepth.ToString();
            MarkDirty();
        }

        /// <summary>
        /// Попытка переименовать объект
        /// </summary>
        /// <returns>true, если успешное переименование, иначе false</returns>
        public bool TryRenameObject(object target, string newName)
        {
            if (target == null || target is Camera || string.IsNullOrWhiteSpace(newName)) return false;

            string currentName = target.ToString();
            if (currentName == newName) return true;

            bool nameExists = _scene.Objects.Exists(o => o.Name == newName) ||
                              _scene.Lights.Exists(l => l.Name == newName) ||
                              (_scene.MainCamera != null && _scene.MainCamera.Name == newName);

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

            MarkDirty();

            return true;
        }

        /// <summary>
        /// Генератор уникальных имен
        /// </summary>
        /// <returns>Уникальное имя</returns>
        private string GetUniqueName(string originalName)
        {
            string baseName = originalName;
            int lastSpace = originalName.LastIndexOf(" (");
            if (lastSpace > 0 && originalName.EndsWith(")"))
                baseName = originalName.Substring(0, lastSpace);

            string newName = baseName;
            int count = 1;

            bool NameExists(string name)
            {
                return _scene.Lights.Exists(l => l.Name == name) || _scene.Objects.Exists(o => o.Name == name) || (_scene.MainCamera != null && _scene.MainCamera.Name == name);
            }

            while (NameExists(newName))
            {
                newName = $"{baseName} ({count})";
                count++;
            }
            return newName;
        }

        /// <summary>
        /// Обработка нажатия кнопки рендера
        /// </summary>
        private void OnRenderClicked(object sender, RoutedEventArgs e)
        {
            StartRender();
        }

        /// <summary>
        /// Запуск рендера световой карты
        /// </summary>
        public async void StartRender()
        {
            if (!_view.BtnRender.IsEnabled) return;

            int traceDepth = (int)_view.SliderTraceDepth.Value;

            _view.BtnRender.IsEnabled = false;
            _view.StatusText.Text = "Запекание световых карт... Пожалуйста, подождите.";

            _scene.MainCamera.CalculateVectors(1.0f);

            Stopwatch sw = Stopwatch.StartNew();
            Vector3 bakeCameraPos = _scene.MainCamera.Position;

            await Task.Run(() => _engine.BakeLightmaps(_scene, traceDepth, bakeCameraPos));

            sw.Stop();

            _view.ClearTextureCache();
            _view.RadioSolid.IsChecked = true;
            ShowLightmapPreview();

            _view.BtnRender.IsEnabled = true;
            _view.StatusText.Text = "Готов к работе";
            _view.RenderTimeText.Text = $"Время запекания: {sw.ElapsedMilliseconds / 1000.0:F2} с";

            MarkDirty();
        }

        /// <summary>
        /// Выводит световую карту выделенного объекта в правое окно
        /// </summary>
        public void ShowLightmapPreview()
        {
            if (_view.SceneObjectsList.SelectedItem is SceneObject obj && obj.Lightmap != null)
            {
                int stride = obj.Lightmap.Width * 3;
                BitmapSource bitmap = BitmapSource.Create(
                    obj.Lightmap.Width, obj.Lightmap.Height, 96, 96,
                    PixelFormats.Rgb24, null, obj.Lightmap.Pixels, stride);

                _view.LightmapPreviewImage.Source = bitmap;
            }
            else
            {
                _view.LightmapPreviewImage.Source = null;
            }
        }

        /// <summary>
        /// Обновление цвета и текста кнопки выбора цвета
        /// </summary>
        /// <param name="color">Цвет</param>
        private void UpdateColorButtonUI(Vector3 color)
        {
            byte r = (byte)(Math.Clamp(color.X, 0f, 1f) * 255);
            byte g = (byte)(Math.Clamp(color.Y, 0f, 1f) * 255);
            byte b = (byte)(Math.Clamp(color.Z, 0f, 1f) * 255);

            _view.TxtColor.Text = $"Цвет: (R:{r} G:{g} B:{b})";

            _view.BtnSelectColor.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(r, g, b));
        }

        /// <summary>
        /// Скрывает все специфичные панели свойств
        /// </summary>
        private void HideAllDynamicPanels()
        {
            _view.PanelCoordinates.Visibility = Visibility.Collapsed;
            _view.PanelDirection.Visibility = Visibility.Collapsed;
            _view.PanelSpotAngle.Visibility = Visibility.Collapsed;

            _view.PanelMaterial.Visibility = Visibility.Collapsed;
            _view.PanelColor.Visibility = Visibility.Collapsed;

            _view.PanelSphere.Visibility = Visibility.Collapsed;
            _view.PanelCube.Visibility = Visibility.Collapsed;
            _view.PanelCylinder.Visibility = Visibility.Collapsed;
  
            _view.PanelLight.Visibility = Visibility.Collapsed;
            _view.PanelLightRange.Visibility = Visibility.Collapsed;

            _view.PanelCamera.Visibility = Visibility.Collapsed;
        }
    }
}
