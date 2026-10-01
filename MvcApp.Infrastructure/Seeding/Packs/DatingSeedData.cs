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
