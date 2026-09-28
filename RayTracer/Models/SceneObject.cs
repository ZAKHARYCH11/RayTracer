namespace RayTracer.Models
{
    /// <summary>
    /// Абстрактный базовый класс для всех геометрических объектов на сцене
    /// </summary>
    public abstract class SceneObject
    {
        private Material _material;

        /// <summary>
        /// Материал объекта
        /// </summary>
        public Material Material
        {
            get { return _material; }
            set { _material = value; }
        }

        protected SceneObject(Material material)
        {
            _material = material;
        }

        /// <summary>
        /// Абстрактный метод проверки пересечения луча с объектом
        /// </summary>
        /// <param name="ray">Испускаемый луч</param>
        /// <param name="hit">Данные о пересечении</param>
        /// <returns>True, если пересечение есть, иначе False</returns>
        public abstract bool Intersect(Ray ray, out Intersection hit);
    }
}
