namespace NetCoreLambda.Configuration
{
    public static class Constants
    {
        public static class EnvironmentVariables
        {
            public const string AspnetCoreEnvironment = "ASPNETCORE_ENVIRONMENT";
            public const string RDSHostname = "RDS_HOSTNAME";
            public const string RDSPort = "RDS_PORT";
            public const string RDSUsername = "RDS_USERNAME";
            public const string RDSPassword = "RDS_PASSWORD";
        }

        public static class Environments
        {
            public const string Production = "Production";
        }
    }
}