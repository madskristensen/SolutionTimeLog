using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Microsoft;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Events;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;
using Timer = System.Timers.Timer;

namespace SolutionTimeLog
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(PackageGuids.TimeLogPackageString)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionOpening_string, PackageAutoLoadFlags.BackgroundLoad)]
    public sealed class SolutionTimeLogPackage : AsyncPackage
    {
        private static readonly TimeSpan _interval = TimeSpan.FromSeconds(60);
        private Timer _timer;
        private TimeLog _log;

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            await OpenTimeLogCommand.InitializeAsync(this);

            var isSolutionLoaded = await IsSolutionLoadedAsync();

            if (isSolutionLoaded)
            {
                OnAfterOpenSolution();
            }

            SolutionEvents.OnAfterBackgroundSolutionLoadComplete += OnAfterOpenSolution;
            SolutionEvents.OnAfterCloseSolution += OnAfterCloseSolution;
            Application.Current.Activated += (s, e) => _timer.Enabled = true;
            Application.Current.Deactivated += (s, e) => _timer.Enabled = false;
        }

        private async Task<bool> IsSolutionLoadedAsync()
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();
            var solService = await GetServiceAsync(typeof(SVsSolution)) as IVsSolution;
            Assumes.Present(solService);

            _log = new TimeLog(solService);
            ErrorHandler.ThrowOnFailure(solService.GetProperty((int)__VSPROPID.VSPROPID_IsSolutionOpen, out var value));

            return value is bool isSolOpen && isSolOpen;
        }

        private void OnAfterOpenSolution(object sender = null, EventArgs e = null)
        {
            _timer = _timer ?? new Timer();
            _timer.AutoReset = true;
            _timer.Interval = _interval.TotalMilliseconds;
            _timer.Elapsed += (s2, e2) => _log?.UpdateAsync(_interval).FileAndForget(nameof(SolutionTimeLogPackage)); ;
            _timer.Start();
        }

        private void OnAfterCloseSolution(object sender, EventArgs e)
        {
            if (_timer != null)
            {
                _timer.Enabled = false;
                _timer.Stop();
            }
        }
    }
}
