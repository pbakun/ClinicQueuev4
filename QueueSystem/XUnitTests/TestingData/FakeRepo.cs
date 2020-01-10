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
    }
}
