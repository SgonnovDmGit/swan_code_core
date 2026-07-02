using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using SwanCode.Core.Chat.Models;
using SwanCode.Core.Helpers;
using SwanCode.Core.Services.Api;

namespace SwanCode.Core.Chat.ViewModels
{
    /// <summary>
    /// Базовый VM чата для продуктовых наследников (1С AiViewModel, Universal AiViewModel).
    /// Держит общее: коллекцию Messages, session id, tool dispatch dictionary, команды.
    /// Наследник переопределяет BuildRequest (свой контекст: ProjectSettings, OneCSettings и т.д.)
    /// и регистрирует свои tool-handlers через RegisterToolHandler.
    /// </summary>
    public abstract class ChatViewModelBase : ViewModelBase
    {
        protected readonly ApiClient Api;

        private readonly Dictionary<string, Func<ToolUseDTO, Task<ToolResultItem>>> _toolHandlers =
            new(StringComparer.Ordinal);

        private string _inputText = string.Empty;
        private string _sessionId = string.Empty;
        private bool _isBusy;

        public ObservableCollection<ChatMessage> Messages { get; } = new();

        public string InputText
        {
            get => _inputText;
            set => SetProperty(ref _inputText, value);
        }

        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        public string SessionId
        {
            get => _sessionId;
            protected set => SetProperty(ref _sessionId, value);
        }

        public ICommand SendMessageCommand { get; }
        public ICommand NewChatCommand { get; }

        protected ChatViewModelBase(ApiClient api)
        {
            Api = api ?? throw new ArgumentNullException(nameof(api));

            SendMessageCommand = new RelayCommand(
                () => _ = SendMessageAsync(InputText, "free_form"),
                () => !string.IsNullOrWhiteSpace(InputText) && !IsBusy);

            NewChatCommand = new RelayCommand(NewChat);
        }

        // --- Tool dispatch ---------------------------------------------------

        /// <summary>
        /// Зарегистрировать handler для tool_use по имени. Продуктовые наследники
        /// вызывают это в конструкторе для legacy (apply_code_change / execute_command)
        /// и продуктовых тулов (1С: onec_*; Universal: только legacy пока).
        /// Повторная регистрация под тем же именем перекрывает предыдущую.
        /// </summary>
        protected void RegisterToolHandler(string name, Func<ToolUseDTO, Task<ToolResultItem>> handler)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentException("Tool name is required", nameof(name));
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            _toolHandlers[name] = handler;
        }

        /// <summary>
        /// Диспатчит массив tool_use'ов из ответа сервера: для каждого зовёт зарегистрированный
        /// handler, собирает ToolResultItem'ы, шлёт обратно через PostToolResultsAsync.
        /// Незарегистрированные тулы возвращают failure-результат «no handler» — сервер увидит
        /// и передаст AI, тот пробует другой подход.
        /// </summary>
        protected async Task DispatchToolUsesAsync(IReadOnlyList<ToolUseDTO> toolUses)
        {
            if (toolUses == null || toolUses.Count == 0) return;

            var results = new List<ToolResultItem>(toolUses.Count);
            foreach (var toolUse in toolUses)
            {
                ToolResultItem result;
                if (_toolHandlers.TryGetValue(toolUse.Name, out var handler))
                {
                    try
                    {
                        result = await handler(toolUse);
                        result.ToolUseId = toolUse.Id;
                        result.Name ??= toolUse.Name;
                    }
                    catch (Exception ex)
                    {
                        result = new ToolResultItem
                        {
                            ToolUseId = toolUse.Id,
                            Name = toolUse.Name,
                            Status = Models.ToolResultStatus.Failure,
                            Message = $"Handler threw: {ex.Message}"
                        };
                    }
                }
                else
                {
                    result = new ToolResultItem
                    {
                        ToolUseId = toolUse.Id,
                        Name = toolUse.Name,
                        Status = Models.ToolResultStatus.Failure,
                        Message = $"No handler registered for tool '{toolUse.Name}'"
                    };
                }
                results.Add(result);
            }

            await PostToolResultsAsync(results);
        }

        /// <summary>
        /// Отправка tool-результатов обратно на сервер. Реализуется в T-000047
        /// (ApiClient.PostToolResultsAsync). Пока — no-op, чтобы наследник компилировался.
        /// </summary>
        protected virtual Task PostToolResultsAsync(IReadOnlyList<ToolResultItem> results)
        {
            return Task.CompletedTask;
        }

        // --- Send / receive pipeline ----------------------------------------

        /// <summary>
        /// Собрать ChatRequest с продуктовым контекстом (ProjectSettings, framework, ide и т.д.).
        /// Каждый наследник знает свой контекст — базе не место.
        /// </summary>
        protected abstract ChatRequest BuildRequest(string message, string promptCode);

        /// <summary>
        /// Публичный API для наследника — отправить сообщение с промпт-кодом.
        /// Добавляет user-message в коллекцию, ставит IsBusy, зовёт API, обрабатывает ответ.
        /// </summary>
        public async Task SendMessageAsync(string message, string promptCode = "free_form")
        {
            if (string.IsNullOrWhiteSpace(message) || IsBusy) return;

            Messages.Add(new ChatMessage { Role = MessageRoles.User, Content = message });
            InputText = string.Empty;
            IsBusy = true;

            try
            {
                var request = BuildRequest(message, promptCode);
                var response = await Api.SendChatAsync(request);

                if (!string.IsNullOrEmpty(response.SessionId))
                    SessionId = response.SessionId;

                await HandleChatResponseAsync(response);
            }
            catch (ApiException ex)
            {
                await OnApiExceptionAsync(ex);
            }
            catch (Exception ex)
            {
                await OnUnexpectedExceptionAsync(ex);
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Обработка ответа /chat: добавить assistant-message, диспатчить toolUses[],
        /// hook на legacy-каналы (наследник обрабатывает codeChanges/executeCommands).
        /// Виртуальный — наследник может переопределить если нужен полный контроль
        /// (например, переиспользование thinking-bubble вместо новой строки).
        /// </summary>
        protected virtual async Task HandleChatResponseAsync(ChatResponse response)
        {
            var msg = new ChatMessage
            {
                Role = MessageRoles.Assistant,
                Content = response.Content,
                ModelName = !string.IsNullOrEmpty(response.ModelDisplayName)
                    ? response.ModelDisplayName
                    : response.Model,
                PromptTokens = response.PromptTokens,
                CompletionTokens = response.CompletionTokens,
                TotalTokens = response.TotalTokens,
                HasCodeChanges = response.CodeChanges is { Length: > 0 }
            };
            Messages.Add(msg);

            await ProcessLegacyChannelsAsync(response);
            // Диспатч response.ToolUses будет включён в T-000047 после расширения ApiModels
        }

        /// <summary>
        /// Hook для наследника — обработка legacy-каналов ChatResponse:
        /// codeChanges[] и executeCommands[]. Наследник переопределяет и собирает
        /// ToolResultItem[] для tool-retry цикла.
        /// </summary>
        protected virtual Task ProcessLegacyChannelsAsync(ChatResponse response) => Task.CompletedTask;

        // --- Error hooks -----------------------------------------------------

        /// <summary>
        /// Обработка ApiException. Наследник переопределяет для локализации сообщений
        /// и специальных action'ов (например, RequestLogout на UNAUTHORIZED).
        /// Default: добавить assistant-сообщение с текстом ошибки.
        /// </summary>
        protected virtual Task OnApiExceptionAsync(ApiException ex)
        {
            Messages.Add(new ChatMessage
            {
                Role = MessageRoles.Assistant,
                Content = $"⚠ {ex.ErrorCode}: {ex.Message}"
            });
            return Task.CompletedTask;
        }

        protected virtual Task OnUnexpectedExceptionAsync(Exception ex)
        {
            Messages.Add(new ChatMessage
            {
                Role = MessageRoles.Assistant,
                Content = $"⚠ {ex.Message}"
            });
            return Task.CompletedTask;
        }

        // --- Streaming hooks (T-000067 задел) --------------------------------

        /// <summary>
        /// Заготовка под SSE-стриминг. В T-000067 (v0.11.4) заменится реальным
        /// SendChatAsyncAsync + polling. Пока не вызывается.
        /// </summary>
        protected virtual Task OnStreamChunkAsync(ChatMessage assistantMessage, string delta) => Task.CompletedTask;

        protected virtual Task OnStreamCompletedAsync(ChatMessage assistantMessage) => Task.CompletedTask;

        // --- New chat --------------------------------------------------------

        protected virtual void NewChat()
        {
            Messages.Clear();
            SessionId = string.Empty;
        }
    }
}
