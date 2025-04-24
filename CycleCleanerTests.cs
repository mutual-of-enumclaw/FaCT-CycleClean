using Fact.BatchCleaner;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MoE.Commercial.Data;
using Moq;
using System.Net.Http;
using Xunit;

public class CycleCleanerTests
{
    [Fact]
    public void Constructor_WithValidDependencies_ShouldNotThrow()
    {
        // Arrange
        var dataProviderMock = new Mock<IDataProvider>();
        var xmlMessageBuilderMock = new Mock<IXmlMessageBuilder>();
        var httpClient = new HttpClient();

        var settings = Options.Create(new Db2Settings
        {
            CommFramework = new CommFrameworkSettings
            {
                RequestUri = "http://localhost/test"
            }
        });

        // Act & Assert
        var cleaner = new CycleCleaner(
            dataProviderMock.Object,
            NullLogger<CycleCleaner>.Instance,
            xmlMessageBuilderMock.Object,
            settings,
            httpClient
        );
    }
}
