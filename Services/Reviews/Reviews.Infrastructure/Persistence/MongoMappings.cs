namespace Reviews.Infrastructure.Persistence;

using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;

/// <summary>
/// Реєстрація зіставлень BsonClassMap для Domain сутностей без жодних Bson-атрибутів у домені (Критерій 2, 10).
/// Безпечна для багатопоточності одноразова ініціалізація.
/// </summary>
public static class MongoMappings
{
    private static readonly object SyncLock = new();
    private static bool _isRegistered;

    public static void Register()
    {
        if (_isRegistered)
        {
            return;
        }

        lock (SyncLock)
        {
            if (_isRegistered)
            {
                return;
            }

            var pack = new ConventionPack
            {
                new CamelCaseElementNameConvention(),
                new IgnoreExtraElementsConvention(true)
            };
            ConventionRegistry.Register("DetalingConventions", pack, _ => true);

            if (!BsonClassMap.IsClassMapRegistered(typeof(BaseEntity)))
            {
                BsonClassMap.RegisterClassMap<BaseEntity>(cm =>
                {
                    cm.AutoMap();
                    cm.SetIsRootClass(true);
                    cm.MapIdMember(x => x.Id);
                    cm.MapMember(x => x.CreatedAt).SetElementName("createdAt");
                    cm.MapMember(x => x.UpdatedAt).SetElementName("updatedAt");
                    cm.MapMember(x => x.Version).SetElementName("version");
                });
            }

            if (!BsonClassMap.IsClassMapRegistered(typeof(Author)))
            {
                BsonClassMap.RegisterClassMap<Author>(cm =>
                {
                    cm.AutoMap();
                    cm.MapCreator(a => new Author(a.ClientId, a.FullName, a.CarInfo));
                    cm.MapMember(a => a.ClientId).SetElementName("clientId");
                    cm.MapMember(a => a.FullName).SetElementName("fullName");
                    cm.MapMember(a => a.CarInfo).SetElementName("carInfo");
                });
            }

            if (!BsonClassMap.IsClassMapRegistered(typeof(ReviewComment)))
            {
                BsonClassMap.RegisterClassMap<ReviewComment>(cm =>
                {
                    cm.AutoMap();
                    cm.MapCreator(c => new ReviewComment(c.AuthorName, c.Role, c.Text, c.CreatedAt));
                    cm.MapMember(c => c.AuthorName).SetElementName("authorName");
                    cm.MapMember(c => c.Role).SetElementName("role");
                    cm.MapMember(c => c.Text).SetElementName("text");
                    cm.MapMember(c => c.CreatedAt).SetElementName("createdAt");
                });
            }

            if (!BsonClassMap.IsClassMapRegistered(typeof(Review)))
            {
                BsonClassMap.RegisterClassMap<Review>(cm =>
                {
                    cm.AutoMap();
                    cm.UnmapMember(x => x.Comments);
                    cm.UnmapMember(x => x.PhotosBefore);
                    cm.UnmapMember(x => x.PhotosAfter);
                    cm.UnmapMember(x => x.Pros);
                    cm.UnmapMember(x => x.Cons);

                    cm.MapField("_comments").SetElementName("comments");
                    cm.MapField("_photosBefore").SetElementName("photosBefore");
                    cm.MapField("_photosAfter").SetElementName("photosAfter");
                    cm.MapField("_pros").SetElementName("pros");
                    cm.MapField("_cons").SetElementName("cons");
                });
            }

            if (!BsonClassMap.IsClassMapRegistered(typeof(Discussion)))
            {
                BsonClassMap.RegisterClassMap<Discussion>(cm =>
                {
                    cm.AutoMap();
                    cm.UnmapMember(x => x.Tags);
                    cm.MapField("_tags").SetElementName("tags");
                });
            }

            _isRegistered = true;
        }
    }
}
