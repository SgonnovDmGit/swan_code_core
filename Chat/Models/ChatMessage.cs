using System.Collections.Generic;
using SwanCode.Core.Helpers;

namespace SwanCode.Core.Chat.Models
{
    public static class MessageRoles
    {
        public const string User = "user";
        public const string Assistant = "assistant";
        public const string ToolUse = "tool_use";
        public const string ToolResult = "tool_result";
        public const string Debug = "debug";
    }

    public class ChatMessage : ViewModelBase
    {
        private string _content = string.Empty;
        private string _modelName = string.Empty;
        private string _providerName = string.Empty;
        private bool _isThinking;
        private bool _hasCodeChanges;
        private List<ToolUseDTO>? _toolUses;
        private List<ToolResultItem>? _toolResults;

        public string Role { get; set; } = string.Empty;

        public string Content
        {
            get => _content;
            set => SetProperty(ref _content, value);
        }

        public string ProviderName
        {
            get => _providerName;
            set => SetProperty(ref _providerName, value);
        }

        public string ModelName
        {
            get => _modelName;
            set => SetProperty(ref _modelName, value);
        }

        public List<ToolUseDTO>? ToolUses
        {
            get => _toolUses;
            set => SetProperty(ref _toolUses, value);
        }

        public List<ToolResultItem>? ToolResults
        {
            get => _toolResults;
            set => SetProperty(ref _toolResults, value);
        }

        public bool IsUser => Role == MessageRoles.User;
        public bool IsAssistant => Role == MessageRoles.Assistant;
        public bool IsToolUse => Role == MessageRoles.ToolUse;
        public bool IsToolResult => Role == MessageRoles.ToolResult;
        public bool IsDebug => Role == MessageRoles.Debug;

        public bool HasCodeChanges
        {
            get => _hasCodeChanges;
            set => SetProperty(ref _hasCodeChanges, value);
        }

        public bool IsThinking
        {
            get => _isThinking;
            set
            {
                if (SetProperty(ref _isThinking, value))
                    OnPropertyChanged(nameof(CanQuote));
            }
        }

        public bool CanQuote => !IsUser && !IsDebug && !IsThinking && !IsToolUse && !IsToolResult;
    }
}
