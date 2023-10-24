using Ashampoo.Domain.Constants;
using AshampooApp.Views;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Regions;

namespace AshampooApp.Modules
{
    public class MainModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            var regionManager = containerProvider.Resolve<RegionManager>();
            regionManager.RegisterViewWithRegion(RegionNames.MainRegion, typeof(SearchDirectoryView));
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
        }
    }
}
