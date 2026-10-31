namespace RayTracer.Models
{
    /// <summary>
    /// Класс сцены, содержащий все объекты и отвечающий за поиск пересечений
    /// </summary>
    public class Scene
    {
        private Camera _mainCamera;
        private List<SceneObject> _objects;
        private List<Light> _lights;
        private Skybox _skybox;
        private byte[] _lightmapData;
        private int _lightmapWidth;
        private int _lightmapHeight;

        /// <summary>
        /// Камера
        /// </summary>
        public Camera MainCamera
        {
            get { return _mainCamera; }
            set { _mainCamera = value; }
        }

        /// <summary>
        /// Список всех геометрических объектов
        /// </summary>
        public List<SceneObject> Objects
        {
            get { return _objects; }
            set { _objects = value; }
        }

        /// <summary>
        /// Список всех источников света
        /// </summary>
        public List<Light> Lights
        {
            get { return _lights; }
            set { _lights = value; }
        }

        /// <summary>
        /// Фон сцены
        /// </summary>
        public Skybox Skybox
        {
            get { return _skybox; }
            set { _skybox = value; }
        }

        /// <summary>
        /// Массив пикселей
        /// </summary>
        public byte[] LightmapData
        {
            get { return _lightmapData; }
            set { _lightmapData = value; }
        }

        /// <summary>
        /// Ширина световой карты
        /// </summary>
        public int LightmapWidth
        {
            get { return _lightmapWidth; }
            set { _lightmapWidth = value; }
        }

        /// <summary>
        /// Высота световой карты
        /// </summary>
        public int LightmapHeight
        {
            get { return _lightmapHeight; }
            set { _lightmapHeight = value; }
        }

        public Scene()
        {
            _objects = new List<SceneObject>();
            _lights = new List<Light>();
        }

        /// <summary>
        /// Проверяет пересечение луча со всеми объектами сцены и находит ближайшее
        /// </summary>
        /// <param name="ray">Испускаемый луч</param>
        /// <param name="closestHit">Данные о ближайшем пересечении</param>
        /// <returns>true, если луч попал хоть в один объект</returns>
        public bool RayCast(Ray ray, out Intersection closestHit)
        {
            bool hitAnything = false;
            float closestDistance = float.MaxValue;
            closestHit = default;

            foreach (var sceneObj in _objects)
            {
                if (sceneObj.Intersect(ray, out Intersection tempHit))
                {
                    if (tempHit.Distance < closestDistance)
                    {
                        hitAnything = true;
                        closestDistance = tempHit.Distance;
                        closestHit = tempHit;
                    }
                }
            }

            return hitAnything;
        }
    }
}
