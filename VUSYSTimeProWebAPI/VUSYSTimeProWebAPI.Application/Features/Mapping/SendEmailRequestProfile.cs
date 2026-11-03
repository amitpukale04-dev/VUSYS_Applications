using AutoMapper;
using VUSYSTimeProWebAPI.Application.Models;

namespace VUSYSTimeProWebAPI.Application.Features.Mapping
{
    public class SendEmailRequestProfile : Profile
    {
        public SendEmailRequestProfile()
        {
            CreateMap<SendEmailRequestParam, CommonLibCore10.SendEmailRequestParam>();
        }
    }
}
