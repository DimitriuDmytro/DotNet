/* =========================================================================
   Проєкт №3: Контекст "Відгуки та звіти" (Reviews)
   Файл: queries.js
   СКБД: MongoDB, база даних: detailing_reviews
   Опис: Набір аналітичних і пошукових запитів для демонстрації можливостей
         документної моделі MongoDB:
         1. Вибірка відгуків за serviceId (використовує індекс ix_reviews_serviceId_rating).
         2. Пошук документів за наявністю специфічних полів ($exists) — гнучка схема.
         3. Пагінація відгуків (sort + skip + limit).
         4. Aggregation pipeline: середня оцінка та кількість відгуків по кожній послузі ($group).
         5. Aggregation pipeline: об'єднання тем обговорень із потоком повідомлень ($lookup).
   Запуск: mongosh "mongodb://localhost:27017/detailing_reviews" project3-reviews/queries.js
   ========================================================================= */

const targetDb = db.getSiblingDB("detailing_reviews");

/* -------------------------------------------------------------------------
   1. Пошук (find) відгуків за serviceId (Послуга #7 — Керамічне покриття кузова 9H)
   ------------------------------------------------------------------------- */
print("\n=== 1. Відгуки для послуги serviceId = 7 (Керамічне покриття кузова 9H) ===");
const service7Reviews = targetDb.reviews
    .find(
        { serviceId: 7 },
        {
            _id: 1,
            serviceId: 1,
            serviceName: 1,
            rating: 1,
            title: 1,
            verifiedVisit: 1,
            "author.fullName": 1,
            "author.carInfo": 1
        }
    )
    .sort({ rating: -1, createdAt: -1 })
    .toArray();
printjson(service7Reviews);

/* -------------------------------------------------------------------------
   2. Пошук документів з певним полем ($exists) — демонстрація гнучкої схеми
   ------------------------------------------------------------------------- */
print("\n=== 2.1. Відгуки клієнтів, які містять відеоогляд (videoUrl: { $exists: true }) ===");
const reviewsWithVideo = targetDb.reviews
    .find(
        { videoUrl: { $exists: true, $ne: null } },
        {
            _id: 1,
            serviceName: 1,
            title: 1,
            videoUrl: 1,
            photosAfter: 1
        }
    )
    .toArray();
printjson(reviewsWithVideo);

print("\n=== 2.2. Звіти майстрів зі специфічним полем для хімчистки (stainsRemoved: { $exists: true }) ===");
const dryCleanReports = targetDb.workReports
    .find(
        { stainsRemoved: { $exists: true } },
        {
            _id: 1,
            appointmentId: 1,
            serviceName: 1,
            masterName: 1,
            stainsRemoved: 1,
            dryingTimeHours: 1,
            seatsDismantled: 1
        }
    )
    .toArray();
printjson(dryCleanReports);

/* -------------------------------------------------------------------------
   3. Пагінація результатів (Сторінка 2, розмір сторінки = 3 відгуки)
   ------------------------------------------------------------------------- */
const pageNumber = 2;
const pageSize = 3;
const skipCount = (pageNumber - 1) * pageSize;

print(`\n=== 3. Пагінація відгуків: сторінка ${pageNumber} (skip=${skipCount}, limit=${pageSize}) ===`);
const pagedReviews = targetDb.reviews
    .find(
        {},
        {
            _id: 1,
            serviceName: 1,
            rating: 1,
            title: 1,
            createdAt: 1,
            "author.fullName": 1
        }
    )
    .sort({ createdAt: -1 })
    .skip(skipCount)
    .limit(pageSize)
    .toArray();
printjson(pagedReviews);

/* -------------------------------------------------------------------------
   4. Aggregation Pipeline: середня оцінка та кількість відгуків по кожній послузі ($group)
   ------------------------------------------------------------------------- */
print("\n=== 4. Агрегація: середня оцінка та кількість відгуків по кожній послузі ($group) ===");
const ratingStatsByService = targetDb.reviews.aggregate([
    {
        $group: {
            _id: "$serviceId",
            serviceName: { $first: "$serviceName" },
            reviewsCount: { $sum: 1 },
            verifiedCount: {
                $sum: { $cond: [{ $eq: ["$verifiedVisit", true] }, 1, 0] }
            },
            avgRatingRaw: { $avg: "$rating" },
            minRating: { $min: "$rating" },
            maxRating: { $max: "$rating" }
        }
    },
    {
        $project: {
            _id: 0,
            serviceId: "$_id",
            serviceName: 1,
            reviewsCount: 1,
            verifiedCount: 1,
            averageRating: { $round: ["$avgRatingRaw", 2] },
            minRating: 1,
            maxRating: 1
        }
    },
    {
        $sort: { averageRating: -1, reviewsCount: -1 }
    }
]).toArray();
printjson(ratingStatsByService);

/* -------------------------------------------------------------------------
   5. Aggregation Pipeline з $lookup: тема обговорення разом з усіма повідомленнями
   ------------------------------------------------------------------------- */
print("\n=== 5. Агрегація ($lookup): обговорення разом із прив'язаними повідомленнями з колекції messages ===");
const discussionsWithMessages = targetDb.discussions.aggregate([
    {
        $lookup: {
            from: "messages",
            localField: "_id",
            foreignField: "discussionId",
            pipeline: [
                { $sort: { createdAt: 1 } },
                {
                    $project: {
                        _id: 0,
                        authorName: 1,
                        authorRole: 1,
                        text: 1,
                        createdAt: 1
                    }
                }
            ],
            as: "threadMessages"
        }
    },
    {
        $addFields: {
            totalMessagesCount: { $size: "$threadMessages" }
        }
    },
    {
        $sort: { isPinned: -1, createdAt: -1 }
    }
]).toArray();
printjson(discussionsWithMessages);
