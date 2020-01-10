using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace WebApp.Models
{
    public class ApplicationSettings
    {
        private List<string> availableRooms;
        private string queueOccupiedMessage;
        private int patientViewNotificationAfterDoctorDisconnectedDelay;

        public List<string> AvailableRooms
        {
            get { return availableRooms; }
            set
            {
                availableRooms = value;
            }
        }
        public string QueueOccupiedMessage
        {
            get { return queueOccupiedMessage; }
            set
            {
                queueOccupiedMessage = value;
            }
        }
        public int PatientViewNotificationAfterDoctorDisconnectedDelay
        {
            get { return patientViewNotificationAfterDoctorDisconnectedDelay; }
            set
            {
                patientViewNotificationAfterDoctorDisconnectedDelay = value;
            }
        }

        public string MessageWhenNoDoctorActiveInQueue { get; set; }

    }
}
