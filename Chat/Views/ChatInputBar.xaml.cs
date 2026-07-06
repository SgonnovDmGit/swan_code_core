using System.Windows.Controls;
using System.Windows.Input;

namespace SwanCode.Core.Chat.Views
{
    /// <summary>
    /// Строка ввода на всю ширину окна (T-000074). DataContext — продуктовый
    /// MainViewModel с ActiveChat и ProjectSettings (биндинги duck-typed по пути).
    /// </summary>
    public partial class ChatInputBar : UserControl
    {
        public ChatInputBar()
        {
            InitializeComponent();
        }

        // Enter → отправка; Shift+Enter → перенос строки (AcceptsReturn=true).
        // По memory feedback_wpf_textbox_enter — обязательно PreviewKeyDown, не KeyDown.
        private void InputTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift) return;

            if (SendButton.Command?.CanExecute(null) == true)
            {
                SendButton.Command.Execute(null);
                e.Handled = true;
            }
        }
    }
}
