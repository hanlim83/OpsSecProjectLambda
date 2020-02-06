using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Amazon.Glue;
using Amazon.Glue.Model;
using Amazon.Lambda.Core;
using Microsoft.Extensions.DependencyInjection;
using OpsSecProjectLambda.Abstractions;
using OpsSecProjectLambda.DI;
using OpsSecProjectLambda.EF;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace OpsSecProjectLambda.CloudWatchEvents
{
    public class Function
    {
        private ILogContextOperations LogContextOperations { get; }
        private IAmazonGlue GlueClient { get; set; }

        public Function()
        {
            var resolver = new DependencyResolver(ConfigureServices);
            LogContextOperations = resolver.ServiceProvider.GetService<ILogContextOperations>();
            GlueClient = new AmazonGlueClient();
        }

        public async Task FunctionHandler(string CWevent, ILambdaContext context)
        {
            List<GlueConsolidatedEntity> retrievedGlueConsolidatedEntities = LogContextOperations.GetGlueConsolidatedEntities();
            foreach (GlueConsolidatedEntity entity in retrievedGlueConsolidatedEntities)
            {
                context.Logger.LogLine(entity.CrawlerName);
                GetCrawlerResponse getCrawlerResponse = await GlueClient.GetCrawlerAsync(new GetCrawlerRequest
                {
                    Name = entity.CrawlerName
                });
                context.Logger.LogLine(getCrawlerResponse.Crawler.Name);
                if (getCrawlerResponse.HttpStatusCode.Equals(HttpStatusCode.OK) && getCrawlerResponse.Crawler.State.Equals(CrawlerState.READY))
                {
                    if (entity.LinkedTable == null)
                    {
                        context.Logger.LogLine("Crawler just completed, creating job now");
                        LogContextOperations.AddGlueDatabaseTable(new GlueDatabaseTable
                        {
                            LinkedDatabaseID = 1,
                            LinkedGlueConsolidatedInputEntityID = entity.ID,
                            Name = getCrawlerResponse.Crawler.Targets.S3Targets[0].Path.Substring(5)
                        });
                        GlueDatabaseTable retrievedGDT = LogContextOperations.GetGlueDatabaseTable(entity.LinkedLogInputID);
                        LogInput retrievedLI = LogContextOperations.GetLogInputByID(entity.LinkedLogInputID);
                        CreateJobRequest createJobRequest = new CreateJobRequest
                        {
                            Name = retrievedLI.Name,
                            DefaultArguments = new Dictionary<string, string>
                                {
                                    { "--enable-spark-ui", "true"},
                                    { "--spark-event-logs-path", "s3://aws-glue-spark-188363912800-ap-southeast-1"},
                                    { "--job-bookmark-option", "job-bookmark-enable"},
                                    { "--job-language", "python"},
                                    { "--TempDir", "s3://aws-glue-temporary-188363912800-ap-southeast-1/root"},
                                    { "--TABLE_NAME", retrievedGDT.Name}
                                },
                            MaxCapacity = 10.0,
                            Role = "GlueServiceRole",
                            Connections = new ConnectionsList
                            {
                                Connections = new List<string>
                                    {
                                        "SmartInsights"
                                    }
                            },
                            Tags = new Dictionary<string, string>
                                {
                                    {"Project","OSPJ" }
                                },
                            MaxRetries = 0,
                            GlueVersion = "1.0",
                            ExecutionProperty = new ExecutionProperty
                            {
                                MaxConcurrentRuns = 1
                            },
                            Timeout = 2880
                        };
                        if (retrievedLI.LogInputCategory.Equals(LogInputCategory.ApacheWebServer))
                        {
                            createJobRequest.Command = new JobCommand
                            {
                                PythonVersion = "3",
                                Name = "glueetl",
                                ScriptLocation = "s3://aws-glue-scripts-188363912800-ap-southeast-1/root/Apache CLF"
                            };
                        }
                        else if (retrievedLI.LogInputCategory.Equals(LogInputCategory.SquidProxy))
                        {
                            createJobRequest.Command = new JobCommand
                            {
                                PythonVersion = "3",
                                Name = "glueetl",
                                ScriptLocation = "s3://aws-glue-scripts-188363912800-ap-southeast-1/root/Cisco Squid Proxy"
                            };
                        }
                        else if (retrievedLI.LogInputCategory.Equals(LogInputCategory.SSH))
                        {
                            createJobRequest.Command = new JobCommand
                            {
                                PythonVersion = "3",
                                Name = "glueetl",
                                ScriptLocation = "s3://aws-glue-scripts-188363912800-ap-southeast-1/root/Splunk SSH"
                            };
                        }
                        else if (retrievedLI.LogInputCategory.Equals(LogInputCategory.WindowsEventLogs))
                        {
                            createJobRequest.Command = new JobCommand
                            {
                                PythonVersion = "3",
                                Name = "glueetl",
                                ScriptLocation = "s3://aws-glue-scripts-188363912800-ap-southeast-1/root/Windows Events"
                            };
                        }
                        CreateJobResponse createJobResponse = await GlueClient.CreateJobAsync(createJobRequest);
                        if (createJobResponse.HttpStatusCode.Equals(HttpStatusCode.OK))
                        {
                            context.Logger.LogLine("Job Created");
                            StartJobRunResponse startJobRunResponse = await GlueClient.StartJobRunAsync(new StartJobRunRequest
                            {
                                JobName = createJobResponse.Name,
                                MaxCapacity = 10.0
                            });
                            if (startJobRunResponse.HttpStatusCode.Equals(HttpStatusCode.OK))
                            {
                                context.Logger.LogLine("Job Just Created Started");
                                entity.JobName = createJobResponse.Name;
                                LogContextOperations.UpdateGlueConsolidatedEntity(entity);
                            }
                        }
                        break;
                    }
                }
            }
        }

        private void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<ILogContextOperations, LogContextOperations>();
        }
    }
}
