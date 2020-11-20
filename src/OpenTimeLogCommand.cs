using System;
using System.ComponentModel.Design;
using System.Globalization;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace SolutionTimeLog
{
    internal sealed class OpenTimeLogCommand
    {
        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            TimeLog log = await package.GetServiceAsync<TimeLog, TimeLog>();

            OleMenuCommandService commandService = await package.GetServiceAsync<IMenuCommandService, OleMenuCommandService>();
            var menuCommandID = new CommandID(PackageGuids.TimeLogCmdSet, PackageIds.OpenTimeLog);
            var menuItem = new OleMenuCommand((s, e) => ExecuteAsync(package, log).ConfigureAwait(false), menuCommandID, false);
            commandService.AddCommand(menuItem);
        }

        private static async Task ExecuteAsync(AsyncPackage package, TimeLog log)
        {
            TimeSpan time = await log.ReadAsync();

            VsShellUtilities.ShowMessageBox(
                package,
                $"This solution has been open for {time.TotalMinutes} minutes",
                nameof(TimeLog),
                OLEMSGICON.OLEMSGICON_INFO,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }
    }
}
