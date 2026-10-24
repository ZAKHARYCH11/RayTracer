using OpenTK.Mathematics;


namespace RayTracer.Models
{
    /// <summary>
    /// Абстрактный базовый класс для всех геометрических объектов на сцене
    /// </summary>
    public abstract class SceneObject
    {
        private Material _material;
        private string _name;
        private Vector3 _center;
        private Lightmap _lightmap;

        /// <summary>
        /// Материал объекта
        /// </summary>
        public Material Material
        {
            get { return _material; }
            set { _material = value; }
        }

        /// <summary>
        /// Имя объекта
        /// </summary>
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        /// <summary>
        /// Центр объекта
        /// </summary>
        public Vector3 Center
        {
            get { return _center; }
            set { _center = value; }
        }

        /// <summary>
        /// Световая карта объекта
        /// </summary>
        public Lightmap Lightmap
        {
            get { return _lightmap; }
            set { _lightmap = value; }
        }

        protected SceneObject(string name, Material material, Vector3 center)
        {
            _name = name;
            _material = material;
            _center = center;

            _lightmap = GenerateDefaultLightmap();
        }

        /// <summary>
        /// Генерирует черно-фиолетовую шахматную доску (текстура заглушка)
        /// </summary>
        private Lightmap GenerateDefaultLightmap()
        {
            int size = 64;
            byte[] pixels = new byte[size * size * 3];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int index = (y * size + x) * 3;
                    bool isMagenta = ((x / 8) % 2) == ((y / 8) % 2);

                    pixels[index] = isMagenta ? (byte)255 : (byte)0;     // R
                    pixels[index + 1] = 0;                               // G
                    pixels[index + 2] = isMagenta ? (byte)255 : (byte)0; // B
                }
            }

            return new Lightmap(size, size, pixels);
        }

        /// <summary>
        /// Абстрактный метод проверки пересечения луча с объектом
        /// </summary>
        /// <param name="ray">Испускаемый луч</param>
        /// <param name="hit">Данные о пересечении</param>
        /// <returns>True, если пересечение есть, иначе False</returns>
        public abstract bool Intersect(Ray ray, out Intersection hit);

        /// <summary>
        /// Клонирование объекта
        /// </summary>
        /// <returns>Копия объекта</returns>
        public abstract SceneObject Clone();

        /// <summary>
        /// Получение точки на поверхности
        /// </summary>
        /// <param name="u">Координата u</param>
        /// <param name="v">Координата v</param>
        /// <param name="point">Точка поверхности</param>
        /// <param name="normal">Нормаль к точке</param>
        /// <returns></returns>
        //public abstract bool TryGetSurfacePoint(float u, float v, out Vector3 point, out Vector3 normal);

        ///public abstract MeshData BuildMesh(int detail);

        public override string ToString()
        {
            return _name;
        }
    }
}
