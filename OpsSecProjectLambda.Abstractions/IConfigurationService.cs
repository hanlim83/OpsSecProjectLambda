using Microsoft.Extensions.Configuration;

namespace OpsSecProjectLambda.Abstractions
{
    public interface IConfigurationService
    {
        IConfiguration GetConfiguration();
    }
}