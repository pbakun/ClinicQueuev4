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

            string doctorFullName = string.Empty;
            string receivedId = string.Empty;
            connection.On<string, string>("ReceiveDoctorFullName", (id, msg) =>
            {
                receivedId = id;
                doctorFullName = msg;
            });

            string receivedQueueNo = string.Empty;
            connection.On<string, string>("ReceiveQueueNo", (id, msg) =>
            {
                receivedQueueNo = msg;
            });

            string receivedAdditionalInfo = string.Empty;
            connection.On<string, string>("ReceiveAdditionalInfo", (id, msg) =>
            {
                receivedAdditionalInfo = msg;
            });

            await connection.StartAsync();
            await connection.InvokeAsync("RegisterDoctor", fakeUser.Id, fakeUser.RoomNo);

            string expected = QueueHelper.GetDoctorFullName(fakeUser);
            Assert.Equal(expected, doctorFullName);
            Assert.Equal(fakeUser.Id, receivedId);

            Assert.Equal(expectedQueue.QueueNoMessage, receivedQueueNo);
            Assert.Equal(expectedQueue.AdditionalMessage, receivedAdditionalInfo);

            await connection.DisposeAsync();
        }

        [Fact]
        public async void CheckHeavyLoad()
        {

        }
    }
}
