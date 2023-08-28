using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApp.Utility
{
    public static class LogMessage
    {
        public static string EmailSent(string email) => $"Email message sent to {email}";

        #region Hub
        public const string logPrefixHub = "HUB | ";
        public static string HubRegisterDoctor(string id, string roomNo) => $"{logPrefixHub} Client[{id}] registered as Doctor for room: [{roomNo}]";
        public static string HubRegisterPatient(string id, string roomNo) => $"{logPrefixHub} Client[{id}] registered as Patient for room: [{roomNo}]";
        public static string HubNewUser(string id) => $"{logPrefixHub} New hub client [{id}]";
        public static string HubNewUserError(string id, string exception) => $"{logPrefixHub}Connecting user [{id}] : {exception}";
        public static string HubOnDisconnectedException(string id, string group, string exception) => $"{logPrefixHub} Disconnecting user: [ {id}  ] for room: [ {group} ]: \n{exception}";
        public static string HubOnDisconnectingUser(string id, string group) => $"{logPrefixHub} Disconnecting user: [{id}] from room: [{group}]";
        public static string HubDisconnectingRoomMaster(string id, string group, string userId) => $"{logPrefixHub} Room: [{group}] master disconnected, connectionId: [{id}] userId: [{userId}]";
        public static string HubRoomInitialized(string group) => $"{logPrefixHub} Room: [ {group} ] view initialized with empty data";
        #endregion
    }
}
