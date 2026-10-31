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

        private Dictionary<SceneObject, int> _textureCache = new Dictionary<SceneObject, int>();

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
            if (Controller == null || Controller.Scene == null) return;

            // Очистка экрана
            GL.ClearColor(0.15f, 0.15f, 0.15f, 1.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            // Настройка виртуальной камеры OpenGL
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

            // Отрисовка заденго фона (неба)
            if (Controller.Scene.Skybox != null)
            {
                DrawSkybox(cam.Position, Controller.Scene.Skybox);
            }

            // Отрисовка объектов сцены
            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.CullFace);
            GL.CullFace(CullFaceMode.Back);

            bool isSolid = RadioSolid.IsChecked == true;

            if (isSolid)
                GL.Enable(EnableCap.Texture2D);

            foreach (var obj in Controller.Scene.Objects)
            {
                int texId = 0;
                if (isSolid)
                {
                    texId = GetOrLoadTexture(obj);
                    GL.BindTexture(TextureTarget.Texture2D, texId);
                }

                if (obj is Sphere sphere)
                    DrawSphere(sphere.Center, sphere.Radius, sphere.Material.Color, isSolid);
                else if (obj is Cube cube)
                    DrawCube(cube.Center, cube.Side, cube.Material.Color, isSolid);
                else if (obj is Cylinder cyl)
                    DrawCylinder(cyl.Center, cyl.Radius, cyl.Height, cyl.Material.Color, isSolid);
            }

            if (isSolid)
                GL.Disable(EnableCap.Texture2D);

            // Отрисовка источников света
            if (!isSolid)
            {
                foreach(var light in Controller.Scene.Lights)
                {
                    if (light is PointLight pointLight)
                        DrawPointLight(pointLight.Position, pointLight.Range, pointLight.Color);
                    else if (light is SpotLight spotLight)
                        DrawSpotLight(spotLight.Position, spotLight.Direction, spotLight.Range, spotLight.CutOffAngle, spotLight.Color);
                }
            }

            // Отрисовка координатной сетки
            if (!isSolid)
                DrawGrid();

            // Движение камеры
            if (_isRmbDown && Controller != null)
            {
                Controller.MoveCamera(_keyW, _keyA, _keyS, _keyD, _keyShift, (float)delta.TotalSeconds);
            }
        }

        /// <summary>
        /// Отрисовка фона сцены (неба)
        /// </summary>
        /// <param name="cameraPosition">Положение камеры</param>
        /// <param name="sky">Скайбокс</param>
        private void DrawSkybox(Vector3 cameraPosition, Skybox sky)
        {
            GL.Disable(EnableCap.DepthTest);
            GL.Disable(EnableCap.CullFace);
            GL.DepthMask(false);

            GL.PushMatrix();

            GL.Translate(cameraPosition.X, cameraPosition.Y, cameraPosition.Z);

            float radius = 10.0f;
            int rings = 16, sectors = 16;

            for (int i = 0; i < rings; i++)
            {
                float theta1 = (float)(i * Math.PI / rings);
                float theta2 = (float)((i + 1) * Math.PI / rings);

                GL.Begin(PrimitiveType.QuadStrip);
                for (int j = 0; j <= sectors; j++)
                {
                    float phi = (float)(j * 2 * Math.PI / sectors);

                    float nx1 = (float)(Math.Sin(theta1) * Math.Cos(phi));
                    float ny1 = (float)Math.Cos(theta1);
                    float nz1 = (float)(Math.Sin(theta1) * Math.Sin(phi));

                    float t1 = 0.5f * (ny1 + 1.0f);
                    Vector3 c1 = (1.0f - t1) * sky.ColorBottom + t1 * sky.ColorTop;

                    GL.Color3(c1.X, c1.Y, c1.Z);
                    GL.Vertex3(radius * nx1, radius * ny1, radius * nz1);

                    float nx2 = (float)(Math.Sin(theta2) * Math.Cos(phi));
                    float ny2 = (float)Math.Cos(theta2);
                    float nz2 = (float)(Math.Sin(theta2) * Math.Sin(phi));

                    float t2 = 0.5f * (ny2 + 1.0f);
                    Vector3 c2 = (1.0f - t2) * sky.ColorBottom + t2 * sky.ColorTop;

                    GL.Color3(c2.X, c2.Y, c2.Z);
                    GL.Vertex3(radius * nx2, radius * ny2, radius * nz2);
                }
                GL.End();
            }

            GL.PopMatrix();
            GL.DepthMask(true);
        }

        /// <summary>
        /// Отрисовка сетки
        /// </summary>
        private void DrawGrid()
        {
            int gridSide = 100;
            GL.Color3(0.3f, 0.3f, 0.3f);
            GL.Begin(PrimitiveType.Lines);
            for (int i = -gridSide; i <= gridSide; i++)
            {
                GL.Vertex3(i, -0.5f, -gridSide);
                GL.Vertex3(i, -0.5f, gridSide);
                GL.Vertex3(-gridSide, -0.5f, i);
                GL.Vertex3(gridSide, -0.5f, i);
            }
            GL.End();
        }

        /// <summary>
        /// Отрисовка сферы
        /// </summary>
        /// <param name="center">Координаты центра сферы</param>
        /// <param name="radius">Радиус сферы</param>
        /// <param name="color">Цвет сферы</param>
        /// <param name="isSolid">Включен ли режим отображения световой карты</param>
        private void DrawSphere(Vector3 center, float radius, Vector3 color, bool isSolid)
        {
            GL.PushMatrix();
            GL.Translate(center.X, center.Y, center.Z);

            int rings = 20;
            int sectors = 20;

            // Отрисовка каркаса
            if (!isSolid)
            {
                GL.Color3(color.X, color.Y, color.Z);

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
                return;
            }

            // Отрисовка со световой картой

            GL.Color3(1.0f, 1.0f, 1.0f);

            for (int i = 0; i < rings; i++)
            {
                float theta1 = (float)(i * Math.PI / rings);
                float theta2 = (float)((i + 1) * Math.PI / rings);

                GL.Begin(PrimitiveType.QuadStrip);
                for (int j = 0; j <= sectors; j++)
                {
                    float phi = (float)(j * 2 * Math.PI / sectors);

                    float u = (float)j / sectors;
                    float v1 = (float)i / rings;
                    float v2 = (float)(i + 1) / rings;

                    GL.TexCoord2(u, v2);
                    GL.Vertex3(radius * Math.Sin(theta2) * Math.Cos(phi), radius * Math.Cos(theta2), radius * Math.Sin(theta2) * Math.Sin(phi));
                    GL.TexCoord2(u, v1);
                    GL.Vertex3(radius * Math.Sin(theta1) * Math.Cos(phi), radius * Math.Cos(theta1), radius * Math.Sin(theta1) * Math.Sin(phi));

                    
                }
                GL.End();
            }
            GL.PopMatrix();
        }

        /// <summary>
        /// Вспомогательный метод длявыбора точки текстуры для куба
        /// </summary>
        /// <param name="col">Столбец текстуры</param>
        /// <param name="row">Строка текстуры</param>
        /// <param name="u">Координата u</param>
        /// <param name="v">Координата v</param>
        private void Tex(int col, int row, float u, float v)
        {
            float pad = 0.01f;
            float localU = u * (1.0f - 2 * pad) + pad;
            float localV = v * (1.0f - 2 * pad) + pad;

            GL.TexCoord2((col + localU) / 3.0f, (row + localV) / 2.0f);
        }

        /// <summary>
        /// Отрисовка куба
        /// </summary>
        /// <param name="center">Координаты центра куба</param>
        /// <param name="side">Длина стороны куба</param>
        /// <param name="color">Цвет куба</param>
        /// <param name="isSolid">Включен ли режим отображения световой карты</param>
        private void DrawCube(Vector3 center, float side, Vector3 color, bool isSolid)
        {
            float half = side / 2.0f;
            GL.PushMatrix();
            GL.Translate(center.X, center.Y, center.Z);

            // Отрисовка каркаса
            if (!isSolid)
            {
                GL.Color3(color.X, color.Y, color.Z);

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
                return;
            }

            // Отрисоква со светиовой картой

            GL.Color3(1.0f, 1.0f, 1.0f);

            GL.Begin(PrimitiveType.Quads);

            // Передняя грань
            Tex(0, 0, 0, 1); GL.Vertex3(-half, -half, half);
            Tex(0, 0, 1, 1); GL.Vertex3(half, -half, half);
            Tex(0, 0, 1, 0); GL.Vertex3(half, half, half);
            Tex(0, 0, 0, 0); GL.Vertex3(-half, half, half);

            // Задняя грань
            Tex(1, 0, 0, 1); GL.Vertex3(half, -half, -half);
            Tex(1, 0, 1, 1); GL.Vertex3(-half, -half, -half);
            Tex(1, 0, 1, 0); GL.Vertex3(-half, half, -half);
            Tex(1, 0, 0, 0); GL.Vertex3(half, half, -half);

            // Верхняя грань
            Tex(2, 0, 0, 1); GL.Vertex3(-half, half, half);
            Tex(2, 0, 1, 1); GL.Vertex3(half, half, half);
            Tex(2, 0, 1, 0); GL.Vertex3(half, half, -half);
            Tex(2, 0, 0, 0); GL.Vertex3(-half, half, -half);

            // Нижняя грань
            Tex(0, 1, 0, 1); GL.Vertex3(-half, -half, -half);
            Tex(0, 1, 1, 1); GL.Vertex3(half, -half, -half);
            Tex(0, 1, 1, 0); GL.Vertex3(half, -half, half);
            Tex(0, 1, 0, 0); GL.Vertex3(-half, -half, half);

            // Правая грань
            Tex(1, 1, 0, 1); GL.Vertex3(half, -half, half);
            Tex(1, 1, 1, 1); GL.Vertex3(half, -half, -half);
            Tex(1, 1, 1, 0); GL.Vertex3(half, half, -half);
            Tex(1, 1, 0, 0); GL.Vertex3(half, half, half);

            // Левая грань
            Tex(2, 1, 0, 1); GL.Vertex3(-half, -half, -half);
            Tex(2, 1, 1, 1); GL.Vertex3(-half, -half, half);
            Tex(2, 1, 1, 0); GL.Vertex3(-half, half, half);
            Tex(2, 1, 0, 0); GL.Vertex3(-half, half, -half);

            GL.End();
            GL.PopMatrix();
        }

        /// <summary>
        /// Отрисовка цилиндра
        /// </summary>
        /// <param name="center">Координаты центра цилиндра</param>
        /// <param name="radius">Рабиус цилиндра</param>
        /// <param name="height">Высота цилиндра</param>
        /// <param name="color">Цвет цилиндра</param>
        /// /// <param name="isSolid">Включен ли режим отображения световой карты</param>
        private void DrawCylinder(Vector3 center, float radius, float height, Vector3 color, bool isSolid)
        {
            float halfHeight = height / 2.0f;
            int segments = 20;

            GL.PushMatrix();
            GL.Translate(center.X, center.Y, center.Z);

            // Отрисовка каркаса
            if (!isSolid)
            {
                GL.Color3(color.X, color.Y, color.Z);

                for (int k = 0; k < 2; k++)
                {
                    GL.Begin(PrimitiveType.LineLoop);
                    for (int i = 0; i < segments; i++)
                    {
                        float theta = 2.0f * (float)Math.PI * i / segments;
                        GL.Vertex3(radius * Math.Cos(theta), -halfHeight + height * k, radius * Math.Sin(theta));
                    }
                    GL.End();
                }

                GL.Begin(PrimitiveType.Lines);
                for (int i = 0; i < segments; i++)
                {
                    float theta = 2.0f * (float)Math.PI * i / segments;
                    float x = (float)(radius * Math.Cos(theta));
                    float z = (float)(radius * Math.Sin(theta));

                    GL.Vertex3(x, halfHeight, z);
                    GL.Vertex3(x, -halfHeight, z);
                }
                GL.End();
                GL.PopMatrix();
                return;
            }

            // Отрисовка со световой картой

            GL.Color3(1.0f, 1.0f, 1.0f);

            for (int k = 0; k < 2; k++)
            {
                GL.Begin(PrimitiveType.Polygon);
                for (int i = 0; i < segments; i++)
                {
                    float theta;
                    if(k == 0)
                        theta = 2.0f * (float)Math.PI * i / segments;
                    else
                        theta = 2.0f * (float)Math.PI * (segments - i) / segments;

                    float u = 0.5f + 0.5f * (float)Math.Cos(theta);
                    float v = 0.5f + 0.5f * (float)Math.Sin(theta);

                    if (k == 0)
                        GL.TexCoord2(0.5f + u * 0.5f, 0.5f + v * 0.5f);
                    else    
                        GL.TexCoord2(0.5f + u * 0.5f, v * 0.5f);

                    GL.Vertex3(radius * Math.Cos(theta), -halfHeight + height * k, radius * Math.Sin(theta));
                }
                GL.End();
            }

            GL.Begin(PrimitiveType.QuadStrip);
            for (int i = 0; i <= segments; i++)
            {
                float theta = 2.0f * (float)Math.PI * i / segments;
                float x = (float)(radius * Math.Cos(theta));
                float z = (float)(radius * Math.Sin(theta));

                float u = ((float)i / segments) * 0.5f;

                GL.TexCoord2(u, 1.0f);
                GL.Vertex3(x, -halfHeight, z);
                GL.TexCoord2(u, 0.0f);
                GL.Vertex3(x, halfHeight, z);
            }
            GL.End();


            GL.PopMatrix();
        }

        /// <summary>
        /// Отрисовка точечного источника света
        /// </summary>
        /// <param name="position">Координаты источника</param>
        /// <param name="range">Дальность света</param>
        /// <param name="color">Цвет света</param>
        private void DrawPointLight(Vector3 position, float range, Vector3 color)
        {
            int segments = 20;

            GL.PushMatrix();
            GL.Translate(position.X, position.Y, position.Z);
            GL.Color3(color.X, color.Y, color.Z);

            GL.Begin(PrimitiveType.LineLoop);
            for (int i = 0;i < segments; i++)
            {
                float theta = 2.0f * (float)Math.PI * i / segments;
                GL.Vertex3(range * Math.Cos(theta), 0, range * Math.Sin(theta));
            }
            GL.End();

            GL.Begin(PrimitiveType.LineLoop);
            for (int i = 0; i < segments; i++)
            {
                float theta = 2.0f * (float)Math.PI * i / segments;
                GL.Vertex3(0, range * Math.Cos(theta), range * Math.Sin(theta));
            }
            GL.End();

            GL.Begin(PrimitiveType.LineLoop);
            for (int i = 0; i < segments; i++)
            {
                float theta = 2.0f * (float)Math.PI * i / segments;
                GL.Vertex3(range * Math.Sin(theta), range * Math.Cos(theta), 0);
            }
            GL.End();

            GL.PopMatrix();
        }

        /// <summary>
        /// Отрисовка направленного источника света
        /// </summary>
        /// <param name="position">Координаты источника</param>
        /// <param name="direction">Направление света</param>
        /// <param name="range">Дальность света</param>
        /// <param name="cutOffAngle">Угол освещения</param>
        /// <param name="color">Цвет света</param>
        private void DrawSpotLight(Vector3 position, Vector3 direction, float range, float cutOffAngle, Vector3 color)
        {
            int segments = 24;
            int lines = segments / 4;

            GL.PushMatrix();
            GL.Translate(position.X, position.Y, position.Z);
            GL.Color3(color.X, color.Y, color.Z);

            Vector3 dir = direction.Normalized();

            Vector3 arbitrary = (Math.Abs(dir.Y) > 0.99f) ? new Vector3(1, 0, 0) : new Vector3(0, 1, 0);

            Vector3 right = Vector3.Cross(arbitrary, dir).Normalized();
            Vector3 up = Vector3.Cross(dir, right).Normalized();

            float angleRad = MathHelper.DegreesToRadians(cutOffAngle);
            float radius = range * (float)Math.Tan(angleRad);

            Vector3 baseCenter = dir * range;

            GL.Begin(PrimitiveType.LineLoop);
            for (int i = 0; i < segments; i++)
            {
                float theta = 2.0f * (float)Math.PI * i / segments;
                Vector3 p = baseCenter + radius * (float)Math.Cos(theta) * right + radius * (float)Math.Sin(theta) * up;
                GL.Vertex3(p.X, p.Y, p.Z);
            }
            GL.End();

            GL.Begin(PrimitiveType.Lines);
            for (int i = 0; i < lines; i++)
            {
                float theta = 2.0f * (float)Math.PI * i / lines;
                Vector3 p = baseCenter + radius * (float)Math.Cos(theta) * right + radius * (float)Math.Sin(theta) * up;
                GL.Vertex3(0, 0, 0);
                GL.Vertex3(p.X, p.Y, p.Z);
            }
            GL.End();

            GL.PopMatrix();
        }

        /// <summary>
        /// Загрузка световой карты в видеокарту
        /// </summary>
        /// <param name="obj">Объект сцены</param>
        /// <returns>ID текстуры OpenGL</returns>
        private int GetOrLoadTexture(SceneObject obj)
        {
            if (obj.Lightmap == null) return 0;

            if (_textureCache.ContainsKey(obj))
                return _textureCache[obj];

            int textureId = GL.GenTexture();

            GL.BindTexture(TextureTarget.Texture2D, textureId);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb,
                          obj.Lightmap.Width, obj.Lightmap.Height, 0,
                          PixelFormat.Rgb, PixelType.UnsignedByte, obj.Lightmap.Pixels);

            _textureCache.Add(obj, textureId);
            return textureId;
        }

        /// <summary>
        /// Очистка кэша текстур
        /// </summary>
        public void ClearTextureCache()
        {
            foreach (var texId in _textureCache.Values)
            {
                GL.DeleteTexture(texId);
            }
            _textureCache.Clear();
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
            if (e.Key == System.Windows.Input.Key.LeftShift) _keyShift = true;
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
                float deltaX = (float)(currentPos.X - _lastMousePos.X);
                float deltaY = (float)(currentPos.Y - _lastMousePos.Y);
                _lastMousePos = currentPos;

                Controller.RotateCamera(deltaX, deltaY);
            }
        }
    }
}