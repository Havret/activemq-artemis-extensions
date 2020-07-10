using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace ActiveMQ.Artemis.Client.Extensions.AspNetCore.Tests.Logging
{
    public class TestLoggerProvider : ILoggerProvider
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public TestLoggerProvider(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new TestLogger(_testOutputHelper, categoryName);
        }

        public void Dispose()
        {
        }
    }
}