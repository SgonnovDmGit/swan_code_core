namespace SwanCode.Core.Branding
{
    /// <summary>
    /// Описывает product-specific брендинг, который ядро (Core) не знает в compile-time:
    /// человекочитаемое имя продукта, product-key для отправки на сервер (X-Product-Key),
    /// акцентный цвет UI. В v0.11.0 — только swan_code_client (1С) и swan_code_universal.
    /// </summary>
    public interface IProductBranding
    {
        /// <summary>
        /// Машинный ключ продукта (например, "swan_code_client" или "swan_code_universal").
        /// Уходит на сервер заголовком X-Product-Key.
        /// </summary>
        string ProductKey { get; }

        /// <summary>
        /// Человекочитаемое имя продукта (например, "Swan Code 1C" или "Swan Code Universal").
        /// Подставляется в заголовки окон, тостеры, диалоги ядра.
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// Акцентный цвет в формате HTML hex ("#RRGGBB"). В v0.11.0 одинаков для всех продуктов ("#0066CC").
        /// </summary>
        string AccentColorHex { get; }
    }
}
