using AutoMapper;
using Entities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApp.Mappings
{
    public class HubUserMappingProfile : Profile
    {
        public HubUserMappingProfile()
        {
            CreateMap<ConnectedHubUser, HubUser>();
            CreateMap<WaitingHubUser, HubUser>();
            CreateMap<HubUser, ConnectedHubUser>();
            CreateMap<HubUser, WaitingHubUser>();
        }
    }
}
