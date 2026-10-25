using OpenTK.Mathematics;

namespace RayTracer.Models
{
    /// <summary>
    /// Геометрический примитив: Сфера
    /// </summary>
    public class Sphere : SceneObject
    {
        private float _radius;

        /// <summary>
        /// Радиус сферы
        /// </summary>
        public float Radius 
        {
            get { return _radius; }
            set { _radius = value; }
        }

        public Sphere(string name, Vector3 center, float radius, Material material, int lightmapRes = 256) : base(name, material, center, lightmapRes)
        {
            _radius = radius;
        }

        /// <summary>
        /// Проверка пересечения луча со сферой
        /// </summary>
        /// <param name="ray">Испускаемый луч</param>
        /// <param name="hit">Данные о пересечении</param>
        /// <returns>True, если пересечение есть, иначе False</returns>
        public override bool Intersect(Ray ray, out Intersection hit)
        {
            hit = default;

            Vector3 oc = ray.Origin - Center;

            float b = 2.0f * Vector3.Dot(oc, ray.Direction);
            float c = Vector3.Dot(oc, oc) - _radius * _radius;

            float discriminant = b * b - 4 * c;

            if (discriminant < 0)
            {
                return false;
            }

            float t = (-b - (float)Math.Sqrt(discriminant)) / 2.0f;

            if (t < 0.001f)
            {
                t = (-b + (float)Math.Sqrt(discriminant)) / 2.0f;
            }

            if (t < 0.001f)
            {
                return false;
            }

            hit.Distance = t;
            hit.Point = ray.GetPoint(t);
            hit.Normal = (hit.Point - Center).Normalized();
            hit.Object = this;

            return true;
        }

        public override SceneObject Clone()
        {
            return new Sphere(Name, Center, _radius, Material.Clone(), LightmapRes);
        }

        public override bool TryGetSurfaceData(float u, float v, out Vector3 position, out Vector3 normal)
        {
            float phi = u * 2.0f * (float)Math.PI;
            float theta = v * (float)Math.PI;

            float nx = (float)(Math.Sin(theta) * Math.Cos(phi));
            float ny = (float)Math.Cos(theta);
            float nz = (float)(Math.Sin(theta) * Math.Sin(phi));

            normal = new Vector3(nx, ny, nz).Normalized();

            position = Center + normal * _radius;
            return true;
        }
    }
}
