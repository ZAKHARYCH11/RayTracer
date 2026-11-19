using System.Configuration;
using System.Data;
using System.Windows;
using RayTracer.Controllers;
using RayTracer.Views;

namespace RayTracer
{
    /// <summary>
    /// Логика взаимодействия для App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        private MainController? _controller;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var mainWindow = new MainWindow();
            _controller = new MainController(mainWindow);

            mainWindow.Show();
        }
    }

}
