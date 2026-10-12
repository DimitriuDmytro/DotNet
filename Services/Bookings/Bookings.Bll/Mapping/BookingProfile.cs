namespace Bookings.Bll.Mapping;

using AutoMapper;
using Bookings.Bll.DTOs;
using Bookings.Domain.Models;

/// <summary>
/// Профіль AutoMapper для мапінгу між доменними сутностями та об'єктами передачі даних (DTO).
/// </summary>
public class BookingProfile : Profile
{
    public BookingProfile()
    {
        // Клієнти
        CreateMap<Client, ClientDto>();
        CreateMap<CreateClientDto, Client>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore());
        CreateMap<UpdateClientDto, Client>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore());

        // Записи та деталі
        CreateMap<Appointment, AppointmentDto>();
        CreateMap<AppointmentDetails, AppointmentDetailsDto>().ReverseMap();
        CreateMap<AppointmentItem, AppointmentItemDto>().ReverseMap();
        CreateMap<StatusHistoryEntry, StatusHistoryDto>();

        // Платежі
        CreateMap<Payment, PaymentDto>();
        CreateMap<RegisterPaymentDto, Payment>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.AppointmentId, opt => opt.Ignore())
            .ForMember(dest => dest.PaidAt, opt => opt.Ignore());

        // Каталог послуг (локальна репліка)
        CreateMap<CatalogService, CatalogServiceDto>();
        CreateMap<UpsertCatalogServiceDto, CatalogService>()
            .ForMember(dest => dest.SyncedAt, opt => opt.Ignore());
    }
}
