using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using RayTracer.Models;
using System;
using System.Collections.Generic;

namespace RayTracer.Views
{
    /// <summary>
    /// Класс, отвечающий за низкоуровневую 3D-отрисовку сцены средствами OpenGL
    /// </summary>
    public class OpenGLRenderer
    {
        private readonly Dictionary<SceneObject, int> _textureCache = new Dictionary<SceneObject, int>();
        private bool _pendingClearTextures = false;

        /// <summary>
        /// Помечает кэш текстур видеокарты на безопасную очистку в следующем кадре
        /// </summary>
        public void ClearTextureCache()
        {
            _pendingClearTextures = true;
        }

        /// <summary>
        /// Выполняет полную отрисовку кадра сцены в текущий контекст OpenGL
        /// </summary>
        /// <param name="scene">Отрисовываемая сцена</param>
        /// <param name="viewportWidth">Ширина области просмотра</param>
        /// <param name="viewportHeight">Высота области просмотра</param>
        /// <param name="isSolid">True — режим световых карт, False — каркас</param>
        public void Render(Scene scene, double viewportWidth, double viewportHeight, bool isSolid)
        {
            // Очистка кэша текстур
            if (_pendingClearTextures)
            {
                foreach (var texId in _textureCache.Values)
                {
                    GL.DeleteTexture(texId);
                }
                _textureCache.Clear();
                _pendingClearTextures = false;
            }

            // Цвет фона
            GL.ClearColor(0.15f, 0.15f, 0.15f, 1.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            if (scene == null || scene.MainCamera == null) return;
            Camera cam = scene.MainCamera;

            // Настройка камеры
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();
            float aspect = viewportHeight > 0 ? (float)(viewportWidth / viewportHeight) : 1.0f;
            if (aspect <= 0) aspect = 1.0f;

            float fovRad = MathHelper.DegreesToRadians(Math.Clamp(cam.FovDegrees, 1f, 179.99f));
            Matrix4 projection = Matrix4.CreatePerspectiveFieldOfView(fovRad, aspect, 0.1f, 100f);
            GL.LoadMatrix(ref projection);

            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadIdentity();
            Matrix4 view = Matrix4.LookAt(cam.Position, cam.Position + cam.Direction, cam.Up);
            GL.LoadMatrix(ref view);

            // Отрисовка неба
            if (scene.Skybox != null)
            {
                DrawSkybox(cam.Position, scene.Skybox);
            }

            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.CullFace);
            GL.CullFace(CullFaceMode.Back);

            // Отрисовка объектов
            if (isSolid)
                GL.Enable(EnableCap.Texture2D);

            foreach (var obj in scene.Objects)
            {
                if (isSolid)
                {
                    int texId = GetOrLoadTexture(obj);
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
                foreach (var light in scene.Lights)
                {
                    if (light is PointLight pointLight)
                        DrawPointLight(pointLight.Position, pointLight.Range, pointLight.Color);
                    else if (light is SpotLight spotLight)
                        DrawSpotLight(spotLight.Position, spotLight.Direction, spotLight.Range, spotLight.CutOffAngle, spotLight.Color);
                }

                // Отрисовка сетки
                DrawGrid();
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

                    Vector3 c1 = sky.ColorBottom;
                    if (ny1 > -0.01f)
                    {
                        float t1 = 0.5f * (ny1 + 1.0f);
                        c1 = (1.0f - t1) * sky.ColorBottom + t1 * sky.ColorTop;
                    }

                    GL.Color3(c1.X, c1.Y, c1.Z);
                    GL.Vertex3(radius * nx1, radius * ny1, radius * nz1);

                    float nx2 = (float)(Math.Sin(theta2) * Math.Cos(phi));
                    float ny2 = (float)Math.Cos(theta2);
                    float nz2 = (float)(Math.Sin(theta2) * Math.Sin(phi));

                    Vector3 c2 = sky.ColorBottom;
                    if (ny2 > -0.01f)
                    {
                        float t2 = 0.5f * (ny2 + 1.0f);
                        c2 = (1.0f - t2) * sky.ColorBottom + t2 * sky.ColorTop;
                    }

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
                    float theta = (k == 0)
                        ? 2.0f * (float)Math.PI * i / segments
                        : 2.0f * (float)Math.PI * (segments - i) / segments;

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
            for (int i = 0; i < segments; i++)
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
    }
}