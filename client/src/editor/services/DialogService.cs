using Avalonia.Controls;

namespace OpenGaugeClient.Editor.Services
{
    public interface IDialogService
    {
        Task<(string? relative, string? absolute)> ShowSelectFileDialogAsync(
            string[]? extensions = null, bool directoriesOnly = false);
    }


    public class DialogService : IDialogService
    {
        private readonly Window _owner;

        public DialogService(Window owner)
        {
            _owner = owner;
        }

        public async Task<(string? relative, string? absolute)> ShowSelectFileDialogAsync(
            string[]? extensions = null, bool directoriesOnly = false)
        {
            var dialog = new SelectFileDialog(extensions, directoriesOnly);
            await dialog.ShowDialog(_owner);
            return (dialog.ViewModel.RelativePath, dialog.ViewModel.AbsolutePath);
        }
    }
}