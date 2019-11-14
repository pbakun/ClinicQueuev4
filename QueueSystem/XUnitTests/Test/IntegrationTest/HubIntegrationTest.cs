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

namespace XUnitTests.Test.IntegrationTest
{
    public class HubIntegrationTest
    {
        private readonly TestServer _server;
        private readonly IRepositoryWrapper _context;
        private readonly IMapper _mapper;

        public string ReceiveQueueNo { get; set; }
        public string ReceiveAdditionalMessage { get; set; }
        public string ReceiveDoctorFullName { get; set; }
        public string ReceiveUserId { get; set; }


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

            string expected = QueueHelper.GetDoctorFullName(fakeUser);
            Assert.Equal(expected, ReceiveDoctorFullName);
            Assert.Equal(fakeUser.Id, ReceiveUserId);

            Assert.Equal(expectedQueue.QueueNoMessage, ReceiveQueueNo);
            Assert.Equal(expectedQueue.AdditionalMessage, ReceiveAdditionalMessage);

            await connection.DisposeAsync();
        }

        [Fact]
        public async void CheckHeavyLoad()
        {

        }

        #region Helpers

        private async void MakeQueueNoReceive(HubConnection connection)
        {
            await Task.Run(() => connection.On<string, string>("ReceiveQueueNo", (id, msg) =>
            {
                //try to use delegate
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

        #endregion
    }
}
