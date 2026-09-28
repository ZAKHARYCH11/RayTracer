using OpenTK.Mathematics;

namespace RayTracer.Models
{
    /// <summary>
    /// Структура, хранящая данные о точке столкновения луча с объектом сцены
    /// </summary>
    public struct Intersection
    {
        /// <summary>
        /// Дистанция от начала луча до точки пересечения
        /// </summary>
        public float Distance;

        /// <summary>
        /// Точка в 3D пространстве, где произошло пересечение
        /// </summary>
        public Vector3 Point;

        /// <summary>
        /// Вектор нормали к поверхности в точке пересечения
        /// </summary>
        public Vector3 Normal;

        /// <summary>
        /// Ссылка на объект, с которым произошло пересечение
        /// </summary>
        public SceneObject Object;
    }
}
