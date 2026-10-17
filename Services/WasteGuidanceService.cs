namespace ReLoop_Technologies_Web_App.Services;

public sealed record WasteGuidanceDto(string Category, string Preparation, string AcceptedItems, string ExcludedItems);

public static class WasteGuidanceService
{
    private static readonly WasteGuidanceDto[] Guidance =
    [
        new("Paper", "Keep clean and dry; flatten boxes.", "Newspapers, office paper, cardboard, cartons.", "Greasy food packaging, tissues, wax-coated paper."),
        new("Plastic", "Rinse containers and sort by local resin rules.", "Clean bottles and rigid containers accepted by your local facility.", "Medical waste, chemical containers, and unaccepted films."),
        new("Glass", "Rinse containers and protect broken glass separately.", "Bottles and jars.", "Window panes, mirrors, ceramics, and light bulbs."),
        new("Metal", "Empty and rinse cans; keep sharp edges contained.", "Aluminium and steel food or drink cans.", "Pressurised gas cylinders, batteries, and contaminated scrap."),
        new("E-waste", "Keep devices intact and use a certified drop-off point.", "Phones, computers, cables, and small electronics.", "Do not place batteries or electronics in household recycling."),
        new("Organic", "Use a sealed compostable caddy where available.", "Fruit and vegetable scraps, coffee grounds, and garden waste.", "Plastic bags, oils, meat, or dairy unless locally accepted.")
    ];

    public static IReadOnlyList<WasteGuidanceDto> GetAll() => Guidance;
    public static WasteGuidanceDto? Get(string category) => Guidance.FirstOrDefault(item => item.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
}
