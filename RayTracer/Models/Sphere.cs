using OpenTK.Mathematics;

namespace RayTracer.Models
{
    /// <summary>
    /// Геометрический примитив: Сфера
    /// </summary>
    public class Sphere : SceneObject
    {
        private Vector3 _center;
        private float _radius;

        /// <summary>
        /// Центр сферы в 3D пространстве
        /// </summary>
        public Vector3 Center
        {
            get { return _center; }
            set { _center = value; }
        }

        /// <summary>
        /// Радиус сферы
        /// </summary>
        public float Radius 
        {
            get { return _radius; }
            set { _radius = value; }
        }

        public Sphere(Vector3 center, float radius, Material material) : base(material)
        {
            _center = center;
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

            Vector3 oc = ray.Origin - _center;

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
            hit.Normal = (hit.Point - _center).Normalized();
            hit.Object = this;

            return true;
        }
    }
}
