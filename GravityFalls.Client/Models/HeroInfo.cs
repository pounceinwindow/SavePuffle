using GravityFalls.Shared;

namespace SavePuffle.Models;

public record HeroInfo(HeroType Type, string Title, string Emoji, string Motto, string Passive);

public static class HeroCatalog
{
    public static IReadOnlyList<HeroInfo> All { get; } = new List<HeroInfo>
    {
        new(HeroType.Dipper, "Диппер", "🧢", "Любопытный исследователь",
            "+2 к движению на клетках помощи: подсказки ведут ближе к Пухле."),
        new(HeroType.Mabel, "Мэйбл", "🎀", "Хаос, но с добром",
            "Озорные клетки сдвигают лишь на 1 и не отбирают Пухлю."),
        new(HeroType.Stan, "Стэн", "💼", "Всегда в плюсе",
            "Сундуки дают +2 вперёд и могут сразу спасти Пухлю, если её нет у игроков."),
        new(HeroType.Soos, "Сус", "🛠️", "Чинит неприятности",
            "Ловушки замедляют только на 1 клетку."),
        new(HeroType.Wendy, "Венди", "🏹", "Ловкая следопытка",
            "Броски 5+ дают +1 шаг (с Пухлей всё равно максимум 3)."),
    };

    public static HeroInfo ByType(HeroType type) => All.First(h => h.Type == type);
}
