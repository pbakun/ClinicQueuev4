using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApp.Models
{
    public class EmailSettings
    {
        public string MailServer { get; set; }
        public int Port { get; set; }
        public string SenderName { get; set; }
        public string SenderMail { get; set; }
        public string Password { get; set; }
        public int Timeout { get; set; }
    }
}
