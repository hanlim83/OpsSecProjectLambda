using System.IO;
using Microsoft.EntityFrameworkCore.Design;
using NetCoreLambda.DI;

namespace NetCoreLambda.EF.Design
{
    public class LogDbContextFactory : IDesignTimeDbContextFactory<LogContext>
    {
        public LogContext CreateDbContext(string[] args)
        {
            // Get DbContext from DI system
            var resolver = new DependencyResolver
            {
                CurrentDirectory = Path.Combine(Directory.GetCurrentDirectory(), "../NetCoreLambda")
            };
            return resolver.ServiceProvider.GetService(typeof(LogContext)) as LogContext;
        }
    }
}