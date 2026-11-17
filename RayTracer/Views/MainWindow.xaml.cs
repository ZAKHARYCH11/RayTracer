using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Wpf;
using RayTracer.Controllers;
using RayTracer.Models;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using Point = System.Windows.Point;


namespace RayTracer.Views
{
    public partial class MainWindow : Window
    {
        private MainController _controller;

        private bool _isCommittingRename = false;

        private bool _isRmbDown = false;
        private bool _keyW, _keyA, _keyS, _keyD, _keyShift;
        private Point _lastMousePos;

        private bool _isOpenGlPaused = false;

        private readonly OpenGLRenderer _renderer = new OpenGLRenderer();

        /// <summary>
        /// Основной контроллер
        /// </summary>
        public MainController Controller
        {
            get { return _controller; }
            private set { _controller = value; }
        }

        public MainWindow()
        {
            InitializeComponent();

            var settings = new GLWpfControlSettings
            {
                MajorVersion = 2,
                MinorVersion = 1,
                RenderContinuously = true
            };
            OpenGlControl.Start(settings);

            _controller = new MainController(this);
        }

        /// <summary>
        /// Отрисовка с помощью OpenGL
        /// </summary>
        /// <param name="delta">Пройденное время между кадрами</param>
        private void OpenGlControl_Render(TimeSpan delta)
        {
            if (_isOpenGlPaused || Controller == null || Controller.Scene == null) return;

            bool isSolid = RadioSolid.IsChecked == true;
            _renderer.Render(Controller.Scene, OpenGlControl.ActualWidth, OpenGlControl.ActualHeight, isSolid);

            // Движение камеры
            if (_isRmbDown)
            {
                int dx = 0;
                int dy = 0;
                if (_keyW) dx += 1;
                if (_keyS) dx -= 1;
                if (_keyD) dy += 1;
                if (_keyA) dy -= 1;
                Controller.MoveCamera(dx, dy, _keyShift, (float)delta.TotalSeconds);
            }
        }

        /// <summary>
        /// Установка паузы на рендер OpenGL
        /// </summary>
        public void SetOpenGLPaused(bool paused)
        {
            _isOpenGlPaused = paused;
            OpenGlControl.RenderContinuously = !paused;
        }

        /// <summary>
        /// Помечает кэш видеокарты на безопасную очистку в следующем кадре рендера
        /// </summary>
        public void ClearTextureCache()
        {
            _renderer.ClearTextureCache();
        }

        /// <summary>
        /// Нажатие кнопки добавления объекта
        /// </summary>
        private void BtnAddObject_Click(object sender, RoutedEventArgs e)
        {
            BtnAddObject.ContextMenu.IsOpen = true;
        }

        /// <summary>
        /// Нажатие кнопки добавления сферы
        /// </summary>
        private void MenuAddSphere_Click(object sender, RoutedEventArgs e) => Controller?.AddSphere();
        /// <summary>
        /// Нажатие кнопки добавления куба
        /// </summary>
        private void MenuAddCube_Click(object sender, RoutedEventArgs e) => Controller?.AddCube();
        /// <summary>
        /// Нажатие кнопки добавления цилиндра
        /// </summary>
        private void MenuAddCylinder_Click(object sender, RoutedEventArgs e) => Controller?.AddCylinder();
        /// <summary>
        /// Нажатие кнопки добавления глобального света
        /// </summary>
        private void MenuAddGlobalLight_Click(object sender, RoutedEventArgs e) => Controller?.AddGlobalLight();
        /// <summary>
        /// Нажатие кнопки добавления направленного света
        /// </summary>
        private void MenuAddSpotLight_Click(object sender, RoutedEventArgs e) => Controller?.AddSpotLight();
        /// <summary>
        /// Нажатие кнопки добавления точечного света
        /// </summary>
        private void MenuAddPointLight_Click(object sender, RoutedEventArgs e) => Controller?.AddPointLight();
        /// <summary>
        /// Нажатие кнопки клонирования объекта
        /// </summary>
        private void MenuClone_Click(object sender, RoutedEventArgs e) => Controller?.CloneSelected();
        /// <summary>
        /// Нажатие кнопки удаления объекта
        /// </summary>
        private void MenuDelete_Click(object sender, RoutedEventArgs e) => Controller?.DeleteSelected();
        /// <summary>
        /// Нажатие кнопки открытия сцены
        /// </summary>
        private void MenuOpenScene_Click(object sender, RoutedEventArgs e) => Controller?.LoadScene();
        /// <summary>
        /// Нажатие кнопки сохранения сцены
        /// </summary>
        private void MenuSaveScene_Click(object sender, RoutedEventArgs e) => Controller?.SaveScene();
        /// <summary>
        /// Нажатие кнопки сохранить как
        /// </summary>
        private void MenuSaveAsScene_Click(object sender, RoutedEventArgs e) => Controller?.SaveSceneAs();
        /// <summary>
        /// Нажатие кнопки выхода
        /// </summary>
        private void MenuExit_Click(object sender, RoutedEventArgs e) => Close();

        /// <summary>
        /// Закрытие приложения
        /// </summary>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (Controller != null && !Controller.ConfirmUnsavedChanges())
            {
                e.Cancel = true;
            }
        }

        /// <summary>
        /// Нажатие ПКМ по элементу списка объектов
        /// </summary>
        private void SceneObjectsList_PreviewMouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            DependencyObject originalSource = (DependencyObject)e.OriginalSource;

            while (originalSource != null && !(originalSource is System.Windows.Controls.ListBoxItem))
            {
                originalSource = System.Windows.Media.VisualTreeHelper.GetParent(originalSource);
            }

            if (originalSource is System.Windows.Controls.ListBoxItem item)
            {
                item.IsSelected = true;
            }
        }

        /// <summary>
        /// Выбор объекта в списке
        /// </summary>
        private void SceneObjectsList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (Controller != null)
            {
                Controller.OnObjectSelected(SceneObjectsList.SelectedItem);
            }
        }

        /// <summary>
        /// Изменение свойств объекта
        /// </summary>
        private void Properties_Changed(object sender, RoutedEventArgs e)
        {
            if (Controller != null)
            {
                Controller.OnPropertiesChanged();
            }
        }

        /// <summary>
        /// Изменение значения слайдера глубины трассировки
        /// </summary>
        private void SliderTraceDepth_Changed(object sender, RoutedEventArgs e)
        {
            if(Controller != null)
            {
                Controller.OnSliderTraceDepthChanged();
            }
        }

        /// <summary>
        /// Изменение значения глубины трассировки
        /// </summary>
        private void TraceDepth_Changed(object sender, RoutedEventArgs e)
        {
            if (Controller != null)
            {
                Controller.OnTraceDepthChanged();
            }
        }

        /// <summary>
        /// Нажатие кнопки выбора цвета
        /// </summary>
        private void BtnSelectColor_Click(object sender, RoutedEventArgs e)
        {
            if (Controller != null)
            {
                Controller.SelectColor();
            }
        }

        /// <summary>
        /// Поиск дочернего элемента
        /// </summary>
        private T FindVisualChild<T>(DependencyObject parent, string name) where T : FrameworkElement
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
        /// Нажатие кнопки переименования объекта
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
        /// Завершение переименования объекта
        /// </summary>
        private void NameInput_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
                CommitRename(sender as System.Windows.Controls.TextBox);
            else if (e.Key == System.Windows.Input.Key.Escape)
                CancelRename(sender as System.Windows.Controls.TextBox);
        }

        /// <summary>
        /// Потеря фокуса поля ввода имени
        /// </summary>
        private void NameInput_LostFocus(object sender, RoutedEventArgs e)
        {
            CommitRename(sender as System.Windows.Controls.TextBox);
        }

        /// <summary>
        /// Сохранение нового имени
        /// </summary>
        private void CommitRename(System.Windows.Controls.TextBox textBox)
        {
            if (_isCommittingRename || textBox == null || textBox.Visibility != Visibility.Visible) return;

            _isCommittingRename = true;

            var obj = textBox.DataContext;
            string newName = textBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(newName))
            {
                CancelRename(textBox);
                return;
            }

            CancelRename(textBox);

            if (Controller != null)
            {
                bool success = Controller.TryRenameObject(obj, newName);
                if (!success)
                {
                    textBox.Text = obj.ToString();
                    System.Windows.MessageBox.Show($"Имя '{newName}' уже используется. Пожалуйста, выберите другое.",
                                "Ошибка переименования", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }

            _isCommittingRename = false;
        }
        
        /// <summary>
        /// Отмена переименования объекта
        /// </summary>
        private void CancelRename(System.Windows.Controls.TextBox textBox)
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
        /// Сброс фокуса элемента
        /// </summary>
        private void ClearFocus_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element)
            {
                element.Focus();
            }
        }

        /// <summary>
        /// Снятие выделения при клике ЛКМ по пустому фону левой панели или по 3D-окну
        /// </summary>
        private void Deselect_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            SceneObjectsList.SelectedItem = null;
        }

        /// <summary>
        /// Снятие выделения при клике ЛКМ по пустой области внутри самого списка объектов
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
        /// Нажатие по изображения световой карты
        /// </summary>
        private void LightmapPreviewImage_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (LightmapPreviewImage.Source == null) return;

            e.Handled = true;

            LightmapWindow viewer = new LightmapWindow(LightmapPreviewImage.Source);

            viewer.Owner = this;

            viewer.Show();
            viewer.Activate();
        }

        /// <summary>
        /// Обработка нажатия кнопок
        /// </summary>
        private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
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
                        Controller?.SaveSceneAs();
                        e.Handled = true;
                    }
                    // Сохранить
                    else if (!isShift && key == System.Windows.Input.Key.S)
                    {
                        Controller?.SaveScene();
                        e.Handled = true;
                    }
                    // Открыть
                    else if (!isShift && key == System.Windows.Input.Key.O)
                    {
                        Controller?.LoadScene();
                        e.Handled = true;
                    }
                    // Клонировать объект
                    else if (!isShift && !isRenaming && key == System.Windows.Input.Key.D)
                    {
                        Controller?.CloneSelected();
                        e.Handled = true;
                    }
                }
                return;
            }

            // Удаление
            if (!e.IsRepeat && !isTypingInTextBox && key == System.Windows.Input.Key.Delete)
            {
                Controller?.DeleteSelected();
                e.Handled = true;
                return;
            }

            // Запуск рендера
            if (!e.IsRepeat && !isRenaming && key == System.Windows.Input.Key.F5)
            {
                Controller?.StartRender();
                e.Handled = true;
                return;
            }

            if (!isTypingInTextBox || _isRmbDown)
            {
                if (key == System.Windows.Input.Key.W) _keyW = true;
                if (key == System.Windows.Input.Key.A) _keyA = true;
                if (key == System.Windows.Input.Key.S) _keyS = true;
                if (key == System.Windows.Input.Key.D) _keyD = true;
                if (key == System.Windows.Input.Key.LeftShift) _keyShift = true;
            }
        }

        /// <summary>
        /// Обработка отжатия кнопок
        /// </summary>
        private void Window_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.W) _keyW = false;
            if (e.Key == System.Windows.Input.Key.A) _keyA = false;
            if (e.Key == System.Windows.Input.Key.S) _keyS = false;
            if (e.Key == System.Windows.Input.Key.D) _keyD = false;
            if (e.Key == System.Windows.Input.Key.LeftShift) _keyShift = false;
        }

        /// <summary>
        /// Обработка нажатия ПКМ
        /// </summary>
        private void OpenGlControl_MouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _isRmbDown = true;
            _lastMousePos = e.GetPosition(null);
            OpenGlControl.CaptureMouse();
        }

        /// <summary>
        /// Обработка отжатия ПКМ
        /// </summary>
        private void OpenGlControl_MouseRightButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _isRmbDown = false;
            OpenGlControl.ReleaseMouseCapture();
        }

        /// <summary>
        /// Обработка перемещения мыши
        /// </summary>
        private void OpenGlControl_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (_isRmbDown && Controller != null)
            {
                Point currentPos = e.GetPosition(null);
                float dx = (float)(currentPos.X - _lastMousePos.X);
                float dy = (float)(currentPos.Y - _lastMousePos.Y);
                _lastMousePos = currentPos;

                Controller.RotateCamera(dx, dy);
            }
        }
    }
}