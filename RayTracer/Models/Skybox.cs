using OpenTK.Mathematics;

namespace RayTracer.Models
{
    /// <summary>
    /// Фон сцены (небо)
    /// </summary>
    public class Skybox
    {
        private Vector3 _colorTop;
        private Vector3 _colorBottom;

        /// <summary>
        /// Цвет верхней части фона
        /// </summary>
        public Vector3 ColorTop
        {
            get { return _colorTop; }
            set { _colorTop = value; }
        }
        /// <summary>
        /// Цвет нижней части фона
        /// </summary>
        public Vector3 ColorBottom
        {
            get { return _colorBottom; }
            set { _colorBottom = value; }
        }

        public Skybox(Vector3 colorTop, Vector3 colorBottom)
        {
            _colorTop = colorTop;
            _colorBottom = colorBottom;
        }

        /// <summary>
        /// Получение цвета неба в зависимости от направления луча
        /// </summary>
        /// <param name="direction">Направление</param>
        /// <returns>Цвет</returns>
        public Vector3 GetColor(Vector3 direction)
        {
            float t = 0.5f * (direction.Y + 1.0f);

            return (1.0f - t) * ColorBottom + t * ColorTop;
        }
    }
}
