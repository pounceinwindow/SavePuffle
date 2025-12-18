namespace GravityFalls.Shared
{
    public static class HeroInfo
    {
        public static string Emoji(HeroType hero) => hero switch
        {
            HeroType.Dipper => "🧢",
            HeroType.Mabel => "🎀",
            HeroType.Stan => "💼",
            HeroType.Soos => "🕶️",
            HeroType.Wendy => "🪓",
            _ => "👤"
        };

        public static string DisplayName(HeroType hero) => hero switch
        {
            HeroType.Dipper => "Диппер",
            HeroType.Mabel => "Мэйбл",
            HeroType.Stan => "Стэн",
            HeroType.Soos => "Зус",
            HeroType.Wendy => "Венди",
            _ => hero.ToString()
        };

        public static string AbilitySummary(HeroType hero) => hero switch
        {
            HeroType.Wendy => "Обмен дешевле: -1 😈",
            HeroType.Dipper => "Если ✨=0 и попал на ✨, получи +1✨",
            HeroType.Soos => "Не ходит по 🔴 стрелкам",
            HeroType.Mabel => "Если 🐷 на клетке с игроком — +1 к кубику",
            HeroType.Stan => "Если 😈=0 и попал на 😈 — без эффекта",
            _ => ""
        };
    }
}
