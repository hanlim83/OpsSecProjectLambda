﻿
namespace OpsSecProjectLambda.Abstractions
{
    public enum SagemakerStatus
    {
        Untrained, Training, Trained, Tuning, Deploying, Reversing, Transforming, Ready, Error
    }
    public enum SagemakerErrorStage
    {
        Training, Tuning, Transforming, Deployment, None
    }
    public enum SagemakerAlgorithm
    {
        IP_Insights, Random_Cut_Forest
    }
    public class SagemakerConsolidatedEntity
    {
        public int ID { get; set; }
        public SagemakerAlgorithm SagemakerAlgorithm { get; set; }
        public SagemakerStatus SagemakerStatus { get; set; }
        public SagemakerErrorStage SagemakerErrorStage { get; set; }
        public string ModelName { get; set; }
        public string TrainingJobName { get; set; }
        public string TrainingJobARN { get; set; }
        public string EndpointConfigurationName { get; set; }
        public string EndpointConfigurationARN { get; set; }
        public string EndpointName { get; set; }
        public string EndpointJobARN { get; set; }
        public string HyperParameterTurningJobName { get; set; }
        public string HyperParameterTurningJobARN { get; set; }
        public string BatchTransformJobName { get; set; }
        public string BatchTransformJobARN { get; set; }
        public int LinkedLogInputID { get; set; }
        public virtual LogInput LinkedLogInput { get; set; }
    }
}
