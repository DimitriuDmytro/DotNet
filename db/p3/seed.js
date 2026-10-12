/* =========================================================================
   Проєкт №3: Контекст "Відгуки та звіти" (Reviews)
   Файл: seed.js
   СКБД: MongoDB, база даних: detailing_reviews
   Опис: Ідемпотентне наповнення колекцій тестовими документами через
         updateOne({ _id }, { $set }, { upsert: true }) із фіксованими ObjectId:
         - 2 теми обговорень (discussions)
         - 10 повідомлень в обговореннях (messages)
         - 9 відгуків клієнтів з гнучкою схемою (reviews)
         - 5 технологічних звітів майстрів для різних видів послуг (workReports)
         Усі serviceId, serviceName та appointmentId узгоджені з Проєктами №1 і №2.
   Запуск: mongosh "mongodb://localhost:27017/detailing_reviews" db/p3/seed.js
   ========================================================================= */

const targetDb = db.getSiblingDB("detailing_reviews");
const toInt = (val) => (typeof Int32 !== "undefined" ? new Int32(val) : NumberInt(val));

print("=== Початок ідемпотентного наповнення [detailing_reviews] (seed.js) ===");

/* -------------------------------------------------------------------------
   1. Теми обговорень (discussions) — 2 документи
   ------------------------------------------------------------------------- */
const discussionsData = [
    {
        _id: ObjectId("670900000000000000000101"),
        targetType: "service",
        targetId: toInt(7),
        targetName: "Керамічне покриття кузова 9H",
        title: "Догляд за авто після нанесення кераміки Gyeon Q2 Mohs EVO: досвід та поради",
        tags: ["кераміка", "9H", "гідрофоб", "зимовий-догляд"],
        isPinned: true,
        createdAt: new Date("2026-09-25T10:00:00Z")
    },
    {
        _id: ObjectId("670900000000000000000102"),
        targetType: "package",
        targetId: toInt(3),
        targetName: "Ceramic Pro",
        title: "Обговорення пакета Ceramic Pro: чи варто брати комплекс перед зимою?",
        tags: ["пакет", "ceramic-pro", "комплексний-захист"],
        isPinned: false,
        createdAt: new Date("2026-09-29T14:30:00Z")
    }
];

discussionsData.forEach((doc) => {
    targetDb.discussions.updateOne(
        { _id: doc._id },
        { $set: doc },
        { upsert: true }
    );
});
print(`[SEED] discussions: оброблено ${discussionsData.length} тем обговорень.`);

/* -------------------------------------------------------------------------
   2. Повідомлення в темах обговорень (messages) — 10 документів
   ------------------------------------------------------------------------- */
const messagesData = [
    // Гілка 1: Керамічне покриття кузова 9H (5 повідомлень)
    {
        _id: ObjectId("670900000000000000000201"),
        discussionId: ObjectId("670900000000000000000101"),
        authorName: "Олександр Коваленко",
        authorRole: "client",
        text: "Підкажіть, через скільки днів після нанесення кераміки 9H можна мити авто на звичайній мийці?",
        createdAt: new Date("2026-09-25T11:15:00Z")
    },
    {
        _id: ObjectId("670900000000000000000202"),
        discussionId: ObjectId("670900000000000000000101"),
        authorName: "Андрій (Старший майстер)",
        authorRole: "master",
        text: "Перші 10-14 днів триває повна полімеризація складу. У цей період бажано уникати лужної хімії, а першу контрольно-двофазну мийку зробити у нас у студії.",
        createdAt: new Date("2026-09-25T11:40:00Z")
    },
    {
        _id: ObjectId("670900000000000000000203"),
        discussionId: ObjectId("670900000000000000000101"),
        authorName: "Дмитро Бондаренко",
        authorRole: "client",
        text: "А якщо потрапив під сильний дощ на третій день після виїзду зі студії — це не пошкодить покриття?",
        createdAt: new Date("2026-09-26T09:10:00Z")
    },
    {
        _id: ObjectId("670900000000000000000204"),
        discussionId: ObjectId("670900000000000000000101"),
        authorName: "Андрій (Старший майстер)",
        authorRole: "master",
        text: "Ні, бо ми фінішуємо покриття складом Gyeon Q2 Cure, який захищає базовий шар від водних плям у перші тижні.",
        createdAt: new Date("2026-09-26T09:35:00Z")
    },
    {
        _id: ObjectId("670900000000000000000205"),
        discussionId: ObjectId("670900000000000000000101"),
        authorName: "Олександр Коваленко",
        authorRole: "client",
        text: "Супер, дякую! Вже зробив у вас на BMW X5, гідрофоб просто космічний.",
        createdAt: new Date("2026-10-03T18:20:00Z")
    },

    // Гілка 2: Пакет Ceramic Pro (5 повідомлень)
    {
        _id: ObjectId("670900000000000000000206"),
        discussionId: ObjectId("670900000000000000000102"),
        authorName: "Олена Мельник",
        authorRole: "client",
        text: "Добрий день! Скільки сумарно часу займає виконання пакета Ceramic Pro, якщо залишити авто зранку?",
        createdAt: new Date("2026-09-30T10:05:00Z")
    },
    {
        _id: ObjectId("670900000000000000000207"),
        discussionId: ObjectId("670900000000000000000102"),
        authorName: "Ірина (Адміністратор)",
        authorRole: "admin",
        text: "Вітаю, Олено! Повний цикл пакета Ceramic Pro разом із технологічною витримкою кераміки триває 2 повні робочі дні (зазвичай приймаємо авто о 09:00 і видаємо наступного дня о 18:00).",
        createdAt: new Date("2026-09-30T10:18:00Z")
    },
    {
        _id: ObjectId("670900000000000000000208"),
        discussionId: ObjectId("670900000000000000000102"),
        authorName: "Віталій Ткаченко",
        authorRole: "client",
        text: "Підкажіть, чи входить у пакет Ceramic Pro захист шкіри керма і підлокітника, чи лише сидіння?",
        createdAt: new Date("2026-10-01T15:00:00Z")
    },
    {
        _id: ObjectId("670900000000000000000209"),
        discussionId: ObjectId("670900000000000000000102"),
        authorName: "Тарас (Майстер інтер'єру)",
        authorRole: "master",
        text: "Так, у межах послуги захисту шкіри обробляються всі шкіряні елементи інтер'єру: передні й задні сидіння, карти дверей, підлокітник та обід керма.",
        createdAt: new Date("2026-10-01T15:42:00Z")
    },
    {
        _id: ObjectId("670900000000000000000210"),
        discussionId: ObjectId("670900000000000000000102"),
        authorName: "Ірина (Адміністратор)",
        authorRole: "admin",
        text: "Крім того, при замовленні пакета Ceramic Pro економія порівняно з окремими послугами становить 3 700 грн.",
        createdAt: new Date("2026-10-01T16:00:00Z")
    }
];

messagesData.forEach((doc) => {
    targetDb.messages.updateOne(
        { _id: doc._id },
        { $set: doc },
        { upsert: true }
    );
});
print(`[SEED] messages: оброблено ${messagesData.length} повідомлень.`);

/* -------------------------------------------------------------------------
   3. Відгуки клієнтів (reviews) — 9 документів з різною структурою (гнучка схема)
   Узгоджені з serviceId (1..10) та appointmentId (1..8)
   ------------------------------------------------------------------------- */
const reviewsData = [
    // Відгук 1: Повний медіа-відгук (є photosBefore, photosAfter, videoUrl, pros, cons, discussionId, comments)
    {
        _id: ObjectId("670900000000000000000301"),
        serviceId: toInt(7),
        serviceName: "Керамічне покриття кузова 9H",
        appointmentId: toInt(1),
        discussionId: ObjectId("670900000000000000000101"),
        rating: toInt(5),
        title: "Неймовірний дзеркальний блиск на чорному BMW X5!",
        text: "Робив двофазну мийку та двошарову кераміку 9H. Колір Black Sapphire став значно глибшим, вода просто злітає з капота на швидкості 50 км/год.",
        verifiedVisit: true,
        author: {
            clientId: toInt(1),
            fullName: "Олександр Коваленко",
            carInfo: "BMW X5 G05"
        },
        photosBefore: [
            "https://cdn.detailing.ua/reviews/app1-bmw-before-1.jpg",
            "https://cdn.detailing.ua/reviews/app1-bmw-before-2.jpg"
        ],
        photosAfter: [
            "https://cdn.detailing.ua/reviews/app1-bmw-after-1.jpg",
            "https://cdn.detailing.ua/reviews/app1-bmw-after-2.jpg"
        ],
        videoUrl: "https://cdn.detailing.ua/reviews/app1-bmw-hydrophobic.mp4",
        pros: [
            "Ідеальна підготовка кузова без жодної голограми",
            "Фотозвіт кожного етапу в месенджер",
            "Гарантійний сертифікат на 24 місяці"
        ],
        cons: [
            "Потрібно залишати авто на дві доби"
        ],
        comments: [
            {
                authorName: "Ірина (Адміністратор)",
                role: "admin",
                text: "Олександре, щиро дякуємо за довіру! Чекаємо на контрольний огляд через 2 тижні.",
                createdAt: new Date("2026-10-03T11:00:00Z")
            },
            {
                authorName: "Андрій (Старший майстер)",
                role: "master",
                text: "Було приємно працювати з вашим авто, чорний колір після Mohs EVO виглядає еталонно!",
                createdAt: new Date("2026-10-03T12:15:00Z")
            }
        ],
        createdAt: new Date("2026-10-03T09:30:00Z")
    },

    // Відгук 2: Мінімалістичний текстовий відгук (без фото, відео, pros/cons)
    {
        _id: ObjectId("670900000000000000000302"),
        serviceId: toInt(1),
        serviceName: "Двофазна детейлінг-мийка",
        appointmentId: toInt(1),
        rating: toInt(5),
        title: "Швидко, акуратно, жодних розводів на склі",
        text: "Регулярно заїжджаю на двофазну мийку. Продувають усі щілини, дзеркала та лючок бензобака турбосушкою — взимку нічого не примерзає.",
        verifiedVisit: true,
        author: {
            clientId: toInt(1),
            fullName: "Олександр Коваленко",
            carInfo: "BMW X5 G05"
        },
        comments: [],
        createdAt: new Date("2026-10-03T09:45:00Z")
    },

    // Відгук 3: Відгук на хімчистку салону (є photosBefore, photosAfter, pros, але без videoUrl та без cons)
    {
        _id: ObjectId("670900000000000000000303"),
        serviceId: toInt(9),
        serviceName: "Повна хімчистка салону з розбором",
        appointmentId: toInt(2),
        rating: toInt(5),
        title: "Світла шкіра Audi Q7 знову як із салону!",
        text: "На задньому дивані була застаріла пляма від кави та сліди від дитячого автокрісла. Після хімчистки з розбором салон став абсолютно чистим і матовим.",
        verifiedVisit: true,
        author: {
            clientId: toInt(2),
            fullName: "Марина Шевченко",
            carInfo: "Audi Q7"
        },
        photosBefore: [
            "https://cdn.detailing.ua/reviews/app2-audi-seat-before.jpg"
        ],
        photosAfter: [
            "https://cdn.detailing.ua/reviews/app2-audi-seat-after.jpg",
            "https://cdn.detailing.ua/reviews/app2-audi-cockpit-after.jpg"
        ],
        pros: [
            "Повністю вивели плями з перфорованої бежевої шкіри",
            "Приємний нейтральний аромат без різкої хімії"
        ],
        comments: [
            {
                authorName: "Тарас (Майстер інтер'єру)",
                role: "master",
                text: "Пані Марино, рекомендуємо раз на пів року оновлювати кондиціонер шкіри Koch Chemie Leather Star.",
                createdAt: new Date("2026-10-05T10:20:00Z")
            }
        ],
        createdAt: new Date("2026-10-05T08:50:00Z")
    },

    // Відгук 4: Відгук на озонування (з pros та cons, оцінка 4)
    {
        _id: ObjectId("670900000000000000000304"),
        serviceId: toInt(10),
        serviceName: "Озонування та антибактеріальна обробка",
        appointmentId: toInt(2),
        rating: toInt(4),
        title: "Запах вологості з кондиціонера зник повністю",
        text: "Замовляла озонування разом із хімчисткою. Клімат-контроль тепер працює без жодного стороннього запаху, хоча перші дві години в авто відчувався специфічний запах озону.",
        verifiedVisit: true,
        author: {
            clientId: toInt(2),
            fullName: "Марина Шевченко",
            carInfo: "Audi Q7"
        },
        pros: ["Швидка процедура (45 хв)", "Ефективна дезінфекція повітроводів"],
        cons: ["Потрібно добре провітрювати салон одразу після процедури"],
        comments: [],
        createdAt: new Date("2026-10-05T09:10:00Z")
    },

    // Відгук 5: Відгук на глибоке абразивне полірування з відео (є videoUrl, photosAfter, pros)
    {
        _id: ObjectId("670900000000000000000305"),
        serviceId: toInt(5),
        serviceName: "Глибоке абразивне полірування кузова",
        appointmentId: toInt(3),
        rating: toInt(5),
        title: "Прибрали 95% подряпин після траси та гілок",
        text: "Приймав роботу під інспекційним ліхтарем Scangrip — кузов Porsche Cayenne як нове дзеркало. Майстер показав заміри товщиноміром до і після.",
        verifiedVisit: true,
        author: {
            clientId: toInt(3),
            fullName: "Дмитро Бондаренко",
            carInfo: "Porsche Cayenne Coupe"
        },
        photosAfter: [
            "https://cdn.detailing.ua/reviews/app3-cayenne-polish-50-50.jpg"
        ],
        videoUrl: "https://cdn.detailing.ua/reviews/app3-cayenne-scangrip-inspection.mp4",
        pros: [
            "Делікатна робота з лаком з контролем товщини",
            "Ідеальна геометрія відблисків"
        ],
        comments: [
            {
                authorName: "Андрій (Старший майстер)",
                role: "master",
                text: "Дмитре, дякую за відгук! Зняли в середньому лише 4-5 мікрон лаку.",
                createdAt: new Date("2026-10-11T19:00:00Z")
            }
        ],
        createdAt: new Date("2026-10-11T17:45:00Z")
    },

    // Відгук 6: Відгук на полірування фар (є photosBefore та photosAfter, оцінка 5)
    {
        _id: ObjectId("670900000000000000000306"),
        serviceId: toInt(6),
        serviceName: "Полірування фар та ліхтарів",
        appointmentId: toInt(4),
        rating: toInt(5),
        title: "Фари світять удвічі яскравіше!",
        text: "На Mercedes E300 фари були вже матові від піскострую. За півтори години повернули заводську прозорість.",
        verifiedVisit: true,
        author: {
            clientId: toInt(4),
            fullName: "Віталій Ткаченко",
            carInfo: "Mercedes-Benz E300 W213"
        },
        photosBefore: ["https://cdn.detailing.ua/reviews/app4-e300-headlight-before.jpg"],
        photosAfter: ["https://cdn.detailing.ua/reviews/app4-e300-headlight-after.jpg"],
        comments: [],
        createdAt: new Date("2026-10-11T19:30:00Z")
    },

    // Відгук 7: Відгук на легке відновлювальне полірування (оцінка 4, є pros/cons)
    {
        _id: ObjectId("670900000000000000000307"),
        serviceId: toInt(4),
        serviceName: "Легке відновлювальне полірування",
        appointmentId: toInt(4),
        rating: toInt(4),
        title: "Чудовий варіант освіжити авто за 1 день",
        text: "Уся дрібна павутинка зникла, з'явився чудовий глянець. Дві глибокі подряпини на задньому крилі залишилися ледь помітними, але майстер одразу попередив, що для них потрібне глибоке полірування.",
        verifiedVisit: true,
        author: {
            clientId: toInt(4),
            fullName: "Віталій Ткаченко",
            carInfo: "Mercedes-Benz E300 W213"
        },
        pros: ["Швидке виконання за один день", "Чесна консультація майстра"],
        cons: ["Не прибирає глибокі подряпини до ґрунту"],
        comments: [],
        createdAt: new Date("2026-10-11T19:40:00Z")
    },

    // Відгук 8: Відгук на антидощ (з відео демонстрації дощу, оцінка 5)
    {
        _id: ObjectId("670900000000000000000308"),
        serviceId: toInt(8),
        serviceName: "Антидощ на всі вікна",
        appointmentId: toInt(3),
        rating: toInt(5),
        title: "На трасі під час зливи двірники взагалі не вмикав",
        text: "Після 60 км/год краплі самі розбігаються вгору по лобовому склу. Вночі немає відблисків від зустрічних фар.",
        verifiedVisit: true,
        author: {
            clientId: toInt(3),
            fullName: "Дмитро Бондаренко",
            carInfo: "Porsche Cayenne Coupe"
        },
        videoUrl: "https://cdn.detailing.ua/reviews/app3-aquapel-highway-test.mp4",
        pros: ["Комфорт у нічну зливу", "Скло менше брудниться комахами"],
        comments: [],
        createdAt: new Date("2026-10-11T20:00:00Z")
    },

    // Відгук 9: Відгук без appointmentId (verifiedVisit: false — клієнт консультувався / робив раніше)
    {
        _id: ObjectId("670900000000000000000309"),
        serviceId: toInt(7),
        serviceName: "Керамічне покриття кузова 9H",
        appointmentId: null,
        discussionId: ObjectId("670900000000000000000101"),
        rating: toInt(5),
        title: "Покриття тримається вже більше року",
        text: "Наносила кераміку на попередньому авто минулого сезону, зараз записала нову Tesla Model Y. Гідрофоб тримається чесно за умови правильних мийок.",
        verifiedVisit: false,
        author: {
            clientId: toInt(5),
            fullName: "Олена Мельник",
            carInfo: "Tesla Model Y Performance"
        },
        pros: ["Довговічність", "Легкість мийки"],
        comments: [],
        createdAt: new Date("2026-10-10T14:00:00Z")
    }
];

reviewsData.forEach((doc) => {
    targetDb.reviews.updateOne(
        { _id: doc._id },
        { $set: doc },
        { upsert: true }
    );
});
print(`[SEED] reviews: оброблено ${reviewsData.length} відгуків клієнтів.`);

/* -------------------------------------------------------------------------
   4. Технологічні звіти майстрів (workReports) — 5 документів з поліморфними полями
   ------------------------------------------------------------------------- */
const workReportsData = [
    // Звіт 1: Керамічне покриття кузова 9H (Запис #1, специфічні поля: layers, curingHours, coatingBrand, irLampTempC)
    {
        _id: ObjectId("670900000000000000000401"),
        appointmentId: toInt(1),
        serviceId: toInt(7),
        serviceName: "Керамічне покриття кузова 9H",
        masterName: "Андрій Ковальчук",
        carPlateNumber: "AA7777OO",
        // Специфічні поля для керамічного покриття
        layers: toInt(2),
        curingHours: toInt(14),
        irLampBakingTempC: toInt(60),
        hydrophobicContactAngleDeg: toInt(112),
        checklist: [
            { step: "Знежирення кузова складом Gyeon Q2M Prep (двічі)", done: true },
            { step: "Нанесення 1-го базового шару Gyeon Q2 Mohs EVO", done: true },
            { step: "Нанесення 2-го фінішного шару Gyeon Q2 Mohs EVO через 2 години", done: true },
            { step: "Сушіння інфрачервоною лампою та консервація Q2 Cure", done: true, notes: "Витримано в сухому боксі 14 годин при вологості 45%" }
        ],
        materialsUsed: [
            { name: "Gyeon Q2 Mohs EVO", amount: 35, unit: "ml" },
            { name: "Gyeon Q2M Prep", amount: 250, unit: "ml" },
            { name: "Gyeon Q2M Cure", amount: 40, unit: "ml" },
            { name: "Аплікатори замшеві Gyeon", amount: 4, unit: "pcs" }
        ],
        completedAt: new Date("2026-10-02T16:20:00Z")
    },

    // Звіт 2: Повна хімчистка салону з розбором (Запис #2, специфічні поля: stainsRemoved, dryingTimeHours, seatsDismantled, leatherMoisturePercent)
    {
        _id: ObjectId("670900000000000000000402"),
        appointmentId: toInt(2),
        serviceId: toInt(9),
        serviceName: "Повна хімчистка салону з розбором",
        masterName: "Тарас Мельничук",
        carPlateNumber: "KA2450CH",
        // Специфічні поля для хімчистки салону
        stainsRemoved: toInt(7),
        dryingTimeHours: toInt(6),
        seatsDismantled: true,
        leatherConditionerApplied: true,
        checklist: [
            { step: "Демонтаж передніх сидінь та дивана другого ряду", done: true },
            { step: "Сухе та вологе чищення ковроліну екстрактором", done: true },
            { step: "Делікатне очищення перфорованої шкіри щіткою Pol Star", done: true, notes: "Виведено плями кави на правому задньому сидінні" },
            { step: "Просочення шкіри кондиціонером Leather Star і сушіння", done: true }
        ],
        materialsUsed: [
            { name: "Koch Chemie Pol Star", amount: 300, unit: "ml" },
            { name: "Koch Chemie Mehrzweckreiniger (MZR)", amount: 200, unit: "ml" },
            { name: "Koch Chemie Leather Star", amount: 120, unit: "ml" }
        ],
        completedAt: new Date("2026-10-04T15:30:00Z")
    },

    // Звіт 3: Озонування та антибактеріальна обробка (Запис #2, специфічні поля: ozoneConcentrationMgPerHour, ozoneCycleMin)
    {
        _id: ObjectId("670900000000000000000403"),
        appointmentId: toInt(2),
        serviceId: toInt(10),
        serviceName: "Озонування та антибактеріальна обробка",
        masterName: "Тарас Мельничук",
        carPlateNumber: "KA2450CH",
        // Специфічні поля для озонування
        ozoneGeneratorOutputMgH: toInt(10000),
        ozoneCycleMin: toInt(30),
        ventilationRecirculationMode: true,
        checklist: [
            { step: "Перевірка стану салонного фільтра", done: true },
            { step: "Цикл озонування у режимі рециркуляції клімат-контролю (30 хв)", done: true },
            { step: "Провітрювання та нейтралізація залишкового озону (15 хв)", done: true }
        ],
        materialsUsed: [
            { name: "Koch Chemie Fresh Up", amount: 50, unit: "ml" }
        ],
        completedAt: new Date("2026-10-04T16:40:00Z")
    },

    // Звіт 4: Глибоке абразивне полірування кузова (Запис #3, специфічні поля: paintThicknessBeforeMicrons, paintThicknessAfterMicrons, polishingStages)
    {
        _id: ObjectId("670900000000000000000404"),
        appointmentId: toInt(3),
        serviceId: toInt(5),
        serviceName: "Глибоке абразивне полірування кузова",
        masterName: "Андрій Ковальчук",
        carPlateNumber: "AA0911PP",
        // Специфічні поля для полірування ЛФП
        paintThicknessBeforeMicrons: toInt(132),
        paintThicknessAfterMicrons: toInt(127),
        polishingStages: toInt(3),
        glossMeterUnitsGU: toInt(96),
        checklist: [
            { step: "Замір товщини ЛФП по 14 точках кузова та маскування пластику", done: true },
            { step: "1-й етап: ріжуча паста Koch Chemie H9.02 на хутряному крузі", done: true },
            { step: "2-й етап: середньоабразивна паста Koch Chemie F6.01", done: true },
            { step: "3-й етап: антиголограмний фініш Koch Chemie M3.02", done: true }
        ],
        materialsUsed: [
            { name: "Koch Chemie Heavy Cut H9.02", amount: 90, unit: "ml" },
            { name: "Koch Chemie Fine Cut F6.01", amount: 70, unit: "ml" },
            { name: "Koch Chemie Micro Cut M3.02", amount: 60, unit: "ml" },
            { name: "Маскувальна стрічка 3M Automotive", amount: 2, unit: "rolls" }
        ],
        completedAt: new Date("2026-10-11T15:10:00Z")
    },

    // Звіт 5: Полірування фар та ліхтарів (Запис #4, специфічні поля: lightTransmittanceGainPercent, uvProtectionApplied)
    {
        _id: ObjectId("670900000000000000000405"),
        appointmentId: toInt(4),
        serviceId: toInt(6),
        serviceName: "Полірування фар та ліхтарів",
        masterName: "Тарас Мельничук",
        carPlateNumber: "BC4500HK",
        // Специфічні поля для реставрації оптики
        lightTransmittanceGainPercent: toInt(38),
        uvProtectionApplied: true,
        checklist: [
            { step: "Маскування контуру крил і бампера навколо фар", done: true },
            { step: "Делікатне шліфування мікроабразивом P2000-P3000", done: true },
            { step: "Двоетапне полірування полікарбонату та нанесення УФ-силанта", done: true }
        ],
        materialsUsed: [
            { name: "Абразивні диски Mirka Abralon P3000", amount: 2, unit: "pcs" },
            { name: "Menzerna Heavy Cut 400", amount: 25, unit: "ml" },
            { name: "Gyeon Q2 Trim (UV-захист оптики)", amount: 5, unit: "ml" }
        ],
        completedAt: new Date("2026-10-11T18:15:00Z")
    }
];

workReportsData.forEach((doc) => {
    targetDb.workReports.updateOne(
        { _id: doc._id },
        { $set: doc },
        { upsert: true }
    );
});
print(`[SEED] workReports: оброблено ${workReportsData.length} технологічних звітів майстрів.`);
print("=== Заповнення бази даних [detailing_reviews] успішно завершено ===");
