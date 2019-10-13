using System;
using System.Diagnostics;
using SystemMonitor.Helpers;
using SystemMonitor.Hub;

namespace SystemMonitor
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Połącz");
            var queueHub = new QueueHub();
            queueHub.Connect();

            Console.Write("Register");
            queueHub.RegisterPatient(12);
            queueHub.ReceiveQueueNo += NewQueueNo;
            queueHub.ReceiveLiveBit += NewLiveBit;

            Console.ReadKey();
            Console.WriteLine("Wciśnij q by wysłać livebit");
            var click = Console.ReadLine();
            
            while(click == "q")
            {
                queueHub.SendLiveBit();
                click = Console.ReadLine();
            }

            //var processes = ShellHelper.GetProcesses("chromium");

            //var cmd = "curl http://queue.hostingasp.pl/patient/12";

            //var output = cmd.Bash();



            Console.ReadKey();
        }

        static void NewQueueNo(object sender, EventArgs e)
        {
            string[] data = (string[])sender;
            Console.WriteLine(data[1]);
        }

        static void NewLiveBit(object sender, EventArgs e)
        {
            Console.WriteLine("LiveBit received");
        }

    }
}
