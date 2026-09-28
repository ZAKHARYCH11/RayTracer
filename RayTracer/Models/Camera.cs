using OpenTK.Mathematics;

namespace RayTracer.Models
{
    /// <summary>
    /// Класс виртуальной камеры для генерации лучей обзора
    /// </summary>
    public class Camera
    {
        private Vector3 _postion;

        public Vector3 Position
        {
            get { return _postion; }
            private set { _postion = value; }
        }

        // Вектора ориентации камеры
        private Vector3 _forward;
        private Vector3 _right;
        private Vector3 _up;

        // Вектора для маппинга экрана в 3D пространство
        private Vector3 _horizontal;
        private Vector3 _vertical;
        private Vector3 _lowerLeftCorner;

        /// <summary>
        /// Конструктор камеры
        /// </summary>
        /// <param name="position">Позиция камеры</param>
        /// <param name="lookAt">Точка, куда смотрит камера</param>
        /// <param name="up">Вектор "верха"</param>
        /// <param name="fovDegrees">Вертикальный угол обзора (в градусах)</param>
        /// <param name="aspectRatio">Соотношение сторон экрана (ширина / высота)</param>
        public Camera(Vector3 position, Vector3 lookAt, Vector3 up, float fovDegrees, float aspectRatio)
        {
            _postion = position;

            float fovRadians = MathHelper.DegreesToRadians(fovDegrees);
            float h = (float)Math.Tan(fovRadians / 2.0f);

            float viewportHeight = 2.0f * h;
            float viewportWidth = aspectRatio * viewportHeight;

            _forward = (position - lookAt).Normalized();
            _right = Vector3.Cross(up, _forward).Normalized();
            _up = Vector3.Cross(_forward, _right);

            _horizontal = viewportWidth * _right;
            _vertical = viewportHeight * _up;

            _lowerLeftCorner = _postion - _horizontal / 2.0f - _vertical / 2.0f - _forward;
        }

        /// <summary>
        /// Генерирует луч из камеры через заданную точку на экране
        /// </summary>
        /// <param name="u">Горизонтальная координата экрана (от 0 до 1)</param>
        /// <param name="v">Вертикальная координата экрана (от 0 до 1)</param>
        /// <returns>Луч, летящий из камеры в 3D сцену</returns>
        public Ray GetRay(float u, float v)
        {
            Vector3 direction = _lowerLeftCorner + u * _horizontal + v * _vertical - _postion;
            return new Ray(_postion, direction);
        }
    }
}
