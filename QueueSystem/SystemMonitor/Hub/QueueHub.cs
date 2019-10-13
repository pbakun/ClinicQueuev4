using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SystemMonitor.Hub
{
    public class QueueHub
    {

        private readonly HubConnection _hub;

        public QueueHub()
        {
            _hub = new HubConnectionBuilder()
                .WithUrl("https://localhost:5001/queuehub")
                .WithAutomaticReconnect()
                .Build();

            _hub.Closed += _hub_Closed;
            
        }

        private async Task _hub_Closed(Exception arg)
        {
            await Task.Delay(new Random().Next(0, 5) * 1000);
            //await _hub.StartAsync();
        }



        public async void Connect()
        {
            _hub.On<string, string>("ReceiveQueueNo", (userId, queueMessage) =>
            {
                OnReceiveQueueNo(new string[] { userId, queueMessage });
            });

            _hub.On("ReceiveLiveBit", () =>
            {
                OnReceiveLiveBit();
            });

            _hub.Reconnecting += _hub_Reconnecting;

            try
            {
                await _hub.StartAsync();
                Console.WriteLine("Connection Started");
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private Task _hub_Reconnecting(Exception arg)
        {
            Console.WriteLine("Hub Reconnecting");
            return Task.CompletedTask;
        }

        public async void RegisterPatient(int roomNo)
        {
            try
            {
                await _hub.InvokeAsync("RegisterPatientView", roomNo);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public async void SendLiveBit()
        {
            try
            {
                await _hub.InvokeAsync("LiveBit");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public EventHandler ReceiveQueueNo;
        public EventHandler ReceiveLiveBit;
        protected virtual void OnReceiveQueueNo(object data)
        {
            if (ReceiveQueueNo != null)
                ReceiveQueueNo(data, new EventArgs());
        }

        protected virtual void OnReceiveLiveBit()
        {
            ReceiveLiveBit?.Invoke(this, new EventArgs());
        }


    }
}
