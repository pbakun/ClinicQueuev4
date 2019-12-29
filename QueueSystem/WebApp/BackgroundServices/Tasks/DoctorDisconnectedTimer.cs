using Entities.Models;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace WebApp.Hubs
{
    public class DoctorDisconnectedTimer : IDisposable
    {
        public System.Timers.Timer Timer;
        public event EventHandler TimerFinished;
        private HubUser _groupMember;
        
        public DoctorDisconnectedTimer(HubUser groupMember, int delay)
        {
            _groupMember = groupMember;
            Timer = new System.Timers.Timer(delay);
            Timer.Start();
            Timer.Elapsed += _timer_Elapsed;
        }

        private void _timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Timer.Stop();
            OnTimerFinish();
        }

        private void OnTimerFinish()
        {
            if (TimerFinished != null)
            {
                TimerFinished.Invoke(_groupMember, new EventArgs());
            }
        }

        public void Dispose()
        {
            Timer.Elapsed -= _timer_Elapsed;
            Timer?.Dispose();
        }

    }
}
