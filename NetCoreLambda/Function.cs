using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.S3Events;
using Microsoft.Extensions.DependencyInjection;
using NetCoreLambda.Abstractions;
using NetCoreLambda.DI;
using NetCoreLambda.EF;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace NetCoreLambda
{
    public class Function
    {
        // Repository
        public ILogInputsRepository LogInputsRepository { get; }

        public Function()
        {
            // Get dependency resolver
            var resolver = new DependencyResolver(ConfigureServices);

            LogInputsRepository = resolver.ServiceProvider.GetService<ILogInputsRepository>();
        }

        public Function(ILogInputsRepository logInputsRepository)
        {
            LogInputsRepository = logInputsRepository;
        }

        public async Task<List<LogInput>> FunctionHandler(S3Event s3event, ILambdaContext context)
        {
            List<LogInput> updatedInputs = new List<LogInput>();
            foreach (var record in s3event.Records)
            {
                string bucket = record.S3.Bucket.Name.Substring(13).Replace('-', ' ');
                if (LogInputsRepository.IfInputExist(bucket) && !LogInputsRepository.InputIngestionStatus(bucket))
                {
                    if (LogInputsRepository.UpdateInputIngestionStatus(bucket))
                    {
                        LogInput retrieved = await LogInputsRepository.GetLogInput(bucket);
                        if (retrieved != null)
                            updatedInputs.Add(retrieved);
                    }
                }
            }
            return updatedInputs;
        }

        // Register services with DI system
        private void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<ILogInputsRepository, LogInputsRepository>();
        }
    }
}
