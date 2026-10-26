namespace Reviews.Application.Reviews.Mapping;

using AutoMapper;

/// <summary>
/// Профіль мапінгу AutoMapper для сутностей та DTO відгуків (Критерій 12).
/// </summary>
public class ReviewMappingProfile : Profile
{
    public ReviewMappingProfile()
    {
        CreateMap<Author, AuthorDto>();

        CreateMap<ReviewComment, ReviewCommentDto>();

        CreateMap<Review, ReviewListDto>()
            .ForCtorParam("Id", opt => opt.MapFrom(src => src.Id.ToString()))
            .ForCtorParam("ServiceId", opt => opt.MapFrom(src => src.ServiceId))
            .ForCtorParam("ServiceName", opt => opt.MapFrom(src => src.ServiceName))
            .ForCtorParam("Rating", opt => opt.MapFrom(src => src.Rating))
            .ForCtorParam("Title", opt => opt.MapFrom(src => src.Title))
            .ForCtorParam("AuthorName", opt => opt.MapFrom(src => src.Author.FullName))
            .ForCtorParam("CommentsCount", opt => opt.MapFrom(src => src.Comments.Count))
            .ForCtorParam("CreatedAt", opt => opt.MapFrom(src => src.CreatedAt))
            .ForCtorParam("Version", opt => opt.MapFrom(src => src.Version));

        CreateMap<Review, ReviewDetailDto>()
            .ForCtorParam("Id", opt => opt.MapFrom(src => src.Id.ToString()))
            .ForCtorParam("DiscussionId", opt => opt.MapFrom(src => src.DiscussionId.HasValue ? src.DiscussionId.Value.ToString() : null))
            .ForCtorParam("Author", opt => opt.MapFrom(src => src.Author))
            .ForCtorParam("Comments", opt => opt.MapFrom(src => src.Comments))
            .ForCtorParam("PhotosBefore", opt => opt.MapFrom(src => src.PhotosBefore))
            .ForCtorParam("PhotosAfter", opt => opt.MapFrom(src => src.PhotosAfter))
            .ForCtorParam("Pros", opt => opt.MapFrom(src => src.Pros))
            .ForCtorParam("Cons", opt => opt.MapFrom(src => src.Cons));
    }
}
