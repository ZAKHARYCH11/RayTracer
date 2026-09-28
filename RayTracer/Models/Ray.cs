using OpenTK.Mathematics;

namespace RayTracer.Models
{
    /// <summary>
    /// Класс, описывающий математический луч в 3D пространстве.
    /// Используется для трассировки: P(t) = Origin + t * Direction.
    /// </summary>
    public class Ray
    {
        private Vector3 _origin;
        private Vector3 _direction;

        /// <summary>
        /// Точка начала луча
        /// </summary>
        public Vector3 Origin
        {
            get { return _origin; }
        }

        /// <summary>
        /// Нормализованный вектор направления луча
        /// </summary>
        public Vector3 Direction
        {
            get { return _direction; }
        }

        /// <summary>
        /// Конструктор луча
        /// </summary>
        /// <param name="origin">Точка испускания луча</param>
        /// <param name="direction">Направление луча</param>
        public Ray(Vector3 origin, Vector3 direction)
        {
            _origin = origin;
            _direction = direction.Normalized();
        }

        /// <summary>
        /// Вычисляет точку в 3D пространстве на заданном расстоянии t вдоль луча
        /// </summary>
        /// <param name="t">Параметр дистанции (t >= 0)</param>
        /// <returns>Координаты точки пересечения</returns>
        public Vector3 GetPoint(float t)
        {
            return _origin + _direction * t;
        }
    }
}
