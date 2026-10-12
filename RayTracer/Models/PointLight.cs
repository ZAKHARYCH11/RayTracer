using OpenTK.Mathematics;

namespace RayTracer.Models
{
    public class PointLight : Light
    {
        private Vector3 _position;
        
        /// <summary>
        /// Координаты источника
        /// </summary>
        public Vector3 Position
        {
            get { return _position; }
            set { _position = value; }
        }

        public PointLight(string name, Vector3 color, float intensity, Vector3 position)
            : base(name, color, intensity)
        {
            _position = position;
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
            return Intensity;
        }

        public override Light Clone()
        {
            return new PointLight(Name, Color, Intensity, _position);
        }
    }
}
