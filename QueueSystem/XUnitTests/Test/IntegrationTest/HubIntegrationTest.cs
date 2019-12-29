using Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using System.Threading.Tasks;
using WebApp;
using Xunit;
using Microsoft.AspNetCore.SignalR.Client;
using Repository.Interfaces;
using System.Linq;
using XUnitTests.TestingData;
using WebApp.Utility;
using WebApp.Helpers;
using AutoMapper;
using WebApp.Mappings;
using WebApp.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace XUnitTests.Test.IntegrationTest
{
    public class HubIntegrationTest
    {
        private readonly TestServer _server;
        private readonly IRepositoryWrapper _context;
        private readonly IMapper _mapper;
        private readonly IManageHubUser _hubUser;

        public string ReceiveQueueNo { get; set; }
        public string ReceiveAdditionalMessage { get; set; }
        public string ReceiveDoctorFullName { get; set; }
        public string ReceiveUserId { get; set; }
        public string ReceiveQueueOccupied { get; set; }


        public HubIntegrationTest()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new MappingProfile());
            });
            _mapper = config.CreateMapper();

            var webHostBuilder = new WebHostBuilder()
                .UseStartup<TStartup>();
            _server = new TestServer(webHostBuilder);
            _context = _server.Host.Services.GetService(typeof(IRepositoryWrapper)) as IRepositoryWrapper;
            _hubUser = _server.Host.Services.GetService(typeof(IManageHubUser)) as IManageHubUser;
        }

        private HubConnection MakeHubConnection()
        {
            return new HubConnectionBuilder()
                .WithUrl("https://localhost:5001/queuehub",
                o => o.HttpMessageHandlerFactory = _ => _server.CreateHandler())
                .Build();
        }


        [Fact]
        public async Task CheckRegisterNewDoctor()
        {
            //System.Diagnostics.Debugger.Launch();
            var repo = new FakeRepo(_context);
            var fakeUser = repo.FakeSingleUser();
            var fakeQueue = repo.FakeSingleQueue();
            var expectedQueue = _mapper.Map<WebApp.Models.Queue>(fakeQueue);

            var connection = MakeHubConnection();

            MakeDoctorFullNameReceive(connection);
            MakeQueueNoReceive(connection);
            MakeQueueAdditionalMessageReceive(connection);

            await connection.StartAsync();
            await connection.InvokeAsync("RegisterDoctor", fakeUser.Id, fakeUser.RoomNo);

            fakeQueue = _context.Queue.FindByCondition(q => q.UserId == fakeUser.Id).SingleOrDefault();

            string expected = QueueHelper.GetDoctorFullName(fakeUser);
            Assert.True(fakeQueue.IsActive);

            Assert.Equal(expected, ReceiveDoctorFullName);
            Assert.Equal(fakeUser.Id, ReceiveUserId);

            Assert.Equal(expectedQueue.QueueNoMessage, ReceiveQueueNo);
            Assert.Equal(expectedQueue.AdditionalMessage, ReceiveAdditionalMessage);

            await connection.DisposeAsync();
        }

        [Fact]
        public async void CheckQueueOccupied()
        {
            var repo = new FakeRepo(_context);
            var fakeUser = repo.FakeSingleUser();
            var fakeQueue = repo.FakeSingleQueue();
            var expectedQueue = _mapper.Map<WebApp.Models.Queue>(fakeQueue);

            //put user in empty groud -> will go to ConnectedUsers list
            _hubUser.AddUser(new FakeHubUser("234", "12345", "12").Build());

            var connection = MakeHubConnection();

            MakeNotifyQueueOccupied(connection);

            await connection.StartAsync();
            await connection.InvokeAsync("RegisterDoctor", fakeUser.Id, fakeUser.RoomNo);

            var expectedMessage = StaticDetails.QueueOccupiedMessage;

            Assert.Equal(expectedMessage, ReceiveQueueOccupied);
        }

        [Fact]
        public async void CheckUserDisconnected()
        {
            var repo = new FakeRepo(_context);
            var fakeUser = repo.FakeSingleUser();
            var fakeQueue = repo.FakeSingleQueue();

            var connection = MakeHubConnection();

            await connection.StartAsync();
            
            await connection.InvokeAsync("RegisterDoctor", fakeUser.Id, fakeUser.RoomNo);

            await connection.StopAsync();
            await Task.Delay(5000);
            fakeQueue = _context.Queue.FindByCondition(q => q.Id == fakeQueue.Id).SingleOrDefault();

            bool result = false;
            var connectedUsers = _hubUser.GetConnectedUserById(fakeUser.Id);
            if (connectedUsers == null)
                result = true;

            Assert.True(!fakeQueue.IsActive);
            Assert.True(result);
        }

        #region Helpers

        private async void MakeQueueNoReceive(HubConnection connection)
        {
            await Task.Run(() => connection.On<string, string>("ReceiveQueueNo", (id, msg) =>
            {
                ReceiveQueueNo = msg;
            }));
        }

        private async void MakeQueueAdditionalMessageReceive(HubConnection connection)
        {
            await Task.Run(() => connection.On<string, string>("ReceiveAdditionalInfo", (id, msg) =>
            {
                ReceiveAdditionalMessage = msg;
            }));
        }

        private async void MakeDoctorFullNameReceive(HubConnection connection)
        {
            await Task.Run(() => connection.On<string, string>("ReceiveDoctorFullName", (id, msg) =>
            {
                ReceiveUserId = id;
                ReceiveDoctorFullName = msg;
            }));
        }

        private async void MakeNotifyQueueOccupied(HubConnection connection)
        {
            await Task.Run(() => connection.On<string>("NotifyQueueOccupied", msg =>
            {
                ReceiveQueueOccupied = msg;
            }));
        }

        #endregion
    }
}
