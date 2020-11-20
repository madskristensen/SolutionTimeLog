using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace SolutionTimeLog
{
    [Guid("eca9d054-bbfc-48a5-b1bf-75d48ad7c3ac")]
    public class TimeLog
    {
        private readonly IVsSolution _solService;

        public TimeLog(IVsSolution solService)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            _solService = solService;
        }

        public async Task UpdateAsync(TimeSpan interval)
        {
            try
            {
                TimeSpan time = await ReadAsync();
                var newTime = time.Add(interval);

                await SaveAsync(newTime);
            }
            catch (Exception)
            {
                // Nothing to do here but to fail silently
            }
        }

        private async Task SaveAsync(TimeSpan span)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            _solService.GetProperty((int)__VSPROPID.VSPROPID_SolutionBaseName, out var solution);

            if (solution == null)
            {
                return;
            }

            var fileName = GetFileName(solution);
            PackageUtilities.EnsureOutputPath(Path.GetDirectoryName(fileName));

            using (var writer = new StreamWriter(fileName, false, Encoding.UTF8))
            {
                await writer.WriteAsync(span.TotalMilliseconds.ToString());
            }
        }

        public async Task<TimeSpan> ReadAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            _solService.GetProperty((int)__VSPROPID.VSPROPID_SolutionBaseName, out var solution);

            if (solution == null)
            {
                return new TimeSpan();
            }

            var fileName = GetFileName(solution);

            if (File.Exists(fileName))
            {
                var content = File.ReadAllText(fileName);

                if (double.TryParse(content, out var time))
                {
                    return TimeSpan.FromMilliseconds(time);
                }
            }

            return new TimeSpan();
        }

        private static string GetFileName(object solution)
        {
            var profile = Environment.ExpandEnvironmentVariables("%userprofile%");
            return Path.Combine(profile, ".vs", (string)solution + ".txt");
        }
    }
}
