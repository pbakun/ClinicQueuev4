using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebApp.Models;

namespace WebApp.ServiceLogic
{
    public interface IQueueService
    {
        Task<Queue> NewQueueNo(string userId, int queueNo);
        Task<Queue> QueueNoUp(string userId);
        Task<Queue> QueueNoDown(string userId);
        Task<Queue> NewAdditionalInfo(string userId, string message);
        Queue ChangeUserRoomNo(string userId, string newRoomNo);
        Queue ResetQueues();
        Queue FindByUserId(string userId);
        Queue FindByRoomNo(string roomNo);
        List<Queue> FindAll();
        Queue CreateQueue(string userId);
        bool CheckRoomSubordination(string userId, string roomNo);
        void SetQueueActive(Entities.Models.Queue queueId);
        void SetQueueInactive(string userId);
        bool UpdateOwnerInitials(Entities.Models.User user);
        string GetRoomNoByUserId(string userId);

    }
}
