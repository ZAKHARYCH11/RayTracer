using OpenTK.Mathematics;
using System;
using System.Linq;

namespace RayTracer.Models
{
    /// <summary>
    /// Класс виртуальной камеры для генерации лучей обзора
    /// </summary>
    public class Camera
    {
        private string _name;
        private Vector3 _position;
        private Vector3 _lookAt;
        private Vector3 _up;
        private float _fovDegrees;

        /// <summary>
        /// Имя камеры
        /// </summary>
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        /// <summary>
        /// Положение камеры
        /// </summary>
        public Vector3 Position
        {
            get { return _position; }
            set { _position = value; }
        }

        /// <summary>
        /// Направление взгяда камеры
        /// </summary>
        public Vector3 LookAt
        {
            get { return _lookAt; }
            set { _lookAt = value; }
        }

        /// <summary>
        /// Вектор "верха" камеры
        /// </summary>
        public Vector3 Up
        {
            get { return _up; }
            set { _up = value; }
        }

        /// <summary>
        /// Угол обзора камеры в градусах
        /// </summary>
        public float FovDegrees
        {
            get { return _fovDegrees; }
            set { _fovDegrees = value; }
        }

        // Вектора ориентации камеры
        private Vector3 _forward;
        private Vector3 _right;
        private Vector3 _localUp;

        // Вектора для маппинга экрана в 3D пространство
        private Vector3 _horizontal;
        private Vector3 _vertical;
        private Vector3 _lowerLeftCorner;

        /// <summary>
        /// Конструктор камеры
        /// </summary>
        /// <param name="name">Имя камеры</param>
        /// <param name="position">Позиция камеры</param>
        /// <param name="lookAt">Точка, куда смотрит камера</param>
        /// <param name="up">Вектор "верха"</param>
        /// <param name="fovDegrees">Вертикальный угол обзора (в градусах)</param>
        public Camera(string name, Vector3 position, Vector3 lookAt, Vector3 up, float fovDegrees)
        {
            _name = name;
            _position = position;
            _lookAt = lookAt;
            _up = up;
            _fovDegrees = fovDegrees;
        }

        /// <summary>
        /// Расчет векторов камеры
        /// </summary>
        /// <param name="aspectRatio">Соотношение сторон</param>
        public void CalculateVectors(float aspectRatio)
        {
            float theta = MathHelper.DegreesToRadians(_fovDegrees);
            float h = (float)Math.Tan(theta / 2.0f);
            float viewportHeight = 2.0f * h;
            float viewportWidth = aspectRatio * viewportHeight;

            _forward = (_position - _lookAt).Normalized();
            _right = Vector3.Cross(_up, _forward).Normalized();
            _localUp = Vector3.Cross(_forward, _right);

            _horizontal = viewportWidth * _right;
            _vertical = viewportHeight * _localUp;
            _lowerLeftCorner = Position - _horizontal / 2.0f - _vertical / 2.0f - _forward;
        }

        /// <summary>
        /// Генерирует луч из камеры через заданную точку на экране
        /// </summary>
        /// <param name="u">Горизонтальная координата экрана (от 0 до 1)</param>
        /// <param name="v">Вертикальная координата экрана (от 0 до 1)</param>
        /// <returns>Луч, летящий из камеры в 3D сцену</returns>
        public Ray GetRay(float u, float v)
        {
            Vector3 direction = _lowerLeftCorner + u * _horizontal + v * _vertical - _position;
            return new Ray(_position, direction);
        }

        public override string ToString()
        {
            return _name;
        }
    }
}
