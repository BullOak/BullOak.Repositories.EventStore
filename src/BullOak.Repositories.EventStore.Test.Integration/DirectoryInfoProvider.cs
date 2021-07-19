namespace BullOak.Repositories.EventStore.Test.Integration
{
    using System;
    using System.IO;
    using System.Reflection;

    internal static class CurrentDirectoryInfoProvider
    {
        private static Lazy<DirectoryInfo> DirectoryInfo => new Lazy<DirectoryInfo>(() =>
        {
            var assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Directory.GetCurrentDirectory();
            return new DirectoryInfo(assemblyPath);
        });

        public static DirectoryInfo Get() => DirectoryInfo.Value;
    }
}
