using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WebApp.ServiceLogic;
using WebApp.Utility;

namespace WebApp.BackgroundServices.Tasks
{
    public class StartupSetUp : ScopedProcessor
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public StartupSetUp(IServiceScopeFactory serviceScopeFactory) : base(serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
        }

        //Process responsible for setting all queues to inactive status, executed at application startup
        public override async Task ProcessInScope(IServiceProvider serviceProvider)
        {
            SetAllQueuesInactive();
            Log.Information(String.Concat(StaticDetails.logPrefixStartup, "All queues set to state inactive"));
            await this.StopAsync(System.Threading.CancellationToken.None);

            await Task.CompletedTask;
        }

        private void SetAllQueuesInactive()
        {
            using(var scope = _serviceScopeFactory.CreateScope())
            {
                var service = scope.ServiceProvider.GetRequiredService<IQueueService>();

                var queues = service.FindAll();
                foreach (var queue in queues)
                {
                    service.SetQueueInactive(queue.UserId);
                }
            }
        }
    }
}
