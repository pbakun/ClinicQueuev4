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

namespace XUnitTests
{
    public class HubIntegrationTest
    {
        private readonly TestServer _server;
        private readonly IRepositoryWrapper _context;

        public HubIntegrationTest()
        {
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
        public async Task CheckLiveBitReceive()
        {
            var connection = MakeHubConnection();
            bool liveBitReceived = false;
            connection.On("ReceiveLiveBit", () =>
            {
                liveBitReceived = true;
            });

            await connection.StartAsync();
            await connection.InvokeAsync("LiveBit");

            Assert.True(liveBitReceived);
        }

        

        [Fact]
        public async Task CheckRegisterNew()
        {
            //System.Diagnostics.Debugger.Launch();
            _context.Queue.Add(new FakeQueue().WithRoomNo(12).WithUserId("123").Build());
            _context.User.Add(new UserData().Build("123", "Piotr", "Bakson", 12));
            _context.Save();

            var test = _context.Queue.FindAll();

            var connection = MakeHubConnection();

            string doctorFullName = string.Empty;
            connection.On<string, string>("ReceiveDoctorFullName", (id, msg) =>
            {
                doctorFullName = msg;
            });

            await connection.StartAsync();
            await connection.InvokeAsync("RegisterDoctor", "123", 12);

            string expected = StaticDetails.DoctorNamePrefix + "Piotr Bakson";
            Assert.Equal(expected, doctorFullName);
        }
    }
}
