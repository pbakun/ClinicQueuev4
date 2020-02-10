using System;
using System.Collections.Generic;
using System.Text;

namespace XUnitTests.Test.IntegrationTest.Models
{
    public class QueueHubResponse
    {
        public string ReceiveQueueNo { get; set; }
        public string ReceiveAdditionalMessage { get; set; }
        public string ReceiveDoctorFullName { get; set; }
        public string ReceiveUserId { get; set; }
        public string ReceiveQueueOccupied { get; set; }
    }
}
