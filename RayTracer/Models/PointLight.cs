using OpenTK.Mathematics;

namespace RayTracer.Models
{
    public class PointLight : Light
    {
        private Vector3 _position;
        private float _range;
        
        /// <summary>
        /// Координаты источника
        /// </summary>
        public Vector3 Position
        {
            get { return _position; }
            set { _position = value; }
        }

        /// <summary>
        /// Дальность света
        /// </summary>
        public float Range
        {
            get { return _range; }
            set { _range = value; }
        }

        public PointLight(string name, Vector3 color, float intensity, Vector3 position, float range)
            : base(name, color, intensity)
        {
            _position = position;
            _range = range;
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
            float distance = (_position - point).Length;

            if (distance > _range)
                return 0f;

            float distanceRatio = distance / _range;

            return Intensity * Math.Max(0, 1.0f - distanceRatio * distanceRatio); ;
        }

        public override Light Clone()
        {
            return new PointLight(Name, Color, Intensity, _position, _range);
        }
    }
}
