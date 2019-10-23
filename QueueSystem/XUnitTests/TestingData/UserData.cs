using System;
using System.Collections.Generic;
using System.Text;

namespace XUnitTests.TestingData
{
    public class UserData
    {
        public Entities.Models.User User { get; set; }

        private int _roomNo;
        private string _id;
        private string _firstName;
        private string _lastName;

        public UserData()
        {
            User = new Entities.Models.User();
            SetDefaults();
        }

        private void SetDefaults()
        {
            
        }

        public UserData WithRoomNo(int roomNo)
        {
            _roomNo = roomNo;
            return this;
        }

        public UserData WithId(string id)
        {
            _id = id;
            return this;
        }

        public UserData WithFirstName(string name)
        {
            _firstName = name;
            return this;
        }

        public UserData WithLastName(string name)
        {
            _lastName = name;
            return this;
        }

        public Entities.Models.User Build()
        {
            User.RoomNo = _roomNo;
            User.Id = _id;
            User.FirstName = _firstName;
            User.LastName = _lastName;
            return User;
        }

        public Entities.Models.User Build(string id, string firstName, string lastName, int roomNo)
        {
            User.RoomNo = roomNo;
            User.Id = id;
            User.FirstName = firstName;
            User.LastName = lastName;
            return User;
        }

        public List<Entities.Models.User> BuildAsList()
        {
            Build();
            var output = new List<Entities.Models.User>();
            output.Add(User);
            return output;
        }
    }
}
