using MvcApp.Core;

namespace MvcApp.Infrastructure.Seeding.Packs;

public static class DatingInterestTags
{
    public static IEnumerable<InterestTag> Build() =>
    [
        Interest(InterestCategory.Interest, "Music"),
        Interest(InterestCategory.Interest, "Movies"),
        Interest(InterestCategory.Interest, "Travel"),
        Interest(InterestCategory.Interest, "Foodie"),
        Interest(InterestCategory.Interest, "Fitness"),
        Interest(InterestCategory.Interest, "Reading"),
        Interest(InterestCategory.Interest, "Gaming"),
        Interest(InterestCategory.Interest, "Dancing"),
        Interest(InterestCategory.Interest, "Art"),
        Interest(InterestCategory.Interest, "Photography"),
        Interest(InterestCategory.Fantasy, "Romance"),
        Interest(InterestCategory.Fantasy, "Adventure"),
        Interest(InterestCategory.Fantasy, "Roleplay"),
        Interest(InterestCategory.Fantasy, "Threesome"),
        Interest(InterestCategory.Fantasy, "BDSM"),
        Interest(InterestCategory.Fantasy, "Polyamory"),
        Interest(InterestCategory.Fantasy, "Erotic Massage"),
        Interest(InterestCategory.Fantasy, "Open Relationship"),
        Interest(InterestCategory.Hobby, "Hiking"),
        Interest(InterestCategory.Hobby, "Cooking"),
        Interest(InterestCategory.Hobby, "Yoga"),
        Interest(InterestCategory.Hobby, "Surfing"),
        Interest(InterestCategory.Hobby, "Camping"),
        Interest(InterestCategory.Hobby, "DIY"),
        Interest(InterestCategory.Hobby, "Gardening"),
        Interest(InterestCategory.Lifestyle, "Vegetarian"),
        Interest(InterestCategory.Lifestyle, "Vegan"),
        Interest(InterestCategory.Lifestyle, "420 Friendly"),
        Interest(InterestCategory.Lifestyle, "Social Drinker"),
        Interest(InterestCategory.Lifestyle, "Non-Smoker"),
        Interest(InterestCategory.Lifestyle, "Dog Lover"),
        Interest(InterestCategory.Lifestyle, "Cat Lover"),
        Interest(InterestCategory.Lifestyle, "Night Owl"),
        Interest(InterestCategory.Lifestyle, "Early Bird"),
        Interest(InterestCategory.Kink, "Submissive"),
        Interest(InterestCategory.Kink, "Dominant"),
        Interest(InterestCategory.Kink, "Switch"),
        Interest(InterestCategory.Kink, "Voyeur"),
        Interest(InterestCategory.Kink, "Exhibitionist"),
        Interest(InterestCategory.Kink, "Pet Play"),
        Interest(InterestCategory.Kink, "Bondage"),
    ];

    private static InterestTag Interest(InterestCategory category, string name) =>
        new() { Name = name, Category = category, IsCurated = true };
}

public static class DatingSubscriptionPlans
{
    public static IEnumerable<SubscriptionPlan> Build() =>
    [
        Plan(
            "Basic", "Essential VIP",
            "The essential VIP upgrade - crown badge and photo reveals.",
            "VIP Crown Badge on your profile\nSee Who Liked You - photos revealed",
            (9.99m, "1 Month Service", 1),
            (26.97m, "3 Month Service, save 10%", 3),
            (47.95m, "6 Month Service, save 20%", 6),
            (83.92m, "12 Month Service, save 30%", 12)),

        Plan(
            "Premium", "Most Popular",
            "Best value - extra Super Likes, VIP chat rooms, and photo reveals.",
            "Everything in Basic\n5 Super Likes per day\nAccess to VIP-only chat rooms",
            (14.99m, "1 Month Service", 1),
            (40.47m, "3 Month Service, save 10%", 3),
            (71.95m, "6 Month Service, save 20%", 6),
            (125.92m, "12 Month Service, save 30%", 12)),

        Plan(
            "Platinum", "Ultimate VIP",
            "The complete VIP experience with additional boosts for maximum visibility.",
            "Everything in Premium\n2 Search Boosts per day",
            (19.99m, "1 Month Service", 1),
            (53.97m, "3 Month Service, save 10%", 3),
            (95.95m, "6 Month Service, save 20%", 6),
            (167.92m, "12 Month Service, save 30%", 12)),
    ];

    private static SubscriptionPlan Plan(
        string name,
        string descriptionShort,
        string description,
        string features,
        params (decimal Price, string Description, int Months)[] durations)
    {
        var plan = new SubscriptionPlan
        {
            Name = name,
            DescriptionShort = descriptionShort,
            Description = description,
            Features = features,
        };

        foreach (var (price, durationDescription, months) in durations)
        {
            plan.SubscriptionDetails.Add(new SubscriptionDetail
            {
                Price = price,
                Description = durationDescription,
                DurationInMonths = months,
            });
        }

        return plan;
    }
}
