using Entities.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace XUnitTests.TestingData
{
    public class FakeHubUser
    {
        public HubUser HubUser { get; set; }

        private string _userId;
        private string _connectionId;
        private string _groupName;

        public FakeHubUser()
        {
            HubUser = new HubUser();
            SetDefaults();
        }
        public FakeHubUser(string userId, string connectionId, string groupName)
        {
            HubUser = new HubUser();
            _userId = userId;
            _connectionId = connectionId;
            _groupName = groupName;
        }
        private void SetDefaults()
        {

        }
        public FakeHubUser WithId(string id)
        {
            _userId = id;
            return this;
        }
        public FakeHubUser WithConnectionId(string connectionId)
        {
            _connectionId = connectionId;
            return this;
        }
        public FakeHubUser WithGroupName(string groupName)
        {
            _groupName = groupName;
            return this;
        }
        public HubUser Build()
        {
            HubUser.UserId = _userId;
            HubUser.ConnectionId = _connectionId;
            HubUser.GroupName = _groupName;
            return HubUser;
        }
        public List<HubUser> BuildAsList()
        {
            Build();
            List<HubUser> collection = new List<HubUser>();
            collection.Add(HubUser);
            return collection;
        }
    }
}
