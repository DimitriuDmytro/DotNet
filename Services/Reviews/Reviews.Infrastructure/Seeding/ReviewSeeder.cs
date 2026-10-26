namespace Reviews.Infrastructure.Seeding;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Reviews.Infrastructure.Persistence;

/// <summary>
/// Ідемпотентний сідер початкових даних для колекції reviews (Критерій 14).
/// Відповідає db/p3/seed.js із фіксованими ObjectId та детальними відгуками.
/// </summary>
public class ReviewSeeder : IDataSeeder
{
    private readonly MongoDbContext _context;
    private readonly IOptions<MongoDbOptions> _options;
    private readonly ILogger<ReviewSeeder> _logger;

    public ReviewSeeder(
        MongoDbContext context,
        IOptions<MongoDbOptions> options,
        ILogger<ReviewSeeder> logger)
    {
        _context = context;
        _options = options;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.Value.RunStartupTasks)
        {
            _logger.LogInformation("ReviewSeeder пропущено (RunStartupTasks = false).");
            return;
        }

        try
        {
            var existingCount = await _context.Reviews.EstimatedDocumentCountAsync(cancellationToken: cancellationToken);
            if (existingCount > 0)
            {
                _logger.LogInformation("Колекція reviews вже містить {Count} документів. Сідінг пропущено.", existingCount);
                return;
            }

            _logger.LogInformation("Початок наповнення тестовими даними (seed) колекції reviews...");

            var seedList = GetSeedReviews();

            foreach (var review in seedList)
            {
                var filter = Builders<Review>.Filter.Eq(r => r.Id, review.Id);
                await _context.Reviews.ReplaceOneAsync(
                    filter,
                    review,
                    new ReplaceOptions { IsUpsert = true },
                    cancellationToken);
            }

            _logger.LogInformation("Сідінг колекції reviews успішно завершено ({Count} документів).", seedList.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Не вдалося виконати сідінг відгуків (можливо база недоступна).");
        }
    }

    private static List<Review> GetSeedReviews()
    {
        var list = new List<Review>();

        // 1. Повний медіа-відгук
        var r1 = Review.CreateForSeeding(
            id: ObjectId.Parse("670900000000000000000301"),
            serviceId: 7,
            serviceName: "Керамічне покриття кузова 9H",
            author: new Author(1, "Олександр Коваленко", "BMW X5 G05"),
            rating: 5,
            title: "Неймовірний дзеркальний блиск на чорному BMW X5!",
            text: "Робив двофазну мийку та двошарову кераміку 9H. Колір Black Sapphire став значно глибшим, вода просто злітає з капота на швидкості 50 км/год.",
            appointmentId: 1,
            discussionId: ObjectId.Parse("670900000000000000000101"),
            verifiedVisit: true,
            createdAt: DateTime.SpecifyKind(DateTime.Parse("2026-10-03T09:30:00Z"), DateTimeKind.Utc));
        r1.AddPhotos(
            new[] { "https://cdn.detailing.ua/reviews/app1-bmw-before-1.jpg", "https://cdn.detailing.ua/reviews/app1-bmw-before-2.jpg" },
            new[] { "https://cdn.detailing.ua/reviews/app1-bmw-after-1.jpg", "https://cdn.detailing.ua/reviews/app1-bmw-after-2.jpg" });
        r1.SetVideoUrl("https://cdn.detailing.ua/reviews/app1-bmw-hydrophobic.mp4");
        r1.AddProsAndCons(
            new[] { "Ідеальна підготовка кузова без жодної голограми", "Фотозвіт кожного етапу в месенджер", "Гарантійний сертифікат на 24 місяці" },
            new[] { "Потрібно залишати авто на дві доби" });
        r1.AddComment(new ReviewComment("Ірина (Адміністратор)", "admin", "Олександре, щиро дякуємо за довіру! Чекаємо на контрольний огляд через 2 тижні.", DateTime.SpecifyKind(DateTime.Parse("2026-10-03T11:00:00Z"), DateTimeKind.Utc)));
        r1.AddComment(new ReviewComment("Андрій (Старший майстер)", "master", "Було приємно працювати з вашим авто, чорний колір після Mohs EVO виглядає еталонно!", DateTime.SpecifyKind(DateTime.Parse("2026-10-03T12:15:00Z"), DateTimeKind.Utc)));
        list.Add(r1);

        // 2. Мінімалістичний відгук
        var r2 = Review.CreateForSeeding(
            id: ObjectId.Parse("670900000000000000000302"),
            serviceId: 1,
            serviceName: "Двофазна детейлінг-мийка",
            author: new Author(1, "Олександр Коваленко", "BMW X5 G05"),
            rating: 5,
            title: "Швидко, акуратно, жодних розводів на склі",
            text: "Регулярно заїжджаю на двофазну мийку. Продувають усі щілини, дзеркала та лючок бензобака турбосушкою — взимку нічого не примерзає.",
            appointmentId: 1,
            verifiedVisit: true,
            createdAt: DateTime.SpecifyKind(DateTime.Parse("2026-10-03T09:45:00Z"), DateTimeKind.Utc));
        list.Add(r2);

        // 3. Відгук на хімчистку салону
        var r3 = Review.CreateForSeeding(
            id: ObjectId.Parse("670900000000000000000303"),
            serviceId: 9,
            serviceName: "Повна хімчистка салону з розбором",
            author: new Author(2, "Марина Шевченко", "Audi Q7"),
            rating: 5,
            title: "Світла шкіра Audi Q7 знову як із салону!",
            text: "На задньому дивані була застаріла пляма від кави та сліди від дитячого автокрісла. Після хімчистки з розбором салон став абсолютно чистим і матовим.",
            appointmentId: 2,
            verifiedVisit: true,
            createdAt: DateTime.SpecifyKind(DateTime.Parse("2026-10-05T08:50:00Z"), DateTimeKind.Utc));
        r3.AddPhotos(
            new[] { "https://cdn.detailing.ua/reviews/app2-audi-seat-before.jpg" },
            new[] { "https://cdn.detailing.ua/reviews/app2-audi-seat-after.jpg", "https://cdn.detailing.ua/reviews/app2-audi-cockpit-after.jpg" });
        r3.AddProsAndCons(
            new[] { "Повністю вивели плями з перфорованої бежевої шкіри", "Приємний нейтральний аромат без різкої хімії" },
            null);
        r3.AddComment(new ReviewComment("Тарас (Майстер інтер'єру)", "master", "Пані Марино, рекомендуємо раз на пів року оновлювати кондиціонер шкіри Koch Chemie Leather Star.", DateTime.SpecifyKind(DateTime.Parse("2026-10-05T10:20:00Z"), DateTimeKind.Utc)));
        list.Add(r3);

        // 4. Відгук на озонування
        var r4 = Review.CreateForSeeding(
            id: ObjectId.Parse("670900000000000000000304"),
            serviceId: 10,
            serviceName: "Озонування та антибактеріальна обробка",
            author: new Author(2, "Марина Шевченко", "Audi Q7"),
            rating: 4,
            title: "Запах вологості з кондиціонера зник повністю",
            text: "Замовляла озонування разом із хімчисткою. Клімат-контроль тепер працює без жодного стороннього запаху, хоча перші дві години в авто відчувався специфічний запах озону.",
            appointmentId: 2,
            verifiedVisit: true,
            createdAt: DateTime.SpecifyKind(DateTime.Parse("2026-10-05T09:10:00Z"), DateTimeKind.Utc));
        r4.AddProsAndCons(
            new[] { "Швидка процедура (45 хв)", "Ефективна дезінфекція повітроводів" },
            new[] { "Потрібно добре провітрювати салон одразу після процедури" });
        list.Add(r4);

        // 5. Відгук на глибоке полірування
        var r5 = Review.CreateForSeeding(
            id: ObjectId.Parse("670900000000000000000305"),
            serviceId: 5,
            serviceName: "Глибоке абразивне полірування кузова",
            author: new Author(3, "Дмитро Бондаренко", "Porsche Cayenne Coupe"),
            rating: 5,
            title: "Прибрали 95% подряпин після траси та гілок",
            text: "Приймав роботу під інспекційним ліхтарем Scangrip — кузов Porsche Cayenne як нове дзеркало. Майстер показав заміри товщиноміром до і після.",
            appointmentId: 3,
            verifiedVisit: true,
            createdAt: DateTime.SpecifyKind(DateTime.Parse("2026-10-11T17:45:00Z"), DateTimeKind.Utc));
        r5.AddPhotos(
            null,
            new[] { "https://cdn.detailing.ua/reviews/app3-cayenne-polish-50-50.jpg" });
        r5.SetVideoUrl("https://cdn.detailing.ua/reviews/app3-cayenne-scangrip-inspection.mp4");
        r5.AddProsAndCons(
            new[] { "Делікатна робота з лаком з контролем товщини", "Ідеальна геометрія відблисків" },
            null);
        r5.AddComment(new ReviewComment("Андрій (Старший майстер)", "master", "Дмитре, дякую за відгук! Зняли в середньому лише 4-5 мікрон лаку.", DateTime.SpecifyKind(DateTime.Parse("2026-10-11T19:00:00Z"), DateTimeKind.Utc)));
        list.Add(r5);

        // 6. Відгук на полірування фар
        var r6 = Review.CreateForSeeding(
            id: ObjectId.Parse("670900000000000000000306"),
            serviceId: 6,
            serviceName: "Полірування фар та ліхтарів",
            author: new Author(4, "Віталій Ткаченко", "Mercedes-Benz E300 W213"),
            rating: 5,
            title: "Фари світять удвічі яскравіше!",
            text: "На Mercedes E300 фари були вже матові від піскострую. За півтори години повернули заводську прозорість.",
            appointmentId: 4,
            verifiedVisit: true,
            createdAt: DateTime.SpecifyKind(DateTime.Parse("2026-10-11T19:30:00Z"), DateTimeKind.Utc));
        r6.AddPhotos(
            new[] { "https://cdn.detailing.ua/reviews/app4-e300-headlight-before.jpg" },
            new[] { "https://cdn.detailing.ua/reviews/app4-e300-headlight-after.jpg" });
        list.Add(r6);

        // 7. Відгук на легке відновлювальне полірування
        var r7 = Review.CreateForSeeding(
            id: ObjectId.Parse("670900000000000000000307"),
            serviceId: 4,
            serviceName: "Легке відновлювальне полірування",
            author: new Author(4, "Віталій Ткаченко", "Mercedes-Benz E300 W213"),
            rating: 4,
            title: "Чудовий варіант освіжити авто за 1 день",
            text: "Уся дрібна павутинка зникла, з'явився чудовий глянець. Дві глибокі подряпини на задньому крилі залишилися ледь помітними, але майстер одразу попередив, що для них потрібне глибоке полірування.",
            appointmentId: 4,
            verifiedVisit: true,
            createdAt: DateTime.SpecifyKind(DateTime.Parse("2026-10-11T19:40:00Z"), DateTimeKind.Utc));
        r7.AddProsAndCons(
            new[] { "Швидке виконання за один день", "Чесна консультація майстра" },
            new[] { "Не прибирає глибокі подряпини до ґрунту" });
        list.Add(r7);

        // 8. Відгук на антидощ
        var r8 = Review.CreateForSeeding(
            id: ObjectId.Parse("670900000000000000000308"),
            serviceId: 8,
            serviceName: "Антидощ на всі вікна",
            author: new Author(3, "Дмитро Бондаренко", "Porsche Cayenne Coupe"),
            rating: 5,
            title: "На трасі під час зливи двірники взагалі не вмикав",
            text: "Після 60 км/год краплі самі розбігаються вгору по лобовому склу. Вночі немає відблисків від зустрічних фар.",
            appointmentId: 3,
            verifiedVisit: true,
            createdAt: DateTime.SpecifyKind(DateTime.Parse("2026-10-11T20:00:00Z"), DateTimeKind.Utc));
        r8.SetVideoUrl("https://cdn.detailing.ua/reviews/app3-aquapel-highway-test.mp4");
        r8.AddProsAndCons(
            new[] { "Комфорт у нічну зливу", "Скло менше брудниться комахами" },
            null);
        list.Add(r8);

        // 9. Відгук без appointmentId
        var r9 = Review.CreateForSeeding(
            id: ObjectId.Parse("670900000000000000000309"),
            serviceId: 7,
            serviceName: "Керамічне покриття кузова 9H",
            author: new Author(5, "Олена Мельник", "Tesla Model Y Performance"),
            rating: 5,
            title: "Покриття тримається вже більше року",
            text: "Наносила кераміку на попередньому авто минулого сезону, зараз записала нову Tesla Model Y. Гідрофоб тримається чесно за умови правильних мийок.",
            appointmentId: null,
            discussionId: ObjectId.Parse("670900000000000000000101"),
            verifiedVisit: false,
            createdAt: DateTime.SpecifyKind(DateTime.Parse("2026-10-10T14:00:00Z"), DateTimeKind.Utc));
        r9.AddProsAndCons(
            new[] { "Довговічність", "Легкість мийки" },
            null);
        list.Add(r9);

        return list;
    }
}
