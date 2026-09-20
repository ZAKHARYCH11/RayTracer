using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using OpenTK.Wpf;
using OpenTK.Graphics.OpenGL;

namespace RayTracer.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // Инициализируем настройки для нашего OpenGL-контрола
            var settings = new GLWpfControlSettings
            {
                MajorVersion = 3,
                MinorVersion = 3
            };
            OpenGlControl.Start(settings);
        }

        // Этот метод вызывается каждый кадр для перерисовки 3D-зоны
        private void OpenGlControl_Render(TimeSpan delta)
        {
            // Очищаем экран темно-синим цветом (проверка, что OpenGL работает)
            GL.ClearColor(0.1f, 0.2f, 0.3f, 1.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            // Позже здесь мы будем рисовать превью нашей сцены
        }
    }
}