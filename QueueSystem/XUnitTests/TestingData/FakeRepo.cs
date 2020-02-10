using Repository.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace XUnitTests.TestingData
{
    public class FakeRepo
    {
        private readonly IRepositoryWrapper _repo;

        public FakeRepo(IRepositoryWrapper repo)
        {
            _repo = repo;
        }

        public Entities.Models.User FakeSingleUser()
        {
            Entities.Models.User user = new UserData().Build("123", "Jan", "Kowalski", "12");

            _repo.User.Add(user);
            _repo.Save();
            return user;
        }

        public Entities.Models.Queue FakeSingleQueue()
        {
            var queue = new FakeQueue().WithUserId("123").WithRoomNo("12").WithQueueNo(14).Build();

            _repo.Queue.Add(queue);
            _repo.Save();
            return queue;
        }

        public List<Entities.Models.User> FakeMultipleUsers(int quantity, string roomNo)
        {
            List<Entities.Models.User> list = new List<Entities.Models.User>();
            for(int i=0; i<quantity; i++)
            {
                Entities.Models.User user = new UserData().Build(i.ToString(), "Jan", "Kowalski", roomNo);
                list.Add(user);
                _repo.User.Add(user);
            }
            _repo.Save();
            return list;
        }

        public List<Entities.Models.Queue> FakeMultipleQueues(IEnumerable<Entities.Models.User> users)
        {
            List<Entities.Models.Queue> queues = new List<Entities.Models.Queue>();
            int i = 0;
            foreach(var user in users)
            {
                Entities.Models.Queue queue = new FakeQueue()
                                                .WithUserId(user.Id)
                                                .WithRoomNo(user.RoomNo)
                                                .WithQueueNo(i++)
                                                .WithOwnerInitials(String.Concat(user.FirstName, user.LastName))
                                                .Build();
                queues.Add(queue);
                _repo.Queue.Add(queue);
            }
            _repo.Save();
            return queues;
        }
    }
}
