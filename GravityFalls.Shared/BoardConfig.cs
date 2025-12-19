namespace GravityFalls.Shared
{
    public static class BoardConfig
    {
        public const int FinishLine = 30;

        public static readonly TileType[] Map = new TileType[FinishLine + 1];

        public static readonly Dictionary<int, int> ArrowDeltaByPos = new();

        static BoardConfig()
        {
            for (int i = 0; i <= FinishLine; i++) Map[i] = TileType.Empty;

            Map[0] = TileType.Start;
            Map[FinishLine] = TileType.Finish;

            SetArrow(5, TileType.ArrowBlue, +2);
            SetArrow(12, TileType.ArrowRed, -2);
            SetArrow(18, TileType.ArrowBlue, +3);
            SetArrow(24, TileType.ArrowRed, -3);

            Map[2] = TileType.Help;
            Map[8] = TileType.Mischief;
            Map[10] = TileType.Help;
            Map[14] = TileType.Mischief;
            Map[16] = TileType.Help;
            Map[20] = TileType.Mischief;
            Map[22] = TileType.Exchange;
            Map[26] = TileType.DiscardHelp;

            Map[7] = TileType.ExtraTurn;
            Map[15] = TileType.SkipTurn;

            Map[4] = TileType.Signpost;
            Map[19] = TileType.Signpost;
            Map[28] = TileType.Totem;
        }

        private static void SetArrow(int pos, TileType type, int delta)
        {
            Map[pos] = type;
            ArrowDeltaByPos[pos] = delta;
        }

        public static TileType GetTile(int position)
        {
            if (position < 0 || position > FinishLine) return TileType.Empty;
            return Map[position];
        }
    }
}
