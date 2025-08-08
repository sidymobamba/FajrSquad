using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using FajrSquad.Core.DTOs;
using FajrSquad.Core.Entities;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace FajrSquad.Core.Profiles
{
    public class FajrProfile : Profile
    {
        public FajrProfile()
        {
            CreateMap<UserSettings, UserSettingsDto>();
            // (Altri mapping già esistenti...)
        }
    }

}
