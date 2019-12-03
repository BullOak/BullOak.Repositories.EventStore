namespace BullOak.Repositories.EventStore.Test.Integration
{
    using System;
    using System.IO;
    using System.Text;
    using Microsoft.Extensions.Configuration;
    using BullOak.Repositories.EventStore.Test.Integration.Contexts.EventStoreIsolation;

    public class IntegrationTestsSettings
    {
        public IsolationMode EventStoreIsolationMode { get; }
        public string EventStoreIsolationCommand { get; }
        public string EventStoreIsolationArguments { get; }

        public IntegrationTestsSettings()
        {
            EventStoreIsolationMode = ReadValueFromConfigOrDefault(
                "EventStore.IsolationMode",
                IsolationMode.None);

            EventStoreIsolationCommand = settingsResolver("EventStore.Server.Command");
            EventStoreIsolationArguments = settingsResolver("EventStore.Server.Arguments");

            description = ComposeSettingsDescription();
        }

        private string ComposeSettingsDescription()
        {
            var builder = new StringBuilder();

            builder.AppendLine($"EventStore.IsolationMode={EventStoreIsolationMode}");
            builder.AppendLine($"EventStore.Server.Command={EventStoreIsolationCommand}");
            builder.AppendLine($"EventStore.Server.Arguments={EventStoreIsolationArguments}");

            return builder.ToString();
        }

        public override string ToString() => description;

        private readonly string description;

        private IsolationMode ReadValueFromConfigOrDefault(
            string configName,
            IsolationMode defaultValue) =>
            Enum.TryParse<IsolationMode>(settingsResolver(configName), out var value) ? value : defaultValue;

        private static readonly Func<string, string> settingsResolver;

        static IntegrationTestsSettings()
        {
            var assemblyPath = (new System.Uri(typeof(IntegrationTestsSettings).Assembly.CodeBase)).AbsolutePath;
            var currentDir = new DirectoryInfo(Path.GetDirectoryName(assemblyPath)).FullName;

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.SetBasePath(currentDir);
            configurationBuilder.AddJsonFile("appsettings.json", true);
            var configuration = configurationBuilder.Build();

            settingsResolver = (name) => configuration.GetSection(name).Value;
        }
    }
}
