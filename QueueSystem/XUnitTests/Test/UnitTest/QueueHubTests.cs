﻿using AutoMapper;
using Entities;
using Microsoft.AspNetCore.SignalR;
using Moq;
using Repository.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebApp.Hubs;
using WebApp.Mappings;
using WebApp.Models;
using WebApp.ServiceLogic;
using Xunit;
using XUnitTests.TestingData;

namespace XUnitTests
{
    public class QueueHubTests
    {
        private readonly IMapper _mapper;
        private readonly Mock<IHubCallerClients> _mockClients;
        private readonly Mock<Microsoft.AspNetCore.SignalR.IClientProxy> _mockClientProxy;
        private readonly Mock<IQueueService> _mockQueueService;
        private readonly Mock<IRepositoryWrapper> _mockRepo;
        private readonly Mock<IGroupManager> _mockGroupManager;
        private readonly Mock<Microsoft.AspNetCore.SignalR.HubCallerContext> _mockHubCallerContext;

        public QueueHubTests()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new MappingProfile());
            });
            _mapper = config.CreateMapper();

            _mockClientProxy = new Mock<Microsoft.AspNetCore.SignalR.IClientProxy>();

            _mockClients = new Mock<IHubCallerClients>();
            _mockQueueService = new Mock<IQueueService>();
            _mockRepo = new Mock<IRepositoryWrapper>();
            _mockGroupManager = new Mock<IGroupManager>();
            _mockHubCallerContext = new Mock<Microsoft.AspNetCore.SignalR.HubCallerContext>();

        }

        public Task<Queue> CallRegisterDoctor(string id, string roomNo,
            Mock<IHubCallerClients> mockClients,
            Mock<Microsoft.AspNetCore.SignalR.IClientProxy> mockClientProxy,
            Mock<IGroupManager> mockGroupManager,
            Mock<IManageHubUser> mockManageHubUser = null)
        {
            if (mockClients == null)
                mockClients = _mockClients;
            if (mockClientProxy == null)
                mockClientProxy = _mockClientProxy;
            if (mockGroupManager == null)
                mockGroupManager = _mockGroupManager;
            if(mockManageHubUser == null)
                mockManageHubUser = new Mock<IManageHubUser>();

            var prepareQueue = new FakeQueue().WithQueueNo(12).WithRoomNo(roomNo).Build();
            var queue = _mapper.Map<Queue>(prepareQueue);

            //var prepareUser = new UserData().WithRoomNo(roomNo).BuildAsList();

            //mockManageHubUser.Setup(h => h.GetGroupMaster(It.IsAny<string>())).Returns(new FakeHubUser(null, roomNo).BuildAsList());
            //_mockRepo.Setup(r => r.User.FindByCondition(It.IsAny<Expression<Func<Entities.Models.User, bool>>>()))
            //    .Returns(prepareUser);
            //_mockQueueService.Setup(q => q.FindByUserId(It.IsAny<string>())).Returns(queue);
            //_mockQueueService.Setup(q => q.NewQueueNo(It.IsAny<string>(), It.IsAny<int>())).Returns(Task.FromResult(queue));

            //_mockHubCallerContext.Setup(c => c.ConnectionId).Returns(It.IsAny<string>());

            //mockClients.Setup(c => c.Group(queue.RoomNo)).Returns(() => mockClientProxy.Object);

            //var hub = new QueueHub(_mockRepo.Object, _mockQueueService.Object, mockManageHubUser.Object)
            //{
            //    Clients = mockClients.Object,
            //    Context = _mockHubCallerContext.Object,
            //    Groups = mockGroupManager.Object
            //};

            //await hub.RegisterDoctor(queue.RoomNo);

            //return queue;
            return Task.FromResult(queue);
        }


        [Theory]
        [InlineData("1", "12")]
        [InlineData("2", "12")]
        public async Task RegisterDoctor_TestOneOwner(string id, string roomNo)
        {
            var mockClientProxy = new Mock<Microsoft.AspNetCore.SignalR.IClientProxy>();
            var mockClients = new Mock<IHubCallerClients>();
            var mockGroupManager = new Mock<IGroupManager>();

            //System.Diagnostics.Debugger.Launch();
            var queue = await CallRegisterDoctor(id, roomNo, mockClients, mockClientProxy, mockGroupManager);

            mockClients.Verify(c => c.Group("12"), Times.AtLeastOnce);

            mockClientProxy.Verify(p => p.SendCoreAsync("ReceiveDoctorFullName",
                It.Is<object[]>(o => o != null && o.Length == 2),
                default(CancellationToken)), Times.Once);

            mockClientProxy.Verify(p => p.SendCoreAsync("ReceiveQueueNo",
                It.Is<object[]>(o => o != null && o.Length == 2 && ((string)o[1]) == queue.QueueNoMessage),
                default(CancellationToken)), Times.Once);

            mockClientProxy.Verify(p => p.SendCoreAsync("ReceiveAdditionalInfo",
                It.Is<object[]>(o => o != null && o.Length == 2 && ((string)o[1]) == queue.AdditionalMessage),
                default(CancellationToken)), Times.Once);

            mockGroupManager.Verify(g => g.AddToGroupAsync(null, roomNo.ToString(), default), Times.Once);
        }

        [Theory]
        [InlineData("12", 2)]
        //[InlineData("12", 3)]
        //[InlineData("13", 5)]
        //[InlineData("10", 30)]
        public async Task RegisterDoctor_TestMoreThanOneOwner(string roomNo, int numberOfCalls)
        {
            var mockClientProxy = new Mock<Microsoft.AspNetCore.SignalR.IClientProxy>();
            var mockClients = new Mock<IHubCallerClients>();
            var mockGroupManager = new Mock<IGroupManager>();

            mockClients.Setup(c => c.Caller).Returns(() => mockClientProxy.Object);

            //System.Diagnostics.Debugger.Launch();
            for (int i = 1; i < numberOfCalls + 1; i++)
            {
                var queue = await CallRegisterDoctor(i.ToString(), roomNo, mockClients, mockClientProxy, mockGroupManager);
            }

            mockClients.Verify(c => c.Group(roomNo), Times.AtLeastOnce);

            mockClientProxy.Verify(p => p.SendCoreAsync("NotifyQueueOccupied",
                It.Is<object[]>(o => o != null && o.Length == 1),
                default(CancellationToken)), Times.Exactly(numberOfCalls - 1));
        }

        [Fact]
        public async Task RegisterPatientTest()
        {
            string fakeRoomNo = "12";

            var mockClients = new Mock<IHubCallerClients>();
            var mockGroupManager = new Mock<IGroupManager>();
            var mockHubCallerContext = new Mock<Microsoft.AspNetCore.SignalR.HubCallerContext>();
            var mockManageHubUsers = new Mock<IManageHubUser>();

            mockHubCallerContext.Setup(c => c.ConnectionId).Returns(It.IsAny<string>());

            var hub = new QueueHub(_mockRepo.Object, _mockQueueService.Object, mockManageHubUsers.Object)
            {
                Context = mockHubCallerContext.Object,
                Groups = mockGroupManager.Object
            };

            await hub.RegisterPatientView(fakeRoomNo);

            mockGroupManager.Verify(g => g.AddToGroupAsync(It.IsAny<string>(), fakeRoomNo.ToString(), default), Times.Once);
        }

        [Theory]
        [InlineData("1", "12")]
        [InlineData("2", "12")]
        public async Task NewQueueNoTest(string id, string roomNo)
        {
            var mockClientProxy = new Mock<Microsoft.AspNetCore.SignalR.IClientProxy>();
            var mockClients = new Mock<IHubCallerClients>();
            var mockGroupManager = new Mock<IGroupManager>();
            var mockManageHubUser = new Mock<IManageHubUser>();

            var prepareQueue = new FakeQueue().WithRoomNo(roomNo).WithQueueNo(15).WithOwnerInitials("PB").Build();
            var preparedQueue = _mapper.Map<Queue>(prepareQueue);

            mockClients.Setup(c => c.Group(preparedQueue.RoomNo.ToString())).Returns(() => mockClientProxy.Object);
            mockManageHubUser.Setup(h => h.GetConnectedUsers()).Returns(new FakeHubUser(id, null, roomNo.ToString()).BuildAsList());
            _mockQueueService.Setup(q => q.NewQueueNo(It.IsAny<string>(), It.IsAny<int>())).Returns(Task.FromResult(preparedQueue));

            var hub = new QueueHub(_mockRepo.Object, _mockQueueService.Object, mockManageHubUser.Object)
            {
                Clients = mockClients.Object,
                Context = _mockHubCallerContext.Object,
                Groups = mockGroupManager.Object
            };
            //System.Diagnostics.Debugger.Launch();
            await hub.NewQueueNo(preparedQueue.QueueNo, roomNo);

            mockClientProxy.Verify(p => p.SendCoreAsync("ReceiveQueueNo",
                It.Is<object[]>(o => o != null && o.Length == 2 && ((string)o[1]) == preparedQueue.QueueNoMessage),
                default(CancellationToken)), Times.AtLeastOnce);
        }

        [Theory]
        [InlineData("1", "12", "bla bla bla")]
        [InlineData("2", "12", "")]
        public async Task NewAdditionalInfoTest(string id, string roomNo, string message)
        {
            var mockClientProxy = new Mock<Microsoft.AspNetCore.SignalR.IClientProxy>();
            var mockClients = new Mock<IHubCallerClients>();
            var mockGroupManager = new Mock<IGroupManager>();
            var mockManageHubUser = new Mock<IManageHubUser>();

            var prepareQueue = new FakeQueue().WithRoomNo(roomNo).WithMessage(message).WithOwnerInitials("PB").Build();
            var preparedQueue = _mapper.Map<Queue>(prepareQueue);

            mockClients.Setup(c => c.Group(preparedQueue.RoomNo.ToString())).Returns(() => mockClientProxy.Object);
            mockManageHubUser.Setup(h => h.GetConnectedUsers()).Returns(new FakeHubUser(id, null, roomNo.ToString()).BuildAsList());
            _mockQueueService.Setup(q => q.NewAdditionalInfo(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult(preparedQueue));

            var hub = new QueueHub(_mockRepo.Object, _mockQueueService.Object, mockManageHubUser.Object)
            {
                Clients = mockClients.Object,
                Context = _mockHubCallerContext.Object,
                Groups = mockGroupManager.Object
            };
            //System.Diagnostics.Debugger.Launch();
            await hub.NewAdditionalInfo(roomNo, preparedQueue.AdditionalMessage);

            mockClientProxy.Verify(p => p.SendCoreAsync("ReceiveAdditionalInfo",
                It.Is<object[]>(o => o != null && o.Length == 2 && ((string)o[1]) == preparedQueue.AdditionalMessage),
                default(CancellationToken)), Times.AtLeastOnce);
        }


        [Fact]
        public async Task OnDisconnectedAsyncTest_OwnerChangesRoom()
        {
            string fakeId = "1";
            string fakeRoomNo = "12";
            string fakeConnectionId = "10";

            var mockClientProxy = new Mock<Microsoft.AspNetCore.SignalR.IClientProxy>();
            var mockClients = new Mock<IHubCallerClients>();
            var mockGroupManager = new Mock<IGroupManager>();
            var mockHubCallerContext = new Mock<Microsoft.AspNetCore.SignalR.HubCallerContext>();
            var mockTimer = new Mock<DoctorDisconnectedTimer>();
            var mockManageHubUser = new Mock<IManageHubUser>();
            //System.Diagnostics.Debugger.Launch();

            mockHubCallerContext.Setup(c => c.ConnectionId).Returns(fakeConnectionId);
            mockManageHubUser.Setup(h => h.GetUserByConnectionId(It.IsAny<string>())).Returns(new FakeHubUser(fakeId, fakeConnectionId, fakeRoomNo).Build());

            _mockQueueService.Setup(q => q.SetQueueInactive(fakeId));
            _mockQueueService.Setup(q => q.CheckRoomSubordination(It.IsAny<string>(), It.IsAny<string>())).Returns(false);

            mockClients.Setup(c => c.Group(fakeRoomNo)).Returns(() => mockClientProxy.Object);

            var hub = new QueueHub(_mockRepo.Object, _mockQueueService.Object, mockManageHubUser.Object)
            {
                Clients = mockClients.Object,
                Context = mockHubCallerContext.Object,
                Groups = mockGroupManager.Object
            };

            await hub.OnDisconnectedAsync(new Exception());

            mockClients.Verify(c => c.Group(fakeRoomNo), Times.AtLeastOnce);
            mockGroupManager.Verify(g => g.RemoveFromGroupAsync(fakeConnectionId, fakeRoomNo, default), Times.AtLeastOnce);
        }

        private Entities.Models.HubUser PrepareHubUser(string id, string roomNo, string connectionID = null)
        {
            return new Entities.Models.HubUser()
            {
                UserId = id,
                GroupName = roomNo,
                ConnectionId = connectionID
            };
        }
    }
}