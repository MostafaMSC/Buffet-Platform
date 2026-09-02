using BuffetDiscovery.Application.Common;
using BuffetDiscovery.Domain.Entities;
using BuffetDiscovery.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace BuffetDiscovery.Infrastructure.Persistence;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        await SeedAreasAsync(db);
        await SeedAdminAsync(db);
        await SeedSampleRestaurantsAsync(db);
    }

    private static async Task SeedAreasAsync(AppDbContext db)
    {
        if (await db.Areas.AnyAsync()) return;

        var areas = new[]
        {
            ("Karrada", "الكرادة"),
            ("Palestine Street", "شارع فلسطين"),
            ("Mansour", "المنصور"),
            ("Jadriya", "الجادرية"),
            ("Zayouna", "زيونة"),
            ("Harthiya", "الحارثية"),
            ("Adhamiyah", "الأعظمية"),
            ("Kadhimiya", "الكاظمية"),
            ("Dora", "الدورة"),
            ("Ghazaliya", "الغزالية"),
            ("Yarmouk", "اليرموك"),
            ("Saadoun", "السعدون"),
            ("Zawraa", "الزوراء"),
            ("New Baghdad", "بغداد الجديدة"),
            ("Sadr City", "مدينة الصدر")
        };

        var i = 0;
        foreach (var (en, ar) in areas)
        {
            db.Areas.Add(new Area { NameEn = en, NameAr = ar, SortOrder = i++ });
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

    private static async Task SeedSampleRestaurantsAsync(AppDbContext db)
    {
        if (await db.Restaurants.AnyAsync()) return;

        var areas = await db.Areas.ToDictionaryAsync(a => a.NameEn, a => a.Id);
        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(3));

        var samples = new List<(string Name, string NameAr, string Area, string Phone, string Desc, string DescAr, List<SampleOffering> Offerings)>
        {
            ("Al-Rasheed Terrace", "شرفة الرشيد", "Karrada", "07711111111",
                "Riverside dining with a daily breakfast and lunch buffet.", "إطلالة على النهر مع بوفيه إفطار وغداء يومي",
                [
                    new(MealType.Breakfast, 15000, "07:00", "10:30", RecurrenceType.Daily),
                    new(MealType.Lunch, 25000, "13:00", "16:00", RecurrenceType.Daily)
                ]),
            ("Palestine Grand Buffet", "بوفيه فلسطين الكبير", "Palestine Street", "07722222222",
                "Wide spread of grilled meats and local dishes for lunch.", "تشكيلة واسعة من المشاوي والأطباق المحلية للغداء",
                [
                    new(MealType.Lunch, 20000, "12:30", "16:00", RecurrenceType.SpecificWeekdays, Weekdays: ["Friday", "Saturday"])
                ]),
            ("Mansour Garden Hall", "قاعة حديقة المنصور", "Mansour", "07733333333",
                "Elegant hall known for its Ramadan iftar tables.", "قاعة أنيقة معروفة بموائد الإفطار الرمضانية",
                [
                    new(MealType.Iftar, 30000, "18:30", "21:00", RecurrenceType.RamadanMode, RamadanStart: today, RamadanEnd: today.AddDays(29)),
                    new(MealType.Sohor, 18000, "01:00", "03:30", RecurrenceType.RamadanMode, RamadanStart: today, RamadanEnd: today.AddDays(29))
                ]),
            ("Jadriya Nile View", "إطلالة الجادرية على النهر", "Jadriya", "07744444444",
                "Breakfast buffet with fresh bread baked on site.", "بوفيه إفطار مع خبز طازج يخبز في الموقع",
                [
                    new(MealType.Breakfast, 12000, "07:30", "11:00", RecurrenceType.Daily)
                ]),
            ("Zayouna Family Restaurant", "مطعم زيونة العائلي", "Zayouna", "07755555555",
                "Family-friendly lunch buffet, popular on weekends.", "بوفيه غداء مناسب للعوائل، رائج في عطلة نهاية الأسبوع",
                [
                    new(MealType.Lunch, 18000, "13:00", "17:00", RecurrenceType.SpecificWeekdays, Weekdays: ["Thursday", "Friday", "Saturday"])
                ]),
            ("Harthiya Morning Table", "مائدة الحارثية الصباحية", "Harthiya", "07766666666",
                "Traditional Iraqi breakfast spread.", "مائدة فطور عراقي تقليدي",
                [
                    new(MealType.Breakfast, 10000, "06:30", "10:00", RecurrenceType.Daily)
                ]),
            ("Adhamiyah Iftar House", "بيت إفطار الأعظمية", "Adhamiyah", "07777777777",
                "Community-style iftar tables during Ramadan.", "موائد إفطار على الطراز المجتمعي خلال رمضان",
                [
                    new(MealType.Iftar, 22000, "18:30", "20:30", RecurrenceType.RamadanMode, RamadanStart: today, RamadanEnd: today.AddDays(29))
                ]),
            ("Dora Sunset Buffet", "بوفيه غروب الدورة", "Dora", "07788888888",
                "Special one-off lunch buffet for a local event.", "بوفيه غداء لمرة واحدة لمناسبة محلية",
                [
                    new(MealType.Lunch, 17000, "12:00", "15:00", RecurrenceType.OneOff, OneOff: today.AddDays(1))
                ]),
            ("Saadoun Downtown Diner", "مطعم السعدون وسط المدينة", "Saadoun", "07799999999",
                "Quick and affordable breakfast and lunch buffets.", "بوفيهات إفطار وغداء سريعة وبأسعار معقولة",
                [
                    new(MealType.Breakfast, 9000, "07:00", "10:00", RecurrenceType.Daily),
                    new(MealType.Lunch, 15000, "12:30", "15:30", RecurrenceType.Daily)
                ]),
        };

        foreach (var s in samples)
        {
            if (!areas.TryGetValue(s.Area, out var areaId)) continue;

            var restaurant = new Restaurant
            {
                Name = s.Name,
                NameAr = s.NameAr,
                AreaId = areaId,
                PhoneNumber = s.Phone,
                Address = $"{s.Area}, Baghdad",
                GoogleMapsUrl = "https://maps.google.com/?q=" + Uri.EscapeDataString($"{s.Name} {s.Area} Baghdad"),
                Description = s.Desc,
                DescriptionAr = s.DescAr,
                LogoUrl = null,
                CoverPhotoUrl = $"https://picsum.photos/seed/{Uri.EscapeDataString(s.Name)}/800/500",
                Status = RestaurantStatus.Approved,
                CreatedAt = DateTime.UtcNow
            };

            foreach (var o in s.Offerings)
            {
                var offering = new BuffetOffering
                {
                    MealType = o.MealType,
                    Price = o.Price,
                    OpensAt = TimeOnly.Parse(o.OpensAt),
                    ClosesAt = TimeOnly.Parse(o.ClosesAt),
                    Recurrence = o.Recurrence,
                    Weekdays = WeekdayMapper.ToFlags(o.Weekdays),
                    RamadanStartDate = o.RamadanStart,
                    RamadanEndDate = o.RamadanEnd,
                    OneOffDate = o.OneOff,
                    Description = $"{o.MealType} buffet at {s.Name}",
                    DescriptionAr = $"بوفيه {s.Name}",
                    Photos =
                    [
                        new OfferingPhoto { Url = $"https://picsum.photos/seed/{Uri.EscapeDataString(s.Name + o.MealType)}1/600/400", SortOrder = 0 },
                        new OfferingPhoto { Url = $"https://picsum.photos/seed/{Uri.EscapeDataString(s.Name + o.MealType)}2/600/400", SortOrder = 1 }
                    ]
                };

                restaurant.Offerings.Add(offering);
            }

            db.Restaurants.Add(restaurant);
        }

        await db.SaveChangesAsync();

        // Materialize today/tomorrow availability so the browse page has data immediately.
        var offerings = await db.Offerings.ToListAsync();
        foreach (var offering in offerings)
        {
            for (var d = today; d <= today.AddDays(1); d = d.AddDays(1))
            {
                var isActive = RecurrenceEvaluator.MatchesRecurrence(offering, d);

                if (isActive)
                {
                    db.AvailabilityStatuses.Add(new AvailabilityStatus { OfferingId = offering.Id, Date = d, IsActive = true });
                }
            }
        }

        await db.SaveChangesAsync();
    }

    private record SampleOffering(
        MealType MealType,
        decimal Price,
        string OpensAt,
        string ClosesAt,
        RecurrenceType Recurrence,
        List<string>? Weekdays = null,
        DateOnly? RamadanStart = null,
        DateOnly? RamadanEnd = null,
        DateOnly? OneOff = null
    );
}
