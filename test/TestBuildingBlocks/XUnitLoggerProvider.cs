using JsonApiDotNetCore;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace TestBuildingBlocks;

public sealed class XUnitLoggerProvider : ILoggerProvider
{
    private readonly ITestOutputHelper _testOutputHelper;

    public XUnitLoggerProvider(ITestOutputHelper testOutputHelper)
    {
        ArgumentGuard.NotNull(testOutputHelper);

        _testOutputHelper = testOutputHelper;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new XUnitLogger(_testOutputHelper, categoryName);
    }

    public void Dispose()
    {
    }
}
