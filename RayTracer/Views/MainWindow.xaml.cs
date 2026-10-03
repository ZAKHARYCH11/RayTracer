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

        private void BtnAddObject_Click(object sender, RoutedEventArgs e)
        {
            BtnAddObject.ContextMenu.IsOpen = true;
        }

        private void MenuAddSphere_Click(object sender, RoutedEventArgs e) => Controller?.AddSphere();
        private void MenuAddDirLight_Click(object sender, RoutedEventArgs e) => Controller?.AddDirectionalLight();
        private void MenuClone_Click(object sender, RoutedEventArgs e) => Controller?.CloneSelected();
        private void MenuDelete_Click(object sender, RoutedEventArgs e) => Controller?.DeleteSelected();

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
    }
}