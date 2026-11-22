using OpenTK.Mathematics;
using OpenTK.Wpf;
using RayTracer.Models;
using System;
using System.Globalization;
using System.Windows;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using Point = System.Windows.Point;

namespace RayTracer.Views
{
    /// <summary>
    /// Основное окно приложения
    /// </summary>
    public partial class MainWindow : Window
    {
        private Scene? _scene;
        private readonly OpenGLRenderer _renderer = new OpenGLRenderer();
        private readonly InspectorView _inspector;
        private HelpWindow? _helpWindow;

        private bool _isCommittingRename = false;
        private bool _isUpdatingTraceDepth = false;
        private bool _isOpenGlPaused = false;

        private bool _isRmbDown = false;
        private bool _keyW, _keyA, _keyS, _keyD, _keyShift;
        private Point _lastMousePos;

        private int _traceDepthBeforeEdit = 3;

        public event Action? AddSphereRequested;
        public event Action? AddCubeRequested;
        public event Action? AddCylinderRequested;
        public event Action? AddGlobalLightRequested;
        public event Action? AddSpotLightRequested;
        public event Action? AddPointLightRequested;

        public event Action<object?>? CloneRequested;
        public event Action<object?>? DeleteRequested;
        public event Func<object?, string, bool>? RenameRequested;

        public event Action<object?, ObjectPropertiesData>? PropertiesChanged;
        public event Action<object?, Vector3>? ColorChanged;

        public event Action<int>? RenderRequested;
        public event Action<float, float>? CameraRotateRequested;
        public event Action<int, int, bool, float>? CameraMoveRequested;

        public event Action? OpenSceneRequested;
        public event Action? SaveSceneRequested;
        public event Action? SaveSceneAsRequested;
        public event Func<bool>? ClosingRequested;

        public MainWindow()
        {
            InitializeComponent();

            _inspector = new InspectorView(this);
            _inspector.FieldCommitted += (item, data) => PropertiesChanged?.Invoke(item, data);

            var settings = new GLWpfControlSettings
            {
                MajorVersion = 2,
                MinorVersion = 1,
                RenderContinuously = true
            };
            OpenGlControl.Start(settings);

            BtnRender.Click += (s, e) =>
            {
                _inspector.CommitActiveField();
                RenderRequested?.Invoke(GetTraceDepth());
            };
        }

        /// <summary>
        /// Устанавливает ссылку на модель сцены
        /// </summary>
        public void SetScene(Scene scene)
        {
            _scene = scene;
            _renderer.ClearTextureCache();
            RefreshHierarchy();
            SceneObjectsList.SelectedItem = null;
            _inspector.DisplayObject(null);
        }

        /// <summary>
        /// Обновляет список объектов сцены в левой панели
        /// </summary>
        public void RefreshHierarchy(object? itemToSelect = null)
        {
            if (_scene == null) return;

            SceneObjectsList.Items.Clear();
            SceneObjectsList.Items.Add(_scene.MainCamera);

            foreach (var light in _scene.Lights)
                SceneObjectsList.Items.Add(light);

            foreach (var obj in _scene.Objects)
                SceneObjectsList.Items.Add(obj);

            if (itemToSelect != null)
                SceneObjectsList.SelectedItem = itemToSelect;
        }

        /// <summary>
        /// Принудительно обновляет отображение имён в списке
        /// </summary>
        public void RefreshHierarchyNames()
        {
            System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                SceneObjectsList.Items.Refresh();
            });
        }

        /// <summary>
        /// Обновляет свойсва камеры в интерфейсе
        /// </summary>
        public void RefreshCameraInspector()
        {
            if (_scene != null && SceneObjectsList.SelectedItem == _scene.MainCamera)
            {
                _inspector.RefreshCameraFields(_scene.MainCamera);
            }
        }

        /// <summary>
        /// Установка заголовка окна
        /// </summary>
        /// <param name="title">Заголовок</param>
        public void SetWindowTitle(string title) => Title = title;
        /// <summary>
        /// Утановка статуса
        /// </summary>
        /// <param name="text">Статус</param>
        public void SetStatusText(string text) => StatusText.Text = text;
        /// <summary>
        /// Установка веремени рендера
        /// </summary>
        /// <param name="text">Текст времени рендера</param>
        public void SetRenderTimeText(string text) => RenderTimeText.Text = text;

        /// <summary>
        /// Установка статуса рендера
        /// </summary>
        /// <param name="isRendering">Статус рендера</param>
        public void SetRenderState(bool isRendering)
        {
            BtnRender.IsEnabled = !isRendering;
            if (!isRendering)
            {
                _renderer.ClearTextureCache();
                RadioSolid.IsChecked = true;
                _inspector.ShowLightmapPreview(SceneObjectsList.SelectedItem);
            }
        }

        /// <summary>
        /// Отображение ошибоки
        /// </summary>
        /// <param name="message">Сообщение</param>
        /// <param name="title">Заголовок</param>
        public void ShowError(string message, string title = "Ошибка")
        {
            System.Windows.MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        /// <summary>
        /// Подтверждение действия
        /// </summary>
        /// <param name="message">Сообщение</param>
        /// <param name="title">Заголовок</param>
        /// <returns></returns>
        public bool ConfirmAction(string message, string title)
        {
            return System.Windows.MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
        }

        /// <summary>
        /// Предложение сохранения перед действием
        /// </summary>
        /// <param name="message">Сообщение</param>
        /// <param name="title">Заголовок</param>
        /// <returns>true - сохранить, false - не сохранять, null - отмена</returns>
        public bool? AskSaveBeforeAction(string message, string title)
        {
            var result = System.Windows.MessageBox.Show(message, title, MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes) return true;
            if (result == MessageBoxResult.No) return false;
            return null;
        }

        /// <summary>
        /// Отображение диалогового окна выбора файла
        /// </summary>
        /// <returns>Путь к файлу, null если открыть не удалось</returns>
        public string? ShowOpenFileDialog()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Открыть сцену",
                Filter = "RayTracer Scene (*.json)|*.json"
            };

            SetOpenGLPaused(true);
            try
            {
                return dialog.ShowDialog(this) == true ? dialog.FileName : null;
            }
            finally
            {
                SetOpenGLPaused(false);
            }
        }

        /// <summary>
        /// Отображение диалогового окна сохранения файла
        /// </summary>
        /// <param name="defaultFileName">Имя файла по умолчанию</param>
        /// <returns>Путь к файлу, null если не удалось сохранить</returns>
        public string? ShowSaveFileDialog(string defaultFileName)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Сохранить сцену",
                Filter = "RayTracer Scene (*.json)|*.json",
                DefaultExt = ".json",
                FileName = defaultFileName
            };

            SetOpenGLPaused(true);
            try
            {
                return dialog.ShowDialog(this) == true ? dialog.FileName : null;
            }
            finally
            {
                SetOpenGLPaused(false);
            }
        }

        /// <summary>
        /// Установка паузы для рендера OpenGL
        /// </summary>
        /// <param name="paused">true - если пауза</param>
        private void SetOpenGLPaused(bool paused)
        {
            _isOpenGlPaused = paused;
            OpenGlControl.RenderContinuously = !paused;
        }

        /// <summary>
        /// Рендер OpenGL
        /// </summary>
        /// <param name="delta">Прошедший промежуток времени</param>
        private void OpenGlControl_Render(TimeSpan delta)
        {
            if (_isOpenGlPaused || _scene == null) return;

            bool isSolid = RadioSolid.IsChecked == true;
            bool isGrid = CheckGrid.IsChecked == true;
            _renderer.Render(_scene, OpenGlControl.ActualWidth, OpenGlControl.ActualHeight, isSolid, isGrid);

            // Упарвление камерой
            if (_isRmbDown)
            {
                int dx = 0;
                int dy = 0;
                if (_keyW) dx += 1;
                if (_keyS) dx -= 1;
                if (_keyD) dy += 1;
                if (_keyA) dy -= 1;

                if (dx != 0 || dy != 0)
                    CameraMoveRequested?.Invoke(dx, dy, _keyShift, (float)delta.TotalSeconds);
            }
        }

        private void BtnAddObject_Click(object sender, RoutedEventArgs e) => BtnAddObject.ContextMenu.IsOpen = true;

        private void MenuAddSphere_Click(object sender, RoutedEventArgs e) => AddSphereRequested?.Invoke();
        private void MenuAddCube_Click(object sender, RoutedEventArgs e) => AddCubeRequested?.Invoke();
        private void MenuAddCylinder_Click(object sender, RoutedEventArgs e) => AddCylinderRequested?.Invoke();
        private void MenuAddGlobalLight_Click(object sender, RoutedEventArgs e) => AddGlobalLightRequested?.Invoke();
        private void MenuAddSpotLight_Click(object sender, RoutedEventArgs e) => AddSpotLightRequested?.Invoke();
        private void MenuAddPointLight_Click(object sender, RoutedEventArgs e) => AddPointLightRequested?.Invoke();

        private void MenuClone_Click(object sender, RoutedEventArgs e) => CloneRequested?.Invoke(SceneObjectsList.SelectedItem);
        private void MenuDelete_Click(object sender, RoutedEventArgs e) => DeleteRequested?.Invoke(SceneObjectsList.SelectedItem);

        private void MenuOpenScene_Click(object sender, RoutedEventArgs e) => OpenSceneRequested?.Invoke();
        private void MenuSaveScene_Click(object sender, RoutedEventArgs e) => SaveSceneRequested?.Invoke();
        private void MenuSaveAsScene_Click(object sender, RoutedEventArgs e) => SaveSceneAsRequested?.Invoke();
        private void MenuExit_Click(object sender, RoutedEventArgs e) => Close();

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (ClosingRequested != null && !ClosingRequested.Invoke())
            {
                e.Cancel = true;
            }
        }

        private void MenuHelp_Click(object sender, RoutedEventArgs e) => OpenHelpWindow();

        /// <summary>
        /// Обработка клика ПКМ по списку объектов
        /// </summary>
        private void SceneObjectsList_PreviewMouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            DependencyObject? originalSource = e.OriginalSource as DependencyObject;
            while (originalSource != null && !(originalSource is System.Windows.Controls.ListBoxItem))
            {
                originalSource = (originalSource is System.Windows.Media.Visual || originalSource is System.Windows.Media.Media3D.Visual3D)
                    ? System.Windows.Media.VisualTreeHelper.GetParent(originalSource)
                    : LogicalTreeHelper.GetParent(originalSource);
            }

            if (originalSource is System.Windows.Controls.ListBoxItem item)
                item.IsSelected = true;
        }

        /// <summary>
        /// Обработка клика ЛКМ по списку объектов
        /// </summary>
        private void SceneObjectsList_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            DependencyObject? current = e.OriginalSource as DependencyObject;
            while (current != null && current != SceneObjectsList)
            {
                if (current is System.Windows.Controls.ListBoxItem ||
                    current is System.Windows.Controls.Primitives.ScrollBar)
                {
                    return;
                }

                current = (current is System.Windows.Media.Visual || current is System.Windows.Media.Media3D.Visual3D)
                    ? System.Windows.Media.VisualTreeHelper.GetParent(current)
                    : LogicalTreeHelper.GetParent(current);
            }

            SceneObjectsList.Focus();
            SceneObjectsList.SelectedItem = null;
        }

        /// <summary>
        /// Выбор объекта
        /// </summary>
        private void SceneObjectsList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            _inspector.DisplayObject(SceneObjectsList.SelectedItem);
        }

        /// <summary>
        /// Обработка изменения свойств объекта
        /// </summary>
        private void Properties_Changed(object sender, RoutedEventArgs e)
        {
            if (_inspector == null || _inspector.IsUpdatingUI) return;
            var selected = SceneObjectsList.SelectedItem;
            if (selected == null) return;

            if (_inspector.TryReadLiveValues(sender, out var values))
            {
                PropertiesChanged?.Invoke(selected, values);
            }
        }

        /// <summary>
        /// Обработка изменения слайдера глубины трассировки
        /// </summary>
        private void SliderTraceDepth_Changed(object sender, RoutedEventArgs e)
        {
            if (_isUpdatingTraceDepth || TraceDepth == null) return;
            _isUpdatingTraceDepth = true;
            TraceDepth.Text = ((int)SliderTraceDepth.Value).ToString();
            _isUpdatingTraceDepth = false;
        }

        /// <summary>
        /// Обработка изменения глубины трассировки через текстовое поле
        /// </summary>
        private void TraceDepth_Changed(object sender, RoutedEventArgs e)
        {
            if (_isUpdatingTraceDepth || SliderTraceDepth == null) return;

            if (int.TryParse(TraceDepth.Text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int depth)
                && depth >= 1 && depth <= 10)
            {
                _isUpdatingTraceDepth = true;
                SliderTraceDepth.Value = depth;
                _isUpdatingTraceDepth = false;
            }
        }

        /// <summary>
        /// Обработка потери фокуса полем ввода глубины трассировки
        /// </summary>
        private void TraceDepth_LostFocus(object sender, RoutedEventArgs e)
        {
            CommitTraceDepth();
        }

        /// <summary>
        /// Обработка отпускания клавиш в поле ввода глубины трассировки
        /// </summary>
        private void TraceDepth_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                CommitTraceDepth();
                System.Windows.Input.Keyboard.ClearFocus();
                e.Handled = true;
            }
            else if (e.Key == System.Windows.Input.Key.Escape)
            {
                _isUpdatingTraceDepth = true;
                SliderTraceDepth.Value = _traceDepthBeforeEdit;
                TraceDepth.Text = _traceDepthBeforeEdit.ToString();
                _isUpdatingTraceDepth = false;
                System.Windows.Input.Keyboard.ClearFocus();
                e.Handled = true;
            }
        }

        /// <summary>
        /// Сохранение изменения глубины трасировки
        /// </summary>
        private void CommitTraceDepth()
        {
            if (TraceDepth == null || SliderTraceDepth == null) return;

            _isUpdatingTraceDepth = true;
            if (int.TryParse(TraceDepth.Text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int depth))
            {
                int clamped = Math.Clamp(depth, 1, 10);
                SliderTraceDepth.Value = clamped;
                TraceDepth.Text = clamped.ToString();
            }
            else
            {
                TraceDepth.Text = ((int)SliderTraceDepth.Value).ToString();
            }
            _isUpdatingTraceDepth = false;
        }

        /// <summary>
        /// Получение глубины трассировки
        /// </summary>
        /// <returns></returns>
        private int GetTraceDepth()
        {
            CommitTraceDepth();
            return (int)SliderTraceDepth.Value;
        }

        /// <summary>
        /// Сохранение предыдущего значения поля ввода глубины трассировки
        /// </summary>
        private void TraceDepth_GotKeyboardFocus(object sender, System.Windows.Input.KeyboardFocusChangedEventArgs e)
        {
            _traceDepthBeforeEdit = (int)SliderTraceDepth.Value;
        }

        /// <summary>
        /// Обработка нажатия на кнопку выбора цвета
        /// </summary>
        private void BtnSelectColor_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = SceneObjectsList.SelectedItem;
            Vector3 currColor;

            if (selectedItem is SceneObject obj) currColor = obj.Material.Color;
            else if (selectedItem is Light light) currColor = light.Color;
            else return;

            var dialog = new System.Windows.Forms.ColorDialog
            {
                Color = System.Drawing.Color.FromArgb(
                    255,
                    (int)(currColor.X * 255),
                    (int)(currColor.Y * 255),
                    (int)(currColor.Z * 255))
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                Vector3 newColor = new Vector3(dialog.Color.R / 255f, dialog.Color.G / 255f, dialog.Color.B / 255f);
                _inspector.UpdateColorButton(newColor);
                ColorChanged?.Invoke(selectedItem, newColor);
            }
        }

        /// <summary>
        /// Определение дочернего элемента в интерфейсе
        /// </summary>
        /// <returns>Найденный дочерний элемент, null если не найден</returns>
        private T? FindVisualChild<T>(DependencyObject parent, string name) where T : FrameworkElement
        {
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is T element && element.Name == name) return element;
                var result = FindVisualChild<T>(child, name);
                if (result != null) return result;
            }
            return null;
        }

        /// <summary>
        /// бработка переименования объекта
        /// </summary>
        private void MenuRename_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.MenuItem menuItem && menuItem.DataContext is object obj)
            {
                if (obj is Camera) return;

                var listBoxItem = (System.Windows.Controls.ListBoxItem)SceneObjectsList.ItemContainerGenerator.ContainerFromItem(obj);
                if (listBoxItem != null)
                {
                    var nameText = FindVisualChild<System.Windows.Controls.TextBlock>(listBoxItem, "NameText");
                    var nameInput = FindVisualChild<System.Windows.Controls.TextBox>(listBoxItem, "NameInput");

                    if (nameText != null && nameInput != null)
                    {
                        nameText.Visibility = Visibility.Collapsed;
                        nameInput.Visibility = Visibility.Visible;
                        nameInput.Text = obj.ToString();
                        nameInput.Focus();
                        nameInput.SelectAll();
                    }
                }
            }
        }

        /// <summary>
        /// Обработка переименования
        /// </summary>
        private void NameInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
                CommitRename(sender as System.Windows.Controls.TextBox);
            else if (e.Key == System.Windows.Input.Key.Escape)
                CancelRename(sender as System.Windows.Controls.TextBox);
        }

        /// <summary>
        /// Обработка переименования при потере фокуса
        /// </summary>
        private void NameInput_LostFocus(object sender, RoutedEventArgs e) => CommitRename(sender as System.Windows.Controls.TextBox);

        /// <summary>
        /// Сохранение нового имени объекта
        /// </summary>
        private void CommitRename(System.Windows.Controls.TextBox? textBox)
        {
            if (_isCommittingRename || textBox == null || textBox.Visibility != Visibility.Visible) return;

            _isCommittingRename = true;
            var obj = textBox.DataContext;
            string newName = textBox.Text.Trim();

            CancelRename(textBox);

            if (!string.IsNullOrWhiteSpace(newName) && RenameRequested != null)
            {
                bool success = RenameRequested.Invoke(obj, newName);
                if (!success)
                {
                    textBox.Text = obj?.ToString() ?? string.Empty;
                    System.Windows.MessageBox.Show($"Имя '{newName}' уже используется. Пожалуйста, выберите другое.",
                                "Ошибка переименования", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }

            _isCommittingRename = false;
        }

        /// <summary>
        /// Отмена переименования
        /// </summary>
        /// <param name="textBox"></param>
        private void CancelRename(System.Windows.Controls.TextBox? textBox)
        {
            if (textBox == null) return;
            var grid = System.Windows.Media.VisualTreeHelper.GetParent(textBox) as System.Windows.Controls.Grid;
            if (grid != null)
            {
                var nameText = FindVisualChild<System.Windows.Controls.TextBlock>(grid, "NameText");
                if (nameText != null) nameText.Visibility = Visibility.Visible;
                textBox.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Сброс фокуса
        /// </summary>
        private void ClearFocus_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element) element.Focus();
        }

        /// <summary>
        /// Сброс выбора объекта
        /// </summary>
        private void Deselect_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            SceneObjectsList.SelectedItem = null;
        }

        /// <summary>
        /// Обработка нажатия по области предпросмотра световой карты
        /// </summary>
        private void LightmapPreviewImage_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (LightmapPreviewImage.Source == null) return;

            e.Handled = true;
            var viewer = new LightmapWindow(LightmapPreviewImage.Source) { Owner = this };
            viewer.Show();
            viewer.Activate();
        }

        /// <summary>
        /// Обработка нажатий клавиш клавиатуры
        /// </summary>
        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            var key = (e.Key == System.Windows.Input.Key.ImeProcessed) ? e.ImeProcessedKey : e.Key;

            bool isCtrl = (System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Control)
                          == System.Windows.Input.ModifierKeys.Control;
            bool isShift = (System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Shift)
                           == System.Windows.Input.ModifierKeys.Shift;

            bool isTypingInTextBox = e.OriginalSource is System.Windows.Controls.TextBox;
            bool isRenaming = e.OriginalSource is System.Windows.Controls.TextBox tb && tb.Name == "NameInput";

            if (isCtrl)
            {
                if (!e.IsRepeat)
                {
                    // Сохранить как
                    if (isShift && key == System.Windows.Input.Key.S)
                    {
                        SaveSceneAsRequested?.Invoke();
                        e.Handled = true;
                    }
                    // Сохранить
                    else if (!isShift && key == System.Windows.Input.Key.S)
                    {
                        SaveSceneRequested?.Invoke();
                        e.Handled = true;
                    }
                    // Открыть
                    else if (!isShift && key == System.Windows.Input.Key.O)
                    {
                        OpenSceneRequested?.Invoke();
                        e.Handled = true;
                    }
                    // Клонировать
                    else if (!isShift && !isRenaming && key == System.Windows.Input.Key.D)
                    {
                        CloneRequested?.Invoke(SceneObjectsList.SelectedItem);
                        e.Handled = true;
                    }
                }
                return;
            }
            // Удалить
            if (!e.IsRepeat && !isTypingInTextBox && key == System.Windows.Input.Key.Delete)
            {
                DeleteRequested?.Invoke(SceneObjectsList.SelectedItem);
                e.Handled = true;
                return;
            }
            // Запуск рендера
            if (!e.IsRepeat && !isRenaming && key == System.Windows.Input.Key.F5)
            {
                RenderRequested?.Invoke(GetTraceDepth());
                e.Handled = true;
                return;
            }
            // Справка
            if (!e.IsRepeat && key == System.Windows.Input.Key.F1)
            {
                _inspector.CommitActiveField();
                OpenHelpWindow();
                e.Handled = true;
                return;
            }
            // Перемещение
            if (!isTypingInTextBox || _isRmbDown)
            {
                if (key == System.Windows.Input.Key.W) _keyW = true;
                if (key == System.Windows.Input.Key.A) _keyA = true;
                if (key == System.Windows.Input.Key.S) _keyS = true;
                if (key == System.Windows.Input.Key.D) _keyD = true;
                if (key == System.Windows.Input.Key.LeftShift || key == System.Windows.Input.Key.RightShift) _keyShift = true;
            }
        }

        /// <summary>
        /// Обработка отпускания клавиш клавиатуры
        /// </summary>
        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.W) _keyW = false;
            if (e.Key == System.Windows.Input.Key.A) _keyA = false;
            if (e.Key == System.Windows.Input.Key.S) _keyS = false;
            if (e.Key == System.Windows.Input.Key.D) _keyD = false;
            if (e.Key == System.Windows.Input.Key.LeftShift || e.Key == System.Windows.Input.Key.RightShift) _keyShift = false;
        }

        /// <summary>
        /// Обработка нажатия ПКМ по области OpenGL
        /// </summary>
        private void OpenGlControl_MouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _isRmbDown = true;
            _lastMousePos = e.GetPosition(null);
            OpenGlControl.CaptureMouse();
        }

        /// <summary>
        /// Обработка отпускания ПКМ по области OprnGL
        /// </summary>
        private void OpenGlControl_MouseRightButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _isRmbDown = false;
            OpenGlControl.ReleaseMouseCapture();
        }

        /// <summary>
        /// Обработка перемещения мыши по области OpenGL
        /// </summary>
        private void OpenGlControl_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (_isRmbDown)
            {
                Point currentPos = e.GetPosition(null);
                float dx = (float)(currentPos.X - _lastMousePos.X);
                float dy = (float)(currentPos.Y - _lastMousePos.Y);
                _lastMousePos = currentPos;

                CameraRotateRequested?.Invoke(dx, dy);
            }
        }

        /// <summary>
        /// Открытие окна справки
        /// </summary>
        private void OpenHelpWindow()
        {
            if (_helpWindow != null && _helpWindow.IsVisible)
            {
                _helpWindow.Activate();
                return;
            }

            _helpWindow = new HelpWindow { Owner = this };
            _helpWindow.Closed += (s, e) => _helpWindow = null;
            _helpWindow.Show();
        }
    }
}