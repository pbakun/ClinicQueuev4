using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace WebApp.Utility
{
    public static class StaticDetails
    {
        public const string AdminUser = "Admin";
        public const string DoctorUser = "Doctor";
        public const string NurseUser = "Nurse";
        public const string PatientUser = "Patient";

        public const string DoctorNamePrefix = "Lek. med. ";

        public const string QueueOccupiedMessage = "Kolejka w wybranym pokoju jest obecnie pod kontrolą innego użytkownika";

        public const string MessageWhenNoDoctorActiveInQueue = "NZMR Modzelewska-Bakun";

    }
}
