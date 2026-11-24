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

        public Cylinder(string name, Vector3 center, float radius, float height, Material material, int lightmapRes = 256) : base(name, material, center, lightmapRes)
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

            // Перевод луча в локальные координаты цилиндра
            Matrix4 rot = GetRotationMatrix();
            Matrix4 invRot = Matrix4.Transpose(rot);

            Vector3 localOrigin = Vector3.TransformVector(ray.Origin - Center, invRot);
            Vector3 localDir = Vector3.TransformVector(ray.Direction, invRot).Normalized();
            Ray localRay = new Ray(localOrigin, localDir);

            float tClosest = float.MaxValue;
            Vector3 bestLocalNormal = Vector3.Zero;

            float halfHeight = _height / 2.0f;
            float yMin = -halfHeight;
            float yMax = halfHeight;

            // Пересечение с боковой поверхностью
            float a = localRay.Direction.X * localRay.Direction.X + localRay.Direction.Z * localRay.Direction.Z;
            if (Math.Abs(a) > 0.00001f)
            {
                float ocX = localRay.Origin.X;
                float ocZ = localRay.Origin.Z;

                // Коэффициенты квадратного уравнения
                float b = 2.0f * (ocX * localRay.Direction.X + ocZ * localRay.Direction.Z);
                float c = ocX * ocX + ocZ * ocZ - _radius * _radius;

                float discriminant = b * b - 4 * a * c;
                if (discriminant >= 0)
                {
                    float sqrtD = (float)Math.Sqrt(discriminant);
                    float t1 = (-b - sqrtD) / (2.0f * a);
                    float t2 = (-b + sqrtD) / (2.0f * a);

                    if (t1 > 0.001f && t1 < tClosest)
                    {
                        float y1 = localRay.Origin.Y + t1 * localRay.Direction.Y;
                        if (y1 >= yMin && y1 <= yMax)
                        {
                            tClosest = t1;
                            bestLocalNormal = new Vector3(
                                (localRay.Origin.X + t1 * localRay.Direction.X) / _radius, 0,
                                (localRay.Origin.Z + t1 * localRay.Direction.Z) / _radius).Normalized();
                        }
                    }

                    if (t2 > 0.001f && t2 < tClosest)
                    {
                        float y2 = localRay.Origin.Y + t2 * localRay.Direction.Y;
                        if (y2 >= yMin && y2 <= yMax)
                        {
                            tClosest = t2;
                            bestLocalNormal = new Vector3(
                                (localRay.Origin.X + t2 * localRay.Direction.X) / _radius, 0,
                                (localRay.Origin.Z + t2 * localRay.Direction.Z) / _radius).Normalized();
                        }
                    }
                }
            }

            // Пересечение с основаниями цилиндра
            if (Math.Abs(localRay.Direction.Y) > 0.00001f)
            {
                float tBottom = (yMin - localRay.Origin.Y) / localRay.Direction.Y;
                if (tBottom > 0.001f && tBottom < tClosest)
                {
                    Vector3 p = localRay.GetPoint(tBottom);
                    if (p.X * p.X + p.Z * p.Z <= _radius * _radius)
                    {
                        tClosest = tBottom;
                        bestLocalNormal = new Vector3(0, -1, 0);
                    }
                }

                float tTop = (yMax - localRay.Origin.Y) / localRay.Direction.Y;
                if (tTop > 0.001f && tTop < tClosest)
                {
                    Vector3 p = localRay.GetPoint(tTop);
                    if (p.X * p.X + p.Z * p.Z <= _radius * _radius)
                    {
                        tClosest = tTop;
                        bestLocalNormal = new Vector3(0, 1, 0);
                    }
                }
            }

            // Нет пересечения
            if (tClosest == float.MaxValue) return false;

            // Перевод в мировые координаты
            hit.Distance = tClosest;
            hit.Point = ray.GetPoint(tClosest);
            hit.Normal = Vector3.TransformVector(bestLocalNormal, rot).Normalized();
            hit.Object = this;
            return true;
        }

        public override bool TryGetSurfaceData(float u, float v, out Vector3 position, out Vector3 normal)
        {
            u = Math.Clamp(u, 0f, 0.999f);
            v = Math.Clamp(v, 0f, 0.999f);

            Vector3 localPos;
            Vector3 localNormal;

            if (u < 0.5f)
            {
                float localU = u * 2.0f;
                float phi = localU * 2.0f * (float)Math.PI;
                float y = Height / 2.0f - v * Height;

                localNormal = new Vector3((float)Math.Cos(phi), 0, (float)Math.Sin(phi)).Normalized();
                localPos = new Vector3(Radius * localNormal.X, y, Radius * localNormal.Z);
            }
            else
            {
                float localU = (u - 0.5f) * 2.0f;
                float localV = (v % 0.5f) * 2.0f;

                float x = (localU - 0.5f) * 2.0f * Radius;
                float z = (localV - 0.5f) * 2.0f * Radius;

                float dist = (float)Math.Sqrt(x * x + z * z);
                if (dist > Radius)
                {
                    x = (x / dist) * Radius;
                    z = (z / dist) * Radius;
                }

                if (v < 0.5f)
                {
                    localNormal = new Vector3(0, 1, 0);
                    localPos = new Vector3(x, Height / 2.0f, z);
                }
                else
                {
                    localNormal = new Vector3(0, -1, 0);
                    localPos = new Vector3(x, -Height / 2.0f, z);
                }
            }

            // Перевод в мировые координаты
            Matrix4 rot = GetRotationMatrix();
            position = Center + Vector3.TransformVector(localPos, rot);
            normal = Vector3.TransformVector(localNormal, rot).Normalized();
            return true;
        }

        public override SceneObject Clone()
        {
            var clone = new Cylinder(Name, Center, _radius, _height, Material.Clone(), LightmapRes);
            clone.Rotation = Rotation;
            return clone;
        }
    }
}
