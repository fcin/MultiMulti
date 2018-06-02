using Caliburn.Micro;
using MultiMulti.Core.ViewModels;
using System;
using System.Collections.Generic;
using MultiMulti.Core.Services;
using MultiMulti.Core.Utils;
using NLog;

namespace MultiMulti.Core
{
    public class Bootstrapper : BootstrapperBase
    {
        private readonly SimpleContainer _container = new SimpleContainer();

        public Bootstrapper()
        {
            Initialize();
        }

        protected override void OnStartup(object sender, System.Windows.StartupEventArgs e)
        {
            DisplayRootViewFor<ShellViewModel>();
        }

        protected override void Configure()
        {
            _container.Singleton<IWindowManager, WindowManager>();
            _container.Singleton<IEventAggregator, EventAggregator>();
            _container.PerRequest<ShellViewModel, ShellViewModel>();
            _container.PerRequest<PermutationProvider, PermutationProvider>();
            _container.PerRequest<DataService, DataService>();
            _container.PerRequest<ExcelExporter, ExcelExporter>();
            _container.PerRequest<DrawScraper, DrawScraper>();
            base.Configure();
        }

        protected override object GetInstance(Type service, string key)
        {
            var instance = _container.GetInstance(service, key);
            if (instance != null)
                return instance;

            throw new InvalidOperationException("Could not locate any instances.");
        }

        protected override IEnumerable<object> GetAllInstances(Type service)
        {
            return _container.GetAllInstances(service);
        }

        protected override void BuildUp(object instance)
        {
            _container.BuildUp(instance);
        }

    }
}
