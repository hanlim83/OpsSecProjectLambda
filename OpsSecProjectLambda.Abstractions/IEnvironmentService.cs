﻿namespace NetCoreLambda.Abstractions
{
    public interface IEnvironmentService
    {
        string EnvironmentName { get; set; }

        string DBConnectionString { get; set; }
    }
}