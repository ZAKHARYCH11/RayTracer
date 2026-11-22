using OpenTK.Mathematics;
using RayTracer.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using Key = System.Windows.Input.Key;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using Keyboard = System.Windows.Input.Keyboard;
using KeyboardFocusChangedEventArgs = System.Windows.Input.KeyboardFocusChangedEventArgs;
using TextBox = System.Windows.Controls.TextBox;

namespace RayTracer.Views
{
    /// <summary>
    /// Перечисление типов свойств объектов
    /// </summary>
    public enum PropertyEditGroup
    {
        None,
        Position,
        Rotation,
        CameraFov,
        SphereRadius,
        CubeSide,
        CylinderSize,
        LightmapRes,
        Reflectivity,
        LightIntensity,
        LightRange,
        SpotAngle
    }

    /// <summary>
    /// Считанные данные свойств объекта
    /// </summary>
    public struct ObjectPropertiesData
    {
        public PropertyEditGroup ChangedGroup;
        public Vector3 Position;
        public float RotX;
        public float RotY;
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
    /// Класс, отвечающий за отображение, считывание и валидацию панели свойств выбранного объекта
    /// </summary>
    public class InspectorView
    {
        private readonly MainWindow _window;
        private readonly Dictionary<TextBox, PropertyEditGroup> _fieldGroups;
        private readonly NumericDragHelper _dragHelper = new NumericDragHelper();

        private object? _currentItem;
        private TextBox? _focusedTextBox;
        private string _textBeforeEdit = string.Empty;

        private float _minObjSize = 0.01f;
        private float _maxObjSize = 10000f;
        private float _minPitch = -90f;
        private float _maxPitch = 90f;
        private float _minPitchCam = -89f;
        private float _maxPitchCam = 89f;
        private float _minFOV = 1f;
        private float _maxFOV = 179.99f;
        private float _minLightmapRes = 16;
        private float _maxLightmapRes = 8192;
        private float _minReflectivity = 0f;
        private float _maxReflectivity = 1f;
        private float _minLightRange = 0.1f;
        private float _maxLightRange = 10000f;
        private float _minLightIntensity = 0f;
        private float _maxLightIntensity = 5f;
        private float _minSpotAngle = 1f;
        private float _maxSpotAngle = 89f;

        public bool IsUpdatingUI { get; private set; } = false;

        /// <summary>
        /// Событие подтверждения изменённых свойств
        /// </summary>
        public event Action<object?, ObjectPropertiesData>? FieldCommitted;

        public InspectorView(MainWindow window)
        {
            _window = window;

            _fieldGroups = new Dictionary<TextBox, PropertyEditGroup>
            {
                { _window.PosX, PropertyEditGroup.Position },
                { _window.PosY, PropertyEditGroup.Position },
                { _window.PosZ, PropertyEditGroup.Position },
                { _window.RotX, PropertyEditGroup.Rotation },
                { _window.RotY, PropertyEditGroup.Rotation },
                { _window.FOV, PropertyEditGroup.CameraFov },
                { _window.Radius, PropertyEditGroup.SphereRadius },
                { _window.Side, PropertyEditGroup.CubeSide },
                { _window.CylRadius, PropertyEditGroup.CylinderSize },
                { _window.Height, PropertyEditGroup.CylinderSize },
                { _window.ObjResolution, PropertyEditGroup.LightmapRes },
                { _window.Reflectivity, PropertyEditGroup.Reflectivity },
                { _window.LightIntensity, PropertyEditGroup.LightIntensity },
                { _window.LightRange, PropertyEditGroup.LightRange },
                { _window.SpotAngle, PropertyEditGroup.SpotAngle }
            };

            foreach (var tb in _fieldGroups.Keys)
            {
                tb.GotKeyboardFocus += Field_GotKeyboardFocus;
                tb.LostFocus += Field_LostFocus;
                tb.KeyDown += Field_KeyDown;
            }

            _dragHelper.Attach(_window.LblPosX, _window.PosX, 0.02f, onBeforeDrag: CommitActiveField);
            _dragHelper.Attach(_window.LblPosY, _window.PosY, 0.02f, onBeforeDrag: CommitActiveField);
            _dragHelper.Attach(_window.LblPosZ, _window.PosZ, 0.02f, onBeforeDrag: CommitActiveField);

            _dragHelper.Attach(_window.LblRotX, _window.RotX, 0.25f,
                () => (_currentItem is Camera) ? (_minPitchCam, _maxPitchCam) : (_minPitch, _maxPitch),
                onBeforeDrag: CommitActiveField);
            _dragHelper.Attach(_window.LblRotY, _window.RotY, 0.25f, onBeforeDrag: CommitActiveField);

            _dragHelper.Attach(_window.LblFov, _window.FOV, 0.25f, _minFOV, _maxFOV, onBeforeDrag: CommitActiveField);
            _dragHelper.Attach(_window.LblRadius, _window.Radius, 0.02f, _minObjSize, _maxObjSize, onBeforeDrag: CommitActiveField);
            _dragHelper.Attach(_window.LblSide, _window.Side, 0.02f, _minObjSize, _maxObjSize, onBeforeDrag: CommitActiveField);
            _dragHelper.Attach(_window.LblCylRadius, _window.CylRadius, 0.02f, _minObjSize, _maxObjSize, onBeforeDrag: CommitActiveField);
            _dragHelper.Attach(_window.LblHeight, _window.Height, 0.02f, _minObjSize, _maxObjSize, onBeforeDrag: CommitActiveField);
            _dragHelper.Attach(_window.LblObjResolution, _window.ObjResolution, 2.0f, _minLightmapRes, _maxLightmapRes, isInteger: true, onBeforeDrag: CommitActiveField);
            _dragHelper.Attach(_window.LblReflectivity, _window.Reflectivity, 0.005f, _minReflectivity, _maxReflectivity, onBeforeDrag: CommitActiveField);
            _dragHelper.Attach(_window.LblLightIntensity, _window.LightIntensity, 0.02f, _minLightIntensity, _maxLightIntensity, onBeforeDrag: CommitActiveField);
            _dragHelper.Attach(_window.LblLightRange, _window.LightRange, 0.05f, _minLightRange, _maxLightRange, onBeforeDrag: CommitActiveField);
            _dragHelper.Attach(_window.LblSpotAngle, _window.SpotAngle, 0.25f, _minSpotAngle, _maxSpotAngle, onBeforeDrag: CommitActiveField);
        }

        /// <summary>
        /// Отображение свойств выбранного объекта
        /// </summary>
        /// <param name="selectedItem">Выбранный объект</param>
        public void DisplayObject(object? selectedItem)
        {
            if (_currentItem != null && _currentItem != selectedItem)
            {
                CommitActiveField();
            }

            _currentItem = selectedItem;
            _focusedTextBox = null;

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

                var (pitch, yaw) = RotationUtils.DirectionToAngles(cam.Direction);
                _window.RotX.Text = pitch.ToString("0.00", culture);
                _window.RotY.Text = yaw.ToString("0.00", culture);

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
                _window.LightIntensity.Text = light.Intensity.ToString("0.00", culture);
                UpdateColorButton(light.Color);

                if (light is GlobalLight gl)
                {
                    _window.TxtObjType.Text = "Тип объекта: Глобальный источник света";
                    _window.PanelDirection.Visibility = Visibility.Visible;

                    var (pitch, yaw) = RotationUtils.DirectionToAngles(gl.Direction);
                    _window.RotX.Text = pitch.ToString("0.00", culture);
                    _window.RotY.Text = yaw.ToString("0.00", culture);
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

                    var (pitch, yaw) = RotationUtils.DirectionToAngles(sl.Direction);
                    _window.RotX.Text = pitch.ToString("0.00", culture);
                    _window.RotY.Text = yaw.ToString("0.00", culture);

                    _window.LightRange.Text = sl.Range.ToString("0.00", culture);
                    _window.SpotAngle.Text = sl.CutOffAngle.ToString("0.00", culture);
                }
            }

            ShowLightmapPreview(selectedItem);
            IsUpdatingUI = false;
        }

        /// <summary>
        /// Обновление панелей свойств камеры
        /// </summary>
        /// <param name="cam">Камера</param>
        public void RefreshCameraFields(Camera cam)
        {
            IsUpdatingUI = true;
            var culture = CultureInfo.InvariantCulture;

            _window.PosX.Text = cam.Position.X.ToString("0.00", culture);
            _window.PosY.Text = cam.Position.Y.ToString("0.00", culture);
            _window.PosZ.Text = cam.Position.Z.ToString("0.00", culture);

            var (pitch, yaw) = RotationUtils.DirectionToAngles(cam.Direction);
            _window.RotX.Text = pitch.ToString("0.00", culture);
            _window.RotY.Text = yaw.ToString("0.00", culture);

            IsUpdatingUI = false;
        }

        /// <summary>
        /// Считывает вводимые значения из поля ввода
        /// </summary>
        /// <param name="sender">Объект, от которого пришло изменение</param>
        /// <param name="data">Выходное значение измененных свойст объекта</param>
        /// <returns>false - если записано некорректное значение в поле ввода</returns>
        public bool TryReadLiveValues(object sender, out ObjectPropertiesData data)
        {
            data = default;
            if (_currentItem == null) return false;

            var culture = CultureInfo.InvariantCulture;

            if (sender == _window.SliderReflectivity)
            {
                float refl = (float)_window.SliderReflectivity.Value;
                IsUpdatingUI = true;
                _window.Reflectivity.Text = refl.ToString("0.00", culture);
                IsUpdatingUI = false;

                data.ChangedGroup = PropertyEditGroup.Reflectivity;
                data.Reflectivity = refl;
                return true;
            }

            if (sender == _window.SliderLightIntensity)
            {
                float intensity = (float)_window.SliderLightIntensity.Value;
                IsUpdatingUI = true;
                _window.LightIntensity.Text = intensity.ToString("0.00", culture);
                IsUpdatingUI = false;

                data.ChangedGroup = PropertyEditGroup.LightIntensity;
                data.LightIntensity = intensity;
                return true;
            }

            if (!(sender is TextBox tb) || !_fieldGroups.TryGetValue(tb, out var group))
                return false;

            return TryBuildValidatedGroupData(group, _currentItem, out data);
        }

        /// <summary>
        /// Сохраняет значения активного поля ввода
        /// </summary>
        public void CommitActiveField()
        {
            if (_focusedTextBox != null)
            {
                CommitField(_focusedTextBox);
            }
        }

        /// <summary>
        /// Получение поля ввода с фокусом
        /// </summary>
        private void Field_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (sender is TextBox tb)
            {
                _focusedTextBox = tb;
                _textBeforeEdit = tb.Text;
            }
        }

        /// <summary>
        /// Обработка потери фокуса полем ввода
        /// </summary>
        private void Field_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb && _focusedTextBox == tb)
            {
                CommitField(tb);
                _focusedTextBox = null;
            }
        }

        /// <summary>
        /// Обработка нажатий клавиш в поле ввода
        /// </summary>
        private void Field_KeyDown(object sender, KeyEventArgs e)
        {
            if (!(sender is TextBox tb)) return;

            if (e.Key == Key.Enter)
            {
                CommitField(tb);
                _focusedTextBox = null;
                Keyboard.ClearFocus();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                IsUpdatingUI = true;
                tb.Text = _textBeforeEdit;
                IsUpdatingUI = false;

                CommitField(tb);
                _focusedTextBox = null;
                Keyboard.ClearFocus();
                e.Handled = true;
            }
        }

        /// <summary>
        /// Валидация введенных значений и их сохранение
        /// </summary>
        private void CommitField(TextBox tb)
        {
            if (IsUpdatingUI || _currentItem == null) return;
            if (!_fieldGroups.TryGetValue(tb, out var group)) return;

            var culture = CultureInfo.InvariantCulture;
            IsUpdatingUI = true;

            if (TryParseFloat(tb.Text, out float val))
            {
                if (tb == _window.PosX || tb == _window.PosY || tb == _window.PosZ || tb == _window.RotY)
                {
                    tb.Text = val.ToString("0.00", culture);
                }
                else if (tb == _window.RotX)
                {
                    float minPitch = (_currentItem is Camera) ? _minPitchCam : _minPitch;
                    float maxPitch = (_currentItem is Camera) ? _maxPitchCam : _maxPitch;
                    tb.Text = Math.Clamp(val, minPitch, maxPitch).ToString("0.00", culture);
                }
                else if (tb == _window.FOV)
                {
                    tb.Text = Math.Clamp(Math.Abs(val), _minFOV, _maxFOV).ToString("0.00", culture);
                }
                else if (tb == _window.Radius || tb == _window.Side || tb == _window.CylRadius || tb == _window.Height)
                {
                    tb.Text = Math.Clamp(Math.Abs(val), _minObjSize, _maxObjSize).ToString("0.00", culture);
                }
                else if (tb == _window.LightIntensity)
                {
                    float clamped = Math.Clamp(val, _minLightIntensity, _maxLightIntensity);
                    tb.Text = clamped.ToString("0.00", culture);
                    _window.SliderLightIntensity.Value = clamped;
                }
                else if (tb == _window.LightRange)
                {
                    tb.Text = Math.Clamp(Math.Abs(val), _minLightRange, _maxLightRange).ToString("0.00", culture);
                }
                else if (tb == _window.SpotAngle)
                {
                    tb.Text = Math.Clamp(Math.Abs(val), _minSpotAngle, _maxSpotAngle).ToString("0.00", culture);
                }
                else if (tb == _window.Reflectivity)
                {
                    float clamped = Math.Clamp(val, _minReflectivity, _maxReflectivity);
                    tb.Text = clamped.ToString("0.00", culture);
                    _window.SliderReflectivity.Value = clamped;
                }
                else if (tb == _window.ObjResolution)
                {
                    int res = Math.Clamp((int)Math.Abs(Math.Round(val)), (int)_minLightmapRes, (int)_maxLightmapRes);
                    tb.Text = res.ToString();
                }
            }
            else
            {
                tb.Text = GetCurrentModelText(tb, _currentItem);
                if (tb == _window.Reflectivity && _currentItem is SceneObject obj)
                    _window.SliderReflectivity.Value = obj.Material.Reflectivity;
                else if (tb == _window.LightIntensity && _currentItem is Light light)
                    _window.SliderLightIntensity.Value = light.Intensity;
            }

            IsUpdatingUI = false;

            if (TryBuildValidatedGroupData(group, _currentItem, out var data))
            {
                FieldCommitted?.Invoke(_currentItem, data);
            }
        }

        /// <summary>
        /// Создает список измененных свойств объекта
        /// </summary>
        /// <param name="group">Группа свойств</param>
        /// <param name="item">Объект</param>
        /// <param name="data">Свойства объекта</param>
        /// <returns>true - если успешное создание</returns>
        private bool TryBuildValidatedGroupData(PropertyEditGroup group, object item, out ObjectPropertiesData data)
        {
            data = new ObjectPropertiesData { ChangedGroup = group };

            switch (group)
            {
                case PropertyEditGroup.Position:
                    if (!TryParseFloat(_window.PosX.Text, out float x) ||
                        !TryParseFloat(_window.PosY.Text, out float y) ||
                        !TryParseFloat(_window.PosZ.Text, out float z))
                        return false;
                    data.Position = new Vector3(x, y, z);
                    return true;

                case PropertyEditGroup.Rotation:
                    if (!TryParseFloat(_window.RotX.Text, out float rx) ||
                        !TryParseFloat(_window.RotY.Text, out float ry))
                        return false;
                    float minPitch = (item is Camera) ? _minPitchCam : _minPitch;
                    float maxPitch = (item is Camera) ? _maxPitchCam : _maxPitch;
                    data.RotX = Math.Clamp(rx, minPitch, maxPitch);
                    data.RotY = ry;
                    return true;

                case PropertyEditGroup.CameraFov:
                    if (!TryParseFloat(_window.FOV.Text, out float fov)) return false;
                    data.FovDegrees = Math.Clamp(Math.Abs(fov), _minFOV, _maxFOV);
                    return true;

                case PropertyEditGroup.SphereRadius:
                    if (!TryParseFloat(_window.Radius.Text, out float r)) return false;
                    data.Radius = Math.Clamp(Math.Abs(r), _minObjSize, _maxObjSize);
                    return true;

                case PropertyEditGroup.CubeSide:
                    if (!TryParseFloat(_window.Side.Text, out float s)) return false;
                    data.Side = Math.Clamp(Math.Abs(s), _minObjSize, _maxObjSize);
                    return true;

                case PropertyEditGroup.CylinderSize:
                    if (!TryParseFloat(_window.CylRadius.Text, out float cr) ||
                        !TryParseFloat(_window.Height.Text, out float h))
                        return false;
                    data.CylRadius = Math.Clamp(Math.Abs(cr), _minObjSize, _maxObjSize);
                    data.Height = Math.Clamp(Math.Abs(h), _minObjSize, _maxObjSize);
                    return true;

                case PropertyEditGroup.LightmapRes:
                    if (!int.TryParse(_window.ObjResolution.Text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int res))
                        return false;
                    data.LightmapRes = Math.Clamp(Math.Abs(res), (int)_minLightmapRes, (int)_maxLightmapRes);
                    return true;

                case PropertyEditGroup.Reflectivity:
                    if (!TryParseFloat(_window.Reflectivity.Text, out float refl)) return false;
                    refl = Math.Clamp(refl, _minReflectivity, _maxReflectivity);
                    IsUpdatingUI = true;
                    _window.SliderReflectivity.Value = refl;
                    IsUpdatingUI = false;
                    data.Reflectivity = refl;
                    return true;

                case PropertyEditGroup.LightIntensity:
                    if (!TryParseFloat(_window.LightIntensity.Text, out float intensity)) return false;
                    intensity = Math.Clamp(intensity, _minLightIntensity, _maxLightIntensity);
                    IsUpdatingUI = true;
                    _window.SliderLightIntensity.Value = intensity;
                    IsUpdatingUI = false;
                    data.LightIntensity = intensity;
                    return true;

                case PropertyEditGroup.LightRange:
                    if (!TryParseFloat(_window.LightRange.Text, out float range)) return false;
                    data.LightRange = Math.Clamp(Math.Abs(range), _minLightRange, _maxLightRange);
                    return true;

                case PropertyEditGroup.SpotAngle:
                    if (!TryParseFloat(_window.SpotAngle.Text, out float angle)) return false;
                    data.SpotAngle = Math.Clamp(Math.Abs(angle), _minSpotAngle, _maxSpotAngle);
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Получение текста для текущего поля ввода
        /// </summary>
        /// <param name="tb">Выбранный TextBox</param>
        /// <param name="item">Выбранный объект</param>
        /// <returns>Строка для текущего TextBox</returns>
        private string GetCurrentModelText(TextBox tb, object item)
        {
            var c = CultureInfo.InvariantCulture;

            if (item is Camera cam)
            {
                if (tb == _window.PosX) return cam.Position.X.ToString("0.00", c);
                if (tb == _window.PosY) return cam.Position.Y.ToString("0.00", c);
                if (tb == _window.PosZ) return cam.Position.Z.ToString("0.00", c);
                if (tb == _window.RotX) return RotationUtils.DirectionToAngles(cam.Direction).pitchDeg.ToString("0.00", c);
                if (tb == _window.RotY) return RotationUtils.DirectionToAngles(cam.Direction).yawDeg.ToString("0.00", c);
                if (tb == _window.FOV) return cam.FovDegrees.ToString("0.00", c);
            }
            else if (item is SceneObject obj)
            {
                if (tb == _window.PosX) return obj.Center.X.ToString("0.00", c);
                if (tb == _window.PosY) return obj.Center.Y.ToString("0.00", c);
                if (tb == _window.PosZ) return obj.Center.Z.ToString("0.00", c);
                if (tb == _window.Reflectivity) return obj.Material.Reflectivity.ToString("0.00", c);
                if (tb == _window.ObjResolution) return obj.LightmapRes.ToString();
                if (tb == _window.Radius && obj is Sphere sp) return sp.Radius.ToString("0.00", c);
                if (tb == _window.Side && obj is Cube cb) return cb.Side.ToString("0.00", c);
                if (tb == _window.CylRadius && obj is Cylinder cy1) return cy1.Radius.ToString("0.00", c);
                if (tb == _window.Height && obj is Cylinder cy2) return cy2.Height.ToString("0.00", c);
            }
            else if (item is Light light)
            {
                if (tb == _window.PosX && light is PointLight pl1) return pl1.Position.X.ToString("0.00", c);
                if (tb == _window.PosY && light is PointLight pl2) return pl2.Position.Y.ToString("0.00", c);
                if (tb == _window.PosZ && light is PointLight pl3) return pl3.Position.Z.ToString("0.00", c);
                if (tb == _window.LightIntensity) return light.Intensity.ToString("0.00", c);
                if (tb == _window.LightRange && light is PointLight pl4) return pl4.Range.ToString("0.00", c);

                if (tb == _window.RotX && light is GlobalLight gl1) return RotationUtils.DirectionToAngles(gl1.Direction).pitchDeg.ToString("0.00", c);
                if (tb == _window.RotY && light is GlobalLight gl2) return RotationUtils.DirectionToAngles(gl2.Direction).yawDeg.ToString("0.00", c);

                if (light is SpotLight sl)
                {
                    if (tb == _window.PosX) return sl.Position.X.ToString("0.00", c);
                    if (tb == _window.PosY) return sl.Position.Y.ToString("0.00", c);
                    if (tb == _window.PosZ) return sl.Position.Z.ToString("0.00", c);
                    if (tb == _window.RotX) return RotationUtils.DirectionToAngles(sl.Direction).pitchDeg.ToString("0.00", c);
                    if (tb == _window.RotY) return RotationUtils.DirectionToAngles(sl.Direction).yawDeg.ToString("0.00", c);
                    if (tb == _window.LightRange) return sl.Range.ToString("0.00", c);
                    if (tb == _window.SpotAngle) return sl.CutOffAngle.ToString("0.00", c);
                }
            }

            return "0.00";
        }

        /// <summary>
        /// Попытка считать float значение
        /// </summary>
        /// <param name="text">Текст, из которого читается число</param>
        /// <param name="value">Считанное число</param>
        /// <returns>true - если успешное чтение</returns>
        private static bool TryParseFloat(string text, out float value)
        {
            string normalized = text.Trim().Replace(',', '.');
            if (!float.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                return false;

            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        /// <summary>
        /// Отображение световой карты в окне предпросмотра
        /// </summary>
        /// <param name="selectedItem"></param>
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
        /// Обновление кнопки выбора цвета
        /// </summary>
        /// <param name="color">Цвет</param>
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