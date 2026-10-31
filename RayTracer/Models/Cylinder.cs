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

            float tClosest = float.MaxValue;
            Vector3 bestNormal = Vector3.Zero;

            float halfHeight = _height / 2.0f;
            float yMin = Center.Y - halfHeight;
            float yMax = Center.Y + halfHeight;

            float a = ray.Direction.X * ray.Direction.X + ray.Direction.Z * ray.Direction.Z;

            // Пересечение с боковой поверхностью
            if (Math.Abs(a) > 0.00001f)
            {
                float ocX = ray.Origin.X - Center.X;
                float ocZ = ray.Origin.Z - Center.Z;

                // Коэффициенты квадратного уравнения
                float b = 2.0f * (ocX * ray.Direction.X + ocZ * ray.Direction.Z);
                float c = ocX * ocX + ocZ * ocZ - _radius * _radius;

                float discriminant = b * b - 4 * a * c;

                if (discriminant >= 0)
                {
                    float sqrtD = (float)Math.Sqrt(discriminant);
                    float t1 = (-b - sqrtD) / (2.0f * a);
                    float t2 = (-b + sqrtD) / (2.0f * a);

                    if (t1 > 0.001f && t1 < tClosest)
                    {
                        float y1 = ray.Origin.Y + t1 * ray.Direction.Y;
                        if (y1 >= yMin && y1 <= yMax)
                        {
                            tClosest = t1;

                            bestNormal = new Vector3((ray.Origin.X + t1 * ray.Direction.X - Center.X) / _radius, 0,
                                                     (ray.Origin.Z + t1 * ray.Direction.Z - Center.Z) / _radius).Normalized();
                        }
                    }

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

            // Пересечение с основаниями цилиндра
            if (Math.Abs(ray.Direction.Y) > 0.00001f)
            {
                float tBottom = (yMin - ray.Origin.Y) / ray.Direction.Y;
                if (tBottom > 0.001f && tBottom < tClosest)
                {
                    Vector3 p = ray.GetPoint(tBottom);

                    if ((p.X - Center.X) * (p.X - Center.X) + (p.Z - Center.Z) * (p.Z - Center.Z) <= _radius * _radius)
                    {
                        tClosest = tBottom;
                        bestNormal = new Vector3(0, -1, 0);
                    }
                }

                float tTop = (yMax - ray.Origin.Y) / ray.Direction.Y;
                if (tTop > 0.001f && tTop < tClosest)
                {
                    Vector3 p = ray.GetPoint(tTop);
                    if ((p.X - Center.X) * (p.X - Center.X) + (p.Z - Center.Z) * (p.Z - Center.Z) <= _radius * _radius)
                    {
                        tClosest = tTop;
                        bestNormal = new Vector3(0, 1, 0);
                    }
                }
            }

            // Нет пересечения
            if (tClosest == float.MaxValue) return false;

            hit.Distance = tClosest;
            hit.Point = ray.GetPoint(tClosest);
            hit.Normal = bestNormal;
            hit.Object = this;
            return true;
        }

        public override SceneObject Clone()
        {
            return new Cylinder(Name, Center, _radius, _height, Material.Clone(), LightmapRes);
        }

        public override bool TryGetSurfaceData(float u, float v, out Vector3 position, out Vector3 normal)
        {
            u = Math.Clamp(u, 0f, 0.999f);
            v = Math.Clamp(v, 0f, 0.999f);

            position = Vector3.Zero;
            normal = Vector3.Zero;

            if (u < 0.5f)
            {
                float localU = u * 2.0f;
                float phi = localU * 2.0f * (float)Math.PI;
                float y = _height / 2.0f - v * _height;

                normal = new Vector3((float)Math.Cos(phi), 0, (float)Math.Sin(phi)).Normalized();
                position = new Vector3(_radius * normal.X, y, _radius * normal.Z);
            }
            else
            {
                float localU = (u - 0.5f) * 2.0f;
                float localV = (v % 0.5f) * 2.0f;

                float x = (localU - 0.5f) * 2.0f * _radius;
                float z = (localV - 0.5f) * 2.0f * _radius;

                float dist = (float)Math.Sqrt(x * x + z * z);

                if (dist > _radius)
                {
                    x = (x / dist) * _radius;
                    z = (z / dist) * _radius;
                }

                if (v < 0.5f)
                {
                    normal = new Vector3(0, 1, 0);
                    position = new Vector3(x, _height / 2.0f, z);
                }
                else
                {
                    normal = new Vector3(0, -1, 0);
                    position = new Vector3(x, -_height / 2.0f, z);
                }
            }

            position += Center;
            return true;
        }
    }
}
