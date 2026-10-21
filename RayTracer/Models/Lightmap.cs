using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RayTracer.Models
{
    /// <summary>
    /// Класс световой карты
    /// </summary>
    public class Lightmap
    {
        private int _width;
        private int _height;
        private byte[] _pixels;

        /// <summary>
        /// Ширина световой карты
        /// </summary>
        public int Width
        {
            get { return _width; }
            set { _width = value; }
        }

        /// <summary>
        /// Высота световой карты
        /// </summary>
        public int Height
        {
            get { return _height; }
            set { _height = value; }
        }

        /// <summary>
        /// Пиксели световой карты
        /// </summary>
        public byte[] Pixels
        {
            get { return _pixels; }
            set { _pixels = value; }
        }

        public Lightmap(int width, int height, byte[] pixels)
        {
            _width = width;
            _height = height;
            _pixels = pixels;
        }
    }
}
