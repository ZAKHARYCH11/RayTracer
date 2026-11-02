using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

using Point = System.Windows.Point;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using MouseButtonEventArgs = System.Windows.Input.MouseButtonEventArgs;
using MouseWheelEventArgs = System.Windows.Input.MouseWheelEventArgs;

namespace RayTracer.Views
{
    /// <summary>
    /// Логика взаимодействия для LightmapWindow.xaml
    /// </summary>
    public partial class LightmapWindow : Window
    {
        private Point _lastMousePosition;
        private bool _leftMouseDown;

        public LightmapWindow(ImageSource source)
        {
            InitializeComponent();
            LargeImage.Source = source;
        }

        /// <summary>
        /// Изменение масштаба изобращения
        /// </summary>
        private void ImageContainer_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            double zoom = e.Delta > 0 ? 1.2 : 0.8;

            if (imgScale.ScaleX * zoom < 0.1 || imgScale.ScaleX * zoom > 50.0) return;

            Point mousePos = e.GetPosition(ImageContainer);
            double mx = mousePos.X - ImageContainer.ActualWidth / 2.0;
            double my = mousePos.Y - ImageContainer.ActualHeight / 2.0;

            imgTranslate.X = mx - (mx - imgTranslate.X) * zoom;
            imgTranslate.Y = my - (my - imgTranslate.Y) * zoom;

            imgScale.ScaleX *= zoom;
            imgScale.ScaleY *= zoom;

            ClampImagePosition();
        }

        /// <summary>
        /// Нажатие ЛКМ
        /// </summary>
        private void ImageContainer_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _lastMousePosition = e.GetPosition(null);
            _leftMouseDown = true;
            ImageContainer.CaptureMouse();
        }

        /// <summary>
        /// Перемещение по изображению
        /// </summary>
        private void ImageContainer_MouseMove(object sender, MouseEventArgs e)
        {
            if (_leftMouseDown && e.LeftButton == MouseButtonState.Pressed)
            {
                Point currentMousePosition = e.GetPosition(null);

                double deltaX = currentMousePosition.X - _lastMousePosition.X;
                double deltaY = currentMousePosition.Y - _lastMousePosition.Y;

                imgTranslate.X += deltaX;
                imgTranslate.Y += deltaY;

                ClampImagePosition();

                _lastMousePosition = currentMousePosition;
            }
            else
            {
                _leftMouseDown = false;
            }
        }

        /// <summary>
        /// Отжатие ЛКМ
        /// </summary>
        private void ImageContainer_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _leftMouseDown = false;
            ImageContainer.ReleaseMouseCapture();
        }

        /// <summary>
        /// Ограничение перемещения изображения
        /// </summary>
        private void ClampImagePosition()
        {
            double halfImageW = LargeImage.ActualWidth * imgScale.ScaleX / 2.0;
            double halfImageH = LargeImage.ActualHeight * imgScale.ScaleY / 2.0;

            double limitX = Math.Max(ImageContainer.ActualWidth / 2.0, halfImageW);
            double limitY = Math.Max(ImageContainer.ActualHeight / 2.0, halfImageH);

            imgTranslate.X = Math.Clamp(imgTranslate.X, -limitX, limitX);
            imgTranslate.Y = Math.Clamp(imgTranslate.Y, -limitY, limitY);
        }
    }
}
