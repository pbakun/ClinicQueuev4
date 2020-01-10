using AutoMapper;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Repository.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WebApp.BackgroundServices.Tasks;
using WebApp.Hubs;
using WebApp.Mappings;
using Xunit;
using XUnitTests.TestingData;
using AutoMapper.QueryableExtensions;
using System.Threading;

namespace XUnitTests
{
    public class TaskServicesTest
    {
        private readonly IMapper _mapper;
        public TaskServicesTest()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new MappingProfile());
            });
            _mapper = config.CreateMapper();
        }

        [Fact]
        public void ResetQueueTest()
        {
            //System.Diagnostics.Debugger.Launch();
            //test data
            List<Entities.Models.Queue> queues = new FakeQueue().WithQueueNo(10).WithRoomNo("10").BuildAsList();
            queues.Add(new FakeQueue().WithQueueNo(17).WithRoomNo("15").Build());
            queues.Add(new FakeQueue().WithQueueNo(251).WithRoomNo("3").Build());
            var querableQueues = queues.AsQueryable();
            
            //mock
            var mockScopeFactory = new Mock<IServiceScopeFactory>();
            var mockHubContext = new Mock<IHubContext<QueueHub>>();
            var mockRepoWrapper = new Mock<IRepositoryWrapper>();
            var mockScope = new Mock<IServiceScope>();
            var mockServiceProvider = new Mock<IServiceProvider>();
            mockScopeFactory.Setup(s => s.CreateScope()).Returns(mockScope.Object);
            mockServiceProvider.Setup(s => s.GetService(typeof(IRepositoryWrapper))).Returns(mockRepoWrapper.Object);
            mockScope.Setup(s => s.ServiceProvider).Returns(mockServiceProvider.Object);
            mockRepoWrapper.Setup(q => q.Queue.FindAll()).Returns(querableQueues);
            mockRepoWrapper.Setup(q => q.Queue.Update(It.IsAny<Entities.Models.Queue>()));
            mockRepoWrapper.Setup(r => r.Save());
            mockHubContext.Setup(h => h.Clients.Group(It.IsAny<string>())
            .SendCoreAsync(It.IsAny<string>(), It.Is<object[]>(o => o != null && o.Length == 1), default));
            
            var resetQueue = new ResetQueue(mockScopeFactory.Object, mockHubContext.Object, _mapper);
            resetQueue.ProcessInScope(null);

            var outputQueues = querableQueues.Select(q => q.QueueNo).ToList();

            Assert.True(outputQueues.Count == queues.Count);

            List<int> assertionList = new List<int>();
            for (int i=0; i < outputQueues.Count; i++)
            {
                assertionList.Add(1);
            }

            Assert.Equal(outputQueues, assertionList);

        }
    }
}
