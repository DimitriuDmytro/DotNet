namespace Catalog.Bll.Mapping;

using AutoMapper;
using Catalog.Bll.DTOs;
using Catalog.Domain.Entities;

public class CatalogProfile : Profile
{
    public CatalogProfile()
    {
        // 1. Категорії
        CreateMap<ServiceCategory, ServiceCategoryDto>()
            .ForMember(d => d.ServicesCount, opt => opt.MapFrom(s => s.Services.Count));

        // 2. Специфікації та фотографії
        CreateMap<ServiceSpec, ServiceSpecDto>();
        CreateMap<ServiceImage, ServiceImageDto>();

        // 3. Послуги
        CreateMap<Service, ServiceDto>()
            .ForMember(d => d.CategoryName, opt => opt.MapFrom(s => s.Category != null ? s.Category.Name : string.Empty))
            .ForMember(d => d.MainImageUrl, opt => opt.MapFrom(s =>
                s.Images.FirstOrDefault(i => i.IsMain) != null
                    ? s.Images.FirstOrDefault(i => i.IsMain)!.Url
                    : s.Images.Select(i => i.Url).FirstOrDefault()));

        CreateMap<Service, ServiceDetailDto>()
            .ForMember(d => d.CategoryName, opt => opt.MapFrom(s => s.Category != null ? s.Category.Name : string.Empty))
            .ForMember(d => d.PackageNames, opt => opt.MapFrom(s =>
                s.PackageServices.Where(ps => ps.Package != null).Select(ps => ps.Package!.Name).ToList()));

        // 4. Пакети послуг
        CreateMap<Package, PackageDto>()
            .ForMember(d => d.ServicesCount, opt => opt.MapFrom(p => p.PackageServices.Count));

        CreateMap<PackageService, PackageItemDto>()
            .ForMember(d => d.ServiceName, opt => opt.MapFrom(ps => ps.Service != null ? ps.Service.Name : string.Empty))
            .ForMember(d => d.ServicePrice, opt => opt.MapFrom(ps => ps.Service != null ? ps.Service.Price : 0m));

        CreateMap<Package, PackageDetailDto>()
            .ForMember(d => d.Services, opt => opt.MapFrom(p => p.PackageServices.OrderBy(ps => ps.SortOrder)));
    }
}
