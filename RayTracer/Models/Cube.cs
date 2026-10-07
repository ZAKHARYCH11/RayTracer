using System;
using OpenTK.Mathematics;

namespace RayTracer.Models
{
    /// <summary>
    /// Геометрический примитив: Куб
    /// </summary>
    public class Cube : SceneObject
    {
        private float _side;

        /// <summary>
        /// Длина стороны куба
        /// </summary>
        public float Side
        {
            get { return _side; }
            set { _side = value; }
        }

        public Cube(string name, Vector3 center, float side, Material material) : base(name, material, center)
        {
            _side = side;
        }

        /// <summary>
        /// Проверка пересечения луча с кубом
        /// </summary>
        /// <param name="ray">Испускаемый луч</param>
        /// <param name="hit">Данные о пересечении</param>
        /// <returns>True, если пересечение есть, иначе False</returns>
        public override bool Intersect(Ray ray, out Intersection hit)
        {
            hit = default;

            // Находим минимальные и максимальные координаты куба (границы)
            float halfSide = _side / 2.0f;
            Vector3 min = Center - new Vector3(halfSide);
            Vector3 max = Center + new Vector3(halfSide);

            // Пересечение луча с плоскостями по оси X
            float tMin = (min.X - ray.Origin.X) / ray.Direction.X;
            float tMax = (max.X - ray.Origin.X) / ray.Direction.X;

            if (tMin > tMax) Swap(ref tMin, ref tMax);

            // Пересечение луча с плоскостями по оси Y
            float tyMin = (min.Y - ray.Origin.Y) / ray.Direction.Y;
            float tyMax = (max.Y - ray.Origin.Y) / ray.Direction.Y;

            if (tyMin > tyMax) Swap(ref tyMin, ref tyMax);

            // Если интервалы по осям X и Y не пересекаются - луч летит мимо куба
            if ((tMin > tyMax) || (tyMin > tMax))
                return false;

            if (tyMin > tMin) tMin = tyMin;
            if (tyMax < tMax) tMax = tyMax;

            // Пересечение луча с плоскостями по оси Z
            float tzMin = (min.Z - ray.Origin.Z) / ray.Direction.Z;
            float tzMax = (max.Z - ray.Origin.Z) / ray.Direction.Z;

            if (tzMin > tzMax) Swap(ref tzMin, ref tzMax);

            // Если интервалы Z не пересекаются с общим отрезком - промах
            if ((tMin > tzMax) || (tzMin > tMax))
                return false;

            if (tzMin > tMin) tMin = tzMin;
            if (tzMax < tMax) tMax = tzMax;

            // tMin - это точка ВХОДА в куб, tMax - точка ВЫХОДА
            float t = tMin;
            if (t < 0.001f) // Если камера внутри куба, берем точку выхода
            {
                t = tMax;
                if (t < 0.001f) return false; // Куб находится полностью позади камеры
            }

            // Ура, мы попали в куб! Записываем данные
            hit.Distance = t;
            hit.Point = ray.GetPoint(t);
            hit.Object = this;

            // --- Вычисление нормали (перпендикуляра к грани) ---
            // Находим вектор от центра куба до точки пересечения
            Vector3 localPoint = hit.Point - Center;

            // Смотрим, какая из координат максимальна - значит в эту грань мы и врезались
            float absX = Math.Abs(localPoint.X);
            float absY = Math.Abs(localPoint.Y);
            float absZ = Math.Abs(localPoint.Z);

            float eps = 0.0001f; // Погрешность вычислений float

            if (absX >= absY - eps && absX >= absZ - eps)
                hit.Normal = new Vector3(Math.Sign(localPoint.X), 0, 0);
            else if (absY >= absZ - eps)
                hit.Normal = new Vector3(0, Math.Sign(localPoint.Y), 0);
            else
                hit.Normal = new Vector3(0, 0, Math.Sign(localPoint.Z));

            return true;
        }

        public override SceneObject Clone()
        {
            return new Cube(Name, Center, _side, Material.Clone());
        }

        private void Swap(ref float a, ref float b)
        {
            float temp = a;
            a = b;
            b = temp;
        }
    }
}
