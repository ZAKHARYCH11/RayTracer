using OpenTK.Mathematics;
using OpenTK.Windowing.Common.Input;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RayTracer.Models
{
    /// <summary>
    /// Вычислитель трассировки лучей
    /// </summary>
    public class RayTracerEngine
    {
        /// <summary>
        /// Рендер сцены
        /// </summary>
        /// <param name="scene">Сцена</param>
        /// <param name="camera">Камера</param>
        /// <param name="width">Ширина изображения</param>
        /// <param name="height">Высота изображения</param>
        /// <param name="maxDepth">Максимальное число переотражения луча (глубина трассировки)</param>
        /// <returns>Массив байт пикселей (3 байта - 1 пиксель)</returns>
        public byte[] RenderView(Scene scene, Camera camera, int width, int height, int maxDepth)
        {
            byte[] pixels = new byte[width * height * 3];

            Parallel.For(0, height, y =>
            {
                for (int x = 0; x < width; x++)
                {
                    float u = (float)x / (width - 1);
                    float v = (float)(height - y - 1) / (height - 1);

                    Ray ray = camera.GetRay(u, v);

                    Vector3 color = TraceRay(ray, scene, maxDepth, 0);

                    int index = (y * width + x) * 3;
                    pixels[index] = (byte)(Math.Clamp(color.X, 0f, 1f) * 255);     // R
                    pixels[index + 1] = (byte)(Math.Clamp(color.Y, 0f, 1f) * 255); // G
                    pixels[index + 2] = (byte)(Math.Clamp(color.Z, 0f, 1f) * 255); // B
                }
            });

            return pixels;
        }

        /// <summary>
        /// Рекурсивная функция трассировки одного луча
        /// </summary>
        /// <param name="ray">Трассируемый луч</param>
        /// <param name="scene">Сцена</param>
        /// <param name="maxDepth">Максимальная глубина трассировки</param>
        /// <param name="currentDepth">Текущая глубина</param>
        /// <returns>Цвет пикселя</returns>
        private Vector3 TraceRay(Ray ray, Scene scene, int maxDepth, int currentDepth)
        {
            if (currentDepth >= maxDepth)
                return Vector3.Zero;

            if (scene.RayCast(ray, out Intersection hit))
            {
                Material mat = hit.Object.Material;
                Vector3 finalColor = Vector3.Zero;

                Vector3 ambient = mat.Color * 0.15f;
                finalColor += ambient;

                foreach (var light in scene.Lights)
                {
                    float currentIntensity = light.GetIntensityAt(hit.Point);

                    if (currentIntensity > 0)
                    {
                        Vector3 lightDir = light.GetDirectionToLight(hit.Point);

                        Vector3 shadowOrigin = hit.Point + hit.Normal * 0.001f;
                        Ray shadowRay = new Ray(shadowOrigin, lightDir);

                        if (!scene.RayCast(shadowRay, out Intersection shadowHit) || shadowHit.Distance > light.GetDistanceToLight(hit.Point))
                        {
                            float diff = Math.Max(Vector3.Dot(hit.Normal, lightDir), 0.0f);
                            Vector3 diffuse = mat.Color * light.Color * currentIntensity * diff;
                            finalColor += diffuse;
                        }
                    }
                }

                if (mat.Reflectivity > 0.0f)
                {
                    Vector3 shadowOrigin = hit.Point + hit.Normal * 0.001f;
                    Vector3 reflectDir = ray.Direction - 2.0f * Vector3.Dot(ray.Direction, hit.Normal) * hit.Normal;
                    Ray reflectRay = new Ray(shadowOrigin, reflectDir);

                    Vector3 reflectColor = TraceRay(reflectRay, scene, maxDepth, currentDepth + 1);

                    finalColor = finalColor * (1.0f - mat.Reflectivity) + reflectColor * mat.Reflectivity;
                }

                return finalColor;
            }

            return scene.Skybox.GetColor(ray.Direction);
        }

        /// <summary>
        /// Генерация световых карт для всех объектов
        /// </summary>
        /// <param name="scene">Сцена</param>
        /// <param name="maxDepth">Глубина трассировки</param>
        public void BakeLightmaps(Scene scene, int maxDepth, Vector3 cameraPosition)
        {
            foreach (var obj in scene.Objects)
            {
                int res = obj.LightmapRes;
                byte[] pixels = new byte[res * res * 3];

                Parallel.For(0, res, y =>
                {
                    for (int x = 0; x < res; x++)
                    {
                        float u = (float)x / (res - 1);
                        float v = (float)y / (res - 1);

                        if (obj.TryGetSurfaceData(u, v, out Vector3 pos, out Vector3 normal))
                        {
                            Vector3 color = CalculateBakeColor(scene, obj, pos, normal, maxDepth, 0, cameraPosition);

                            int index = (y * res + x) * 3;
                            pixels[index] = (byte)(Math.Clamp(color.X, 0f, 1f) * 255);     // R
                            pixels[index + 1] = (byte)(Math.Clamp(color.Y, 0f, 1f) * 255); // G
                            pixels[index + 2] = (byte)(Math.Clamp(color.Z, 0f, 1f) * 255); // B
                        }
                    }
                });

                obj.Lightmap = new Lightmap(res, res, pixels);
            }
        }

        /// <summary>
        /// Рассчет освещения для одной точки поверхности объекта
        /// </summary>
        /// <param name="scene">Сцена</param>
        /// <param name="targetObj">Объект, для которого рассчитывается освещение</param>
        /// <param name="point">Точка, для которой рассчитывается освещение</param>
        /// <param name="normal">Нормаль к точке</param>
        /// <param name="maxDepth">Глубина трассировки</param>
        /// <param name="currentDepth">Текущая глубина</param>
        /// <param name="cameraPosition">Положение камеры</param>
        private Vector3 CalculateBakeColor(Scene scene, SceneObject targetObj, Vector3 point, Vector3 normal, int maxDepth, int currentDepth, Vector3 cameraPosition)
        {
            if (currentDepth >= maxDepth) return Vector3.Zero;

            Vector3 finalColor = targetObj.Material.Color * 0.15f;

            foreach (var light in scene.Lights)
            {
                float intensity = light.GetIntensityAt(point);

                if (intensity > 0)
                {
                    Vector3 lightDir = light.GetDirectionToLight(point);
                    Vector3 shadowOrigin = point + normal * 0.001f;
                    Ray shadowRay = new Ray(shadowOrigin, lightDir);

                    if (!scene.RayCast(shadowRay, out Intersection hit) || hit.Distance > light.GetDistanceToLight(point))
                    {
                        float diff = Math.Max(Vector3.Dot(normal, lightDir), 0.0f);
                        finalColor += targetObj.Material.Color * light.Color * intensity * diff;
                    }
                }
            }

            if (targetObj.Material.Reflectivity > 0.0f)
            {
                Vector3 viewDir = (point - cameraPosition).Normalized();

                Vector3 reflectDir = viewDir - 2.0f * Vector3.Dot(viewDir, normal) * normal;

                Vector3 shadowOrigin = point + normal * 0.02f;
                Ray reflectRay = new Ray(shadowOrigin, reflectDir);

                Vector3 reflectColor = TraceRay(reflectRay, scene, maxDepth, currentDepth + 1);

                finalColor = finalColor * (1.0f - targetObj.Material.Reflectivity) + reflectColor * targetObj.Material.Reflectivity;
            }

            return finalColor;
        }
    }
}
