using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SwanCode.Core.Chat.Models;
using SwanCode.Core.Chat.ViewModels;

namespace SwanCode.Core.Chat.Views
{
    /// <summary>
    /// T-000082 (REQ-021): доска задач над тредом чата. Данные — ChatViewModelBase.TasksView
    /// (срез TaskBoard по режиму/фильтру/сортировке). Ведёт доску AI тулом task_board_update,
    /// но пользователь правит её и руками.
    ///
    /// Здесь — только перетаскивание строк (порядок задач): чистая UI-механика, в VM ей делать
    /// нечего. Сама перестановка — ChatViewModelBase.MoveTask.
    /// </summary>
    public partial class TaskBoardView : UserControl
    {
        public TaskBoardView()
        {
            InitializeComponent();
        }

        private void DragHandle_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not FrameworkElement handle || handle.DataContext is not TaskItem item) return;

            DragDrop.DoDragDrop(handle, item, DragDropEffects.Move);
            e.Handled = true;
        }

        private void TaskRow_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(typeof(TaskItem))
                ? DragDropEffects.Move
                : DragDropEffects.None;
            e.Handled = true;
        }

        private void TaskRow_Drop(object sender, DragEventArgs e)
        {
            if (sender is not FrameworkElement row || row.DataContext is not TaskItem target) return;
            if (e.Data.GetData(typeof(TaskItem)) is not TaskItem dragged) return;
            if (DataContext is not ChatViewModelBase vm) return;

            vm.MoveTask(dragged, target);
            e.Handled = true;
        }
    }
}
