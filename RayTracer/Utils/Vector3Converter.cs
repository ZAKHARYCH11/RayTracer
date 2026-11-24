using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenTK.Mathematics;

namespace RayTracer.Utils
{
    /// <summary>
    /// Конвертер для правильного сохранения векторов OpenTK в JSON
    /// </summary>
    public class Vector3Converter : JsonConverter<Vector3>
    {
        /// <summary>
        /// Запись вектора в JSON
        /// </summary>
        /// <param name="writer">JSON писатель</param>
        /// <param name="value">Вектор</param>
        /// <param name="serializer">JSON сериалайзер</param>
        public override void WriteJson(JsonWriter writer, Vector3 value, JsonSerializer serializer)
        {
            var jo = new JObject
            {
                { "X", value.X },
                { "Y", value.Y },
                { "Z", value.Z }
            };
            jo.WriteTo(writer);
        }

        /// <summary>
        /// Чтение ветора из JSON
        /// </summary>
        /// <param name="reader">JSON читатель</param>
        /// <param name="objectType">Тип объекта</param>
        /// <param name="existingValue">Существующий вектор</param>
        /// <param name="hasExistingValue">Существует ли вектор</param>
        /// <param name="serializer">JSON сериалайзер</param>
        /// <returns>Считанный из JSON вектор</returns>
        public override Vector3 ReadJson(JsonReader reader, Type objectType, Vector3 existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var jo = JObject.Load(reader);
            float x = jo["X"]?.Value<float>() ?? 0f;
            float y = jo["Y"]?.Value<float>() ?? 0f;
            float z = jo["Z"]?.Value<float>() ?? 0f;

            return new Vector3(x, y, z);
        }
    }
}