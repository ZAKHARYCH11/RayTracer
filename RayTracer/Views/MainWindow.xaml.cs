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
using RayTracer.Controllers;

namespace RayTracer.Views
{
    public partial class MainWindow : Window
    {
        private MainController _controller;

        public MainWindow()
        {
            InitializeComponent();

            var settings = new GLWpfControlSettings
            {
                MajorVersion = 3,
                MinorVersion = 3
            };
            OpenGlControl.Start(settings);

            _controller = new MainController(this);
        }

        private void OpenGlControl_Render(TimeSpan delta)
        {
            GL.ClearColor(0.1f, 0.2f, 0.3f, 1.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        }
    }
}