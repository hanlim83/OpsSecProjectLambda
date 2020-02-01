using System.IO;
using Microsoft.EntityFrameworkCore.Design;
using OpsSecProjectLambda.DI;

namespace OpsSecProjectLambda.EF.Design
{
    public class LogDbContextFactory : IDesignTimeDbContextFactory<LogContext>
    {
        public LogContext CreateDbContext(string[] args)
        {
            var resolver = new DependencyResolver
            {
                CurrentDirectory = Path.Combine(Directory.GetCurrentDirectory(), "../OpsSecProjectLambda")
            };
            return resolver.ServiceProvider.GetService(typeof(LogContext)) as LogContext;
        }
    }
}