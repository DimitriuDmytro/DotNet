/* =========================================================================
   Проєкт №3: Контекст "Відгуки та звіти" (Reviews)
   Файл: init.js
   СКБД: MongoDB, база даних: detailing_reviews
   Опис: Ідемпотентна ініціалізація колекцій з $jsonSchema-валідацією
         та створення індексів. Повторний запуск не спричиняє помилок.
   Запуск: mongosh "mongodb://localhost:27017/detailing_reviews" project3-reviews/init.js
   ========================================================================= */

const targetDb = db.getSiblingDB("detailing_reviews");
print("=== Початок ініціалізації бази даних [detailing_reviews] ===");

/**
 * Допоміжна функція для ідемпотентного створення колекції або оновлення її $jsonSchema
 */
function ensureCollectionWithValidator(collectionName, validatorSchema) {
    const existingCollections = targetDb.getCollectionNames();
    if (!existingCollections.includes(collectionName)) {
        targetDb.createCollection(collectionName, {
            validator: { $jsonSchema: validatorSchema },
            validationLevel: "strict",
            validationAction: "error"
        });
        print(`[CREATE] Колекцію '${collectionName}' створено з $jsonSchema-валідацією.`);
    } else {
        targetDb.runCommand({
            collMod: collectionName,
            validator: { $jsonSchema: validatorSchema },
            validationLevel: "strict",
            validationAction: "error"
        });
        print(`[EXISTS] Колекція '${collectionName}' вже існує — схему валідації оновлено.`);
    }
}

/* -------------------------------------------------------------------------
   1. Колекція reviews — відгуки клієнтів про послуги
   Особливості:
   - rating: ціле число (int) від 1 до 5
   - author: вкладений об'єкт (embedding)
   - comments: вкладений масив з 2-3 останніми коментарями (embedding)
   - serviceId, appointmentId: логічні ідентифікатори без FK
   - serviceName: денормалізована назва послуги з Каталогу
   - verifiedVisit: булевий знімок підтвердженого візиту з БД Записів
   - Гнучка схема: додаткові поля (photosBefore, photosAfter, videoUrl, pros, cons, discussionId) дозволені
   ------------------------------------------------------------------------- */
const reviewsSchema = {
    bsonType: "object",
    required: [
        "serviceId",
        "serviceName",
        "rating",
        "title",
        "text",
        "verifiedVisit",
        "author",
        "createdAt"
    ],
    properties: {
        _id: { bsonType: "objectId" },
        serviceId: {
            bsonType: ["int", "long"],
            minimum: 1,
            description: "Логічний ідентифікатор послуги з бази DetailingCatalog"
        },
        serviceName: {
            bsonType: "string",
            minLength: 2,
            description: "Денормалізована назва послуги (оновлюється подіями з Каталогу)"
        },
        appointmentId: {
            bsonType: ["int", "long", "null"],
            description: "Логічний ідентифікатор візиту з бази DetailingBookings (без FK)"
        },
        discussionId: {
            bsonType: ["objectId", "null"],
            description: "Посилання (reference) на тему обговорення в колекції discussions"
        },
        rating: {
            bsonType: "int",
            minimum: 1,
            maximum: 5,
            description: "Оцінка послуги: ціле число від 1 до 5"
        },
        title: { bsonType: "string", minLength: 3 },
        text: { bsonType: "string", minLength: 5 },
        verifiedVisit: {
            bsonType: "bool",
            description: "Знімок підтвердженого візиту на момент публікації відгуку"
        },
        author: {
            bsonType: "object",
            required: ["clientId", "fullName"],
            properties: {
                clientId: { bsonType: ["int", "long"] },
                fullName: { bsonType: "string" },
                carInfo: { bsonType: "string" }
            }
        },
        comments: {
            bsonType: "array",
            maxItems: 3,
            description: "Вкладений масив (до 3 останніх коментарів/відповідей)",
            items: {
                bsonType: "object",
                required: ["authorName", "role", "text", "createdAt"],
                properties: {
                    authorName: { bsonType: "string" },
                    role: { bsonType: "string", enum: ["admin", "master", "client"] },
                    text: { bsonType: "string" },
                    createdAt: { bsonType: "date" }
                }
            }
        },
        photosBefore: {
            bsonType: "array",
            items: { bsonType: "string" }
        },
        photosAfter: {
            bsonType: "array",
            items: { bsonType: "string" }
        },
        videoUrl: { bsonType: "string" },
        pros: {
            bsonType: "array",
            items: { bsonType: "string" }
        },
        cons: {
            bsonType: "array",
            items: { bsonType: "string" }
        },
        createdAt: { bsonType: "date" }
    }
};

ensureCollectionWithValidator("reviews", reviewsSchema);

/* -------------------------------------------------------------------------
   2. Колекція workReports — технологічні звіти майстрів по виконаних роботах
   Особливості:
   - вкладені масиви checklist[] та materialsUsed[]
   - поліморфні поля для різних послуг (кераміка: layers, curingHours;
     хімчистка: stainsRemoved, dryingTimeHours; полірування: paintThicknessBeforeMicrons тощо)
   ------------------------------------------------------------------------- */
const workReportsSchema = {
    bsonType: "object",
    required: [
        "appointmentId",
        "serviceId",
        "serviceName",
        "masterName",
        "checklist",
        "materialsUsed",
        "completedAt"
    ],
    properties: {
        _id: { bsonType: "objectId" },
        appointmentId: {
            bsonType: ["int", "long"],
            description: "Логічний ідентифікатор запису з DetailingBookings (без FK)"
        },
        serviceId: {
            bsonType: ["int", "long"],
            description: "Логічний ідентифікатор послуги з DetailingCatalog"
        },
        serviceName: {
            bsonType: "string",
            description: "Знімок назви послуги на момент складання звіту"
        },
        masterName: { bsonType: "string" },
        checklist: {
            bsonType: "array",
            minItems: 1,
            items: {
                bsonType: "object",
                required: ["step", "done"],
                properties: {
                    step: { bsonType: "string" },
                    done: { bsonType: "bool" },
                    notes: { bsonType: "string" }
                }
            }
        },
        materialsUsed: {
            bsonType: "array",
            minItems: 1,
            items: {
                bsonType: "object",
                required: ["name", "amount", "unit"],
                properties: {
                    name: { bsonType: "string" },
                    amount: { bsonType: ["int", "double", "decimal"] },
                    unit: { bsonType: "string" }
                }
            }
        },
        completedAt: { bsonType: "date" }
    }
};

ensureCollectionWithValidator("workReports", workReportsSchema);

/* -------------------------------------------------------------------------
   3. Колекція discussions — теми обговорень послуг або пакетів
   ------------------------------------------------------------------------- */
const discussionsSchema = {
    bsonType: "object",
    required: ["targetType", "targetId", "targetName", "title", "createdAt"],
    properties: {
        _id: { bsonType: "objectId" },
        targetType: {
            bsonType: "string",
            enum: ["service", "package"],
            description: "Тип об'єкта обговорення: окрема послуга чи комплексний пакет"
        },
        targetId: {
            bsonType: ["int", "long"],
            description: "Ідентифікатор послуги або пакета з бази DetailingCatalog"
        },
        targetName: { bsonType: "string" },
        title: { bsonType: "string", minLength: 5 },
        tags: {
            bsonType: "array",
            items: { bsonType: "string" }
        },
        isPinned: { bsonType: "bool" },
        createdAt: { bsonType: "date" }
    }
};

ensureCollectionWithValidator("discussions", discussionsSchema);

/* -------------------------------------------------------------------------
   4. Колекція messages — повідомлення в обговореннях (окрема колекція з reference
      discussionId, оскільки кількість повідомлень може бути необмеженою)
   ------------------------------------------------------------------------- */
const messagesSchema = {
    bsonType: "object",
    required: ["discussionId", "authorName", "authorRole", "text", "createdAt"],
    properties: {
        _id: { bsonType: "objectId" },
        discussionId: {
            bsonType: "objectId",
            description: "Посилання (reference) на _id теми у колекції discussions"
        },
        authorName: { bsonType: "string" },
        authorRole: {
            bsonType: "string",
            enum: ["client", "master", "admin"]
        },
        text: { bsonType: "string", minLength: 1 },
        createdAt: { bsonType: "date" }
    }
};

ensureCollectionWithValidator("messages", messagesSchema);

/* -------------------------------------------------------------------------
   5. Створення індексів (createIndex у MongoDB є ідемпотентною операцією)
   ------------------------------------------------------------------------- */
// Індекс для вибірки та сортування відгуків по конкретній послузі за спаданням оцінки
targetDb.reviews.createIndex(
    { serviceId: 1, rating: -1 },
    { name: "ix_reviews_serviceId_rating" }
);

// Індекс для швидкого пошуку відгуків по конкретному візиту (appointmentId)
targetDb.reviews.createIndex(
    { appointmentId: 1 },
    { name: "ix_reviews_appointmentId" }
);

// Індекс для хронологічної вибірки повідомлень у межах теми обговорення
targetDb.messages.createIndex(
    { discussionId: 1, createdAt: 1 },
    { name: "ix_messages_discussionId_createdAt" }
);

// Індекс для пошуку звітів майстра по конкретному запису (appointmentId)
targetDb.workReports.createIndex(
    { appointmentId: 1 },
    { name: "ix_workReports_appointmentId" }
);

print("[INDEXES] Усі індекси успішно створено/перевірено.");
print("=== Ініціалізацію бази даних [detailing_reviews] завершено ===");
