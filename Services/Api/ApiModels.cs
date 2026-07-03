using System;
using System.Text.Json.Serialization;
using SwanCode.Core.Chat.Models;

namespace SwanCode.Core.Services.Api
{
    public class ServiceInfo
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("errorCode")]
        public string? ErrorCode { get; set; }
    }

    public class ChatRequest
    {
        [JsonPropertyName("sessionId")]
        public string SessionId { get; set; } = string.Empty;

        [JsonPropertyName("provider")]
        public string Provider { get; set; } = string.Empty;

        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("promptCode")]
        public string PromptCode { get; set; } = string.Empty;

        [JsonPropertyName("framework")]
        public string Framework { get; set; } = string.Empty;

        [JsonPropertyName("ide")]
        public string Ide { get; set; } = string.Empty;

        [JsonPropertyName("version")]
        public string Version { get; set; } = string.Empty;

        [JsonPropertyName("projectText")]
        public string ProjectText { get; set; } = string.Empty;

        [JsonPropertyName("temperature")]
        public double Temperature { get; set; } = 0.7;

        [JsonPropertyName("thinking")]
        public bool Thinking { get; set; }

        /// <summary>
        /// REQ-007 (v0.59.0): none | minimal | low | medium | high. Приоритетнее thinking.
        /// omitempty → сервер идёт back-compat через thinking. Невалидное значение →
        /// сервер вернёт 400 INVALID_REASONING_EFFORT.
        /// </summary>
        [JsonPropertyName("reasoningEffort")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ReasoningEffort { get; set; }

        /// <summary>
        /// ANNOUNCE-007 (server v0.51.0): только для POST /v1/api/chat/async — сколько ms
        /// сервер держит submit ждущим быстрого ответа. Default 2500, диапазон [100, 30000].
        /// omitempty → сервер применит собственный default. На /v1/api/chat молча игнорируется.
        /// </summary>
        [JsonPropertyName("asyncWaitMs")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? AsyncWaitMs { get; set; }
    }

    // --- Async chat (ANNOUNCE-007 / server v0.51.0) ---

    public static class AsyncChatStatus
    {
        public const string Pending = "pending";
        public const string Completed = "completed";
        public const string Failed = "failed";
        public const string Abandoned = "abandoned";
    }

    /// <summary>
    /// HTTP 202 envelope от POST /v1/api/chat/async, когда AI не успел ответить за asyncWaitMs.
    /// Клиент читает ticketId и переходит к GET /v1/api/chat/{ticketId}/result.
    /// </summary>
    public class AsyncChatSubmitResponse
    {
        [JsonPropertyName("serviceInfo")]
        public ServiceInfo ServiceInfo { get; set; } = new();

        [JsonPropertyName("ticketId")]
        public string TicketId { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("sessionId")]
        public string SessionId { get; set; } = string.Empty;

        [JsonPropertyName("userMessageId")]
        public int? UserMessageId { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }
    }

    /// <summary>
    /// Ответ GET /v1/api/chat/{ticketId}/result. Разные статусы дают разный набор полей:
    /// pending → только ticketId/status/sessionId; completed → result (ChatResponse);
    /// failed/abandoned → errorCode/errorMessage (serviceInfo.success=false, но HTTP всё ещё 200).
    /// </summary>
    public class AsyncChatPollResponse
    {
        [JsonPropertyName("serviceInfo")]
        public ServiceInfo ServiceInfo { get; set; } = new();

        [JsonPropertyName("ticketId")]
        public string TicketId { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("sessionId")]
        public string SessionId { get; set; } = string.Empty;

        [JsonPropertyName("result")]
        public ChatResponse? Result { get; set; }

        [JsonPropertyName("errorCode")]
        public string? ErrorCode { get; set; }

        [JsonPropertyName("errorMessage")]
        public string? ErrorMessage { get; set; }
    }

    public class ChatResponse
    {
        [JsonPropertyName("serviceInfo")]
        public ServiceInfo ServiceInfo { get; set; } = new();

        [JsonPropertyName("sessionId")]
        public string SessionId { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;

        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        /// <summary>
        /// Человекочитаемое имя модели для UI («Claude Opus 4.6», «GPT-5»). Сервер шлёт omitempty.
        /// </summary>
        [JsonPropertyName("modelDisplayName")]
        public string ModelDisplayName { get; set; } = string.Empty;

        [JsonPropertyName("promptTokens")]
        public int PromptTokens { get; set; }

        [JsonPropertyName("completionTokens")]
        public int CompletionTokens { get; set; }

        [JsonPropertyName("totalTokens")]
        public int TotalTokens { get; set; }

        [JsonPropertyName("codeChanges")]
        public CodeChangeDto[]? CodeChanges { get; set; }

        [JsonPropertyName("executeCommands")]
        public ExecuteCommandDto[]? ExecuteCommands { get; set; }

        // v0.23.0+ generic tool_use channel (ANNOUNCE-001). Для default продукта
        // заполняется для onec_* тулов; для query_console_1c — для всех console-тулов.
        [JsonPropertyName("toolUses")]
        public ToolUseDTO[]? ToolUses { get; set; }

        // v0.59.0 reasoning (REQ-007/008). Present ⟺ в ходе реально были размышления.
        [JsonPropertyName("reasoningText")]
        public string? ReasoningText { get; set; }

        [JsonPropertyName("reasoningEffort")]
        public string? ReasoningEffort { get; set; }

        // T-000106 billing. Present ⟺ commit прошёл. Отсутствие ≠ «0».
        [JsonPropertyName("balanceCoins")]
        public decimal? BalanceCoins { get; set; }

        [JsonPropertyName("costCoins")]
        public decimal? CostCoins { get; set; }

        [JsonPropertyName("costUsd")]
        public decimal? CostUsd { get; set; }

        [JsonPropertyName("costRub")]
        public decimal? CostRub { get; set; }

        // T-000107 SERIAL id user-message на сервере — стабильный якорь для retry/attribution.
        [JsonPropertyName("userMessageId")]
        public int? UserMessageId { get; set; }

        // REQ-021 (Lensa_Query, опционально): заголовок сессии.
        [JsonPropertyName("title")]
        public string? Title { get; set; }
    }

    public class CodeChangeDto
    {
        [JsonPropertyName("file")]
        public string File { get; set; } = string.Empty;

        [JsonPropertyName("operation")]
        public string Operation { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("searchB64")]
        public string SearchB64 { get; set; } = string.Empty;

        [JsonPropertyName("replaceB64")]
        public string ReplaceB64 { get; set; } = string.Empty;

        [JsonPropertyName("toolUseId")]
        public string ToolUseId { get; set; } = string.Empty;
    }

    public class ExecuteCommandDto
    {
        [JsonPropertyName("command")]
        public string Command { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("toolUseId")]
        public string ToolUseId { get; set; } = string.Empty;
    }

    public class ModelDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("subProvider")]
        public string SubProvider { get; set; } = string.Empty;

        [JsonPropertyName("contextLength")]
        public int ContextLength { get; set; }

        // Coin-цены (ANNOUNCE-004 v0.33.0 — USD-поля promptPrice/completionPrice/priceOverridden удалены)
        [JsonPropertyName("promptPriceCoins")]
        public decimal PromptPriceCoins { get; set; }

        [JsonPropertyName("completionPriceCoins")]
        public decimal CompletionPriceCoins { get; set; }

        // Per-1M токенов для читаемости (ANNOUNCE-005 v0.40.0)
        [JsonPropertyName("promptPriceCoinsPer1M")]
        public decimal PromptPriceCoinsPer1M { get; set; }

        [JsonPropertyName("completionPriceCoinsPer1M")]
        public decimal CompletionPriceCoinsPer1M { get; set; }

        [JsonIgnore]
        public string DisplayContext => ContextLength > 0
            ? $"{ContextLength / 1000}K"
            : string.Empty;

        [JsonIgnore]
        public string DisplayPrice => PromptPriceCoinsPer1M > 0
            ? $"{PromptPriceCoinsPer1M:F0}/{CompletionPriceCoinsPer1M:F0} coins/1M"
            : string.Empty;

        public override string ToString() => !string.IsNullOrEmpty(Name) ? Name : Id;
    }

    public class ModelsResponse
    {
        [JsonPropertyName("serviceInfo")]
        public ServiceInfo ServiceInfo { get; set; } = new();

        [JsonPropertyName("provider")]
        public string Provider { get; set; } = string.Empty;

        [JsonPropertyName("models")]
        public ModelDto[] Models { get; set; } = Array.Empty<ModelDto>();
    }

    public class ErrorResponse
    {
        [JsonPropertyName("serviceInfo")]
        public ServiceInfo ServiceInfo { get; set; } = new();

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
    }

    public static class ToolFailureTypes
    {
        public const string SearchNotFound = "search_not_found";
        public const string FileNotFound = "file_not_found";
        public const string BuildFailed = "build_failed";
        public const string CommandFailed = "command_failed";
    }

    public class ChatToolRetryResponse
    {
        [JsonPropertyName("serviceInfo")]
        public ServiceInfo ServiceInfo { get; set; } = new();

        [JsonPropertyName("sessionId")]
        public string SessionId { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;

        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        /// <summary>
        /// Человекочитаемое имя модели для UI. Сервер шлёт omitempty.
        /// </summary>
        [JsonPropertyName("modelDisplayName")]
        public string ModelDisplayName { get; set; } = string.Empty;

        [JsonPropertyName("codeChanges")]
        public CodeChangeDto[]? CodeChanges { get; set; }

        [JsonPropertyName("executeCommands")]
        public ExecuteCommandDto[]? ExecuteCommands { get; set; }

        [JsonPropertyName("attemptsUsed")]
        public int AttemptsUsed { get; set; }

        [JsonPropertyName("attemptsMax")]
        public int AttemptsMax { get; set; }

        [JsonPropertyName("retryExhausted")]
        public bool RetryExhausted { get; set; }

        [JsonPropertyName("promptTokens")]
        public int PromptTokens { get; set; }

        [JsonPropertyName("completionTokens")]
        public int CompletionTokens { get; set; }

        [JsonPropertyName("totalTokens")]
        public int TotalTokens { get; set; }

        [JsonPropertyName("toolUses")]
        public ToolUseDTO[]? ToolUses { get; set; }

        [JsonPropertyName("reasoningText")]
        public string? ReasoningText { get; set; }

        [JsonPropertyName("reasoningEffort")]
        public string? ReasoningEffort { get; set; }

        [JsonPropertyName("balanceCoins")]
        public decimal? BalanceCoins { get; set; }

        [JsonPropertyName("costCoins")]
        public decimal? CostCoins { get; set; }

        [JsonPropertyName("costUsd")]
        public decimal? CostUsd { get; set; }

        [JsonPropertyName("costRub")]
        public decimal? CostRub { get; set; }
    }

    // --- User DTOs ---

    public class MeResponse
    {
        [JsonPropertyName("serviceInfo")]
        public ServiceInfo ServiceInfo { get; set; } = new();

        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("infraconnectId")]
        public string InfraConnectId { get; set; } = string.Empty;

        [JsonPropertyName("balanceCoins")]
        public decimal BalanceCoins { get; set; }

        [JsonPropertyName("balanceReservedCoins")]
        public decimal BalanceReservedCoins { get; set; }
    }

    public class UsageEntry
    {
        // provider убран с v0.40.0 (ANNOUNCE-005) — Router-канонический SwanCode всегда

        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("promptTokens")]
        public int PromptTokens { get; set; }

        [JsonPropertyName("completionTokens")]
        public int CompletionTokens { get; set; }

        [JsonPropertyName("totalTokens")]
        public int TotalTokens { get; set; }

        [JsonPropertyName("costCoins")]
        public decimal CostCoins { get; set; }

        [JsonPropertyName("createdAt")]
        public string CreatedAt { get; set; } = string.Empty;
    }

    public class UsageResponse
    {
        [JsonPropertyName("serviceInfo")]
        public ServiceInfo ServiceInfo { get; set; } = new();

        [JsonPropertyName("usage")]
        public UsageEntry[] Usage { get; set; } = Array.Empty<UsageEntry>();

        [JsonPropertyName("total")]
        public int Total { get; set; }

        [JsonPropertyName("page")]
        public int Page { get; set; }

        [JsonPropertyName("size")]
        public int Size { get; set; }
    }

    // --- Recommendations (REQ-008) ---

    public class RecommendationDto
    {
        // provider убран с v0.40.0 (ANNOUNCE-005) — Router-канонический SwanCode всегда

        [JsonPropertyName("modelId")]
        public string ModelId { get; set; } = string.Empty;

        [JsonPropertyName("modelName")]
        public string ModelName { get; set; } = string.Empty;

        [JsonPropertyName("reason")]
        public string Reason { get; set; } = string.Empty;
    }

    public class RecommendationsResponse
    {
        [JsonPropertyName("serviceInfo")]
        public ServiceInfo ServiceInfo { get; set; } = new();

        [JsonPropertyName("language")]
        public string Language { get; set; } = string.Empty;

        [JsonPropertyName("recommendations")]
        public RecommendationDto[] Recommendations { get; set; } = Array.Empty<RecommendationDto>();
    }

    // --- Tool Results (REQ-009) ---

    public static class ToolResultStatus
    {
        public const string Success = "success";
        public const string Failure = "failure";
    }

    public class ToolResult
    {
        [JsonPropertyName("toolUseId")]
        public string ToolUseId { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("failureType")]
        public string? FailureType { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("contextB64")]
        public string? ContextB64 { get; set; }
    }

    public class ChatToolResultsRequest
    {
        [JsonPropertyName("sessionId")]
        public string SessionId { get; set; } = string.Empty;

        [JsonPropertyName("results")]
        public ToolResult[] Results { get; set; } = Array.Empty<ToolResult>();
    }

    /// <summary>
    /// Новая обёртка под ApiClient.PostToolResultsAsync — с ToolResultItem envelope
    /// (SwanCode.Core.Chat.Models). Используется ChatViewModelBase. Старый
    /// ChatToolResultsRequest с ToolResult остаётся для 1С AiViewModel до T-000048.
    /// </summary>
    public class ChatToolResultsEnvelope
    {
        [JsonPropertyName("sessionId")]
        public string SessionId { get; set; } = string.Empty;

        [JsonPropertyName("results")]
        public ToolResultItem[] Results { get; set; } = Array.Empty<ToolResultItem>();
    }

    public class ApiException : Exception
    {
        public string ErrorCode { get; }

        public ApiException(string errorCode, string message) : base(message)
        {
            ErrorCode = errorCode;
        }
    }
}
