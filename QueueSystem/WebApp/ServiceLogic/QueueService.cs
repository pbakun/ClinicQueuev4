using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using Repository.Interfaces;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebApp.Models;
using WebApp.Utility;

namespace WebApp.ServiceLogic
{
    public class QueueService : IQueueService
    {
        private readonly IRepositoryWrapper _repo;
        private readonly IMapper _mapper;
        private readonly IServiceScopeFactory _scopeFactory;

        public QueueService(IRepositoryWrapper repo, IMapper mapper, IServiceScopeFactory serviceScopeFactory)
        {
            _repo = repo;
            _mapper = mapper;
            _scopeFactory = serviceScopeFactory;
        }

        #region Implmenting interface

        public Queue ChangeUserRoomNo(string userId, string newRoomNo)
        {
            var user = _repo.User.FindByCondition(u => u.Id == userId).FirstOrDefault();

            Log.Information(String.Concat(StaticDetails.logPrefixQueue, StaticDetails.logPrefixUser, "User id: [ ",
                                        userId, " ] changed room from: [ ", user.RoomNo, "to room: [ ", newRoomNo, " ]"));

            user.RoomNo = newRoomNo;
            _repo.User.Update(user);
            var queue = _repo.Queue.FindByCondition(q => q.UserId == userId).FirstOrDefault();
            queue.RoomNo = newRoomNo;
            _repo.Queue.Update(queue);
            _repo.Save();

            return _mapper.Map<Queue>(queue);
        }

        public Queue FindByRoomNo(string roomNo)
        {
            //returns queue with newest Timestamp
            var queue = _repo.Queue.FindByCondition(r => r.RoomNo.Equals(roomNo) && r.IsActive).OrderByDescending(t => t.Timestamp).FirstOrDefault();

            return _mapper.Map<Queue>(queue);
        }

        public Queue FindByUserId(string userId)
        {
            var queue = _repo.Queue.FindByCondition(u => u.UserId == userId).FirstOrDefault();
            Queue output = new Queue();
            if (queue == null)
                output = CreateQueue(userId);
            else
            {
                queue.Timestamp = DateTime.UtcNow;
                _repo.Queue.Update(queue);
                _repo.Save();
                output = _mapper.Map<Queue>(queue);
            }
            return output;
        }

        public async Task<Queue> NewAdditionalInfo(string userId, string message)
        {
            var queue = _repo.Queue.FindByCondition(i => i.UserId == userId).FirstOrDefault();
            if (message.Length > 0)
                queue.AdditionalMessage = message;
            else queue.AdditionalMessage = string.Empty;

            queue.Timestamp = DateTime.UtcNow;

            _repo.Queue.Update(queue);
            await _repo.SaveAsync();

            return _mapper.Map<Queue>(queue);
        }

        public async Task<Queue> NewQueueNo(string userId, int queueNo)
        {
            var queue = _repo.Queue.FindByCondition(i => i.UserId == userId).FirstOrDefault();

            //if queueNo == -1 (Break) // == -2 (SpecialNo)
            if (queueNo > 0)
            {
                queue.QueueNo = queueNo;
                queue.IsBreak = false;
                queue.IsSpecial = false;
            }
            else if (queueNo == -1 && queue.IsBreak == false)
            {
                queue.IsBreak = true;
            }
            else if (queueNo == -2 && queue.IsSpecial == false)
            {
                queue.IsSpecial = true;
            }
            else
            {
                queue.IsBreak = false;
                queue.IsSpecial = false;
            }

            queue.Timestamp = DateTime.UtcNow;
            _repo.Queue.Update(queue);
            await _repo.SaveAsync();

            return _mapper.Map<Queue>(queue);
        }

        public Queue ResetQueues()
        {
            throw new NotImplementedException();
        }

        public Queue CreateQueue(string userId)
        {
            var user = _repo.User.FindByCondition(u => u.Id == userId).FirstOrDefault();
            Queue queue = new Queue
            {
                UserId = user.Id,
                RoomNo = user.RoomNo,
                QueueNo = 1,
                OwnerInitials = String.Concat(user.FirstName.First(), user.LastName.First()),
                Timestamp = DateTime.UtcNow
            };
            _repo.Queue.Add(_mapper.Map<Entities.Models.Queue>(queue));
            _repo.Save();

            return queue;
        }

        public bool CheckRoomSubordination(string userId, string roomNo)
        {
            var queue = _repo.Queue.FindByCondition(u => u.UserId == userId).FirstOrDefault();
            if (queue.RoomNo.Equals(roomNo))
                return true;

            return false;
        }

        public void SetQueueActive(Entities.Models.Queue queue)
        {//can I do something like this?
            using (var scope = _scopeFactory.CreateScope())
            {
                var repo = scope.ServiceProvider.GetRequiredService<IRepositoryWrapper>();

                queue.IsActive = true;

                repo.Queue.Update(queue);
                repo.Save();
            }
        }

        public void SetQueueInactive(string userId)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var repo = scope.ServiceProvider.GetRequiredService<IRepositoryWrapper>();
                var queue = repo.Queue.FindByCondition(q => q.UserId == userId).FirstOrDefault();
                if (queue != null)
                {
                    queue.IsActive = false;
                    repo.Queue.Update(queue);
                    repo.Save();
                }
            }
        }

        public List<Queue> FindAll()
        {
            var queues = _repo.Queue.FindAll().ToList();

            return _mapper.Map<List<Queue>>(queues);
        }

        public async Task<Queue> QueueNoUp(string userId)
        {
            var queue = _repo.Queue.FindByCondition(i => i.UserId == userId).FirstOrDefault();

            queue.QueueNo++;
            TurnOffBreakAndSpecial(queue);

            queue.Timestamp = DateTime.UtcNow;
            _repo.Queue.Update(queue);
            await _repo.SaveAsync();

            return _mapper.Map<Queue>(queue);
        }

        public async Task<Queue> QueueNoDown(string userId)
        {
            var queue = _repo.Queue.FindByCondition(i => i.UserId == userId).FirstOrDefault();

            if(queue.QueueNo>1)
            {
                queue.QueueNo--;
                TurnOffBreakAndSpecial(queue);
            }

            queue.Timestamp = DateTime.UtcNow;
            _repo.Queue.Update(queue);
            await _repo.SaveAsync();

            return _mapper.Map<Queue>(queue);
        }

        public bool UpdateOwnerInitials(Entities.Models.User user)
        {
            if (user == null)
                return false;

            var queue = _repo.Queue.FindByCondition(q => q.UserId == user.Id).ToList().FirstOrDefault();
            if (queue == null)
                return false;

            using (var scope = _scopeFactory.CreateScope())
            {
                var repo = scope.ServiceProvider.GetRequiredService<IRepositoryWrapper>();

                queue.OwnerInitials = String.Concat(user.FirstName.First(), user.LastName.First()); ;

                repo.Queue.Update(queue);
                repo.Save();
            }

           return true;
        }

        public string GetRoomNoByUserId(string userId)
        {
            if (userId == null)
                throw new NullReferenceException("User Id cannot be null");

            var queue = _repo.Queue.FindByCondition(q => q.UserId == userId).SingleOrDefault();
            return queue.RoomNo;
        }

        #endregion

        #region Custom Private Methods
        private void TurnOffBreakAndSpecial(Entities.Models.Queue queue)
        {
            queue.IsBreak = false;
            queue.IsSpecial = false;
        }

        
        #endregion
    }
}
