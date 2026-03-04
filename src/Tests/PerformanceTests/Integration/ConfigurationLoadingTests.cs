using CompanyName.MyMeetings.PerformanceTests.Configuration;
using CompanyName.MyMeetings.PerformanceTests.Models;
using NUnit.Framework;

namespace CompanyName.MyMeetings.PerformanceTests.Integration;

[TestFixture]
public class ConfigurationLoadingTests
{
    [Test]
    public void LoadConfiguration_MeetingCreationTest_ShouldLoadSuccessfully()
    {
        // Arrange
        var projectDir = Path.GetDirectoryName(typeof(ConfigurationLoadingTests).Assembly.Location)!;
        var configPath = Path.Combine(projectDir, "SampleConfigs", "meeting-creation-test.yaml");
        var loader = new ConfigurationLoader();

        // Act
        var config = loader.LoadFromFile(configPath);

        // Assert
        Assert.That(config, Is.Not.Null);
        Assert.That(config.ScenarioName, Is.EqualTo("Meeting Creation Load Test"));
        Assert.That(config.Endpoints, Has.Length.EqualTo(1));
        Assert.That(config.Endpoints[0].Url, Is.EqualTo("/api/meetings"));
        Assert.That(config.Endpoints[0].Method, Is.EqualTo(HttpMethod.Post));
        Assert.That(config.LoadParams.VirtualUsers, Is.EqualTo(20));
        Assert.That(config.Duration, Is.EqualTo(TimeSpan.FromMinutes(2)));
        Assert.That(config.WarmupPeriod, Is.EqualTo(TimeSpan.FromSeconds(15)));
        Assert.That(config.Criteria.MaxResponseTime, Is.EqualTo(TimeSpan.FromSeconds(3)));
        Assert.That(config.Criteria.MinThroughput, Is.EqualTo(10.0));
        Assert.That(config.Criteria.MaxErrorRate, Is.EqualTo(0.05));
        Assert.That(config.Authentication, Is.Not.Null);
        Assert.That(config.Authentication!.Type, Is.EqualTo("Bearer"));
    }

    [Test]
    public void ValidateConfiguration_MeetingCreationTest_ShouldPassValidation()
    {
        // Arrange
        var projectDir = Path.GetDirectoryName(typeof(ConfigurationLoadingTests).Assembly.Location)!;
        var configPath = Path.Combine(projectDir, "SampleConfigs", "meeting-creation-test.yaml");
        var loader = new ConfigurationLoader();
        var config = loader.LoadFromFile(configPath);
        var validator = new ConfigurationValidator();

        // Act
        var result = validator.Validate(config);

        // Assert
        Assert.That(result.IsValid, Is.True, $"Configuration validation failed: {string.Join(", ", result.Errors)}");
    }

    [Test]
    public void LoadConfiguration_SimpleLoadTest_ShouldLoadSuccessfully()
    {
        // Arrange
        var projectDir = Path.GetDirectoryName(typeof(ConfigurationLoadingTests).Assembly.Location)!;
        var configPath = Path.Combine(projectDir, "SampleConfigs", "simple-load-test.yaml");
        var loader = new ConfigurationLoader();

        // Act
        var config = loader.LoadFromFile(configPath);

        // Assert
        Assert.That(config, Is.Not.Null);
        Assert.That(config.ScenarioName, Is.EqualTo("Simple Load Test"));
        Assert.That(config.Endpoints, Has.Length.EqualTo(1));
        Assert.That(config.LoadParams.VirtualUsers, Is.EqualTo(10));
    }
}
