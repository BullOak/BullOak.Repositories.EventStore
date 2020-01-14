namespace BullOak.Repositories.EventStore.Test.Integration
{
    using System;
    using System.IO;
    using System.Reflection;

    internal static class CurrentDirectoryInfoProvider
    {
        private static Lazy<DirectoryInfo> DirectoryInfo => new Lazy<DirectoryInfo>(() =>
        {
            var assemblyPath = (new Uri(Assembly.GetExecutingAssembly().CodeBase)).LocalPath;
            return new DirectoryInfo(Path.GetDirectoryName(assemblyPath));
        });

        public static DirectoryInfo Get() => DirectoryInfo.Value;
    }
}
