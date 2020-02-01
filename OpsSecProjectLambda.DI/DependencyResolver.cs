using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpsSecProjectLambda.Abstractions;
using OpsSecProjectLambda.Configuration;
using OpsSecProjectLambda.EF;

namespace OpsSecProjectLambda.DI
{
    public class DependencyResolver
    {
        public IServiceProvider ServiceProvider { get; }
        public string CurrentDirectory { get; set; }
        public Action<IServiceCollection> RegisterServices { get; }

        public DependencyResolver(Action<IServiceCollection> registerServices = null)
        {
            // Set up Dependency Injection
            var serviceCollection = new ServiceCollection();
            RegisterServices = registerServices;
            ConfigureServices(serviceCollection);
            ServiceProvider = serviceCollection.BuildServiceProvider();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Register env and config services
            services.AddTransient<IEnvironmentService, EnvironmentService>();
            services.AddTransient<IConfigurationService, ConfigurationService>
                (provider => new ConfigurationService(provider.GetService<IEnvironmentService>())
                {
                    CurrentDirectory = CurrentDirectory
                });

            // Register DbContext class
            services.AddTransient(provider =>
            {
                var configService = provider.GetService<IConfigurationService>();
                var environmentService = provider.GetService<IEnvironmentService>();
                var connectionString = environmentService.DBConnectionString;
                var optionsBuilder = new DbContextOptionsBuilder<LogContext>();
                optionsBuilder.UseLazyLoadingProxies().UseSqlServer(connectionString);
                return new LogContext(optionsBuilder.Options);
            });
            // Register other services
            RegisterServices?.Invoke(services);
        }
    }
}