using System;
using System.Text.Json.Serialization;

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

        [JsonPropertyName("apiKey")]
        public string ApiKey { get; set; } = string.Empty;

        [JsonPropertyName("thinking")]
        public bool Thinking { get; set; }
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

    public class ProvidersResponse
    {
        [JsonPropertyName("serviceInfo")]
        public ServiceInfo ServiceInfo { get; set; } = new();

        [JsonPropertyName("providers")]
        public string[] Providers { get; set; } = Array.Empty<string>();
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

        [JsonPropertyName("promptPrice")]
        public decimal PromptPrice { get; set; }

        [JsonPropertyName("completionPrice")]
        public decimal CompletionPrice { get; set; }

        [JsonIgnore]
        public string DisplayContext => ContextLength > 0
            ? $"{ContextLength / 1000}K"
            : string.Empty;

        [JsonIgnore]
        public string DisplayPrice => PromptPrice > 0
            ? $"${PromptPrice * 1_000_000:F1}/{CompletionPrice * 1_000_000:F1}"
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

    public class BalanceResponse
    {
        [JsonPropertyName("serviceInfo")]
        public ServiceInfo ServiceInfo { get; set; } = new();

        [JsonPropertyName("provider")]
        public string Provider { get; set; } = string.Empty;

        [JsonPropertyName("balance")]
        public decimal Balance { get; set; }

        [JsonPropertyName("currency")]
        public string Currency { get; set; } = string.Empty;
    }

    public class ErrorResponse
    {
        [JsonPropertyName("serviceInfo")]
        public ServiceInfo ServiceInfo { get; set; } = new();

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
    }

    public class SseDoneData
    {
        [JsonPropertyName("sessionId")]
        public string SessionId { get; set; } = string.Empty;

        [JsonPropertyName("promptTokens")]
        public int PromptTokens { get; set; }

        [JsonPropertyName("completionTokens")]
        public int CompletionTokens { get; set; }
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
        [JsonPropertyName("provider")]
        public string Provider { get; set; } = string.Empty;

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
        [JsonPropertyName("provider")]
        public string Provider { get; set; } = string.Empty;

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

    public class ApiException : Exception
    {
        public string ErrorCode { get; }

        public ApiException(string errorCode, string message) : base(message)
        {
            ErrorCode = errorCode;
        }
    }
}
