using BuffetDiscovery.Application.Common;
using BuffetDiscovery.Domain.Entities;
using BuffetDiscovery.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace BuffetDiscovery.Infrastructure.Persistence;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        await SeedLocationsAsync(db);
        await SeedAdminAsync(db);
        await SeedRestaurantsAndServicesAsync(db);
        await SeedRestaurantOwnersAsync(db);
        await SeedBookingSampleDataAsync(db);
    }

    /// Stock food/buffet photography, served straight from the frontend's own public/
    /// folder — no external host, so nothing here depends on outbound internet access or
    /// the API being up. Picked deterministically per seed string so the same
    /// restaurant/service always gets the same photo across reseeds.
    ///
    /// Only the clean (unwatermarked) files from the batch are listed — the "-2048x2048"
    /// sized ones in that drop all carry a visible "gettyimages / Credit: ..." watermark
    /// (unlicensed comp downloads), so they're excluded rather than shipped in the app.
    private static readonly string[] PhotoPool =
    [
        "catering-buffet.jpg",
        "gettyimages-123063989-612x612.jpg",
        "gettyimages-1438809400-612x612.jpg",
        "gettyimages-1441053227-612x612.jpg",
        "gettyimages-1441333698-612x612.jpg",
        "gettyimages-1441334460-612x612.jpg",
        "gettyimages-1492861210-612x612.jpg",
        "gettyimages-155033848-612x612.jpg",
        "gettyimages-175506580-612x612.jpg",
        "gettyimages-2157940277-612x612.jpg",
        "gettyimages-2163411570-612x612.jpg",
        "gettyimages-2202434182-612x612.jpg",
        "gettyimages-2231131197-612x612.jpg",
        "gettyimages-2244146867-612x612.jpg",
        "gettyimages-2254400591-612x612.jpg",
        "gettyimages-531306158-612x612.jpg",
        "gettyimages-657021234-612x612.jpg",
        "gettyimages-755656679-612x612.jpg",
        "gettyimages-80027136-612x612.jpg",
        "gettyimages-91509530-612x612.jpg",
        "luxury-plate-meal-vintage-celebration.jpg",
        "open-food-containers.jpg",
    ];

    private static string Photo(string seed, int w = 900, int h = 600)
    {
        _ = w; _ = h; // kept so call sites reading "intended size" don't need to change
        var index = Math.Abs(seed.GetHashCode()) % PhotoPool.Length;
        return $"/{PhotoPool[index]}";
    }

    // ---------------------------------------------------------------- locations

    private static async Task SeedLocationsAsync(AppDbContext db)
    {
        if (await db.Countries.AnyAsync()) return;

        var iraq = new Country { NameEn = "Iraq", NameAr = "العراق", Code = "IQ", CurrencyCode = "IQD", SortOrder = 0 };
        db.Countries.Add(iraq);
        await db.SaveChangesAsync();

        var cities = new (string En, string Ar, string Slug, double Lat, double Lng, string[] AreasEn, string[] AreasAr)[]
        {
            ("Baghdad", "بغداد", "baghdad", 33.3152, 44.3661,
                ["Karrada", "Mansour", "Jadriya", "Zayouna", "Harthiya", "Adhamiyah", "Kadhimiya", "Dora", "Palestine Street", "Saadoun", "Yarmouk", "Ghazaliya"],
                ["الكرادة", "المنصور", "الجادرية", "زيونة", "الحارثية", "الأعظمية", "الكاظمية", "الدورة", "شارع فلسطين", "السعدون", "اليرموك", "الغزالية"]),
            ("Erbil", "أربيل", "erbil", 36.1911, 44.0092,
                ["Ankawa", "Downtown", "Italian Village", "Dream City"],
                ["عنكاوا", "وسط المدينة", "القرية الإيطالية", "مدينة الأحلام"]),
            ("Basra", "البصرة", "basra", 30.5081, 47.7835,
                ["Ashar", "Jubaila", "Corniche"],
                ["العشار", "الجبيلة", "الكورنيش"]),
            ("Najaf", "النجف", "najaf", 32.0000, 44.3350,
                ["Old City", "Al-Ameer"],
                ["المدينة القديمة", "حي الأمير"]),
            ("Karbala", "كربلاء", "karbala", 32.6160, 44.0242,
                ["City Centre", "Al-Hurr"],
                ["مركز المدينة", "الحر"]),
            ("Mosul", "الموصل", "mosul", 36.3350, 43.1189,
                ["Al-Majmoua", "Left Bank"],
                ["المجموعة الثقافية", "الساحل الأيسر"]),
        };

        var citySort = 0;
        foreach (var c in cities)
        {
            var city = new City
            {
                CountryId = iraq.Id,
                NameEn = c.En,
                NameAr = c.Ar,
                Slug = c.Slug,
                Latitude = c.Lat,
                Longitude = c.Lng,
                ImageUrl = Photo($"city-{c.Slug}", 800, 600),
                SortOrder = citySort++
            };

            for (var i = 0; i < c.AreasEn.Length; i++)
            {
                city.Areas.Add(new Area
                {
                    NameEn = c.AreasEn[i],
                    NameAr = c.AreasAr[i],
                    Slug = c.AreasEn[i].ToLowerInvariant().Replace(' ', '-'),
                    SortOrder = i
                });
            }

            db.Cities.Add(city);
        }

        await db.SaveChangesAsync();
    }

    private static async Task SeedAdminAsync(AppDbContext db)
    {
        const string adminPhone = "07700000000";
        if (await db.Users.AnyAsync(u => u.PhoneNumber == adminPhone)) return;

        db.Users.Add(new User
        {
            PhoneNumber = adminPhone,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
            Role = UserRole.Admin
        });

        await db.SaveChangesAsync();
    }

    // ------------------------------------------------------ restaurant owner logins

    /// Every seeded restaurant gets an owner account so the demo data can actually be
    /// managed: the login is the restaurant's own phone number.
    private static async Task SeedRestaurantOwnersAsync(AppDbContext db)
    {
        var restaurants = await db.Restaurants.ToListAsync();
        if (restaurants.Count == 0) return;

        var taken = await db.Users.Select(u => u.PhoneNumber).ToListAsync();
        var existing = taken.ToHashSet();
        var hash = BCrypt.Net.BCrypt.HashPassword("Owner@123");

        foreach (var restaurant in restaurants)
        {
            if (!existing.Add(restaurant.PhoneNumber)) continue;

            db.Users.Add(new User
            {
                PhoneNumber = restaurant.PhoneNumber,
                PasswordHash = hash,
                Role = UserRole.RestaurantOwner,
                RestaurantId = restaurant.Id
            });
        }

        await db.SaveChangesAsync();
    }

    // ------------------------------------------------------ restaurants & services

    private record SeedMenu(string Name, string NameAr, (string En, string Ar, DietaryTags Diet)[] Items);

    private record SeedService(
        ServiceType Type,
        string Name,
        string NameAr,
        string Desc,
        string DescAr,
        MealType Meal,
        Cuisines Cuisines,
        DietaryTags Dietary,
        decimal AdultPrice,
        decimal? ChildPrice,
        string OpensAt,
        string ClosesAt,
        RecurrenceType Recurrence,
        int? Capacity,
        SeedMenu[] Menu,
        string[]? Weekdays = null,
        int? DurationMinutes = null,
        PricingModel Pricing = PricingModel.PerPerson,
        decimal? PackagePrice = null,
        int? PackageGuests = null,
        int MinGuests = 1,
        int? MaxGuests = null,
        int? FreeUnderAge = null,
        BookingMode BookingMode = BookingMode.Instant,
        (string Start, string End, int Capacity)[]? Slots = null);

    private record SeedRestaurant(
        string Name,
        string NameAr,
        string CitySlug,
        string Area,
        string Phone,
        string Desc,
        string DescAr,
        RestaurantFeatures Features,
        double Lat,
        double Lng,
        SeedService[] Services,
        (string Name, int Rating, string Comment)[] Reviews);

    private static async Task SeedRestaurantsAndServicesAsync(AppDbContext db)
    {
        if (await db.Restaurants.AnyAsync()) return;

        var areas = await db.Areas.Include(a => a.City)
            .ToDictionaryAsync(a => $"{a.City!.Slug}|{a.NameEn}", a => a.Id);

        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(3));

        foreach (var seed in SampleRestaurants())
        {
            if (!areas.TryGetValue($"{seed.CitySlug}|{seed.Area}", out var areaId)) continue;

            var restaurant = new Restaurant
            {
                Name = seed.Name,
                NameAr = seed.NameAr,
                AreaId = areaId,
                PhoneNumber = seed.Phone,
                Address = $"{seed.Area}, {char.ToUpperInvariant(seed.CitySlug[0])}{seed.CitySlug[1..]}",
                GoogleMapsUrl = "https://maps.google.com/?q=" + Uri.EscapeDataString($"{seed.Name} {seed.Area}"),
                Latitude = seed.Lat,
                Longitude = seed.Lng,
                Description = seed.Desc,
                DescriptionAr = seed.DescAr,
                Features = seed.Features,
                CoverPhotoUrl = Photo(seed.Name, 1200, 800),
                Status = RestaurantStatus.Approved
            };

            foreach (var s in seed.Services)
            {
                var service = new Service
                {
                    ServiceType = s.Type,
                    Name = s.Name,
                    NameAr = s.NameAr,
                    Description = s.Desc,
                    DescriptionAr = s.DescAr,
                    MealType = s.Meal,
                    Cuisines = s.Cuisines,
                    Dietary = s.Dietary,
                    PricingModel = s.Pricing,
                    PricePerAdult = s.AdultPrice,
                    PricePerChild = s.ChildPrice,
                    ChildAgeFrom = s.ChildPrice.HasValue ? 6 : null,
                    ChildAgeTo = s.ChildPrice.HasValue ? 12 : null,
                    FreeUnderAge = s.FreeUnderAge,
                    PackagePrice = s.PackagePrice,
                    PackageGuests = s.PackageGuests,
                    MinGuests = s.MinGuests,
                    MaxGuests = s.MaxGuests,
                    DurationMinutes = s.DurationMinutes,
                    OpensAt = TimeOnly.Parse(s.OpensAt),
                    ClosesAt = TimeOnly.Parse(s.ClosesAt),
                    Recurrence = s.Recurrence,
                    Weekdays = WeekdayMapper.ToFlags(s.Weekdays?.ToList()),
                    RamadanStartDate = s.Recurrence == RecurrenceType.RamadanMode ? today : null,
                    RamadanEndDate = s.Recurrence == RecurrenceType.RamadanMode ? today.AddDays(29) : null,
                    OneOffDate = s.Recurrence == RecurrenceType.OneOff ? today.AddDays(3) : null,
                    BookingMode = s.BookingMode,
                    Capacity = s.Slots is null ? s.Capacity : null,
                    Status = ServiceStatus.Active,
                    Photos =
                    [
                        new ServicePhoto { Url = Photo($"{seed.Name}-{s.Name}-1"), SortOrder = 0 },
                        new ServicePhoto { Url = Photo($"{seed.Name}-{s.Name}-2"), SortOrder = 1 },
                        new ServicePhoto { Url = Photo($"{seed.Name}-{s.Name}-3"), SortOrder = 2 },
                    ]
                };

                var sectionOrder = 0;
                foreach (var m in s.Menu)
                {
                    var section = new MenuSection { Name = m.Name, NameAr = m.NameAr, SortOrder = sectionOrder++ };
                    var itemOrder = 0;
                    foreach (var (en, ar, diet) in m.Items)
                    {
                        section.Items.Add(new MenuItem { Name = en, NameAr = ar, Dietary = diet, SortOrder = itemOrder++ });
                    }
                    service.MenuSections.Add(section);
                }

                if (s.Slots is not null)
                {
                    foreach (var (start, end, cap) in s.Slots)
                    {
                        service.TimeSlots.Add(new TimeSlot
                        {
                            StartTime = TimeOnly.Parse(start),
                            EndTime = TimeOnly.Parse(end),
                            Capacity = cap
                        });
                    }
                }

                restaurant.Services.Add(service);
            }

            var reviewDay = 1;
            foreach (var (name, rating, comment) in seed.Reviews)
            {
                restaurant.Reviews.Add(new Review
                {
                    CustomerName = name,
                    Rating = rating,
                    Comment = comment,
                    CreatedAt = DateTime.UtcNow.AddDays(-reviewDay++ * 3)
                });
            }

            db.Restaurants.Add(restaurant);
        }

        await db.SaveChangesAsync();

        // Materialize today/tomorrow availability so discovery has data immediately.
        var services = await db.Services.ToListAsync();
        foreach (var service in services)
        {
            for (var d = today; d <= today.AddDays(1); d = d.AddDays(1))
            {
                if (RecurrenceEvaluator.MatchesRecurrence(service, d))
                {
                    db.AvailabilityStatuses.Add(new AvailabilityStatus { ServiceId = service.Id, Date = d, IsActive = true });
                }
            }
        }

        await db.SaveChangesAsync();
    }

    private static SeedRestaurant[] SampleRestaurants() =>
    [
        new("Al-Rasheed Terrace", "شرفة الرشيد", "baghdad", "Karrada", "07711111111",
            "Riverside dining on the Tigris with an all-day international spread and a quiet terrace.",
            "مطعم على ضفاف دجلة مع بوفيه عالمي طوال اليوم وتراس هادئ.",
            RestaurantFeatures.FamilyFriendly | RestaurantFeatures.OutdoorSeating | RestaurantFeatures.Parking | RestaurantFeatures.PrivateRoom,
            33.3080, 44.4200,
            [
                new(ServiceType.Buffet, "International Breakfast Buffet", "بوفيه الفطور العالمي",
                    "Over 40 hot and cold dishes, an omelette station and fresh bakery, served every morning.",
                    "أكثر من 40 طبقاً حاراً وبارداً مع ركن العجة والمخبوزات الطازجة كل صباح.",
                    MealType.Breakfast, Cuisines.International | Cuisines.Arabic, DietaryTags.Halal | DietaryTags.Vegetarian,
                    15000, 8000, "07:00", "10:30", RecurrenceType.Daily, null,
                    [
                        new("Hot Dishes", "الأطباق الحارة",
                            [("Shakshuka", "شكشوكة", DietaryTags.Vegetarian), ("Grilled Halloumi", "حلوم مشوي", DietaryTags.Vegetarian), ("Foul Medames", "فول مدمس", DietaryTags.Vegan)]),
                        new("Bakery", "المخبوزات",
                            [("Fresh Samoon", "صمون طازج", DietaryTags.Vegetarian), ("Croissants", "كرواسون", DietaryTags.Vegetarian), ("Date Ma'amoul", "معمول التمر", DietaryTags.Vegan)]),
                        new("Live Stations", "الأركان الحية",
                            [("Omelette Station", "ركن العجة", DietaryTags.Vegetarian), ("Pancake Bar", "ركن البان كيك", DietaryTags.Vegetarian)]),
                    ],
                    DurationMinutes: 120, FreeUnderAge: 6,
                    Slots: [("07:00", "08:30", 60), ("08:30", "10:30", 60)]),

                new(ServiceType.Buffet, "Friday Family Buffet", "بوفيه العائلة يوم الجمعة",
                    "The full Friday table — grills, quzi, live pasta and a dessert wall, with room for big families.",
                    "مائدة الجمعة الكاملة — مشاوي وقوزي ومعكرونة حية وحائط الحلويات، مع مساحة للعوائل الكبيرة.",
                    MealType.Lunch, Cuisines.Iraqi | Cuisines.Grill | Cuisines.International, DietaryTags.Halal,
                    25000, 14000, "13:00", "17:00", RecurrenceType.Daily, null,
                    [
                        new("Appetizers", "المقبلات",
                            [("Hummus & Mutabbal", "حمص ومتبل", DietaryTags.Vegan), ("Fattoush", "فتوش", DietaryTags.Vegan), ("Stuffed Vine Leaves", "دولمة", DietaryTags.Vegetarian)]),
                        new("Main Courses", "الأطباق الرئيسية",
                            [("Quzi with Rice", "قوزي بالرز", DietaryTags.Halal), ("Mixed Grill", "مشاوي مشكلة", DietaryTags.Halal), ("Masgouf", "مسكوف", DietaryTags.Halal), ("Vegetable Biryani", "برياني خضار", DietaryTags.Vegetarian)]),
                        new("Desserts", "الحلويات",
                            [("Kunafa", "كنافة", DietaryTags.Vegetarian), ("Baklava", "بقلاوة", DietaryTags.Vegetarian), ("Seasonal Fruit", "فواكه موسمية", DietaryTags.Vegan)]),
                    ],
                    DurationMinutes: 180, FreeUnderAge: 5, MaxGuests: 20,
                    Slots: [("13:00", "15:00", 80), ("15:00", "17:00", 80)]),

                new(ServiceType.SetMenu, "Business Lunch", "غداء الأعمال",
                    "A three-course lunch served in under an hour, built for the working day.",
                    "غداء من ثلاثة أطباق يُقدم في أقل من ساعة، مصمم ليوم العمل.",
                    MealType.Lunch, Cuisines.International, DietaryTags.Halal,
                    18000, null, "12:00", "15:00", RecurrenceType.SpecificWeekdays, 40,
                    [
                        new("Starter", "المقبلات", [("Lentil Soup", "شوربة عدس", DietaryTags.Vegan), ("Caesar Salad", "سلطة قيصر", DietaryTags.Vegetarian)]),
                        new("Main", "الطبق الرئيسي", [("Grilled Chicken", "دجاج مشوي", DietaryTags.Halal), ("Beef Stroganoff", "ستروغانوف اللحم", DietaryTags.Halal), ("Mushroom Risotto", "ريزوتو الفطر", DietaryTags.Vegetarian)]),
                        new("Dessert", "الحلوى", [("Chocolate Fondant", "فوندان الشوكولاتة", DietaryTags.Vegetarian)]),
                    ],
                    Weekdays: ["Sunday", "Monday", "Tuesday", "Wednesday", "Thursday"],
                    DurationMinutes: 60, MinGuests: 1, MaxGuests: 8),
            ],
            [
                ("Ahmed K.", 5, "The Friday buffet is the best in Karrada. Quzi was excellent and the staff kept everything stocked."),
                ("Noor H.", 4, "Great variety at breakfast. Terrace fills up early so book ahead."),
                ("Sara J.", 5, "Took the whole family, kids under five ate free. Will be back."),
                ("Omar F.", 4, "Business lunch is quick and genuinely good value."),
            ]),

        new("Mansour Garden Hall", "قاعة حديقة المنصور", "baghdad", "Mansour", "07733333333",
            "An elegant garden hall known across Baghdad for its Ramadan tables and private dining rooms.",
            "قاعة حديقة أنيقة معروفة في بغداد بموائد رمضان وغرف الطعام الخاصة.",
            RestaurantFeatures.PrivateDining | RestaurantFeatures.PrivateRoom | RestaurantFeatures.Parking | RestaurantFeatures.FamilyFriendly,
            33.3120, 44.3320,
            [
                new(ServiceType.Buffet, "Ramadan Iftar Buffet", "بوفيه إفطار رمضان",
                    "A full iftar table from the call to prayer — soups, mezze, grills and Ramadan sweets.",
                    "مائدة إفطار كاملة من الأذان — شوربات ومقبلات ومشاوي وحلويات رمضانية.",
                    MealType.Iftar, Cuisines.Iraqi | Cuisines.Arabic, DietaryTags.Halal,
                    30000, 18000, "18:30", "21:00", RecurrenceType.RamadanMode, null,
                    [
                        new("To Break the Fast", "للإفطار", [("Dates & Laban", "تمر ولبن", DietaryTags.Vegetarian), ("Lentil Soup", "شوربة عدس", DietaryTags.Vegan)]),
                        new("Mezze", "المقبلات", [("Tabbouleh", "تبولة", DietaryTags.Vegan), ("Baba Ghanoush", "بابا غنوج", DietaryTags.Vegan), ("Sambousek", "سمبوسك", DietaryTags.Vegetarian)]),
                        new("Main Courses", "الأطباق الرئيسية", [("Lamb Quzi", "قوزي غنم", DietaryTags.Halal), ("Chicken Machboos", "مجبوس دجاج", DietaryTags.Halal), ("Grilled River Fish", "سمك نهري مشوي", DietaryTags.Halal)]),
                        new("Ramadan Sweets", "حلويات رمضان", [("Zalabia", "زلابية", DietaryTags.Vegetarian), ("Qatayef", "قطايف", DietaryTags.Vegetarian)]),
                    ],
                    DurationMinutes: 150, FreeUnderAge: 6,
                    Slots: [("18:30", "20:00", 120), ("20:00", "21:30", 90)]),

                new(ServiceType.SetMenu, "Romantic Dinner for Two", "عشاء رومانسي لشخصين",
                    "A four-course dinner for two in the garden, candlelit and served at your own table.",
                    "عشاء من أربعة أطباق لشخصين في الحديقة على ضوء الشموع.",
                    MealType.Dinner, Cuisines.International | Cuisines.Italian, DietaryTags.Halal,
                    0, null, "19:00", "23:00", RecurrenceType.Daily, 30,
                    [
                        new("Starter", "المقبلات", [("Caesar Salad", "سلطة قيصر", DietaryTags.Vegetarian), ("Bruschetta", "بروشيتا", DietaryTags.Vegetarian)]),
                        new("Main", "الطبق الرئيسي", [("Grilled Steak", "ستيك مشوي", DietaryTags.Halal), ("Truffle Pasta", "باستا الكمأة", DietaryTags.Vegetarian)]),
                        new("Dessert", "الحلوى", [("New York Cheesecake", "تشيز كيك", DietaryTags.Vegetarian)]),
                        new("Drinks", "المشروبات", [("Two Soft Drinks", "مشروبان غازيان", DietaryTags.Vegan), ("Arabic Coffee", "قهوة عربية", DietaryTags.Vegan)]),
                    ],
                    DurationMinutes: 120, Pricing: PricingModel.PerPackage, PackagePrice: 70000, PackageGuests: 2,
                    MinGuests: 2, MaxGuests: 2, BookingMode: BookingMode.Request),
            ],
            [
                ("Zaid A.", 5, "Booked the private room for a family iftar — service was flawless."),
                ("Layla M.", 5, "The romantic set menu is worth every dinar. Garden is beautiful at night."),
                ("Hussein T.", 4, "Excellent food, parking gets tight during Ramadan."),
            ]),

        new("Palestine Grand Buffet", "بوفيه فلسطين الكبير", "baghdad", "Palestine Street", "07722222222",
            "A weekend grill house with one of the widest lunch spreads on Palestine Street.",
            "مطعم مشاوي في عطلة نهاية الأسبوع مع أوسع بوفيه غداء في شارع فلسطين.",
            RestaurantFeatures.FamilyFriendly | RestaurantFeatures.Parking | RestaurantFeatures.KidsArea,
            33.3600, 44.4400,
            [
                new(ServiceType.Buffet, "Weekend Grill Buffet", "بوفيه المشاوي في العطلة",
                    "Charcoal grills carved to order, plus mezze, rice dishes and a kids' corner.",
                    "مشاوي على الفحم تُقطع أمامك، مع مقبلات وأطباق رز وركن للأطفال.",
                    MealType.Lunch, Cuisines.Grill | Cuisines.Iraqi, DietaryTags.Halal,
                    20000, 12000, "12:30", "16:00", RecurrenceType.SpecificWeekdays, null,
                    [
                        new("From the Grill", "من المشوى", [("Lamb Kebab", "كباب غنم", DietaryTags.Halal), ("Shish Tawook", "شيش طاووق", DietaryTags.Halal), ("Grilled Kofta", "كفتة مشوية", DietaryTags.Halal)]),
                        new("Sides", "الأطباق الجانبية", [("Timman Bagilla", "تمن باقلاء", DietaryTags.Vegetarian), ("Grilled Vegetables", "خضار مشوية", DietaryTags.Vegan)]),
                        new("Desserts", "الحلويات", [("Halawat Al-Jibn", "حلاوة الجبن", DietaryTags.Vegetarian), ("Ice Cream Bar", "ركن المثلجات", DietaryTags.Vegetarian)]),
                    ],
                    Weekdays: ["Friday", "Saturday"], DurationMinutes: 150, FreeUnderAge: 5,
                    Slots: [("12:30", "14:15", 20), ("14:15", "16:00", 20)]),
            ],
            [
                ("Mustafa R.", 4, "Grills are the highlight — everything comes off the charcoal hot."),
                ("Dina S.", 4, "Kids area kept the little ones busy while we ate."),
            ]),

        new("Ankawa Terrace", "تراس عنكاوا", "erbil", "Ankawa", "07501111111",
            "A hillside terrace in Ankawa serving a Levantine breakfast and evening set menus.",
            "تراس على التل في عنكاوا يقدم فطوراً شامياً وقوائم عشاء محددة.",
            RestaurantFeatures.OutdoorSeating | RestaurantFeatures.FamilyFriendly | RestaurantFeatures.WheelchairAccessible,
            36.2350, 43.9950,
            [
                new(ServiceType.Buffet, "Levantine Breakfast Buffet", "بوفيه الفطور الشامي",
                    "Mezze, manakish from the stone oven and mountain honey, served until noon.",
                    "مقبلات ومناقيش من الفرن الحجري وعسل الجبل، حتى الظهر.",
                    MealType.Breakfast, Cuisines.Lebanese | Cuisines.Arabic, DietaryTags.Halal | DietaryTags.Vegetarian,
                    17000, 9000, "08:00", "12:00", RecurrenceType.Daily, 70,
                    [
                        new("Mezze", "المقبلات", [("Labneh with Zaatar", "لبنة بالزعتر", DietaryTags.Vegetarian), ("Makdous", "مكدوس", DietaryTags.Vegan), ("Olives & Cheese", "زيتون وجبن", DietaryTags.Vegetarian)]),
                        new("From the Oven", "من الفرن", [("Zaatar Manakish", "منقوشة زعتر", DietaryTags.Vegan), ("Cheese Manakish", "منقوشة جبنة", DietaryTags.Vegetarian)]),
                        new("Sweet", "الحلو", [("Mountain Honey & Cream", "عسل جبلي وقشطة", DietaryTags.Vegetarian)]),
                    ],
                    DurationMinutes: 120, FreeUnderAge: 6),

                new(ServiceType.SetMenu, "Kurdish Tasting Menu", "قائمة التذوق الكردية",
                    "Five regional courses — dolma, kubba, and slow-cooked lamb from the mountains.",
                    "خمسة أطباق إقليمية — دولمة وكبة ولحم غنم مطهو ببطء من الجبال.",
                    MealType.Dinner, Cuisines.Iraqi | Cuisines.Turkish, DietaryTags.Halal,
                    28000, null, "18:00", "23:00", RecurrenceType.Daily, 45,
                    [
                        new("First Course", "الطبق الأول", [("Dolma", "دولمة", DietaryTags.Vegetarian)]),
                        new("Second Course", "الطبق الثاني", [("Kubba Mosul", "كبة موصلية", DietaryTags.Halal)]),
                        new("Main", "الطبق الرئيسي", [("Slow-cooked Lamb Shank", "موزة غنم مطهوة ببطء", DietaryTags.Halal)]),
                        new("Dessert", "الحلوى", [("Rice Pudding with Pistachio", "رز بحليب بالفستق", DietaryTags.Vegetarian)]),
                    ],
                    DurationMinutes: 120, MinGuests: 2, MaxGuests: 10),
            ],
            [
                ("Rezan B.", 5, "The tasting menu is a proper introduction to Kurdish cooking."),
                ("Sami N.", 4, "Beautiful view over Ankawa at sunset."),
                ("Hana Q.", 5, "Breakfast manakish straight from the oven — excellent."),
            ]),

        new("Basra Corniche Kitchen", "مطبخ كورنيش البصرة", "basra", "Corniche", "07801111111",
            "Seafood on the Shatt al-Arab, with a Thursday catch buffet and a family set menu.",
            "مأكولات بحرية على شط العرب، مع بوفيه صيد الخميس وقائمة عائلية.",
            RestaurantFeatures.OutdoorSeating | RestaurantFeatures.FamilyFriendly | RestaurantFeatures.Parking,
            30.5200, 47.8100,
            [
                new(ServiceType.Buffet, "Catch of the Day Buffet", "بوفيه صيد اليوم",
                    "Whatever came in that morning — grilled, fried, and cooked into stews on the spot.",
                    "ما وصل من الصيد صباحاً — مشوي ومقلي ومطهو في اليخني أمامك.",
                    MealType.Dinner, Cuisines.Seafood | Cuisines.Iraqi, DietaryTags.Halal,
                    32000, 16000, "18:00", "22:00", RecurrenceType.SpecificWeekdays, null,
                    [
                        new("From the Water", "من الماء", [("Grilled Zubaidi", "زبيدي مشوي", DietaryTags.Halal), ("Fried Shrimp", "روبيان مقلي", DietaryTags.Halal), ("Fish Tashreeb", "تشريب سمك", DietaryTags.Halal)]),
                        new("Sides", "الأطباق الجانبية", [("Saffron Rice", "رز بالزعفران", DietaryTags.Vegan), ("Grilled Tomato Salad", "سلطة طماطم مشوية", DietaryTags.Vegan)]),
                        new("Desserts", "الحلويات", [("Date Cake", "كيك التمر", DietaryTags.Vegetarian)]),
                    ],
                    Weekdays: ["Thursday", "Friday", "Saturday"], DurationMinutes: 150, FreeUnderAge: 5,
                    Slots: [("18:00", "20:00", 50), ("20:00", "22:00", 50)]),

                new(ServiceType.SetMenu, "Family Seafood Platter", "طبق المأكولات البحرية العائلي",
                    "A shared platter for four with rice, salads and bread — one price, no ordering needed.",
                    "طبق مشترك لأربعة أشخاص مع الرز والسلطات والخبز — سعر واحد بلا طلبات.",
                    MealType.Dinner, Cuisines.Seafood, DietaryTags.Halal,
                    0, null, "13:00", "23:00", RecurrenceType.Daily, 40,
                    [
                        new("The Platter", "الطبق", [("Mixed Grilled Fish", "سمك مشوي مشكل", DietaryTags.Halal), ("Shrimp Machboos", "مجبوس روبيان", DietaryTags.Halal), ("Fresh Salads", "سلطات طازجة", DietaryTags.Vegan)]),
                        new("Included", "يشمل", [("Bread Basket", "سلة خبز", DietaryTags.Vegan), ("Four Soft Drinks", "أربعة مشروبات غازية", DietaryTags.Vegan)]),
                    ],
                    DurationMinutes: 90, Pricing: PricingModel.PerPackage, PackagePrice: 95000, PackageGuests: 4,
                    MinGuests: 4, MaxGuests: 4),
            ],
            [
                ("Karim D.", 5, "Zubaidi grilled properly, which is harder to find than it should be."),
                ("Anwar L.", 4, "The family platter feeds four generously."),
            ]),

        new("Najaf Pilgrim's Table", "مائدة زائر النجف", "najaf", "Al-Ameer", "07811111111",
            "A quiet dining hall near the shrine serving traditional Iraqi lunches all week.",
            "قاعة طعام هادئة قرب الحرم تقدم غداءً عراقياً تقليدياً طوال الأسبوع.",
            RestaurantFeatures.FamilyFriendly | RestaurantFeatures.PrivateRoom | RestaurantFeatures.WheelchairAccessible,
            31.9950, 44.3300,
            [
                new(ServiceType.Buffet, "Traditional Iraqi Lunch Buffet", "بوفيه الغداء العراقي التقليدي",
                    "Tashreeb, dolma, quzi and the rest of the everyday Iraqi table.",
                    "تشريب ودولمة وقوزي وبقية المائدة العراقية اليومية.",
                    MealType.Lunch, Cuisines.Iraqi, DietaryTags.Halal,
                    14000, 7000, "12:00", "16:00", RecurrenceType.Daily, 120,
                    [
                        new("Main Courses", "الأطباق الرئيسية", [("Tashreeb", "تشريب", DietaryTags.Halal), ("Dolma", "دولمة", DietaryTags.Vegetarian), ("Quzi", "قوزي", DietaryTags.Halal), ("Bamia Stew", "مرق بامية", DietaryTags.Halal)]),
                        new("Sides", "الأطباق الجانبية", [("Timman", "تمن", DietaryTags.Vegan), ("Torshi", "طرشي", DietaryTags.Vegan)]),
                        new("Desserts", "الحلويات", [("Zarda", "زردة", DietaryTags.Vegetarian)]),
                    ],
                    DurationMinutes: 90, FreeUnderAge: 6),
            ],
            [
                ("Jaafar M.", 5, "Home cooking, honestly priced. The tashreeb is the reason to come."),
                ("Batool S.", 4, "Clean, calm and welcoming for families."),
            ]),

        new("Karbala Hospitality House", "بيت ضيافة كربلاء", "karbala", "City Centre", "07821111111",
            "A large hall built for group bookings, with buffet service through the pilgrimage seasons.",
            "قاعة كبيرة للحجوزات الجماعية مع خدمة بوفيه خلال مواسم الزيارة.",
            RestaurantFeatures.FamilyFriendly | RestaurantFeatures.PrivateDining | RestaurantFeatures.Parking,
            32.6150, 44.0300,
            [
                new(ServiceType.Buffet, "Group Iftar Buffet", "بوفيه الإفطار الجماعي",
                    "Built for large groups — one seating, one price, seats for two hundred.",
                    "مصمم للمجاميع الكبيرة — جلسة واحدة وسعر واحد ومقاعد لمئتي شخص.",
                    MealType.Iftar, Cuisines.Iraqi | Cuisines.Arabic, DietaryTags.Halal,
                    22000, 12000, "18:00", "20:30", RecurrenceType.RamadanMode, null,
                    [
                        new("Soups", "الشوربات", [("Lentil Soup", "شوربة عدس", DietaryTags.Vegan), ("Chicken Soup", "شوربة دجاج", DietaryTags.Halal)]),
                        new("Main Courses", "الأطباق الرئيسية", [("Chicken Biryani", "برياني دجاج", DietaryTags.Halal), ("Beef Tashreeb", "تشريب لحم", DietaryTags.Halal)]),
                        new("Desserts", "الحلويات", [("Kleicha", "كليجة", DietaryTags.Vegetarian)]),
                    ],
                    DurationMinutes: 120, MinGuests: 1, MaxGuests: 60, FreeUnderAge: 6,
                    Slots: [("18:00", "19:15", 200), ("19:15", "20:30", 200)]),

                new(ServiceType.SetMenu, "Pilgrim Group Menu", "قائمة مجموعة الزائرين",
                    "A fixed three-course meal priced per head for visiting groups, served on arrival.",
                    "وجبة ثابتة من ثلاثة أطباق بسعر للفرد لمجاميع الزائرين، تُقدم عند الوصول.",
                    MealType.Lunch, Cuisines.Iraqi, DietaryTags.Halal,
                    12000, 7000, "11:00", "16:00", RecurrenceType.Daily, 150,
                    [
                        new("Starter", "المقبلات", [("Soup of the Day", "شوربة اليوم", DietaryTags.Vegetarian)]),
                        new("Main", "الطبق الرئيسي", [("Chicken & Rice", "دجاج ورز", DietaryTags.Halal)]),
                        new("Dessert", "الحلوى", [("Seasonal Fruit", "فواكه موسمية", DietaryTags.Vegan)]),
                    ],
                    DurationMinutes: 60, MinGuests: 10, MaxGuests: 80, BookingMode: BookingMode.Request),
            ],
            [
                ("Ali H.", 4, "Organised a group of forty without any trouble."),
                ("Fatima Z.", 5, "They handled our whole family group and everyone ate at once."),
            ]),

        new("Zayouna Family Restaurant", "مطعم زيونة العائلي", "baghdad", "Zayouna", "07755555555",
            "A neighbourhood favourite with a weekend lunch buffet and a well-priced family set menu.",
            "مطعم الحي المفضل مع بوفيه غداء في العطلة وقائمة عائلية بسعر مناسب.",
            RestaurantFeatures.FamilyFriendly | RestaurantFeatures.KidsArea | RestaurantFeatures.Parking,
            33.3400, 44.4600,
            [
                new(ServiceType.Buffet, "Weekend Lunch Buffet", "بوفيه غداء العطلة",
                    "The neighbourhood's Thursday-to-Saturday spread — generous, familiar and quick.",
                    "بوفيه الحي من الخميس إلى السبت — وفير ومألوف وسريع.",
                    MealType.Lunch, Cuisines.Iraqi | Cuisines.International, DietaryTags.Halal,
                    18000, 10000, "13:00", "17:00", RecurrenceType.SpecificWeekdays, 90,
                    [
                        new("Appetizers", "المقبلات", [("Mixed Salads", "سلطات مشكلة", DietaryTags.Vegan), ("Hummus", "حمص", DietaryTags.Vegan)]),
                        new("Main Courses", "الأطباق الرئيسية", [("Grilled Chicken", "دجاج مشوي", DietaryTags.Halal), ("Beef Kebab", "كباب لحم", DietaryTags.Halal), ("Pasta Bake", "معكرونة بالفرن", DietaryTags.Vegetarian)]),
                        new("Desserts", "الحلويات", [("Basbousa", "بسبوسة", DietaryTags.Vegetarian)]),
                    ],
                    Weekdays: ["Thursday", "Friday", "Saturday"], DurationMinutes: 120, FreeUnderAge: 5),

                new(ServiceType.SetMenu, "Family Set Menu", "القائمة العائلية",
                    "A shared four-person meal — one starter each, two mains to share, and dessert.",
                    "وجبة مشتركة لأربعة أشخاص — مقبلات للجميع وطبقان رئيسيان وحلوى.",
                    MealType.Dinner, Cuisines.Iraqi | Cuisines.Grill, DietaryTags.Halal,
                    0, null, "18:00", "23:00", RecurrenceType.Daily, 50,
                    [
                        new("Starters", "المقبلات", [("Soup & Salad", "شوربة وسلطة", DietaryTags.Vegetarian)]),
                        new("Mains to Share", "أطباق للمشاركة", [("Mixed Grill Platter", "طبق مشاوي مشكل", DietaryTags.Halal), ("Chicken Biryani", "برياني دجاج", DietaryTags.Halal)]),
                        new("Dessert", "الحلوى", [("Kunafa to Share", "كنافة للمشاركة", DietaryTags.Vegetarian)]),
                    ],
                    DurationMinutes: 90, Pricing: PricingModel.PerPackage, PackagePrice: 60000, PackageGuests: 4,
                    MinGuests: 4, MaxGuests: 6),
            ],
            [
                ("Hiba A.", 4, "Reliable weekend lunch, and the kids area is a real one."),
                ("Yousif K.", 5, "The family set menu feeds four properly for the price of two mains elsewhere."),
                ("Maryam T.", 4, "Busy on Fridays — book a slot."),
            ]),

        new("Harthiya Morning Table", "مائدة الحارثية الصباحية", "baghdad", "Harthiya", "07766666666",
            "A small, precise breakfast room serving one thing well since 2011.",
            "غرفة فطور صغيرة ودقيقة تقدم شيئاً واحداً بإتقان منذ 2011.",
            RestaurantFeatures.WheelchairAccessible | RestaurantFeatures.Parking,
            33.3200, 44.3700,
            [
                new(ServiceType.Buffet, "Iraqi Breakfast Buffet", "بوفيه الفطور العراقي",
                    "Geymar, honey, fresh samoon and eggs cooked to order, every morning of the year.",
                    "قيمر وعسل وصمون طازج وبيض يُطهى حسب الطلب، كل صباح من أيام السنة.",
                    MealType.Breakfast, Cuisines.Iraqi, DietaryTags.Halal | DietaryTags.Vegetarian,
                    10000, 6000, "06:30", "10:00", RecurrenceType.Daily, 50,
                    [
                        new("The Table", "المائدة", [("Geymar & Honey", "قيمر وعسل", DietaryTags.Vegetarian), ("Fresh Samoon", "صمون طازج", DietaryTags.Vegan), ("Eggs to Order", "بيض حسب الطلب", DietaryTags.Vegetarian), ("Fried Kahi", "كاهي", DietaryTags.Vegetarian)]),
                        new("Drinks", "المشروبات", [("Iraqi Tea", "چاي عراقي", DietaryTags.Vegan)]),
                    ],
                    DurationMinutes: 75, FreeUnderAge: 5),
            ],
            [
                ("Salim W.", 5, "Geymar is the real thing. Get there before 8."),
                ("Rana E.", 5, "Small place, perfect breakfast."),
            ]),
    ];

    // -------------------------------------------------------------- demo bookings

    /// Demo bookings and booking settings so the restaurant dashboard, availability display
    /// and admin views all have something real to show right after seeding.
    private static async Task SeedBookingSampleDataAsync(AppDbContext db)
    {
        if (await db.Bookings.AnyAsync()) return;

        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(3));

        var restaurants = await db.Restaurants
            .Include(r => r.Services).ThenInclude(s => s.TimeSlots)
            .ToListAsync();

        var alRasheed = restaurants.FirstOrDefault(r => r.Name == "Al-Rasheed Terrace");
        var zayouna = restaurants.FirstOrDefault(r => r.Name == "Zayouna Family Restaurant");
        var palestine = restaurants.FirstOrDefault(r => r.Name == "Palestine Grand Buffet");
        if (alRasheed is null || zayouna is null || palestine is null) return;

        db.RestaurantSettings.Add(new RestaurantSettings { RestaurantId = alRasheed.Id, IsFoundingRestaurant = true, FeaturedScore = 10 });
        db.RestaurantSettings.Add(new RestaurantSettings { RestaurantId = zayouna.Id, FeaturedScore = 5 });
        db.RestaurantSettings.Add(new RestaurantSettings { RestaurantId = palestine.Id, OverbookingTolerancePercent = 10 });

        var guests = new (string Name, string Phone, int Adults, int Children)[]
        {
            ("Ahmed Khalil", "07711112222", 2, 2),
            ("Noor Hassan", "07733334444", 4, 0),
            ("Sara Jassim", "07755556666", 6, 4),
            ("Omar Fadhil", "07777778888", 2, 0),
            ("Hiba Adnan", "07799990000", 3, 1),
        };

        var i = 0;
        foreach (var service in alRasheed.Services.Concat(zayouna.Services).Where(s => s.Status == ServiceStatus.Active))
        {
            // Book each service on the next date it actually runs, so demo bookings sit on
            // days the service is really served rather than an arbitrary "today".
            var date = today;
            var guard = 0;
            while (!RecurrenceEvaluator.MatchesRecurrence(service, date) && guard++ < 14)
            {
                date = date.AddDays(1);
            }
            if (guard >= 14) continue;

            var g = guests[i % guests.Length];
            i++;

            var slot = service.TimeSlots.FirstOrDefault(s => !s.IsDeleted);

            db.Bookings.Add(new Booking
            {
                ServiceId = service.Id,
                TimeSlotId = slot?.Id,
                Date = date,
                CustomerName = g.Name,
                CustomerPhone = g.Phone,
                Adults = g.Adults,
                Children = g.Children,
                PartySize = g.Adults + g.Children,
                TotalPrice = PriceCalculator.Total(service, g.Adults, g.Children),
                Status = BookingStatus.Confirmed,
                ConfirmationCode = ConfirmationCodeGenerator.Generate(service.ServiceType)
            });
        }

        await db.SaveChangesAsync();
    }
}
