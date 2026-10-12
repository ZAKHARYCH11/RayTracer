using System;
using OpenTK.Mathematics;

namespace RayTracer.Models
{
    /// <summary>
    /// Направленный источник света
    /// </summary>
    public class SpotLight : Light
    {
        private Vector3 _position;
        private Vector3 _direction;
        private float _cutOffAngle;

        /// <summary>
        /// Координаты источника
        /// </summary>
        public Vector3 Position
        {
            get { return _position; }
            set { _position = value; }
        }

        /// <summary>
        /// Направление света
        /// </summary>
        public Vector3 Direction
        {
            get { return _direction; }
            set { _direction = value.Normalized(); }
        }

        /// <summary>
        /// Угол освещения
        /// </summary>
        public float CutOffAngle
        {
            get { return _cutOffAngle; }
            set { _cutOffAngle = value; }
        }

        public SpotLight(string name, Vector3 color, float intensity, Vector3 position, Vector3 direction, float cutOffAngle)
            : base(name, color, intensity)
        {
            _position = position;
            _direction = direction.Normalized();
            _cutOffAngle = cutOffAngle;
        }

        public override Vector3 GetDirectionToLight(Vector3 point)
        {
            return (_position - point).Normalized();
        }
        public override float GetDistanceToLight(Vector3 point)
        {
            return (_position - point).Length;
        }

        public override float GetIntensityAt(Vector3 point)
        {
            Vector3 lightToPoint = (point - _position).Normalized();

            float cosTheta = Vector3.Dot(lightToPoint, Direction);

            float cosCutOff = (float)Math.Cos(MathHelper.DegreesToRadians(_cutOffAngle));

            return cosTheta >= cosCutOff ? Intensity : 0f;
        }

        public override Light Clone()
        {
            return new SpotLight(Name, Color, Intensity, _position, _direction, _cutOffAngle);
        }
    }
}