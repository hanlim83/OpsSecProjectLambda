using System;
using System.Collections.Generic;
using System.Text;

namespace OpsSecProjectLambda.Abstractions
{
    public enum QuestionableEventStatus
    {
        PendingReview, UserAccepted, UserRejected, AdminAccepted, AdminRejected, Locked
    }
    public class QuestionableEvent
    {
        public int ID { get; set; }
        public string FullEventData { get; set; }
        public string UserField { get; set; }
        public string IPAddressField { get; set; }
        public DateTime EventTimestamp { get; set; }
        public int LinkedAlertTriggerID { get; set; }
        public virtual Trigger LinkedAlertTrigger { get; set; }
        public QuestionableEventStatus status { get; set; }
        public DateTime UpdatedTimestamp { get; set; }
        public int ReviewUserID { get; set; }
        public int AdministratorID { get; set; }
    }
}
