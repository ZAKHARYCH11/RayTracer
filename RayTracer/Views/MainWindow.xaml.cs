using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Wpf;
using RayTracer.Controllers;
using RayTracer.Models;
using System;
using System.Windows;


namespace RayTracer.Views
{
    public partial class MainWindow : Window
    {
        private MainController _controller;

        private bool _isCommittingRename = false;

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

        private void OpenGlControl_Render(TimeSpan delta)
        {
            // 1. Очистка экрана
            GL.ClearColor(0.15f, 0.15f, 0.15f, 1.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            if (Controller == null || Controller.Scene == null) return;

            // 2. Настройка виртуальной камеры OpenGL
            // Матрица проекции (угол обзора, соотношение сторон)
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();
            float aspect = (float)(OpenGlControl.ActualWidth / OpenGlControl.ActualHeight);
            Matrix4 projection = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(60f), aspect, 0.1f, 100f);
            GL.LoadMatrix(ref projection);

            // Матрица вида (позиция камеры в пространстве)
            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadIdentity();

            Matrix4 view = Matrix4.LookAt(new Vector3(0, 0, 1.5f), new Vector3(0, 0, -1), new Vector3(0, 1, 0));
            GL.LoadMatrix(ref view);

            // Включаем тест глубины, чтобы дальние линии не перекрывали ближние
            GL.Enable(EnableCap.DepthTest);

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

        private void BtnAddObject_Click(object sender, RoutedEventArgs e)
        {
            BtnAddObject.ContextMenu.IsOpen = true;
        }

        private void MenuAddSphere_Click(object sender, RoutedEventArgs e) => Controller?.AddSphere();
        private void MenuAddCube_Click(object sender, RoutedEventArgs e) => Controller?.AddCube();
        private void MenuAddCylinder_Click(object sender, RoutedEventArgs e) => Controller?.AddCylinder();
        private void MenuAddDirLight_Click(object sender, RoutedEventArgs e) => Controller?.AddDirectionalLight();
        private void MenuClone_Click(object sender, RoutedEventArgs e) => Controller?.CloneSelected();
        private void MenuDelete_Click(object sender, RoutedEventArgs e) => Controller?.DeleteSelected();
        private void MenuOpenScene_Click(object sender, RoutedEventArgs e) => Controller?.LoadScene();
        private void MenuSaveScene_Click(object sender, RoutedEventArgs e) => Controller?.SaveScene();
        private void MenuExit_Click(object sender, RoutedEventArgs e) => System.Windows.Application.Current.Shutdown();

        private void ListBoxItem_RightClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is System.Windows.Controls.ListBoxItem item)
            {
                item.IsSelected = true;
            }
        }

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

        private void SceneObjectsList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (Controller != null)
            {
                Controller.OnObjectSelected(SceneObjectsList.SelectedItem);
            }
        }

        private void Properties_Changed(object sender, RoutedEventArgs e)
        {
            if (Controller != null)
            {
                Controller.OnPropertiesChanged();
            }
        }

        private void BtnSelectColor_Click(object sender, RoutedEventArgs e)
        {
            if (Controller != null)
            {
                Controller.SelectColor();
            }
        }

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

        private void NameInput_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
                CommitRename(sender as System.Windows.Controls.TextBox);
            else if (e.Key == System.Windows.Input.Key.Escape)
                CancelRename(sender as System.Windows.Controls.TextBox);
        }

        private void NameInput_LostFocus(object sender, RoutedEventArgs e)
        {
            CommitRename(sender as System.Windows.Controls.TextBox);
        }

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

        private void ClearFocus_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element)
            {
                element.Focus();
            }
        }
    }
}