using System.Threading.Tasks;
using OpenTK.Mathematics;

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
            // Превышена максимальная глубина трассировки
            if (currentDepth >= maxDepth)
                return Vector3.Zero;

            // Есть пересечение с объектом
            if (scene.RayCast(ray, out Intersection hit))
            {
                Material mat = hit.Object.Material;
                Vector3 finalColor = Vector3.Zero;

                // 1. Базовое освещение
                Vector3 ambient = mat.Color * 0.15f;
                finalColor += ambient;

                // Цикл по всем источникам света на сцене
                foreach (var light in scene.Lights)
                {
                    Vector3 lightDir = light.GetDirectionToLight(hit.Point);

                    // 2. Проверка теней (Shadow Ray)
                    Vector3 shadowOrigin = hit.Point + hit.Normal * 0.001f;
                    Ray shadowRay = new Ray(shadowOrigin, lightDir);

                    // Проверяем, не перекрыт ли свет
                    if (!scene.RayCast(shadowRay, out Intersection shadowHit) || shadowHit.Distance > light.GetDistanceToLight(hit.Point))
                    {
                        // Освещение по Ламберту (Diffuse)
                        float diff = Math.Max(Vector3.Dot(hit.Normal, lightDir), 0.0f);
                        Vector3 diffuse = mat.Color * light.Color * light.Intensity * diff;
                        finalColor += diffuse;
                    }
                }

                // 3. Зеркальное отражение
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

            // Луч улетел в путоту
            float t = 0.5f * (ray.Direction.Y + 1.0f);
            return (1.0f - t) * new Vector3(1.0f, 1.0f, 1.0f) + t * new Vector3(0.5f, 0.7f, 1.0f);
        }
    }
}
