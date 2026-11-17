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
        private Vector3 _direction;
        private Vector3 _up;
        private float _fovDegrees;
        private float _speed;
        private float _sensitivity;

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
        public Vector3 Direction
        {
            get { return _direction; }
            set { _direction = value.Normalized(); }
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
        /// <param name="direction">Направление камеры</param>
        /// <param name="up">Вектор "верха"</param>
        /// <param name="fovDegrees">Вертикальный угол обзора (в градусах)</param>
        /// <param name="speed">Скорость перемещения</param>
        public Camera(string name, Vector3 position, Vector3 direction, Vector3 up, float fovDegrees, float speed, float sensitivity)
        {
            _name = name;
            _position = position;
            _direction = direction;
            _up = up;
            _fovDegrees = fovDegrees;
            _speed = speed;
            _sensitivity = sensitivity;
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

            _forward = -_direction;
            _right = Vector3.Cross(_up, _forward).Normalized();
            _localUp = Vector3.Cross(_forward, _right);

            _horizontal = viewportWidth * _right;
            _vertical = viewportHeight * _localUp;
            _lowerLeftCorner = _position - _horizontal / 2.0f - _vertical / 2.0f - _forward;
        }

        /// <summary>
        /// Перемещение камеры
        /// </summary>
        /// <param name="dx">Направление по X</param>
        /// <param name="dy">Направление по Y</param>
        /// <param name="shift">Нажата ли клавиша shift</param>
        /// <param name="deltaTime">Пройденное время</param>
        public void Move(int dx, int dy, bool shift, float deltaTime)
        {
            float speed = _speed * deltaTime;
            if(shift) speed *= 2;

            _forward = -_direction;
            _right = Vector3.Cross(_up, _forward).Normalized();

            _position += dx * _direction * speed;
            _position += dy * _right * speed;
        }

        public void Rotate(float dx, float dy)
        {
            float pitch = (float)Math.Asin(_direction.Y);
            float yaw = (float)Math.Atan2(_direction.Z, _direction.X);

            yaw += dx * _sensitivity;
            pitch -= dy * _sensitivity;

            float limit = MathHelper.DegreesToRadians(89.0f);
            pitch = Math.Clamp(pitch, -limit, limit);

            _direction = new Vector3(
                (float)(Math.Cos(pitch) * Math.Cos(yaw)),
                (float)Math.Sin(pitch),
                (float)(Math.Cos(pitch) * Math.Sin(yaw))
            ).Normalized();
        }

        public override string ToString()
        {
            return _name;
        }
    }
}
