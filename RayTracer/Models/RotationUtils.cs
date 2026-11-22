using OpenTK.Mathematics;
using System;

namespace RayTracer.Models
{
    /// <summary>
    /// Вспомогательный класс для перевода углов поворота в градусах в вектор направления и обратно
    /// </summary>
    public static class RotationUtils
    {
        /// <summary>
        /// Преобразует углы поворота в градусах в нормализованный вектор направления
        /// </summary>
        /// <param name="pitchDeg">Угол поворота по вертикали</param>
        /// <param name="yawDeg">Угол поворота по горизонтали</param>
        /// <returns>Вектор направления</returns>
        public static Vector3 AnglesToDirection(float pitchDeg, float yawDeg)
        {
            float pitchRad = MathHelper.DegreesToRadians(pitchDeg);
            float yawRad = MathHelper.DegreesToRadians(yawDeg);

            return new Vector3(
                (float)(Math.Cos(pitchRad) * Math.Sin(yawRad)),
                (float)Math.Sin(pitchRad),
                (float)(-Math.Cos(pitchRad) * Math.Cos(yawRad))
            ).Normalized();
        }

        /// <summary>
        /// Преобразует вектор направления в углы  в градусах
        /// </summary>
        /// <param name="direction">Вектор направления</param>
        /// <returns>Два угла в градусах: поворот по вертикали, поворот по горизонтали</returns>
        public static (float pitchDeg, float yawDeg) DirectionToAngles(Vector3 direction)
        {
            if (direction.LengthSquared < 1e-6f)
                return (0f, 0f);

            Vector3 d = direction.Normalized();
            float pitchDeg = MathHelper.RadiansToDegrees((float)Math.Asin(Math.Clamp(d.Y, -1f, 1f)));
            float yawDeg = MathHelper.RadiansToDegrees((float)Math.Atan2(d.X, -d.Z));

            return (pitchDeg, yawDeg);
        }
    }
}