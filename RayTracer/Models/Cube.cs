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

        public Cube(string name, Vector3 center, float side, Material material, int lightmapRes = 256) : base(name, material, center, lightmapRes)
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

            float halfSide = _side / 2.0f;
            Vector3 min = Center - new Vector3(halfSide);
            Vector3 max = Center + new Vector3(halfSide);

            float tMin = (min.X - ray.Origin.X) / ray.Direction.X;
            float tMax = (max.X - ray.Origin.X) / ray.Direction.X;

            if (tMin > tMax) Swap(ref tMin, ref tMax);

            float tyMin = (min.Y - ray.Origin.Y) / ray.Direction.Y;
            float tyMax = (max.Y - ray.Origin.Y) / ray.Direction.Y;

            if (tyMin > tyMax) Swap(ref tyMin, ref tyMax);

            if ((tMin > tyMax) || (tyMin > tMax))
                return false;

            if (tyMin > tMin) tMin = tyMin;
            if (tyMax < tMax) tMax = tyMax;

            float tzMin = (min.Z - ray.Origin.Z) / ray.Direction.Z;
            float tzMax = (max.Z - ray.Origin.Z) / ray.Direction.Z;

            if (tzMin > tzMax) Swap(ref tzMin, ref tzMax);

            if ((tMin > tzMax) || (tzMin > tMax))
                return false;

            if (tzMin > tMin) tMin = tzMin;
            if (tzMax < tMax) tMax = tzMax;

            float t = tMin;
            if (t < 0.001f)
            {
                t = tMax;
                if (t < 0.001f) return false;
            }

            hit.Distance = t;
            hit.Point = ray.GetPoint(t);
            hit.Object = this;

            Vector3 localPoint = hit.Point - Center;

            float absX = Math.Abs(localPoint.X);
            float absY = Math.Abs(localPoint.Y);
            float absZ = Math.Abs(localPoint.Z);

            float eps = 0.0001f;

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
            return new Cube(Name, Center, _side, Material.Clone(), LightmapRes);
        }

        public override bool TryGetSurfaceData(float u, float v, out Vector3 position, out Vector3 normal)
        {
            u = Math.Clamp(u, 0f, 0.999f);
            v = Math.Clamp(v, 0f, 0.999f);

            int col = (int)(u * 3);
            int row = (int)(v * 2);
            int face = row * 3 + col;

            float localU = (u - col / 3.0f) * 3.0f;
            float localV = (v - row / 2.0f) * 2.0f;

            float half = _side / 2.0f;
            float lx = -half + localU * _side;
            float ly = half - localV * _side;

            position = Vector3.Zero;
            normal = Vector3.Zero;

            switch (face)
            {
                case 0:
                    normal = new Vector3(0, 0, 1);
                    position = new Vector3(lx, ly, half); break;
                case 1:
                    normal = new Vector3(0, 0, -1);
                    position = new Vector3(-lx, ly, -half); break;
                case 2:
                    normal = new Vector3(0, 1, 0);
                    position = new Vector3(lx, half, -ly); break;
                case 3:
                    normal = new Vector3(0, -1, 0);
                    position = new Vector3(lx, -half, ly); break;
                case 4:
                    normal = new Vector3(1, 0, 0);
                    position = new Vector3(half, ly, -lx); break;
                case 5:
                    normal = new Vector3(-1, 0, 0);
                    position = new Vector3(-half, ly, lx); break;
            }

            position += Center;
            return true;
        }

        /// <summary>
        /// Обмен значениями двух переменных
        /// </summary>
        /// <param name="a">Первая переменая</param>
        /// <param name="b">Вторая переменная</param>
        private void Swap(ref float a, ref float b)
        {
            float temp = a;
            a = b;
            b = temp;
        }
    }
}
