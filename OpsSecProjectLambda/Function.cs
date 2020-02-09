using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Amazon.Glue;
using Amazon.Glue.Model;
using Amazon.Lambda.Core;
using Amazon.Lambda.S3Events;
using Microsoft.Extensions.DependencyInjection;
using OpsSecProjectLambda.Abstractions;
using OpsSecProjectLambda.DI;
using OpsSecProjectLambda.EF;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace OpsSecProjectLambda
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

        public async Task FunctionHandler(S3Event s3event, ILambdaContext context)
        {
            if (s3event != null)
            {
                foreach (var record in s3event.Records)
                {
                    context.Logger.LogLine(record.S3.Bucket.Name);
                    string bucket = record.S3.Bucket.Name.Substring(14).Replace('-', ' ');
                    context.Logger.LogLine(bucket);
                    if (LogContextOperations.IfInputExist(bucket))
                    {
                        LogInput retrievedLI = LogContextOperations.GetLogInputByName(bucket);
                        GlueConsolidatedEntity retrievedGCE = LogContextOperations.GetGlueConsolidatedEntity(retrievedLI.ID);
                        GlueDatabaseTable retrievedGDT = LogContextOperations.GetGlueDatabaseTable(retrievedLI.ID);
                        context.Logger.LogLine(retrievedLI.ID + " | " + retrievedLI.Name);
                        if (retrievedLI.InitialIngest == false && retrievedGCE == null)
                        {
                            context.Logger.LogLine("Log Input has not be crawled before and has no crawler");
                            CreateCrawlerResponse createCrawlerResponse = await GlueClient.CreateCrawlerAsync(new CreateCrawlerRequest
                            {
                                Name = retrievedLI.Name,
                                DatabaseName = LogContextOperations.GetGlueDatabase().Name,
                                Role = "GlueServiceRole",
                                SchemaChangePolicy = new SchemaChangePolicy
                                {
                                    DeleteBehavior = DeleteBehavior.DEPRECATE_IN_DATABASE,
                                    UpdateBehavior = UpdateBehavior.UPDATE_IN_DATABASE
                                },
                                Tags = new Dictionary<string, string>
                                {
                                    {"Project","OSPJ" }
                                },
                                Targets = new CrawlerTargets
                                {
                                    S3Targets = new List<S3Target>
                                    {
                                        new S3Target
                                        {
                                            Path = "s3://"+LogContextOperations.GetInputS3BucketName(retrievedLI.ID)
                                        }
                                    }
                                }
                            });
                            if (createCrawlerResponse.HttpStatusCode.Equals(HttpStatusCode.OK))
                            {
                                context.Logger.LogLine("Crawler Created");
                                StartCrawlerResponse startCrawlerResponse = await GlueClient.StartCrawlerAsync(new StartCrawlerRequest
                                {
                                    Name = retrievedLI.Name
                                });
                                if (startCrawlerResponse.HttpStatusCode.Equals(HttpStatusCode.OK))
                                {
                                    context.Logger.LogLine("Crawler Just Created Started");
                                    LogContextOperations.AddGlueConsolidatedEntity(new GlueConsolidatedEntity
                                    {
                                        CrawlerName = retrievedLI.Name,
                                        LinkedLogInputID = retrievedLI.ID
                                    });
                                }
                            }
                        }
                        else if (retrievedLI.InitialIngest == false && retrievedGCE != null)
                        {
                            context.Logger.LogLine("Log Input has not be crawled before but has a crawler");
                            GetCrawlerResponse getCrawlerResponse = await GlueClient.GetCrawlerAsync(new GetCrawlerRequest
                            {
                                Name = retrievedGCE.CrawlerName
                            });
                            if (getCrawlerResponse.Crawler.State.Equals(CrawlerState.READY) && getCrawlerResponse.Crawler.LastCrawl == null)
                            {
                                StartCrawlerResponse startCrawlerResponse = await GlueClient.StartCrawlerAsync(new StartCrawlerRequest
                                {
                                    Name = retrievedGCE.CrawlerName
                                });
                                if (startCrawlerResponse.HttpStatusCode.Equals(HttpStatusCode.OK))
                                {
                                    context.Logger.LogLine("Crawler Started");
                                }
                            }
                            else if (getCrawlerResponse.Crawler.State.Equals(CrawlerState.READY) && getCrawlerResponse.Crawler.LastCrawl != null)
                            {
                                context.Logger.LogLine("Log Input has been crawled before, has a crawler but not a job");
                                LogContextOperations.AddGlueDatabaseTable(new GlueDatabaseTable
                                {
                                    LinkedDatabaseID = 1,
                                    LinkedGlueConsolidatedInputEntityID = retrievedGCE.ID,
                                    Name = getCrawlerResponse.Crawler.Targets.S3Targets[0].Path.Substring(5).Replace("-", "_")
                                });
                                GetTableResponse getTableResponse = await GlueClient.GetTableAsync(new GetTableRequest
                                {
                                    DatabaseName = "master-database",
                                    Name = getCrawlerResponse.Crawler.Targets.S3Targets[0].Path.Substring(5).Replace("-", "_")
                                });
                                if (getTableResponse.HttpStatusCode.Equals(HttpStatusCode.OK))
                                {
                                    UpdateTableResponse updateTableResponse = await GlueClient.UpdateTableAsync(new UpdateTableRequest
                                    {
                                        DatabaseName = getTableResponse.Table.DatabaseName,
                                        TableInput = new TableInput
                                        {
                                            Name = getTableResponse.Table.Name,
                                            Parameters = getTableResponse.Table.Parameters,
                                            LastAccessTime = getTableResponse.Table.LastAccessTime,
                                            LastAnalyzedTime = getTableResponse.Table.LastAnalyzedTime,
                                            Owner = getTableResponse.Table.Owner,
                                            StorageDescriptor = getTableResponse.Table.StorageDescriptor,
                                            Retention = getTableResponse.Table.Retention,
                                            TableType = getTableResponse.Table.TableType
                                        }
                                    });
                                    if (updateTableResponse.HttpStatusCode.Equals(HttpStatusCode.OK))
                                    {
                                        retrievedGDT = LogContextOperations.GetGlueDatabaseTable(retrievedLI.ID);
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
                                                retrievedGCE.JobName = createJobResponse.Name;
                                                LogContextOperations.UpdateGlueConsolidatedEntity(retrievedGCE);
                                                LogContextOperations.UpdateInputIngestionStatus(bucket);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        else if (retrievedLI.InitialIngest == true)
                        {
                            context.Logger.LogLine("Log Input has been crawled before");
                            if (retrievedGCE.JobName == null && retrievedGDT == null)
                            {
                                context.Logger.LogLine("Log Input has not be transferred over to RDS before due to no job");
                                GetCrawlerResponse getCrawlerResponse = await GlueClient.GetCrawlerAsync(new GetCrawlerRequest
                                {
                                    Name = retrievedGCE.CrawlerName
                                });
                                if (getCrawlerResponse.HttpStatusCode.Equals(HttpStatusCode.OK))
                                {
                                    LogContextOperations.AddGlueDatabaseTable(new GlueDatabaseTable
                                    {
                                        LinkedDatabaseID = 1,
                                        LinkedGlueConsolidatedInputEntityID = retrievedGCE.ID,
                                        Name = getCrawlerResponse.Crawler.Targets.S3Targets[0].Path.Substring(5).Replace("-", "_")
                                    });
                                    GetTableResponse getTableResponse = await GlueClient.GetTableAsync(new GetTableRequest
                                    {
                                        DatabaseName = "master-database",
                                        Name = getCrawlerResponse.Crawler.Targets.S3Targets[0].Path.Substring(5).Replace("-", "_")
                                    });
                                    if (getTableResponse.HttpStatusCode.Equals(HttpStatusCode.OK))
                                    {
                                        UpdateTableResponse updateTableResponse = await GlueClient.UpdateTableAsync(new UpdateTableRequest
                                        {
                                            DatabaseName = getTableResponse.Table.DatabaseName,
                                            TableInput = new TableInput
                                            {
                                                Name = getTableResponse.Table.Name,
                                                Parameters = getTableResponse.Table.Parameters,
                                                LastAccessTime = getTableResponse.Table.LastAccessTime,
                                                LastAnalyzedTime = getTableResponse.Table.LastAnalyzedTime,
                                                Owner = getTableResponse.Table.Owner,
                                                StorageDescriptor = getTableResponse.Table.StorageDescriptor,
                                                Retention = getTableResponse.Table.Retention,
                                                TableType = getTableResponse.Table.TableType
                                            }
                                        });
                                        if (updateTableResponse.HttpStatusCode.Equals(HttpStatusCode.OK))
                                        {
                                            retrievedGDT = LogContextOperations.GetGlueDatabaseTable(retrievedLI.ID);
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
                                                    retrievedGCE.JobName = createJobResponse.Name;
                                                    LogContextOperations.UpdateGlueConsolidatedEntity(retrievedGCE);
                                                    LogContextOperations.UpdateInputIngestionStatus(bucket);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                GetCrawlerResponse getCrawlerResponse = await GlueClient.GetCrawlerAsync(new GetCrawlerRequest
                                {
                                    Name = retrievedGCE.CrawlerName
                                });
                                if (getCrawlerResponse.HttpStatusCode.Equals(HttpStatusCode.OK) && getCrawlerResponse.Crawler.State.Equals(CrawlerState.READY))
                                {
                                    if ((getCrawlerResponse.Crawler.LastCrawl.StartTime.Hour < DateTime.Now.Hour && getCrawlerResponse.Crawler.LastCrawl.StartTime.Day == DateTime.Now.Day) || getCrawlerResponse.Crawler.LastCrawl.StartTime.Day != DateTime.Now.Day)
                                    {
                                        context.Logger.LogLine("Log Input has been transferred over to RDS before but time condition not met");
                                        StartCrawlerResponse startCrawlerResponse = await GlueClient.StartCrawlerAsync(new StartCrawlerRequest
                                        {
                                            Name = retrievedGCE.CrawlerName
                                        });
                                        if (startCrawlerResponse.HttpStatusCode.Equals(HttpStatusCode.OK))
                                        {
                                            context.Logger.LogLine("Crawler Started");
                                        }
                                    }
                                    else
                                    {
                                        context.Logger.LogLine("Log Input has been transferred over to RDS before and time condition met");
                                        GetTableResponse getTableResponse = await GlueClient.GetTableAsync(new GetTableRequest
                                        {
                                            DatabaseName = "master-database",
                                            Name = getCrawlerResponse.Crawler.Targets.S3Targets[0].Path.Substring(5).Replace("-", "_")
                                        });
                                        if (getTableResponse.HttpStatusCode.Equals(HttpStatusCode.OK))
                                        {
                                            UpdateTableResponse updateTableResponse = await GlueClient.UpdateTableAsync(new UpdateTableRequest
                                            {
                                                DatabaseName = getTableResponse.Table.DatabaseName,
                                                TableInput = new TableInput
                                                {
                                                    Name = getTableResponse.Table.Name,
                                                    Parameters = getTableResponse.Table.Parameters,
                                                    LastAccessTime = getTableResponse.Table.LastAccessTime,
                                                    LastAnalyzedTime = getTableResponse.Table.LastAnalyzedTime,
                                                    Owner = getTableResponse.Table.Owner,
                                                    StorageDescriptor = getTableResponse.Table.StorageDescriptor,
                                                    Retention = getTableResponse.Table.Retention,
                                                    TableType = getTableResponse.Table.TableType
                                                }
                                            });
                                            if (updateTableResponse.HttpStatusCode.Equals(HttpStatusCode.OK))
                                            {
                                                context.Logger.LogLine("Table updated before running job");
                                                context.Logger.LogLine(retrievedGCE.JobName);
                                                GetJobRunsResponse getJobRunsResponse = await GlueClient.GetJobRunsAsync(new GetJobRunsRequest
                                                {
                                                    JobName = retrievedGCE.JobName
                                                });
                                                bool jobRunning = false;
                                                context.Logger.LogLine(getJobRunsResponse.JobRuns.Count().ToString());
                                                foreach (JobRun j in getJobRunsResponse.JobRuns)
                                                {
                                                    context.Logger.LogLine(j.Id + " | " + j.JobRunState);
                                                    if (j.JobRunState.Equals(JobRunState.STARTING) || j.JobRunState.Equals(JobRunState.RUNNING) || j.JobRunState.Equals(JobRunState.STOPPING))
                                                    {
                                                        jobRunning = true;
                                                        break;
                                                    }
                                                }
                                                context.Logger.LogLine(jobRunning.ToString());
                                                if (!jobRunning)
                                                {
                                                    StartJobRunResponse startJobRunResponse = await GlueClient.StartJobRunAsync(new StartJobRunRequest
                                                    {
                                                        JobName = retrievedGCE.JobName,
                                                        MaxCapacity = 10.0
                                                    });
                                                    if (startJobRunResponse.HttpStatusCode.Equals(HttpStatusCode.OK))
                                                        context.Logger.LogLine("Job Started");
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
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
