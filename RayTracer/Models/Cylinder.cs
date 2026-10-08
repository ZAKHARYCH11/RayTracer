using System;
using OpenTK.Mathematics;

namespace RayTracer.Models
{
    /// <summary>
    /// Геометрический примитив: Цилиндр
    /// </summary>
    public class Cylinder : SceneObject
    {
        private float _radius;
        private float _height;

        /// <summary>
        /// Радиус основания цилиндра
        /// </summary>
        public float Radius
        {
            get { return _radius; }
            set { _radius = value; }
        }

        /// <summary>
        /// Высота цилиндра
        /// </summary>
        public float Height
        {
            get { return _height; }
            set { _height = value; }
        }

        public Cylinder(string name, Vector3 center, float radius, float height, Material material) : base(name, material, center)
        {
            _radius = radius;
            _height = height;
        }

        /// <summary>
        /// Проверка пересечения луча с цилиндпром
        /// </summary>
        /// <param name="ray">Испускаемый луч</param>
        /// <param name="hit">Данные о пересечении</param>
        /// <returns>True, если пересечение есть, иначе False</returns>
        public override bool Intersect(Ray ray, out Intersection hit)
        {
            hit = default;

            float tClosest = float.MaxValue;
            Vector3 bestNormal = Vector3.Zero;

            // Находим координаты Y для крышек цилиндра
            float halfHeight = _height / 2.0f;
            float yMin = Center.Y - halfHeight;
            float yMax = Center.Y + halfHeight;

            // --- 1. Пересечение с боковой поверхностью (трубой) ---
            // Решаем квадратное уравнение, игнорируя компоненту Y (смотрим на цилиндр сверху)
            float a = ray.Direction.X * ray.Direction.X + ray.Direction.Z * ray.Direction.Z;

            // Если a != 0, значит луч не строго вертикальный
            if (Math.Abs(a) > 0.00001f)
            {
                float ocX = ray.Origin.X - Center.X;
                float ocZ = ray.Origin.Z - Center.Z;

                float b = 2.0f * (ocX * ray.Direction.X + ocZ * ray.Direction.Z);
                float c = ocX * ocX + ocZ * ocZ - _radius * _radius;

                float discriminant = b * b - 4 * a * c;

                if (discriminant >= 0)
                {
                    float sqrtD = (float)Math.Sqrt(discriminant);
                    float t1 = (-b - sqrtD) / (2.0f * a);
                    float t2 = (-b + sqrtD) / (2.0f * a);

                    // Проверяем первый корень (ближняя стенка)
                    if (t1 > 0.001f && t1 < tClosest)
                    {
                        // Проверяем, не вышли ли мы за пределы высоты
                        float y1 = ray.Origin.Y + t1 * ray.Direction.Y;
                        if (y1 >= yMin && y1 <= yMax)
                        {
                            tClosest = t1;
                            // Нормаль указывает от оси (центра) строго вбок
                            bestNormal = new Vector3((ray.Origin.X + t1 * ray.Direction.X - Center.X) / _radius, 0,
                                                     (ray.Origin.Z + t1 * ray.Direction.Z - Center.Z) / _radius).Normalized();
                        }
                    }

                    // Проверяем второй корень (дальняя стенка, если камера внутри)
                    if (t2 > 0.001f && t2 < tClosest)
                    {
                        float y2 = ray.Origin.Y + t2 * ray.Direction.Y;
                        if (y2 >= yMin && y2 <= yMax)
                        {
                            tClosest = t2;
                            bestNormal = new Vector3((ray.Origin.X + t2 * ray.Direction.X - Center.X) / _radius, 0,
                                                     (ray.Origin.Z + t2 * ray.Direction.Z - Center.Z) / _radius).Normalized();
                        }
                    }
                }
            }

            // --- 2. Пересечение с плоскими крышками (основаниями) ---
            if (Math.Abs(ray.Direction.Y) > 0.00001f)
            {
                // Проверяем нижнюю крышку
                float tBottom = (yMin - ray.Origin.Y) / ray.Direction.Y;
                if (tBottom > 0.001f && tBottom < tClosest)
                {
                    Vector3 p = ray.GetPoint(tBottom);
                    // Проверяем, попали ли мы в круг радиуса R
                    if ((p.X - Center.X) * (p.X - Center.X) + (p.Z - Center.Z) * (p.Z - Center.Z) <= _radius * _radius)
                    {
                        tClosest = tBottom;
                        bestNormal = new Vector3(0, -1, 0); // Нормаль смотрит строго вниз
                    }
                }

                // Проверяем верхнюю крышку
                float tTop = (yMax - ray.Origin.Y) / ray.Direction.Y;
                if (tTop > 0.001f && tTop < tClosest)
                {
                    Vector3 p = ray.GetPoint(tTop);
                    if ((p.X - Center.X) * (p.X - Center.X) + (p.Z - Center.Z) * (p.Z - Center.Z) <= _radius * _radius)
                    {
                        tClosest = tTop;
                        bestNormal = new Vector3(0, 1, 0); // Нормаль смотрит строго вверх
                    }
                }
            }

            // Если пересечений не было, возвращаем false
            if (tClosest == float.MaxValue) return false;

            // Записываем финальный результат
            hit.Distance = tClosest;
            hit.Point = ray.GetPoint(tClosest);
            hit.Normal = bestNormal;
            hit.Object = this;
            return true;
        }

        public override SceneObject Clone()
        {
            return new Cylinder(Name, Center, _radius, _height, Material.Clone());
        }
    }
}
