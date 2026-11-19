using OpenTK.Mathematics;
using RayTracer.Models;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace RayTracer.Views
{
    /// <summary>
    /// Данные свойств объекта, считанные из интерфейса
    /// </summary>
    public struct ObjectPropertiesData
    {
        public Vector3 Position;
        public Vector3 Direction;
        public float SpotAngle;
        public float Radius;
        public float Side;
        public float CylRadius;
        public float Height;
        public float Reflectivity;
        public int LightmapRes;
        public float FovDegrees;
        public float LightIntensity;
        public float LightRange;
    }

    /// <summary>
    /// Класс, отвечающий за отображение и считывание свойств выбранного объекта
    /// </summary>
    public class InspectorView
    {
        private readonly MainWindow _window;
        private bool _isUpdatingUI = false;

        /// <summary>
        /// Статус обновления элементов UI
        /// </summary>
        public bool IsUpdatingUI
        {
            get { return _isUpdatingUI; }
            set { _isUpdatingUI = value; }
        }

        public InspectorView(MainWindow window)
        {
            _window = window;
        }

        /// <summary>
        /// Отображение свойства выбранного объекта
        /// </summary>
        public void DisplayObject(object? selectedItem)
        {
            if (selectedItem == null)
            {
                _window.PropertiesContainer.Visibility = Visibility.Hidden;
                _window.LightmapPreviewImage.Source = null;
                return;
            }

            _window.PropertiesContainer.Visibility = Visibility.Visible;
            HideAllDynamicPanels();

            IsUpdatingUI = true;
            var culture = CultureInfo.InvariantCulture;

            if (selectedItem is Camera cam)
            {
                _window.TxtObjType.Text = "Тип объекта: Камера";
                _window.PanelCoordinates.Visibility = Visibility.Visible;
                _window.PanelDirection.Visibility = Visibility.Visible;
                _window.PanelCamera.Visibility = Visibility.Visible;

                _window.PosX.Text = cam.Position.X.ToString("0.00", culture);
                _window.PosY.Text = cam.Position.Y.ToString("0.00", culture);
                _window.PosZ.Text = cam.Position.Z.ToString("0.00", culture);
                _window.DirX.Text = cam.Direction.X.ToString("0.00", culture);
                _window.DirY.Text = cam.Direction.Y.ToString("0.00", culture);
                _window.DirZ.Text = cam.Direction.Z.ToString("0.00", culture);
                _window.FOV.Text = cam.FovDegrees.ToString("0.00", culture);
            }
            else if (selectedItem is SceneObject obj)
            {
                _window.PanelCoordinates.Visibility = Visibility.Visible;
                _window.PosX.Text = obj.Center.X.ToString("0.00", culture);
                _window.PosY.Text = obj.Center.Y.ToString("0.00", culture);
                _window.PosZ.Text = obj.Center.Z.ToString("0.00", culture);

                _window.PanelMaterial.Visibility = Visibility.Visible;
                _window.PanelColor.Visibility = Visibility.Visible;

                _window.SliderReflectivity.Value = obj.Material.Reflectivity;
                _window.Reflectivity.Text = obj.Material.Reflectivity.ToString("0.00", culture);
                _window.ObjResolution.Text = obj.LightmapRes.ToString();

                UpdateColorButton(obj.Material.Color);

                if (obj is Sphere sphere)
                {
                    _window.TxtObjType.Text = "Тип объекта: Сфера";
                    _window.PanelSphere.Visibility = Visibility.Visible;
                    _window.Radius.Text = sphere.Radius.ToString("0.00", culture);
                }
                else if (obj is Cube cube)
                {
                    _window.TxtObjType.Text = "Тип объекта: Куб";
                    _window.PanelCube.Visibility = Visibility.Visible;
                    _window.Side.Text = cube.Side.ToString("0.00", culture);
                }
                else if (obj is Cylinder cyl)
                {
                    _window.TxtObjType.Text = "Тип объекта: Цилиндр";
                    _window.PanelCylinder.Visibility = Visibility.Visible;
                    _window.CylRadius.Text = cyl.Radius.ToString("0.00", culture);
                    _window.Height.Text = cyl.Height.ToString("0.00", culture);
                }
            }
            else if (selectedItem is Light light)
            {
                _window.PanelLight.Visibility = Visibility.Visible;
                _window.PanelColor.Visibility = Visibility.Visible;
                _window.SliderLightIntensity.Value = light.Intensity;
                UpdateColorButton(light.Color);

                if (light is GlobalLight gl)
                {
                    _window.TxtObjType.Text = "Тип объекта: Глобальный источник света";
                    _window.PanelDirection.Visibility = Visibility.Visible;
                    _window.DirX.Text = gl.Direction.X.ToString("0.00", culture);
                    _window.DirY.Text = gl.Direction.Y.ToString("0.00", culture);
                    _window.DirZ.Text = gl.Direction.Z.ToString("0.00", culture);
                }
                else if (light is PointLight pl)
                {
                    _window.TxtObjType.Text = "Тип объекта: Точечный источник света";
                    _window.PanelCoordinates.Visibility = Visibility.Visible;
                    _window.PanelLightRange.Visibility = Visibility.Visible;

                    _window.PosX.Text = pl.Position.X.ToString("0.00", culture);
                    _window.PosY.Text = pl.Position.Y.ToString("0.00", culture);
                    _window.PosZ.Text = pl.Position.Z.ToString("0.00", culture);
                    _window.LightRange.Text = pl.Range.ToString("0.00", culture);
                }
                else if (light is SpotLight sl)
                {
                    _window.TxtObjType.Text = "Тип объекта: Направленный источник света";
                    _window.PanelCoordinates.Visibility = Visibility.Visible;
                    _window.PanelLightRange.Visibility = Visibility.Visible;
                    _window.PanelDirection.Visibility = Visibility.Visible;
                    _window.PanelSpotAngle.Visibility = Visibility.Visible;

                    _window.PosX.Text = sl.Position.X.ToString("0.00", culture);
                    _window.PosY.Text = sl.Position.Y.ToString("0.00", culture);
                    _window.PosZ.Text = sl.Position.Z.ToString("0.00", culture);
                    _window.DirX.Text = sl.Direction.X.ToString("0.00", culture);
                    _window.DirY.Text = sl.Direction.Y.ToString("0.00", culture);
                    _window.DirZ.Text = sl.Direction.Z.ToString("0.00", culture);
                    _window.LightRange.Text = sl.Range.ToString("0.00", culture);
                    _window.SpotAngle.Text = sl.CutOffAngle.ToString("0.00", culture);
                }
            }

            ShowLightmapPreview(selectedItem);
            IsUpdatingUI = false;
        }

        /// <summary>
        /// Обновеление свойств камеры в интерфейсе
        /// </summary>
        public void RefreshCameraFields(Camera cam)
        {
            IsUpdatingUI = true;
            var culture = CultureInfo.InvariantCulture;

            _window.PosX.Text = cam.Position.X.ToString("0.00", culture);
            _window.PosY.Text = cam.Position.Y.ToString("0.00", culture);
            _window.PosZ.Text = cam.Position.Z.ToString("0.00", culture);
            _window.DirX.Text = cam.Direction.X.ToString("0.00", culture);
            _window.DirY.Text = cam.Direction.Y.ToString("0.00", culture);
            _window.DirZ.Text = cam.Direction.Z.ToString("0.00", culture);

            IsUpdatingUI = false;
        }

        /// <summary>
        /// Обработка изменения свойств объекта в интерфейсе
        /// </summary>
        public ObjectPropertiesData ReadValues(object sender)
        {
            var style = NumberStyles.Any;
            var culture = CultureInfo.InvariantCulture;

            float.TryParse(_window.PosX.Text.Replace(',', '.'), style, culture, out float x);
            float.TryParse(_window.PosY.Text.Replace(',', '.'), style, culture, out float y);
            float.TryParse(_window.PosZ.Text.Replace(',', '.'), style, culture, out float z);

            float.TryParse(_window.DirX.Text.Replace(',', '.'), style, culture, out float dx);
            float.TryParse(_window.DirY.Text.Replace(',', '.'), style, culture, out float dy);
            float.TryParse(_window.DirZ.Text.Replace(',', '.'), style, culture, out float dz);
            Vector3 dir = new Vector3(dx, dy, dz);
            if (dir.Length == 0) dir = new Vector3(0, -1, 0);

            float.TryParse(_window.SpotAngle.Text.Replace(',', '.'), style, culture, out float angle);
            float.TryParse(_window.Radius.Text.Replace(',', '.'), style, culture, out float radius);
            float.TryParse(_window.Side.Text.Replace(',', '.'), style, culture, out float side);
            float.TryParse(_window.CylRadius.Text.Replace(',', '.'), style, culture, out float cylRadius);
            float.TryParse(_window.Height.Text.Replace(',', '.'), style, culture, out float height);

            int.TryParse(_window.ObjResolution.Text, out int lightmapRes);
            lightmapRes = Math.Clamp(lightmapRes, 16, 8192);

            float.TryParse(_window.FOV.Text.Replace(',', '.'), style, culture, out float fov);
            fov = Math.Clamp(fov, 1f, 179.99f);

            float.TryParse(_window.LightRange.Text.Replace(',', '.'), style, culture, out float lightRange);
            lightRange = Math.Max(0.1f, lightRange);

            float reflectivity;
            IsUpdatingUI = true;
            if (sender == _window.SliderReflectivity)
            {
                reflectivity = (float)_window.SliderReflectivity.Value;
                _window.Reflectivity.Text = reflectivity.ToString("0.00", culture);
            }
            else
            {
                float.TryParse(_window.Reflectivity.Text.Replace(',', '.'), style, culture, out reflectivity);
                reflectivity = Math.Clamp(reflectivity, 0f, 1f);
                if (sender == _window.Reflectivity)
                    _window.SliderReflectivity.Value = reflectivity;
            }
            IsUpdatingUI = false;

            return new ObjectPropertiesData
            {
                Position = new Vector3(x, y, z),
                Direction = dir,
                SpotAngle = Math.Clamp(angle, 1f, 180f),
                Radius = radius,
                Side = side,
                CylRadius = cylRadius,
                Height = height,
                Reflectivity = reflectivity,
                LightmapRes = lightmapRes,
                FovDegrees = fov,
                LightIntensity = (float)_window.SliderLightIntensity.Value,
                LightRange = lightRange
            };
        }

        /// <summary>
        /// Выводит световую карту выбранного объекта в область предпросмотра
        /// </summary>
        public void ShowLightmapPreview(object? selectedItem)
        {
            if (selectedItem is SceneObject obj && obj.Lightmap != null)
            {
                int stride = obj.Lightmap.Width * 3;
                BitmapSource bitmap = BitmapSource.Create(
                    obj.Lightmap.Width, obj.Lightmap.Height, 96, 96,
                    PixelFormats.Rgb24, null, obj.Lightmap.Pixels, stride);

                _window.LightmapPreviewImage.Source = bitmap;
            }
            else
            {
                _window.LightmapPreviewImage.Source = null;
            }
        }

        /// <summary>
        /// Обновление цвета и текста кнопки выбора цвета
        /// </summary>
        public void UpdateColorButton(Vector3 color)
        {
            byte r = (byte)(Math.Clamp(color.X, 0f, 1f) * 255);
            byte g = (byte)(Math.Clamp(color.Y, 0f, 1f) * 255);
            byte b = (byte)(Math.Clamp(color.Z, 0f, 1f) * 255);

            _window.TxtColor.Text = $"Цвет: (R:{r} G:{g} B:{b})";
            _window.BtnSelectColor.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(r, g, b));
        }

        /// <summary>
        /// Скрытие всех панелей свойств объектов
        /// </summary>
        private void HideAllDynamicPanels()
        {
            _window.PanelCoordinates.Visibility = Visibility.Collapsed;
            _window.PanelDirection.Visibility = Visibility.Collapsed;
            _window.PanelSpotAngle.Visibility = Visibility.Collapsed;
            _window.PanelMaterial.Visibility = Visibility.Collapsed;
            _window.PanelColor.Visibility = Visibility.Collapsed;
            _window.PanelSphere.Visibility = Visibility.Collapsed;
            _window.PanelCube.Visibility = Visibility.Collapsed;
            _window.PanelCylinder.Visibility = Visibility.Collapsed;
            _window.PanelLight.Visibility = Visibility.Collapsed;
            _window.PanelLightRange.Visibility = Visibility.Collapsed;
            _window.PanelCamera.Visibility = Visibility.Collapsed;
        }
    }
}
