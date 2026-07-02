using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using SwanCode.Core.Chat.Models;

namespace SwanCode.Core.Services.Api
{
    public class ApiClient
    {
        private readonly HttpClient _http;
        private string _baseUrl;

        public string BaseUrl
        {
            get => _baseUrl;
            set
            {
                // localhost / 127.0.0.1 — dev-режим на http; внешние домены — https.
                // Реальные окружения (nginx / openresty) редиректят http → https через 301, а
                // .NET HttpClient стрипает Authorization заголовок на cross-scheme redirect
                // (стандартная защита) — итог: сервер видит запрос без user-key и отвечает
                // USER_REQUIRED. Auto-upgrade убирает redirect и Authorization доходит целым.
                var raw = value.TrimEnd('/');
                var stripped = raw;
                if (stripped.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
                    stripped = stripped[7..];
                else if (stripped.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                    stripped = stripped[8..];

                var isLocal = stripped.StartsWith("localhost", StringComparison.OrdinalIgnoreCase) ||
                              stripped.StartsWith("127.0.0.1") ||
                              stripped.StartsWith("[::1]");
                _baseUrl = (isLocal ? "http://" : "https://") + stripped;
            }
        }

        public string? ProductKey { get; set; }
        public string? UserKey { get; set; }

        public ApiClient(string baseUrl)
        {
            _baseUrl = string.Empty;
            BaseUrl = baseUrl;
            // ANNOUNCE-002: server v0.25.0 has WriteTimeout=300s; client buffer above that
            _http = new HttpClient { Timeout = TimeSpan.FromSeconds(330) };
        }

        public async Task<bool> CheckHealthAsync()
        {
            try
            {
                using var request = CreateRequest(HttpMethod.Get, "/health");
                using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<ModelDto[]> GetModelsAsync(string provider)
        {
            // С v0.40.0 (ANNOUNCE-005) path-параметр {provider} игнорируется — Router-канонический SwanCode.
            // GetProvidersAsync / GetBalanceAsync удалены (T-000068): UI выбора провайдера убран,
            // баланс всегда GetMeAsync().BalanceCoins (внутренние монеты).
            using var request = CreateRequest(HttpMethod.Get, $"/v1/api/models/{Uri.EscapeDataString(provider)}");
            var result = await SendAsync<ModelsResponse>(request);
            return result.Models;
        }

        public async Task<ChatResponse> SendChatAsync(ChatRequest chatRequest)
        {
            using var request = CreateRequest(HttpMethod.Post, "/v1/api/chat");
            var json = JsonSerializer.Serialize(chatRequest);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            return await SendAsync<ChatResponse>(request);
        }

        public async Task<MeResponse> GetMeAsync()
        {
            using var request = CreateRequest(HttpMethod.Get, "/v1/api/me");
            return await SendAsync<MeResponse>(request);
        }

        public async Task<UsageResponse> GetUsageAsync(int page = 1, int size = 50)
        {
            using var request = CreateRequest(HttpMethod.Get, $"/v1/api/me/usage?page={page}&size={size}");
            return await SendAsync<UsageResponse>(request);
        }

        public async Task<RecommendationDto[]> GetRecommendationsAsync(string language)
        {
            using var request = CreateRequest(HttpMethod.Get, $"/v1/api/recommendations/{Uri.EscapeDataString(language)}");
            try
            {
                var result = await SendAsync<RecommendationsResponse>(request);
                return result.Recommendations;
            }
            catch (ApiException ex) when (ex.ErrorCode == "LANGUAGE_NOT_FOUND")
            {
                return Array.Empty<RecommendationDto>();
            }
        }

        public async Task<ChatToolRetryResponse> SendChatToolResultsAsync(ChatToolResultsRequest resultsRequest)
        {
            using var request = CreateRequest(HttpMethod.Post, "/v1/api/chat/tool-results");
            var json = JsonSerializer.Serialize(resultsRequest);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            return await SendAsync<ChatToolRetryResponse>(request);
        }

        /// <summary>
        /// Отправить tool-результаты (unified envelope из SwanCode.Core.Chat.Models.ToolResultItem).
        /// Используется ChatViewModelBase после DispatchToolUsesAsync. Ответ — стандартный
        /// ChatToolRetryResponse (следующий assistant-ход с возможными новыми toolUses[]).
        /// Old SendChatToolResultsAsync с legacy ToolResult остаётся для 1С AiViewModel до T-000048.
        /// </summary>
        public async Task<ChatToolRetryResponse> PostToolResultsAsync(
            string sessionId,
            IReadOnlyList<ToolResultItem> results)
        {
            using var request = CreateRequest(HttpMethod.Post, "/v1/api/chat/tool-results");
            var body = new ChatToolResultsEnvelope
            {
                SessionId = sessionId,
                Results = results is ToolResultItem[] arr ? arr : System.Linq.Enumerable.ToArray(results)
            };
            var json = JsonSerializer.Serialize(body);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            return await SendAsync<ChatToolRetryResponse>(request);
        }

        // SSE-стрим (POST /v1/api/chat/stream) отключён на сервере с ANNOUNCE-004 v0.33.0 —
        // сервер безусловно отвечает 501 STREAMING_NOT_SUPPORTED, пока Router-SSE не вернётся.
        // Клиентский метод SendChatStreamAsync (и модель SseDoneData) удалены — восстановим,
        // когда стрим появится обратно (T-000003).

        private HttpRequestMessage CreateRequest(HttpMethod method, string path)
        {
            var request = new HttpRequestMessage(method, $"{_baseUrl}{path}");
            request.Headers.Add("X-Request-ID", Guid.NewGuid().ToString());

            // Product identity (статический ключ сборки) — на всех /v1/api/* и /v1/feedback и /v1/auth/otp/*
            if (!string.IsNullOrEmpty(ProductKey))
                request.Headers.Add("X-Product-Key", ProductKey);

            // User identity — long-lived bearer-ключ. Не отправляется на OTP-эндпоинтах (юзера ещё нет).
            // Используем TryAddWithoutValidation — HeaderValidation .NET-а может тихо отбросить
            // заголовок для нестандартных user-key (генерация ключа уехала в микросервис,
            // формат теперь произвольный). AuthenticationHeaderValue ctor валидирует token68 по RFC 6750,
            // а значит символы вне [A-Za-z0-9._~+/=-] отклоняются.
            if (!string.IsNullOrEmpty(UserKey) && !path.StartsWith("/v1/auth/otp"))
                request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {UserKey}");

            return request;
        }

        private async Task<T> SendAsync<T>(HttpRequestMessage request)
        {
            HttpResponseMessage response;
            try
            {
                response = await _http.SendAsync(request);
            }
            catch (HttpRequestException ex)
            {
                throw new ApiException("CONNECTION_ERROR", ex.Message);
            }
            catch (TaskCanceledException)
            {
                throw new ApiException("TIMEOUT", "Request timed out");
            }

            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                try
                {
                    var error = JsonSerializer.Deserialize<ErrorResponse>(body);
                    if (error?.ServiceInfo != null)
                        throw new ApiException(
                            error.ServiceInfo.ErrorCode ?? "UNKNOWN",
                            error.Message ?? response.ReasonPhrase ?? "Unknown error");
                }
                catch (JsonException) { }

                throw new ApiException(
                    $"HTTP_{(int)response.StatusCode}",
                    response.ReasonPhrase ?? "Unknown error");
            }

            var result = JsonSerializer.Deserialize<T>(body);
            if (result == null)
                throw new ApiException("PARSE_ERROR", "Failed to parse response");

            return result;
        }
    }
}
