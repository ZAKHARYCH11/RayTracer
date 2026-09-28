using OpenTK.Mathematics;

namespace RayTracer.Models
{
    /// <summary>
    /// Абстрактный базовый класс для всех источников света
    /// </summary>
    public abstract class Light
    {
        private Vector3 _color;
        private float _intensity;

        /// <summary>
        /// Цвет света
        /// </summary>
        public Vector3 Color
        {
            get { return _color; }
            set { _color = value; }
        }

        /// <summary>
        /// Интенсивность света
        /// </summary>
        public float Intensity
        {
            get { return _intensity; }
            set { _intensity = value; }
        }

        protected Light(Vector3 color, float intensity)
        {
            _color = color;
            _intensity = intensity;
        }

        /// <summary>
        /// Получить вектор направления от точки к источнику света
        /// </summary>
        public abstract Vector3 GetDirectionToLight(Vector3 point);

        /// <summary>
        /// Получить дистанцию до источника света 
        /// </summary>
        public abstract float GetDistanceToLight(Vector3 point);
    }
}
