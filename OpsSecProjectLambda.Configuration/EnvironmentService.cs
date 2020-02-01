using System;
using OpsSecProjectLambda.Abstractions;

namespace OpsSecProjectLambda.Configuration
{
    public class EnvironmentService : IEnvironmentService
    {
        public EnvironmentService()
        {
            EnvironmentName = Environment.GetEnvironmentVariable(Constants.EnvironmentVariables.AspnetCoreEnvironment)
                ?? Constants.Environments.Production;
            DBConnectionString = "Data Source="+ Environment.GetEnvironmentVariable(Constants.EnvironmentVariables.RDSHostname)+"," + Environment.GetEnvironmentVariable(Constants.EnvironmentVariables.RDSPort) + ";Initial Catalog=LogInputs;User ID=" + Environment.GetEnvironmentVariable(Constants.EnvironmentVariables.RDSUsername) + ";Password=" + Environment.GetEnvironmentVariable(Constants.EnvironmentVariables.RDSPassword) + ";";
            GlueExecutionRole = Environment.GetEnvironmentVariable(Constants.EnvironmentVariables.GLUEExecutionRole);
        }

        public string EnvironmentName { get; set; }
        public string DBConnectionString { get; set; }
        public string GlueExecutionRole { get; set; }
    }
}