using System.Windows;
using AshampooApp.Abstraction;
using AshampooApp.Modules;
using AshampooApp.ViewModels;
using Prism.DryIoc;
using Prism.Ioc;
using Prism.Modularity;

namespace AshampooApp
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
