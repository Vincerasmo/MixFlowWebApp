using AutoMapper;
using MixFlowWebApp.DTOs.LeaderboardDTOs;
using MixFlowWebApp.DTOs.MatchDTOs;
using MixFlowWebApp.DTOs.OrganizerDTOs;
using MixFlowWebApp.DTOs.PlayerDTOs;        
using MixFlowWebApp.DTOs.QueueEntryDTOs;
using MixFlowWebApp.DTOs.SessionDTOs;
using MixFlowWebApp.DTOs.SessionPlayerDTOs;
using MixFlowWebApp.Models;

namespace MixFlowWebApp.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Organizer Mappings
            CreateMap<Organizer, OrganizerDto>();

            // Player Mappings
            CreateMap<Player, PlayerDto>().ReverseMap();
            CreateMap<CreatePlayerDto, Player>()
                .ForMember(dest => dest.SkillLevel, opt => opt.Ignore()); // handled in service
            CreateMap<UpdatePlayerDto, Player>()
                .ForMember(dest => dest.SkillLevel, opt => opt.Ignore())  // handled in service
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            // Session Mappings
            CreateMap<Session, SessionDto>().ReverseMap();
            CreateMap<CreateSessionDto, Session>();
            CreateMap<UpdateSessionDto, Session>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            // SessionPlayer Mappings
            CreateMap<SessionPlayer, SessionPlayerDto>()
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.Player.FullName))
                .ForMember(dest => dest.SkillCategory, opt => opt.MapFrom(src => src.Player.SkillCategory))
                .ForMember(dest => dest.SkillLevel, opt => opt.MapFrom(src => src.Player.SkillLevel));

            // Queue Mappings
            CreateMap<QueueEntry, QueueEntryDto>()
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.Player.FullName))
                .ForMember(dest => dest.SkillCategory, opt => opt.MapFrom(src => src.Player.SkillCategory))
                .ForMember(dest => dest.SkillLevel, opt => opt.MapFrom(src => src.Player.SkillLevel));

            // Match Mappings 
            CreateMap<Match, MatchDto>()
                .ForMember(dest => dest.Team1, opt => opt.MapFrom(src =>
                    src.MatchPlayers.Where(mp => mp.TeamNumber == 1)))
                .ForMember(dest => dest.Team2, opt => opt.MapFrom(src =>
                    src.MatchPlayers.Where(mp => mp.TeamNumber == 2)));

            CreateMap<MatchPlayer, MatchPlayerDto>()
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.Player.FullName));

            // Leaderboard Mappings
            CreateMap<Player, LeaderboardPlayerDto>()
            .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.FullName))
            .ForMember(dest => dest.Wins, opt => opt.MapFrom(src => src.TotalWins))
            .ForMember(dest => dest.GamesPlayed, opt => opt.MapFrom(src => src.GamesPlayed));

            // Match Result (DTO to Entity is handled manually in service)
        }
    }
}