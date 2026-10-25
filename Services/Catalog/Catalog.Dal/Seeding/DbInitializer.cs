namespace Catalog.Dal.Seeding;

using Microsoft.EntityFrameworkCore;
using Catalog.Domain.Entities;

/// <summary>
/// Програмний ідемпотентний ініціалізатор початкових даних (Критерій 9(б)).
/// Виконується в Development/Staging середовищі після застосування міграцій.
/// Перевіряє наявність кожного запису перед вставкою (AnyAsync), унеможливлюючи появу дублікатів.
/// </summary>
public static class DbInitializer
{
    public static async Task SeedAsync(CatalogDbContext db, CancellationToken cancellationToken = default)
    {
        // 1. Послуги (Services)
        if (!await db.Services.IgnoreQueryFilters().AnyAsync(cancellationToken))
        {
            var services = new List<Service>
            {
                new(1, "Двофазна детейлінг-мийка", 800.00m, 60, "catalog.editor", id: 1),
                new(1, "Трифазна нано-мийка з консервацією", 1200.00m, 90, "catalog.editor", id: 2),
                new(1, "Деконтамінація кузова та дисків", 1500.00m, 120, "catalog.editor", id: 3),
                new(2, "Легке відновлювальне полірування", 4500.00m, 360, "catalog.editor", id: 4),
                new(2, "Глибоке абразивне полірування кузова", 8500.00m, 600, "catalog.editor", id: 5),
                new(2, "Полірування фар та ліхтарів", 1400.00m, 90, "catalog.editor", id: 6),
                new(3, "Керамічне покриття кузова 9H", 12000.00m, 720, "catalog.editor", id: 7),
                new(3, "Антидощ на всі вікна", 1800.00m, 120, "catalog.editor", id: 8),
                new(4, "Повна хімчистка салону з розбором", 5500.00m, 480, "catalog.editor", id: 9),
                new(4, "Озонування та антибактеріальна обробка", 900.00m, 45, "catalog.editor", id: 10),
                new(4, "Захист шкіри салону керамічним складом", 2800.00m, 180, "catalog.editor", id: 11),
                new(3, "Оклейка зон ризику антигравійною плівкою PPF", 16500.00m, 540, "catalog.editor", id: 12)
            };

            await db.Services.AddRangeAsync(services, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
        }

        // 2. Технічні специфікації (ServiceSpecs - 1:1)
        if (!await db.ServiceSpecs.AnyAsync(cancellationToken))
        {
            var specs = new List<ServiceSpec>
            {
                new(1, "Koch Chemie Green Star, Gentle Snow Foam, мікрофіброві рукавиці", 0, "Безконтактна передмийка активною піною та ручна мийка шампунем за системою двох відер із сушінням турбосушкою."),
                new(2, "Koch Chemie NanoMagicShampoo, ProtectorWax", 1, "Трифазне очищення кузова з нанесенням гідрофобного консервуючого воску преміум-класу."),
                new(3, "Gyeon Q2M Iron, Q2M Tar, синтетична глина Clay Bar", 0, "Видалення металевих вкраплень, колодкового пилу, бітуму та деревних смол з поверхні ЛФП і колісних дисків."),
                new(4, "Menzerna Medium Cut 2500, Super Finish 3800, круги Rupes", 6, "Одно-двокрокове м'яке полірування для усунення дрібної павутинки (до 70% подряпин) і повернення глибокого глянцю."),
                new(5, "Koch Chemie H9.02 Heavy Cut, F6.01, M3.02, хутряні та поролонові круги Rupes", 12, "Трьохетапна глибока корекція лакофарбового покриття з усуненням до 95% подряпин, голограм та оксидного шару."),
                new(6, "Абразиви Mirka Abralon, паста Menzerna 400 / 3800", 6, "Відновлення прозорості полікарбонатних лінз передніх фар та задніх ліхтарів."),
                new(7, "Gyeon Q2 Mohs EVO (2 шари) + Q2 Cure", 24, "Двошарове кварцово-керамічне покриття твердістю 9H з потужним гідрофобним ефектом, захистом від УФ та реагентів."),
                new(8, "Aquapel Glass Treatment / Gyeon Q2 View", 6, "Полірування скла та нанесення фторполімерного гідрофобного покриття на лобове, бічні та заднє скло."),
                new(9, "Koch Chemie Mehrzweckreiniger, Pol Star, Leather Star, торнадор і екстрактор", 3, "Глибока хімчистка стелі, підлоги, сидінь, ременів безпеки та багажника з демонтажем крісел і делікатною сушкою."),
                new(10, "Промисловий генератор озону O3 + нейтралізатор запахів Koch Chemie Fresh Up", 1, "Дезінфекція системи кондиціонування та салону озоном, повне знищення бактерій, грибка та запаху тютюну."),
                new(11, "Gyeon Q2 LeatherShield / Colourlock Leather Shield", 12, "Захисне нанопокриття для шкіряних сидінь та керма проти потертостей, фарбування від джинсів та пересихання."),
                new(12, "Поліуретанова плівка LLumar Platinum PPF (200 мкм)", 60, "Бронювання переднього бампера, капота, крил, фар, дзеркал і зон під ручками антигравійною плівкою з ефектом самовідновлення.")
            };

            await db.ServiceSpecs.AddRangeAsync(specs, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
        }

        // 3. Комплексні пакети (Packages)
        if (!await db.Packages.IgnoreQueryFilters().AnyAsync(cancellationToken))
        {
            var packages = new List<Package>
            {
                new("Basic", 4200.00m, "Базовий сезонний догляд: двофазна мийка, глибока деконтамінація кузова, антидощ на всі вікна та озонування салону.", "catalog.editor", id: 1),
                new("Premium", 10800.00m, "Комплексне оновлення екстер'єру та інтер'єру: трифазна мийка, відновлювальне полірування кузова і фар, повна хімчистка салону.", "catalog.editor", id: 2),
                new("Ceramic Pro", 22900.00m, "Максимальний преміум-комплекс: деконтамінація, глибоке абразивне полірування, кераміка кузова 9H, антидощ і керамічний захист шкіри.", "catalog.editor", id: 3)
            };

            await db.Packages.AddRangeAsync(packages, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
        }

        // 4. Склад пакетів (PackageServices - M:N)
        if (!await db.PackageServices.AnyAsync(cancellationToken))
        {
            var packageServices = new List<PackageService>
            {
                // Basic (пакет 1)
                new(1, 1, 1),
                new(1, 3, 2),
                new(1, 8, 3),
                new(1, 10, 4),

                // Premium (пакет 2)
                new(2, 2, 1),
                new(2, 9, 2),
                new(2, 4, 3),
                new(2, 6, 4),

                // Ceramic Pro (пакет 3)
                new(3, 3, 1),
                new(3, 5, 2),
                new(3, 7, 3),
                new(3, 8, 4),
                new(3, 11, 5)
            };

            await db.PackageServices.AddRangeAsync(packageServices, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
        }

        // 5. Фотогалерея послуг (ServiceImages)
        if (!await db.ServiceImages.AnyAsync(cancellationToken))
        {
            var images = new List<ServiceImage>
            {
                new(1, "https://cdn.detailing.ua/catalog/services/two-phase-wash-main.jpg", isMain: true, id: 1),
                new(5, "https://cdn.detailing.ua/catalog/services/heavy-polish-main.jpg", isMain: true, id: 2),
                new(5, "https://cdn.detailing.ua/catalog/services/heavy-polish-50-50.jpg", isMain: false, id: 3),
                new(7, "https://cdn.detailing.ua/catalog/services/ceramic-9h-main.jpg", isMain: true, id: 4),
                new(7, "https://cdn.detailing.ua/catalog/services/ceramic-9h-hydrophobic.jpg", isMain: false, id: 5),
                new(9, "https://cdn.detailing.ua/catalog/services/interior-dryclean-main.jpg", isMain: true, id: 6),
                new(12, "https://cdn.detailing.ua/catalog/services/ppf-armor-main.jpg", isMain: true, id: 7)
            };

            await db.ServiceImages.AddRangeAsync(images, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
