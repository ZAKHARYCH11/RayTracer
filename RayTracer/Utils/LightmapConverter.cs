using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RayTracer.Models;
using System;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace RayTracer.Utils
{
    /// <summary>
    /// Конвертер для сжатого хранения световых карт в формате PNG внутри JSON
    /// </summary>
    public class LightmapConverter : JsonConverter<Lightmap>
    {
        /// <summary>
        /// Запись световой карты в JSON
        /// </summary>
        /// <param name="writer">JSON писатель</param>
        /// <param name="value">Световая карта</param>
        /// <param name="serializer">JSON сериалайзер</param>
        public override void WriteJson(JsonWriter writer, Lightmap? value, JsonSerializer serializer)
        {
            if (value == null || value.Pixels == null || value.Width <= 0 || value.Height <= 0)
            {
                writer.WriteNull();
                return;
            }

            var bitmap = BitmapSource.Create(
                value.Width, value.Height, 96, 96,
                PixelFormats.Rgb24, null, value.Pixels, value.Width * 3);

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));

            using var ms = new MemoryStream();
            encoder.Save(ms);
            byte[] pngBytes = ms.ToArray();

            var jo = new JObject
            {
                { "Width", value.Width },
                { "Height", value.Height },
                { "PngData", Convert.ToBase64String(pngBytes) }
            };
            jo.WriteTo(writer);
        }

        /// <summary>
        /// Чтение световой карты из JSON
        /// </summary>
        /// <param name="reader">JSON читатель</param>
        /// <param name="objectType">Тип объекта</param>
        /// <param name="existingValue">Существующая световая карты</param>
        /// <param name="hasExistingValue">Существует ли световая карта</param>
        /// <param name="serializer">JSON сериалайзер</param>
        /// <returns>Прочитанная из JSON световая карта</returns>
        public override Lightmap? ReadJson(JsonReader reader, Type objectType, Lightmap? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            var jo = JObject.Load(reader);
            int width = jo["Width"]?.Value<int>() ?? 0;
            int height = jo["Height"]?.Value<int>() ?? 0;

            if (jo["PngData"] != null)
            {
                string? base64 = jo["PngData"]?.Value<string>();
                if (string.IsNullOrEmpty(base64)) return null;

                byte[] pngBytes = Convert.FromBase64String(base64);
                using var ms = new MemoryStream(pngBytes);

                var decoder = new PngBitmapDecoder(ms, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
                BitmapSource frame = decoder.Frames[0];

                if (frame.Format != PixelFormats.Rgb24)
                {
                    frame = new FormatConvertedBitmap(frame, PixelFormats.Rgb24, null, 0);
                }

                int w = frame.PixelWidth;
                int h = frame.PixelHeight;
                byte[] pixels = new byte[w * h * 3];
                frame.CopyPixels(pixels, w * 3, 0);

                return new Lightmap(w, h, pixels);
            }

            return null;
        }
    }
}