using System;
using NetCoreLambda.Abstractions;

namespace NetCoreLambda.Configuration
{
    public class EnvironmentService : IEnvironmentService
    {
        public EnvironmentService()
        {
            EnvironmentName = Environment.GetEnvironmentVariable(Constants.EnvironmentVariables.AspnetCoreEnvironment)
                ?? Constants.Environments.Production;
            DBConnectionString = "Data Source="+ Environment.GetEnvironmentVariable(Constants.EnvironmentVariables.RDSHostname)+"," + Environment.GetEnvironmentVariable(Constants.EnvironmentVariables.RDSPort) + ";Initial Catalog=LogInputs;User ID=" + Environment.GetEnvironmentVariable(Constants.EnvironmentVariables.RDSUsername) + ";Password=" + Environment.GetEnvironmentVariable(Constants.EnvironmentVariables.RDSPassword) + ";";
        }

        public string EnvironmentName { get; set; }
        public string DBConnectionString { get; set; }
    }
}