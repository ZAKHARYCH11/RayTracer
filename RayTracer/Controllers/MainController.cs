using OpenTK.Mathematics;
using RayTracer.Models;
using RayTracer.Utils;
using RayTracer.Views;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace RayTracer.Controllers
{
    /// <summary>
    /// Основной контроллер
    /// </summary>
    public class MainController
    {
        private readonly MainWindow _view;
        private Scene _scene;
        private readonly RayTracerEngine _engine;

        private string? _currentFilePath = null;
        private bool _isDirty = false;
        private bool _isRendering = false;

        public MainController(MainWindow view)
        {
            _view = view;
            _engine = new RayTracerEngine();
            _scene = new Scene();

            InitializeStartScene();

            _view.SetScene(_scene);
            UpdateWindowTitle();

            SubscribeToViewEvents();
        }

        /// <summary>
        /// Подпись на события
        /// </summary>
        private void SubscribeToViewEvents()
        {
            _view.AddSphereRequested += AddSphere;
            _view.AddCubeRequested += AddCube;
            _view.AddCylinderRequested += AddCylinder;
            _view.AddGlobalLightRequested += AddGlobalLight;
            _view.AddSpotLightRequested += AddSpotLight;
            _view.AddPointLightRequested += AddPointLight;

            _view.CloneRequested += CloneSelected;
            _view.DeleteRequested += DeleteSelected;
            _view.RenameRequested += TryRenameObject;

            _view.PropertiesChanged += ApplyObjectProperties;
            _view.ColorChanged += ApplyObjectColor;

            _view.RenderRequested += StartRender;
            _view.CameraRotateRequested += RotateCamera;
            _view.CameraMoveRequested += MoveCamera;

            _view.OpenSceneRequested += LoadScene;
            _view.SaveSceneRequested += () => SaveScene();
            _view.SaveSceneAsRequested += () => SaveSceneAs();
            _view.ClosingRequested += ConfirmUnsavedChanges;
        }

        /// <summary>
        /// Инициализация стартовой сцены
        /// </summary>
        private void InitializeStartScene()
        {
            _scene.Skybox = new Skybox(new Vector3(0.49f, 0.75f, 1.0f), new Vector3(0.2f, 0.2f, 0.2f));
            _scene.MainCamera = new Camera("Камера", new Vector3(0, 0, 1.5f), new Vector3(0, 0, -1), new Vector3(0, 1, 0), 75f, 2.0f, 0.005f);

            var floorMaterial = new Material(new Vector3(0.5f, 0.5f, 0.5f), 0.1f);
            _scene.Objects.Add(new Cube("Пол", new Vector3(0, -5.5f, 0), 10f, floorMaterial, 4096));

            var redMat = new Material(new Vector3(0.8f, 0.2f, 0.2f), 0.0f);
            _scene.Objects.Add(new Sphere("Красная сфера", new Vector3(0, 0, -1), 0.5f, redMat));

            _scene.Lights.Add(new GlobalLight("Солнце", new Vector3(1, 1, 1), 1.0f, new Vector3(-1, -1, -1)));
        }

        /// <summary>
        /// Обновление заголовка окна
        /// </summary>
        private void UpdateWindowTitle()
        {
            string fileName = string.IsNullOrEmpty(_currentFilePath)
                ? "Безымянный.json"
                : System.IO.Path.GetFileName(_currentFilePath);

            string dirtyMark = _isDirty ? "*" : "";
            _view.SetWindowTitle($"Трассировщик лучей — {fileName}{dirtyMark}");
        }

        /// <summary>
        /// Поментка изменения сцены
        /// </summary>
        private void MarkDirty()
        {
            if (_isDirty) return;
            _isDirty = true;
            UpdateWindowTitle();
        }

        /// <summary>
        /// Добавление сферы
        /// </summary>
        private void AddSphere()
        {
            var item = new Sphere(_scene.GetUniqueName("Новая сфера"), new Vector3(0, 0, 0), 0.5f, new Material(new Vector3(0.8f, 0.8f, 0.8f)));
            _scene.Objects.Add(item);
            _view.RefreshHierarchy(item);
            MarkDirty();
        }

        /// <summary>
        /// Добавление куба
        /// </summary>
        private void AddCube()
        {
            var item = new Cube(_scene.GetUniqueName("Новый куб"), new Vector3(0, 0, 0), 1.0f, new Material(new Vector3(0.8f, 0.8f, 0.8f)));
            _scene.Objects.Add(item);
            _view.RefreshHierarchy(item);
            MarkDirty();
        }

        /// <summary>
        /// Добавление цилиндра
        /// </summary>
        private void AddCylinder()
        {
            var item = new Cylinder(_scene.GetUniqueName("Новый цилиндр"), new Vector3(0, 0, 0), 0.5f, 1.0f, new Material(new Vector3(0.8f, 0.8f, 0.8f)));
            _scene.Objects.Add(item);
            _view.RefreshHierarchy(item);
            MarkDirty();
        }

        /// <summary>
        /// Добавление глобального источника света
        /// </summary>
        private void AddGlobalLight()
        {
            var item = new GlobalLight(_scene.GetUniqueName("Новый глобальный свет"), new Vector3(1, 1, 1), 1.0f, new Vector3(0, -1, 0));
            _scene.Lights.Add(item);
            _view.RefreshHierarchy(item);
            MarkDirty();
        }

        /// <summary>
        /// Добавление направленного источника света
        /// </summary>
        private void AddSpotLight()
        {
            var item = new SpotLight(_scene.GetUniqueName("Новый направленный свет"), new Vector3(1, 1, 1), 1.0f, new Vector3(0, 0, 0), new Vector3(0, -1, 0), 30f, 10f);
            _scene.Lights.Add(item);
            _view.RefreshHierarchy(item);
            MarkDirty();
        }

        /// <summary>
        /// Добавление точечного источника света
        /// </summary>
        private void AddPointLight()
        {
            var item = new PointLight(_scene.GetUniqueName("Новый точечный свет"), new Vector3(1, 1, 1), 1.0f, new Vector3(0, 0, 0), 10f);
            _scene.Lights.Add(item);
            _view.RefreshHierarchy(item);
            MarkDirty();
        }

        /// <summary>
        /// Клонирование выбранного объекта
        /// </summary>
        /// <param name="selected">Выбранный объект</param>
        private void CloneSelected(object? selected)
        {
            if (selected == null || selected is Camera) return;

            object? newItem = null;
            if (selected is SceneObject obj)
            {
                var clone = obj.Clone();
                clone.Name = _scene.GetUniqueName(obj.Name);
                _scene.Objects.Add(clone);
                newItem = clone;
            }
            else if (selected is Light light)
            {
                var clone = light.Clone();
                clone.Name = _scene.GetUniqueName(light.Name);
                _scene.Lights.Add(clone);
                newItem = clone;
            }

            if (newItem != null)
            {
                _view.RefreshHierarchy(newItem);
                MarkDirty();
            }
        }

        /// <summary>
        /// Удаление выбранного объекта
        /// </summary>
        /// <param name="selected">Выбранный объект</param>
        private void DeleteSelected(object? selected)
        {
            if (selected == null || selected is Camera) return;

            string objectName = selected.ToString() ?? "объект";
            if (!_view.ConfirmAction($"Вы действительно хотите удалить \"{objectName}\"? Это действие нельзя отменить.", "Подтверждение удаления"))
                return;

            if (selected is SceneObject obj)
                _scene.Objects.Remove(obj);
            else if (selected is Light light)
                _scene.Lights.Remove(light);

            _view.RefreshHierarchy();
            MarkDirty();
        }

        /// <summary>
        /// Попытка переименования объекта
        /// </summary>
        /// <param name="target">Объект</param>
        /// <param name="newName">Новое имя</param>
        /// <returns>true - если успешное переименование</returns>
        private bool TryRenameObject(object? target, string newName)
        {
            if (target == null || target is Camera || string.IsNullOrWhiteSpace(newName)) return false;

            string currentName = target.ToString() ?? string.Empty;
            if (currentName == newName) return true;

            if (_scene.IsNameTaken(newName)) return false;

            if (target is SceneObject obj) obj.Name = newName;
            else if (target is Light light) light.Name = newName;

            _view.RefreshHierarchyNames();
            MarkDirty();
            return true;
        }

        /// <summary>
        /// Изменение свойств объекта
        /// </summary>
        /// <param name="selectedItem">Объект</param>
        /// <param name="data">Свойства</param>
        private void ApplyObjectProperties(object? selectedItem, ObjectPropertiesData data)
        {
            if (selectedItem == null || data.ChangedGroup == PropertyEditGroup.None) return;

            if (selectedItem is Camera cam)
            {
                switch (data.ChangedGroup)
                {
                    case PropertyEditGroup.Position:
                        cam.Position = data.Position;
                        break;
                    case PropertyEditGroup.Rotation:
                        cam.Direction = RotationUtils.AnglesToDirection(Math.Clamp(data.RotX, -89f, 89f), data.RotY);
                        break;
                    case PropertyEditGroup.CameraFov:
                        cam.FovDegrees = data.FovDegrees;
                        break;
                }
            }
            else if (selectedItem is SceneObject obj)
            {
                switch (data.ChangedGroup)
                {
                    case PropertyEditGroup.Position:
                        obj.Center = data.Position;
                        break;
                    case PropertyEditGroup.LightmapRes:
                        obj.LightmapRes = data.LightmapRes;
                        break;
                    case PropertyEditGroup.Reflectivity:
                        obj.Material.Reflectivity = data.Reflectivity;
                        break;
                    case PropertyEditGroup.SphereRadius when obj is Sphere sphere:
                        sphere.Radius = data.Radius;
                        break;
                    case PropertyEditGroup.CubeSide when obj is Cube cube:
                        cube.Side = data.Side;
                        break;
                    case PropertyEditGroup.CylinderSize when obj is Cylinder cyl:
                        cyl.Radius = data.CylRadius;
                        cyl.Height = data.Height;
                        break;
                }
            }
            else if (selectedItem is Light light)
            {
                switch (data.ChangedGroup)
                {
                    case PropertyEditGroup.LightIntensity:
                        light.Intensity = data.LightIntensity;
                        break;
                    case PropertyEditGroup.Position when light is PointLight pl:
                        pl.Position = data.Position;
                        break;
                    case PropertyEditGroup.Position when light is SpotLight sl:
                        sl.Position = data.Position;
                        break;
                    case PropertyEditGroup.Rotation when light is GlobalLight gl:
                        gl.Direction = RotationUtils.AnglesToDirection(Math.Clamp(data.RotX, -90f, 90f), data.RotY);
                        break;
                    case PropertyEditGroup.Rotation when light is SpotLight sl:
                        sl.Direction = RotationUtils.AnglesToDirection(Math.Clamp(data.RotX, -90f, 90f), data.RotY);
                        break;
                    case PropertyEditGroup.LightRange when light is PointLight pl:
                        pl.Range = data.LightRange;
                        break;
                    case PropertyEditGroup.LightRange when light is SpotLight sl:
                        sl.Range = data.LightRange;
                        break;
                    case PropertyEditGroup.SpotAngle when light is SpotLight sl:
                        sl.CutOffAngle = data.SpotAngle;
                        break;
                }
            }

            MarkDirty();
        }

        /// <summary>
        /// Применение нового цвета
        /// </summary>
        /// <param name="selectedItem">Объект</param>
        /// <param name="newColor">Новый цвет</param>
        private void ApplyObjectColor(object? selectedItem, Vector3 newColor)
        {
            if (selectedItem is SceneObject obj) obj.Material.Color = newColor;
            else if (selectedItem is Light light) light.Color = newColor;
            else return;

            MarkDirty();
        }

        /// <summary>
        /// Вращение камеры
        /// </summary>
        /// <param name="dx">Пермещение по X</param>
        /// <param name="dy">Перемещение по Y</param>
        private void RotateCamera(float dx, float dy)
        {
            _scene.MainCamera.Rotate(dx, dy);
            _view.RefreshCameraInspector();
            MarkDirty();
        }

        /// <summary>
        /// Перемещение камеры
        /// </summary>
        /// <param name="dx">Направление по X</param>
        /// <param name="dy">Направление по Y</param>
        /// <param name="shift">Нажата ли клавиша shift</param>
        /// <param name="deltaTime">Пройденное время</param>
        private void MoveCamera(int dx, int dy, bool shift, float deltaTime)
        {
            _scene.MainCamera.Move(dx, dy, shift, deltaTime);
            _view.RefreshCameraInspector();
            MarkDirty();
        }

        /// <summary>
        /// Запуск рендера
        /// </summary>
        /// <param name="traceDepth">Глубина трассировки</param>
        private async void StartRender(int traceDepth)
        {
            if (_isRendering) return;
            _isRendering = true;

            _view.SetRenderState(true);
            _view.SetStatusText("Запекание световых карт... Пожалуйста, подождите.");

            _scene.MainCamera.CalculateVectors(1.0f);
            Stopwatch sw = Stopwatch.StartNew();
            Vector3 bakeCameraPos = _scene.MainCamera.Position;

            await Task.Run(() => _engine.BakeLightmaps(_scene, traceDepth, bakeCameraPos));

            sw.Stop();
            _view.SetRenderState(false);
            _view.SetStatusText("Готов к работе");
            _view.SetRenderTimeText($"Время запекания: {sw.ElapsedMilliseconds / 1000.0:F2} с");

            MarkDirty();
            _isRendering = false;
        }

        /// <summary>
        /// Сохранение изменений
        /// </summary>
        /// <returns>true - если успешное сохранение</returns>
        private bool ConfirmUnsavedChanges()
        {
            if (!_isDirty) return true;

            string fileName = string.IsNullOrEmpty(_currentFilePath)
                ? "Безымянный.json"
                : System.IO.Path.GetFileName(_currentFilePath);

            bool? choice = _view.AskSaveBeforeAction(
                $"В сцене \"{fileName}\" есть несохранённые изменения.\nСохранить изменения?",
                "Несохранённые изменения");

            if (choice == true) return SaveScene();
            if (choice == false) return true;
            return false;
        }

        /// <summary>
        /// Сохранение сцены
        /// </summary>
        /// <returns></returns>
        private bool SaveScene()
        {
            if (string.IsNullOrEmpty(_currentFilePath))
                return SaveSceneAs();

            return WriteSceneToFile(_currentFilePath);
        }

        /// <summary>
        /// Сохранить как
        /// </summary>
        /// <returns>true - если успешно сохранена</returns>
        private bool SaveSceneAs()
        {
            string defaultName = string.IsNullOrEmpty(_currentFilePath)
                ? "Безымянный"
                : System.IO.Path.GetFileNameWithoutExtension(_currentFilePath);

            string? path = _view.ShowSaveFileDialog(defaultName);
            return path != null && WriteSceneToFile(path);
        }

        /// <summary>
        /// Запись в файл по пути
        /// </summary>
        /// <param name="filePath">Путь к файлу</param>
        /// <returns>true - если успешная запись</returns>
        private bool WriteSceneToFile(string filePath)
        {
            try
            {
                SceneSerializer.SaveScene(_scene, filePath);
                _currentFilePath = filePath;
                _isDirty = false;
                UpdateWindowTitle();
                _view.SetStatusText($"Сцена сохранена: {System.IO.Path.GetFileName(filePath)}");
                return true;
            }
            catch (Exception ex)
            {
                _view.ShowError($"Ошибка при сохранении:\n{ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Загрузка сцены
        /// </summary>
        private async void LoadScene()
        {
            if (!ConfirmUnsavedChanges()) return;

            string? fileName = _view.ShowOpenFileDialog();
            if (fileName == null) return;

            _view.SetStatusText("Загрузка сцены...");

            try
            {
                Scene? loadedScene = await Task.Run(() => SceneSerializer.LoadScene(fileName));
                if (loadedScene != null)
                {
                    if (loadedScene.Skybox == null)
                        loadedScene.Skybox = new Skybox(new Vector3(0.49f, 0.75f, 1.0f), new Vector3(0.2f, 0.2f, 0.2f));

                    _scene = loadedScene;
                    _currentFilePath = fileName;
                    _isDirty = false;
                    UpdateWindowTitle();

                    _view.SetScene(_scene);
                    _view.SetStatusText($"Сцена загружена: {System.IO.Path.GetFileName(fileName)}");
                }
            }
            catch (Exception ex)
            {
                _view.SetStatusText("Ошибка загрузки сцены");
                _view.ShowError($"Ошибка при загрузке:\n{ex.Message}");
            }
        }
    }
}