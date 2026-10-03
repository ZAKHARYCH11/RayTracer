using OpenTK.Mathematics;

namespace RayTracer.Models
{
    /// <summary>
    /// Класс, описывающий оптические свойства поверхности объекта
    /// </summary>
    public class Material
    {
        private Vector3 _color;
        private float _reflectivity;

        /// <summary>
        /// Базовый цвет объекта (RGB от 0.0 до 1.0)
        /// </summary>
        public Vector3 Color
        {
            get { return _color; }
            set { _color = value; }
        }

        /// <summary>
        /// Коэффициент зеркальности (0 - матовый, 1 - идеальное зеркало)
        /// </summary>
        public float Reflectivity 
        {
            get { return _reflectivity; }
            set { _reflectivity = value; }
        }

        public Material(Vector3 color, float reflectivity = 0.0f)
        {
            _color = color;
            _reflectivity = reflectivity;
        }

        /// <summary>
        /// Клонирование материала
        /// </summary>
        /// <returns>Материал с те ме же свойствами</returns>
        public Material Clone()
        {
            return new Material(_color, _reflectivity);
        }
    }
}
