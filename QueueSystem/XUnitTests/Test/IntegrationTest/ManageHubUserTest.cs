using Entities;
using Entities.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WebApp.Hubs;
using Xunit;
using XUnitTests.TestingData;

namespace XUnitTests.Test.IntegrationTest
{
    public class ManageHubUserTest
    {
        private readonly TestServer _server;
        private readonly HubUserContext _hubUserContext;
        private readonly IManageHubUser _manageHubUser;

        public ManageHubUserTest()
        {
            var webHostBuilder = new WebHostBuilder()
                .UseStartup<TStartup>();
            _server = new TestServer(webHostBuilder);
            _hubUserContext = _server.Host.Services.GetService(typeof(HubUserContext)) as HubUserContext;
            _manageHubUser = _server.Host.Services.GetService(typeof(IManageHubUser)) as IManageHubUser;
        }

        [Fact]
        public void TestAddingSingleUser()
        {
            var hubUser = new FakeHubUser("1", "123", "12").Build();

            _manageHubUser.AddUser(hubUser);
            //_manageHubUser.AddUser(hubUser);

            var userInDb = _hubUserContext.ConnectedUsers.Where(u => u == hubUser).Single();
            var connectedUsersCount = _hubUserContext.ConnectedUsers.ToList().Count();
            var waitingUsersCount = _hubUserContext.WaitingUsers.ToList().Count();

            Assert.Equal(userInDb, hubUser);
            Assert.Equal(1, connectedUsersCount);
            Assert.Equal(0, waitingUsersCount);
        }

        [Fact]
        public void TestRemovingSingleUser()
        {
            var hubUser = new FakeHubUser("1", "123", "12").Build();
            _hubUserContext.ConnectedUsers.Add(hubUser);
            _hubUserContext.SaveChanges();

            _manageHubUser.RemoveUser(hubUser);

            var userInDb = _hubUserContext.ConnectedUsers.Where(u => u == hubUser).SingleOrDefault();

            Assert.Equal(null, userInDb);
        }

        [Fact]
        public void TestAddingTwoUsersToSameGroup()
        {
            var hubUser1 = new FakeHubUser("1", "123", "12").Build();
            var hubUser2 = new FakeHubUser("2", "234", "12").Build();

            _manageHubUser.AddUser(hubUser1);
            _manageHubUser.AddUser(hubUser2);

            var userInConnectedList = _hubUserContext.ConnectedUsers.Where(u => u == hubUser1).Single();
            var userInWaitingList = _hubUserContext.WaitingUsers.Where(u => u == hubUser2).Single();
            var connectedUsersCount = _hubUserContext.ConnectedUsers.ToList().Count();
            var waitingUsersCount = _hubUserContext.WaitingUsers.ToList().Count();

            Assert.Equal(hubUser1, userInConnectedList);
            Assert.Equal(hubUser2, userInWaitingList);
            Assert.Equal(1, connectedUsersCount);
            Assert.Equal(1, waitingUsersCount);
        }

        [Fact]
        public void TestAddingUsersToOneGroup()
        {
            var hubUser = new FakeHubUser().Build();

            _manageHubUser.AddUser(hubUser);
        }

        private List<HubUser> PrepareHubUsers()
        {
            var hubUser = new FakeHubUser("1", "123", "12").BuildAsList();
            hubUser.Add(new FakeHubUser("2", "234", "12").Build());
            hubUser.Add(new FakeHubUser("1", "345", "12").Build());
            hubUser.Add(new FakeHubUser(string.Empty, "456", "12").Build());
            hubUser.Add(new FakeHubUser(string.Empty, "456", "12").Build());
            return hubUser;
        }
    }
}
