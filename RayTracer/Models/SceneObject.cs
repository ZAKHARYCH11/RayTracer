using OpenTK.Mathematics;

namespace RayTracer.Models
{
    /// <summary>
    /// Абстрактный базовый класс для всех геометрических объектов на сцене
    /// </summary>
    public abstract class SceneObject
    {
        private Material _material;
        private string _name;
        private Vector3 _center;

        /// <summary>
        /// Материал объекта
        /// </summary>
        public Material Material
        {
            get { return _material; }
            set { _material = value; }
        }

        /// <summary>
        /// Имя объекта
        /// </summary>
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        /// <summary>
        /// Центр объекта
        /// </summary>
        public Vector3 Center
        {
            get { return _center; }
            set { _center = value; }
        }

        protected SceneObject(string name, Material material, Vector3 center)
        {
            _name = name;
            _material = material;
            _center = center;
        }

        /// <summary>
        /// Абстрактный метод проверки пересечения луча с объектом
        /// </summary>
        /// <param name="ray">Испускаемый луч</param>
        /// <param name="hit">Данные о пересечении</param>
        /// <returns>True, если пересечение есть, иначе False</returns>
        public abstract bool Intersect(Ray ray, out Intersection hit);

        /// <summary>
        /// Клонирование объекта
        /// </summary>
        /// <returns>Копия объекта</returns>
        public abstract SceneObject Clone();

        public override string ToString()
        {
            return _name;
        }
    }
}
