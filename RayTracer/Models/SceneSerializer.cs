using System.IO;
using Newtonsoft.Json;

namespace RayTracer.Models
{
    /// <summary>
    /// Класс для сохранения и загрузки сцены в формате JSON
    /// </summary>
    public static class SceneSerializer
    {
        private static readonly JsonSerializerSettings _settings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Auto,
            Formatting = Formatting.Indented,
            Converters = new JsonConverter[] { new Vector3Converter() }
        };

        /// <summary>
        /// Сохранение сцены
        /// </summary>
        /// <param name="scene">Объект сцены</param>
        /// <param name="filePath">Путь к файлу</param>
        public static void SaveScene(Scene scene, string filePath)
        {
            string json = JsonConvert.SerializeObject(scene, _settings);
            File.WriteAllText(filePath, json);
        }

        /// <summary>
        /// Загрузка сцены
        /// </summary>
        /// <param name="filePath">Путь к файлу</param>
        /// <returns>Объект сцены</returns>
        public static Scene LoadScene(string filePath)
        {
            string json = File.ReadAllText(filePath);
            return JsonConvert.DeserializeObject<Scene>(json, _settings);
        }
    }
}