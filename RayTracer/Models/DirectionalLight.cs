using OpenTK.Mathematics;

namespace RayTracer.Models
{
    /// <summary>
    /// Направленный источник света
    /// </summary>
    public class DirectionalLight : Light
    {
        private Vector3 _direction;

        /// <summary>
        /// Направление света
        /// </summary>
        public Vector3 Direction
        {
            get { return _direction; }
            set { _direction = value.Normalized(); }
        }

        public DirectionalLight(string name, Vector3 color, float intensity, Vector3 direction)
            : base(name, color, intensity)
        {
            _direction = direction.Normalized();
        }

        public override Vector3 GetDirectionToLight(Vector3 point)
        {
            return -_direction;
        }

        public override float GetDistanceToLight(Vector3 point)
        {
            return float.MaxValue;
        }

        public override Light Clone()
        {
            return new DirectionalLight(Name, Color, Intensity, _direction);
        }
    }
}
