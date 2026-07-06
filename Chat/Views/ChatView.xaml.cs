using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SwanCode.Core.Chat.Models;
using SwanCode.Core.Chat.ViewModels;

namespace SwanCode.Core.Chat.Views
{
    public partial class ChatView : UserControl
    {
        public ChatView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is ChatViewModelBase oldVm)
                oldVm.Messages.CollectionChanged -= OnMessagesChanged;
            if (e.NewValue is ChatViewModelBase newVm)
                newVm.Messages.CollectionChanged += OnMessagesChanged;
        }

        private void OnMessagesChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            // Автопрокрутка к последнему сообщению
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                Dispatcher.BeginInvoke(new System.Action(() => ThreadScroll.ScrollToEnd()),
                    System.Windows.Threading.DispatcherPriority.Background);
            }
        }

        // --- Кнопки под сообщением ассистента (T-000104) --------------------

        private void CopyMessage_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is not ChatMessage msg) return;
            try { Clipboard.SetText(msg.Content ?? string.Empty); } catch { /* клипборд занят другим процессом */ }
        }

        private void QuoteMessage_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is not ChatMessage msg) return;
            if (DataContext is not ChatViewModelBase vm) return;

            var lines = (msg.Content ?? string.Empty).Replace("\r\n", "\n").Split('\n');
            var quoted = string.Join("\n", System.Linq.Enumerable.Select(lines, l => "> " + l)) + "\n\n";
            vm.InputText = quoted + vm.InputText;

            InputTextBox.Focus();
            InputTextBox.CaretIndex = InputTextBox.Text.Length;
        }

        // Регенерации на сервере нет (сессия хранит историю) — retry повторяет
        // предыдущий юзерский запрос новым ходом.
        private void RetryMessage_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is not ChatMessage msg) return;
            if (DataContext is not ChatViewModelBase vm) return;

            var idx = vm.Messages.IndexOf(msg);
            for (int i = (idx >= 0 ? idx : vm.Messages.Count) - 1; i >= 0; i--)
            {
                if (vm.Messages[i].Role != MessageRoles.User) continue;

                vm.InputText = vm.Messages[i].Content ?? string.Empty;
                if (vm.SendMessageCommand.CanExecute(null))
                    vm.SendMessageCommand.Execute(null);
                return;
            }
        }

        private void InfoMessage_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.ContextMenu == null) return;
            btn.ContextMenu.PlacementTarget = btn;
            btn.ContextMenu.IsOpen = true;
        }

        // RichTextBox сообщений глотает колесо мыши даже без своих скроллбаров —
        // пробрасываем событие родительскому ScrollViewer треда.
        private void MessageBody_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Handled) return;
            e.Handled = true;
            ThreadScroll.RaiseEvent(new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
            {
                RoutedEvent = UIElement.MouseWheelEvent,
                Source = sender
            });
        }

        // Enter → отправка; Shift+Enter → перенос строки (AcceptsReturn=true в XAML).
        // По memory feedback_wpf_textbox_enter — обязательно PreviewKeyDown, а не KeyDown.
        private void InputTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift) return;

            if (DataContext is ChatViewModelBase vm && vm.SendMessageCommand.CanExecute(null))
            {
                vm.SendMessageCommand.Execute(null);
                e.Handled = true;
            }
        }
    }
}
