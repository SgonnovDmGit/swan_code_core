using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

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
                _baseUrl = value.TrimEnd('/');
                if (!_baseUrl.StartsWith("http"))
                    _baseUrl = "http://" + _baseUrl;
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

        public async Task<string[]> GetProvidersAsync()
        {
            using var request = CreateRequest(HttpMethod.Get, "/v1/api/providers");
            var result = await SendAsync<ProvidersResponse>(request);
            return result.Providers;
        }

        public async Task<ModelDto[]> GetModelsAsync(string provider)
        {
            using var request = CreateRequest(HttpMethod.Get, $"/v1/api/models/{Uri.EscapeDataString(provider)}");
            var result = await SendAsync<ModelsResponse>(request);
            return result.Models;
        }

        public async Task<BalanceResponse> GetBalanceAsync(string provider)
        {
            using var request = CreateRequest(HttpMethod.Get, $"/v1/api/balance/{Uri.EscapeDataString(provider)}");
            return await SendAsync<BalanceResponse>(request);
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

        public async Task SendChatStreamAsync(
            ChatRequest chatRequest,
            Action<string> onChunk,
            Action<SseDoneData> onDone,
            CancellationToken ct = default)
        {
            using var request = CreateRequest(HttpMethod.Post, "/v1/api/chat/stream");
            var json = JsonSerializer.Serialize(chatRequest);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var reader = new StreamReader(stream);

            string? eventType = null;

            while (!reader.EndOfStream && !ct.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(ct);
                if (line == null) break;

                if (line.StartsWith("event: "))
                {
                    eventType = line[7..];
                }
                else if (line.StartsWith("data: "))
                {
                    var data = line[6..];

                    if (eventType == "done")
                    {
                        var doneData = JsonSerializer.Deserialize<SseDoneData>(data) ?? new SseDoneData();
                        onDone(doneData);
                    }
                    else
                    {
                        onChunk(data);
                    }
                    eventType = null;
                }
                else if (string.IsNullOrEmpty(line))
                {
                    eventType = null;
                }
            }
        }

        private HttpRequestMessage CreateRequest(HttpMethod method, string path)
        {
            var request = new HttpRequestMessage(method, $"{_baseUrl}{path}");
            request.Headers.Add("X-Request-ID", Guid.NewGuid().ToString());

            // Product identity (статический ключ сборки) — на всех /v1/api/* и /v1/feedback и /v1/auth/otp/*
            if (!string.IsNullOrEmpty(ProductKey))
                request.Headers.Add("X-Product-Key", ProductKey);

            // User identity — long-lived bearer-ключ. Не отправляется на OTP-эндпоинтах (юзера ещё нет).
            if (!string.IsNullOrEmpty(UserKey) && !path.StartsWith("/v1/auth/otp"))
                request.Headers.Add("Authorization", $"Bearer {UserKey}");

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
