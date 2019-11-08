using AutoMapper;
using Entities;
using Entities.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WebApp.Hubs;
using WebApp.Mappings;
using Xunit;
using XUnitTests.TestingData;

namespace XUnitTests.Test.IntegrationTest
{
    public class ManageHubUserTest
    {
        private readonly TestServer _server;
        private readonly HubUserContext _hubUserContext;
        private readonly IManageHubUser _manageHubUser;
        private readonly IMapper _mapper;

        public ManageHubUserTest()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new HubUserMappingProfile());
            });
            _mapper = config.CreateMapper();

            var webHostBuilder = new WebHostBuilder()
                .UseStartup<TStartup>();
            _server = new TestServer(webHostBuilder);
            _hubUserContext = _server.Host.Services.GetService(typeof(HubUserContext)) as HubUserContext;
            _manageHubUser = _server.Host.Services.GetService(typeof(IManageHubUser)) as IManageHubUser;
        }

        #region Add User

        [Theory]
        [InlineData("1", "123", "12")]
        [InlineData("3", "423", "13")]
        public void TestAddingSingleUser(string userId, string connectionId, string groupName)
        {
            var hubUser = new FakeHubUser(userId, connectionId, groupName).Build();
            _manageHubUser.AddUser(hubUser);
            //_manageHubUser.AddUser(hubUser);

            var userInDb = _hubUserContext.ConnectedUsers.Where(u => u.ConnectionId == hubUser.ConnectionId).Single();
            var connectedUsersCount = _hubUserContext.ConnectedUsers.ToList().Count();
            var waitingUsersCount = _hubUserContext.WaitingUsers.ToList().Count();

            Assert.Equal(userInDb.UserId, hubUser.UserId);
            Assert.Equal(userInDb.ConnectionId, hubUser.ConnectionId);
            Assert.Equal(userInDb.GroupName, hubUser.GroupName);
            Assert.Equal(1, connectedUsersCount);
            Assert.Equal(0, waitingUsersCount);
        }

        [Fact]
        public void TestAddingTwoUsersToSameGroup()
        {
            var hubUser1 = new FakeHubUser("1", "123", "12").Build();
            var hubUser2 = new FakeHubUser("2", "234", "12").Build();

            _manageHubUser.AddUser(hubUser1);
            _manageHubUser.AddUser(hubUser2);

            var userInConnectedList = _hubUserContext.ConnectedUsers.Where(u => u.UserId == hubUser1.UserId).Single();
            var userInWaitingList = _hubUserContext.WaitingUsers.Where(u => u.UserId == hubUser2.UserId).Single();
            var connectedUsersCount = _hubUserContext.ConnectedUsers.ToList().Count();
            var waitingUsersCount = _hubUserContext.WaitingUsers.ToList().Count();

            Assert.Equal(hubUser1.ConnectionId, userInConnectedList.ConnectionId);
            Assert.Equal(hubUser2.ConnectionId, userInWaitingList.ConnectionId);
            Assert.Equal(1, connectedUsersCount);
            Assert.Equal(1, waitingUsersCount);
        }

        [Fact]
        public void TestAddingUserWithoutUserId()
        {
            var hubUser = new FakeHubUser(string.Empty, "123", "12").Build();

            _manageHubUser.AddUser(hubUser);

            var userInDb = _hubUserContext.ConnectedUsers.Where(u => u.ConnectionId == hubUser.ConnectionId).SingleOrDefault();

            Assert.Equal(hubUser.ConnectionId, userInDb.ConnectionId);
            Assert.Equal(hubUser.GroupName, userInDb.GroupName);
        }

        #endregion

        #region Remove User

        [Fact]
        public void TestRemovingSingleConnectedUser()
        {
            var hubUser = new FakeHubUser("1", "123", "12").Build();
            _hubUserContext.ConnectedUsers.Add(_mapper.Map<ConnectedHubUser>(hubUser));
            _hubUserContext.SaveChanges();
            var userFromDb = _hubUserContext.ConnectedUsers.Where(u => u.ConnectionId == u.ConnectionId).SingleOrDefault();

            _manageHubUser.RemoveUser(_mapper.Map<HubUser>(userFromDb));

            var userInDb = _hubUserContext.ConnectedUsers.Where(u => u.ConnectionId == userFromDb.ConnectionId).SingleOrDefault();
            var userCount = _hubUserContext.ConnectedUsers.ToList().Count;

            Assert.Equal(null, userInDb);
            Assert.Equal(0, userCount);
        }

        [Fact]
        public void TestRemovingSingleWaitingUser()
        {
            var hubUser = new FakeHubUser("1", "123", "12").Build();
            _hubUserContext.WaitingUsers.Add(_mapper.Map<WaitingHubUser>(hubUser));
            _hubUserContext.SaveChanges();
            var userFromDb = _hubUserContext.WaitingUsers.Where(u => u.ConnectionId == u.ConnectionId).SingleOrDefault();

            _manageHubUser.RemoveUser(_mapper.Map<HubUser>(userFromDb));

            var userInDb = _hubUserContext.WaitingUsers.Where(u => u.ConnectionId == userFromDb.ConnectionId).SingleOrDefault();
            var waitingUserCount = _hubUserContext.WaitingUsers.ToList().Count;

            Assert.Equal(null, userInDb);
            Assert.Equal(0, waitingUserCount);
        }
        [Fact]
        public void TestAddingAndRemovingUser()
        {
            var hubUser = new FakeHubUser("1", "123", "12").Build();

            _manageHubUser.AddUser(hubUser);
            var userToRemove = _manageHubUser.GetUserByConnectionId(hubUser.ConnectionId);
            _manageHubUser.RemoveUser(userToRemove);

            var userInDb = _hubUserContext.ConnectedUsers.Where(u => u.ConnectionId == userToRemove.ConnectionId).SingleOrDefault();
            var userCount = _hubUserContext.ConnectedUsers.ToList().Count;
            
            Assert.Equal(null, userInDb);
            Assert.Equal(0, userCount);

        }

        #endregion

        #region Get All Users

        [Fact]
        public void TestGettingAllConnectedUsers()
        {
            var hubUsers = PrepareHubUsers();
            foreach(var user in hubUsers)
            {
                _manageHubUser.AddUser(user);
            }

            var connectedUsers = _manageHubUser.GetConnectedUsers();

            Assert.Equal(4, connectedUsers.Count());
        }

        [Fact]
        public void TestGettingAllWaitingUsers()
        {
            var hubUsers = PrepareHubUsers();
            foreach (var user in hubUsers)
            {
                _manageHubUser.AddUser(user);
            }

            var waitingUsers = _manageHubUser.GetWaitingUsers();

            Assert.Equal(3, waitingUsers.Count());
        }

        #endregion

        #region Get User By Condition

        [Theory]
        [InlineData(1)]
        [InlineData(0)]
        [InlineData(4)]
        public void TestGetUserByUserId(int index)
        {
            var hubUsers = AddPreparedUsers();
            
            var user = _manageHubUser.GetUserById(hubUsers[index].UserId);

            Assert.Equal(hubUsers[index].ConnectionId, user.ConnectionId);
            Assert.Equal(hubUsers[index].UserId, user.UserId);
            Assert.Equal(hubUsers[index].GroupName, user.GroupName);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(0)]
        [InlineData(5)]
        public void TestGetUserByConnectionId(int index)
        {
            var hubUsers = AddPreparedUsers();

            var user = _manageHubUser.GetUserByConnectionId(hubUsers[index].ConnectionId);

            Assert.Equal(hubUsers[index].ConnectionId, user.ConnectionId);
            Assert.Equal(hubUsers[index].UserId, user.UserId);
            Assert.Equal(hubUsers[index].GroupName, user.GroupName);
        }

        #endregion

        #region Helpers

        private List<HubUser> AddPreparedUsers()
        {
            var hubUsers = PrepareHubUsers();
            foreach (var user in hubUsers)
            {
                _manageHubUser.AddUser(user);
            }
            return hubUsers;
        }

        #endregion

        private List<HubUser> PrepareHubUsers()
        {
            var hubUser = new FakeHubUser("1", "123", "12").BuildAsList();
            hubUser.Add(new FakeHubUser("2", "234", "12").Build());
            hubUser.Add(new FakeHubUser("1", "345", "12").Build());
            hubUser.Add(new FakeHubUser(string.Empty, "456", "12").Build());
            hubUser.Add(new FakeHubUser("5", "789", "12").Build());
            hubUser.Add(new FakeHubUser(string.Empty, "567", "12").Build());
            hubUser.Add(new FakeHubUser("3", "678", "12").Build());
            return hubUser;
        }
    }
}
