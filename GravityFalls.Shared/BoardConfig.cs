namespace GravityFalls.Shared
{
    /// <summary>
    /// Shared board layout (0..30). Both server and client use this.
    /// Positions are the same as the numeric track in the rulebook.
    /// </summary>
    public static class BoardConfig
    {
        public const int FinishLine = 30;

        /// <summary>
        /// Tile type for each position.
        /// This layout is "closer to the PDF" (more special cells than the initial prototype).
        /// You can tweak positions here without touching server/client code.
        /// </summary>
        public static readonly TileType[] Map = new TileType[FinishLine + 1];

        /// <summary>
        /// Arrow jumps: when you stop on a cell that starts an arrow, you immediately move by the delta.
        /// </summary>
        public static readonly Dictionary<int, int> ArrowDeltaByPos = new();

        static BoardConfig()
        {
            for (int i = 0; i <= FinishLine; i++) Map[i] = TileType.Empty;

            Map[0] = TileType.Start;
            Map[FinishLine] = TileType.Finish;

            // --- Arrows (example values; feel free to tweak) ---
            SetArrow(5, TileType.ArrowBlue, +2);
            SetArrow(12, TileType.ArrowRed, -2);
            SetArrow(18, TileType.ArrowBlue, +3);
            SetArrow(24, TileType.ArrowRed, -3);

            // --- Tokens / events ---
            Map[2] = TileType.Help;
            Map[8] = TileType.Mischief;
            Map[10] = TileType.Help;
            Map[14] = TileType.Mischief;
            Map[16] = TileType.Help;
            Map[20] = TileType.Mischief;
            Map[22] = TileType.Exchange;
            Map[26] = TileType.DiscardHelp;

            // --- Turn modifiers ---
            Map[7] = TileType.ExtraTurn;
            Map[15] = TileType.SkipTurn;

            // --- Signposts / totem (spawn Waddles / "mystery") ---
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
