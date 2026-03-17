using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using POCoutlookManageIdentity.OutlookManageTrigger.Options;
using POCoutlookManageIdentity.OutlookManageTrigger.Services;
using Xunit;
using PLN.Clients.BlobStorage.Interfaces;
using Microsoft.Extensions.Configuration;

namespace POCoutlookManageIdentity.UnitTests.OutlookManageTrigger;

public class OutlookManageTriggerServiceUnitTests
{
    private readonly ILogger<OutlookManageTriggerService> _logger = Substitute.For<ILogger<OutlookManageTriggerService>>();
    private readonly IOptions<OutlookManageTriggerOptions> _options = Substitute.For<IOptions<OutlookManageTriggerOptions>>();
    private readonly IBlobStorageService _blobStorageService = Substitute.For<IBlobStorageService>();
    private readonly IConfiguration _configuration = Substitute.For<IConfiguration>();
    private readonly IBlobStorageServiceFactory _blobStorageServiceFactory = Substitute.For<IBlobStorageServiceFactory>();
    private readonly OutlookManageTriggerService _sut;

    public OutlookManageTriggerServiceUnitTests()
    {
        _options.Value.Returns(new OutlookManageTriggerOptions());

        _sut = new OutlookManageTriggerService(
            _blobStorageService, _configuration, _blobStorageServiceFactory,
            _options,
            _logger);
    }

    [Fact]
    public void Constructor_WithValidDependencies_CreatesInstance()
    {
        _sut.ShouldNotBeNull();
    }
}
