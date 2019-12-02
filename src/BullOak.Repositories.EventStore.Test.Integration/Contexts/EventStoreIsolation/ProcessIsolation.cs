namespace BullOak.Repositories.EventStore.Test.Integration.Contexts.EventStoreIsolation
{
    using System;
    using System.IO;
    using System.Diagnostics;

    internal class ProcessIsolation : IDisposable
    {
        private readonly Process process;

        public ProcessIsolation(
            string isolationCommand,
            string isolationArguments)
        {
            var assemblyPath = (new System.Uri(typeof(IntegrationTestsSettings).Assembly.CodeBase)).AbsolutePath;
            var currentDir = new DirectoryInfo(Path.GetDirectoryName(assemblyPath)).FullName;

            var command = Path.IsPathFullyQualified(isolationCommand)
                ? isolationCommand :
                Path.Combine(currentDir, isolationCommand);

            process = StartProcess(command, isolationArguments, currentDir);
        }

        public void Dispose()
        {
            StopProcess(process);
            GC.SuppressFinalize(this);
        }

        private static Process StartProcess(
            string command,
            string arguments,
            string workingDirectory)
        {
            var p = new Process
            {
                StartInfo =
                {
                    FileName = command,
                    Arguments = arguments,
                    RedirectStandardOutput = false,
                    UseShellExecute = false,
                    WorkingDirectory = workingDirectory
                }
            };
            p.Start();
            return p;
        }

        private static void StopProcess(Process process)
        {
            if (process == null || process.HasExited)
            {
                return;
            }

            try
            {
                process.Kill();
                process.WaitForExit();
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString());
            }
        }
    }
}
