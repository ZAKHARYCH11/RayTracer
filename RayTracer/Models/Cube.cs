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
        /// <returns>true, если пересечение есть, иначе false</returns>
        public override bool Intersect(Ray ray, out Intersection hit)
        {
            hit = default;

            // Перевод луча в локальные координаты куба
            Matrix4 rot = GetRotationMatrix();
            Matrix4 invRot = Matrix4.Transpose(rot);

            Vector3 localOrigin = Vector3.TransformVector(ray.Origin - Center, invRot);
            Vector3 localDir = Vector3.TransformVector(ray.Direction, invRot).Normalized();

            float halfSide = _side / 2.0f;
            Vector3 min = new Vector3(-halfSide);
            Vector3 max = new Vector3(halfSide);

            // Персечение с плосткостью по оси X
            float tMin = (min.X - localOrigin.X) / localDir.X;
            float tMax = (max.X - localOrigin.X) / localDir.X;
            if (tMin > tMax) Swap(ref tMin, ref tMax);

            // Персечение с плоскостью по оси Y
            float tyMin = (min.Y - localOrigin.Y) / localDir.Y;
            float tyMax = (max.Y - localOrigin.Y) / localDir.Y;
            if (tyMin > tyMax) Swap(ref tyMin, ref tyMax);

            // Нет пересечения
            if ((tMin > tyMax) || (tyMin > tMax)) return false;

            if (tyMin > tMin) tMin = tyMin;
            if (tyMax < tMax) tMax = tyMax;

            // Пересечение с плоскостью по оси Z
            float tzMin = (min.Z - localOrigin.Z) / localDir.Z;
            float tzMax = (max.Z - localOrigin.Z) / localDir.Z;
            if (tzMin > tzMax) Swap(ref tzMin, ref tzMax);

            // Нет пересечения
            if ((tMin > tzMax) || (tzMin > tMax)) return false;

            if (tzMin > tMin) tMin = tzMin;
            if (tzMax < tMax) tMax = tzMax;

            float t = tMin;
            if (t < 0.001f)
            {
                t = tMax;
                if (t < 0.001f) return false;
            }

            // Вычисление локальной точки и нормали
            Vector3 localPoint = localOrigin + localDir * t;
            float absX = Math.Abs(localPoint.X);
            float absY = Math.Abs(localPoint.Y);
            float absZ = Math.Abs(localPoint.Z);
            float epsilon = 0.0001f;

            Vector3 localNormal;
            if (absX >= absY - epsilon && absX >= absZ - epsilon)
                localNormal = new Vector3(Math.Sign(localPoint.X), 0, 0);
            else if (absY >= absZ - epsilon)
                localNormal = new Vector3(0, Math.Sign(localPoint.Y), 0);
            else
                localNormal = new Vector3(0, 0, Math.Sign(localPoint.Z));

            // Перевод точки и нормали в мировые координаты
            hit.Distance = t;
            hit.Point = ray.GetPoint(t);
            hit.Normal = Vector3.TransformVector(localNormal, rot).Normalized();
            hit.Object = this;

            return true;
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

            float half = Side / 2.0f;
            float lx = -half + localU * Side;
            float ly = half - localV * Side;

            Vector3 localPos = Vector3.Zero;
            Vector3 localNormal = Vector3.Zero;

            switch (face)
            {
                case 0: localNormal = new Vector3(0, 0, 1); localPos = new Vector3(lx, ly, half); break;
                case 1: localNormal = new Vector3(0, 0, -1); localPos = new Vector3(-lx, ly, -half); break;
                case 2: localNormal = new Vector3(0, 1, 0); localPos = new Vector3(lx, half, -ly); break;
                case 3: localNormal = new Vector3(0, -1, 0); localPos = new Vector3(lx, -half, ly); break;
                case 4: localNormal = new Vector3(1, 0, 0); localPos = new Vector3(half, ly, -lx); break;
                case 5: localNormal = new Vector3(-1, 0, 0); localPos = new Vector3(-half, ly, lx); break;
            }

            // Перевод в мировые координаты
            Matrix4 rot = GetRotationMatrix();
            position = Center + Vector3.TransformVector(localPos, rot);
            normal = Vector3.TransformVector(localNormal, rot).Normalized();
            return true;
        }

        public override SceneObject Clone()
        {
            var clone = new Cube(Name, Center, _side, Material.Clone(), LightmapRes);
            clone.Rotation = Rotation;
            return clone;
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
