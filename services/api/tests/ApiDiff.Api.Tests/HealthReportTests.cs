using ApiDiff.Api.Health;
using Xunit;

namespace ApiDiff.Api.Tests;

public class HealthReportTests
{
    [Fact]
    public void Current_ReportsServiceOk()
    {
        var status = HealthReport.Current();

        Assert.Equal("api", status.Service);
        Assert.Equal("ok", status.Status);
        Assert.Equal(HealthReport.Version, status.Version);
    }
}
