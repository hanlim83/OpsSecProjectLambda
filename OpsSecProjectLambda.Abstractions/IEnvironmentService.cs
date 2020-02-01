namespace OpsSecProjectLambda.Abstractions
{
    public interface IEnvironmentService
    {
        string EnvironmentName { get; set; }

        string DBConnectionString { get; set; }

        string GlueExecutionRole { get; set; }
    }
}