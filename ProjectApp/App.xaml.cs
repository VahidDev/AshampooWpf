using System.Windows;
using ProjectApp.Abstraction;
using ProjectApp.Modules;
using ProjectApp.ViewModels;
using Prism.DryIoc;
using Prism.Ioc;
using Prism.Modularity;

namespace ProjectApp
{
    public partial class App : PrismApplication
    {

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.Register<ISearchDirectoryViewModel, SearchDirectoryViewModel>();
            containerRegistry.Register<IMainWindowViewModel, MainWindowViewModel>();
        }

        protected override Window CreateShell()
        {
            var w = Container.Resolve<MainWindow>();
            return w;
        }

        protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
        {
            base.ConfigureModuleCatalog(moduleCatalog);
            moduleCatalog.AddModule<MainModule>();
        }
    }
}
