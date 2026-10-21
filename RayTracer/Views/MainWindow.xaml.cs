using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Wpf;
using RayTracer.Controllers;
using RayTracer.Models;
using System;
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
        private bool _keyW, _keyA, _keyS, _keyD;
        private Point _lastMousePos;

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
                MinorVersion = 1
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
            // 1. Очистка экрана
            GL.ClearColor(0.15f, 0.15f, 0.15f, 1.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            if (Controller == null || Controller.Scene == null) return;

            // 2. Настройка виртуальной камеры OpenGL
            Camera cam = Controller.Scene.MainCamera;
            
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();
            float aspect = (float)(OpenGlControl.ActualWidth / OpenGlControl.ActualHeight);

            if (aspect <= 0) aspect = 1;

            float fovRad = MathHelper.DegreesToRadians(Math.Clamp(cam.FovDegrees, 1f, 179.99f));
            Matrix4 projection = Matrix4.CreatePerspectiveFieldOfView(fovRad, aspect, 0.1f, 100f);
            GL.LoadMatrix(ref projection);

            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadIdentity();

            Matrix4 view = Matrix4.LookAt(cam.Position, cam.Position + cam.Direction, cam.Up);
            GL.LoadMatrix(ref view);

            // 3. Отрисовка координатной сетки (пола)
            DrawGrid();

            // 4. Отрисовка объектов сцены
            foreach (var obj in Controller.Scene.Objects)
            {
                if (obj is Sphere sphere)
                {
                    DrawWireframeSphere(sphere.Center, sphere.Radius, sphere.Material.Color);
                }
                else if (obj is Cube cube)
                {
                    DrawWireframeCube(cube.Center, cube.Side, cube.Material.Color);
                }
                else if (obj is Cylinder cyl)
                {
                    DrawWireframeCylinder(cyl.Center, cyl.Radius, cyl.Height, cyl.Material.Color);
                }
            }

            // 5. Движение камеры
            if (_isRmbDown && Controller != null)
            {
                Controller.MoveCamera(_keyW, _keyA, _keyS, _keyD, (float)delta.TotalSeconds);
            }
        }

        /// <summary>
        /// Вспомогательный метод для отрисовки сетки (пола)
        /// </summary>
        private void DrawGrid()
        {
            GL.Color3(0.3f, 0.3f, 0.3f);
            GL.Begin(PrimitiveType.Lines);
            for (int i = -10; i <= 10; i++)
            {
                GL.Vertex3(i, -0.5f, -10);
                GL.Vertex3(i, -0.5f, 10);
                GL.Vertex3(-10, -0.5f, i);
                GL.Vertex3(10, -0.5f, i);
            }
            GL.End();
        }

        /// <summary>
        /// Вспомогательный метод для отрисовки каркаса сферы
        /// </summary>
        private void DrawWireframeSphere(Vector3 center, float radius, Vector3 color)
        {
            GL.Color3(color.X, color.Y, color.Z);

            GL.PushMatrix();

            GL.Translate(center.X, center.Y, center.Z);

            int rings = 12;
            int sectors = 12;

            for (int i = 0; i < rings; i++)
            {
                float theta1 = (float)(i * Math.PI / rings);
                float theta2 = (float)((i + 1) * Math.PI / rings);

                GL.Begin(PrimitiveType.LineLoop);
                for (int j = 0; j <= sectors; j++)
                {
                    float phi = (float)(j * 2 * Math.PI / sectors);

                    float x = radius * (float)(Math.Sin(theta1) * Math.Cos(phi));
                    float y = radius * (float)Math.Cos(theta1);
                    float z = radius * (float)(Math.Sin(theta1) * Math.Sin(phi));

                    GL.Vertex3(x, y, z);
                }
                GL.End();
            }

            GL.PopMatrix();
        }

        /// <summary>
        /// Вспомогательный метод для отрисовки каркаса куба
        /// </summary>
        private void DrawWireframeCube(Vector3 center, float side, Vector3 color)
        {
            GL.Color3(color.X, color.Y, color.Z);
            float half = side / 2.0f;

            GL.PushMatrix();
            GL.Translate(center.X, center.Y, center.Z);

            GL.Begin(PrimitiveType.LineLoop);
            GL.Vertex3(-half, -half, -half);
            GL.Vertex3(half, -half, -half);
            GL.Vertex3(half, half, -half);
            GL.Vertex3(-half, half, -half);
            GL.End();

            GL.Begin(PrimitiveType.LineLoop);
            GL.Vertex3(-half, -half, half);
            GL.Vertex3(half, -half, half);
            GL.Vertex3(half, half, half);
            GL.Vertex3(-half, half, half);
            GL.End();

            GL.Begin(PrimitiveType.Lines);
            GL.Vertex3(-half, -half, -half); GL.Vertex3(-half, -half, half);
            GL.Vertex3(half, -half, -half); GL.Vertex3(half, -half, half);
            GL.Vertex3(half, half, -half); GL.Vertex3(half, half, half);
            GL.Vertex3(-half, half, -half); GL.Vertex3(-half, half, half);
            GL.End();

            GL.PopMatrix();
        }

        /// <summary>
        /// Вспомогательный метод для отрисовки каркаса цилиндра
        /// </summary>
        private void DrawWireframeCylinder(Vector3 center, float radius, float height, Vector3 color)
        {
            GL.Color3(color.X, color.Y, color.Z);
            float halfHeight = height / 2.0f;
            int segments = 16;

            GL.PushMatrix();
            GL.Translate(center.X, center.Y, center.Z);

            for(int k = 0; k < 2; k++)
            {
                GL.Begin(PrimitiveType.LineLoop);
                for (int i = 0; i < segments; i++)
                {
                    float theta = 2.0f * (float)Math.PI * i / segments;
                    GL.Vertex3(radius * Math.Cos(theta), -halfHeight + height * k, radius * Math.Sin(theta));
                }
                GL.End();
            }

            int verticalLines = segments / 2;
            GL.Begin(PrimitiveType.Lines);
            for (int i = 0; i < verticalLines; i++)
            {
                float theta = 2.0f * (float)Math.PI * i / verticalLines;
                float x = (float)(radius * Math.Cos(theta));
                float z = (float)(radius * Math.Sin(theta));

                GL.Vertex3(x, halfHeight, z);
                GL.Vertex3(x, -halfHeight, z);
            }
            GL.End();

            GL.PopMatrix();
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
        /// Нажатие кнопки выхода
        /// </summary>
        private void MenuExit_Click(object sender, RoutedEventArgs e) => System.Windows.Application.Current.Shutdown();

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
        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.W) _keyW = true;
            if (e.Key == System.Windows.Input.Key.A) _keyA = true;
            if (e.Key == System.Windows.Input.Key.S) _keyS = true;
            if (e.Key == System.Windows.Input.Key.D) _keyD = true;
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
                float deltaX = (float)(currentPos.X - _lastMousePos.X);
                float deltaY = (float)(currentPos.Y - _lastMousePos.Y);
                _lastMousePos = currentPos;

                Controller.RotateCamera(deltaX, deltaY);
            }
        }
    }
}