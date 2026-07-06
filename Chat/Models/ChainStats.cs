using System;

namespace SwanCode.Core.Chat.Models
{
    /// <summary>
    /// Агрегат одного хода AI (T-000074): контент→тул→…→контент = одна цепочка.
    /// Время — полное wall-time от отправки запроса до финального контента,
    /// включая исполнение тулов на клиенте (решение юзера от 2026-07-06).
    /// </summary>
    public class ChainStats
    {
        public int PromptTokens { get; set; }
        public int CompletionTokens { get; set; }
        public int TotalTokens => PromptTokens + CompletionTokens;
        public decimal? CostCoins { get; set; }
        public double WallSeconds { get; set; }

        /// <summary>Токены ответа / полное wall-time хода.</summary>
        public double TokensPerSecond =>
            WallSeconds > 0.1 ? CompletionTokens / WallSeconds : 0;

        public string SpeedDisplay => $"{TokensPerSecond:F1} tok/s";

        // Локаленезависимый формат: до 90с — «38 s», дальше — «2:05».
        public string WallDisplay => WallSeconds >= 90
            ? $"{(int)(WallSeconds / 60)}:{(int)(WallSeconds % 60):00}"
            : $"{WallSeconds:F0} s";
        public string CoinsDisplay => CostCoins.HasValue ? $"{CostCoins.Value:F2}" : "—";
        public bool HasCoins => CostCoins.HasValue;
    }
}
