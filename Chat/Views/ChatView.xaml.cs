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
