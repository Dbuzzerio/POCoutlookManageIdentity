using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using POCoutlookManageIdentity.OutlookManageTrigger.Services;
using Xunit;
using Microsoft.Azure.Functions.Worker;

namespace POCoutlookManageIdentity.UnitTests.OutlookManageTrigger;

/// <summary>
/// Test class for the <see cref="OutlookManageTrigger"/> Azure Function.
/// </summary>
public class OutlookManageTriggerTests
{
    private readonly ILogger<POCoutlookManageIdentity.OutlookManageTrigger.OutlookManageTrigger> _loggerMock;
    private readonly IConfiguration _configurationMock;
    private readonly IOutlookManageTriggerService _outlookmanagetriggerServiceMock;
    private readonly POCoutlookManageIdentity.OutlookManageTrigger.OutlookManageTrigger _function;

    /// <summary>
    /// Initializes a new instance of the <see cref="OutlookManageTriggerTests"/> class.
    /// Sets up all required mock objects and dependencies for testing the OutlookManageTrigger function.
    /// </summary>
    public OutlookManageTriggerTests()
    {
        _loggerMock = Substitute.For<ILogger<POCoutlookManageIdentity.OutlookManageTrigger.OutlookManageTrigger>>();
        _configurationMock = Substitute.For<IConfiguration>();
        _outlookmanagetriggerServiceMock = Substitute.For<IOutlookManageTriggerService>();

        _function = new POCoutlookManageIdentity.OutlookManageTrigger.OutlookManageTrigger(
            configuration: _configurationMock,
            logger: _loggerMock,
            outlookmanagetriggerService: _outlookmanagetriggerServiceMock
        );
    }

    [Fact]
    public async Task Constructor_WithValidDependencies_CreatesInstance()
    {
        _function.ShouldNotBeNull();
        //await _function.Run(new TimerInfo());
    }
}
