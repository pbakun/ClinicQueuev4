using Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using System.Threading.Tasks;
using WebApp;
using Xunit;
using Microsoft.AspNetCore.SignalR.Client;

namespace XUnitTests
{
    public class HubIntegrationTest
    {
        private readonly TestServer _server;

        public HubIntegrationTest()
        {
            var webHostBuilder = new WebHostBuilder()
                .UseStartup<Startup>();

            _server = new TestServer(webHostBuilder);
        }

        private HubConnection MakeHubConnection()
        {
            var connection = new HubConnectionBuilder()
                .WithUrl("https://localhost:5001/queuehub",
                o => o.HttpMessageHandlerFactory = _ => _server.CreateHandler())
                .Build();
            return connection;
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
        public async Task CheckNewQueueNo()
        {
            var connection = MakeHubConnection();

            string doctorFullName = string.Empty;
            connection.On<string, string>("ReceiveDoctorFullName", (id, msg) =>
            {
                doctorFullName = msg;
            });

            await connection.StartAsync();
            await connection.InvokeAsync("RegisterDoctor", "1", 12);


        }
    }
}
