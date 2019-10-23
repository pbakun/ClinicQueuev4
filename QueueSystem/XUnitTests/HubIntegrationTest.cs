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
        private readonly HubConnection _connection;

        public HubIntegrationTest()
        {
            var webHostBuilder = new WebHostBuilder()
                .UseStartup<Startup>();

            _server = new TestServer(webHostBuilder);

            _connection = new HubConnectionBuilder()
                .WithUrl("https://localhost:5001/queuehub",
                o => o.HttpMessageHandlerFactory = _ => _server.CreateHandler())
                .Build();
        }

        [Fact]
        public async Task CheckLiveBitReceive()
        {
            bool liveBitReceived = false;
            _connection.On("ReceiveLiveBit", () =>
            {
                liveBitReceived = true;
            });

            await _connection.StartAsync();
            await _connection.InvokeAsync("LiveBit");

            Assert.True(liveBitReceived);
        }
    }
}
