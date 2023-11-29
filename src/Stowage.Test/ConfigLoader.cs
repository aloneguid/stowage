using Config.Net;

namespace Stowage.Test {
    static class ConfigLoader {
        public static ITestSettings Load() {
            return new ConfigurationBuilder<ITestSettings>()
                .UseIniFile("c:\\tmp\\integration-tests.ini")
                .Build();
        }
    }
}
