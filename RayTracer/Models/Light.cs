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
        private string _name;

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

        /// <summary>
        /// Имя источника света
        /// </summary>
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        protected Light(string name, Vector3 color, float intensity)
        {
            _name = name;
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

        /// <summary>
        /// Клонирование источника света
        /// </summary>
        /// <returns>Копия источника света</returns>
        public abstract Light Clone();
    }
}
