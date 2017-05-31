using Prism.Unity;
using Microsoft.Practices.Unity;
using System.Windows;
using HurtradeDesktopClient.Views;

[assembly: log4net.Config.XmlConfigurator(Watch = true)]

namespace HurtradeDesktopClient
{
    class Bootstrapper : UnityBootstrapper
    {
        protected override DependencyObject CreateShell()
        {
            return Container.Resolve<MainWindow>();
        }

        protected override void InitializeShell()
        {
            base.InitializeShell();

            Application.Current.MainWindow = (Window)this.Shell;
            Application.Current.MainWindow.Show();
        }
    }
}
