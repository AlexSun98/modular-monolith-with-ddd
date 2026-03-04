using System.Text.Json;
using CompanyName.MyMeetings.PerformanceTests.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace CompanyName.MyMeetings.PerformanceTests.Configuration;

public class ConfigurationLoader
{
    public TestConfiguration LoadFromFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Configuration file not found: {filePath}");
        }

        var content = File.ReadAllText(filePath);
        var extension = Path.GetExtension(filePath).ToLowerInvariant();

        return extension switch
        {
            ".yaml" or ".yml" => LoadFromYaml(content),
            ".json" => LoadFromJson(content),
            _ => throw new NotSupportedException($"Unsupported configuration file format: {extension}")
        };
    }

    private TestConfiguration LoadFromYaml(string yamlContent)
    {
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        var configDto = deserializer.Deserialize<TestConfigurationDto>(yamlContent);
        return MapToTestConfiguration(configDto);
    }

    private TestConfiguration LoadFromJson(string jsonContent)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var configDto = JsonSerializer.Deserialize<TestConfigurationDto>(jsonContent, options);
        if (configDto == null)
        {
            throw new InvalidOperationException("Failed to deserialize configuration");
        }

        return MapToTestConfiguration(configDto);
    }

    private TestConfiguration MapToTestConfiguration(TestConfigurationDto dto)
    {
        return new TestConfiguration
        {
            ScenarioName = dto.ScenarioName ?? string.Empty,
            Endpoints = dto.Endpoints?.Select(e => new TestEndpoint(
                e.Url ?? string.Empty,
                ParseHttpMethod(e.Method),
                e.RequestBody,
                e.Headers)).ToArray() ?? Array.Empty<TestEndpoint>(),
            LoadParams = new LoadParameters(
                dto.LoadParameters?.VirtualUsers ?? 0,
                new RampUpStrategy(
                    ParseRampUpType(dto.LoadParameters?.RampUpStrategy?.Type),
                    dto.LoadParameters?.RampUpStrategy?.StepSize,
                    ParseTimeSpan(dto.LoadParameters?.RampUpStrategy?.Duration)),
                ParseTimeSpan(dto.LoadParameters?.RampUpStrategy?.Duration) ?? TimeSpan.Zero,
                dto.LoadParameters?.ThinkTime != null
                    ? new ThinkTime(
                        ParseTimeSpan(dto.LoadParameters.ThinkTime.MinDelay) ?? TimeSpan.Zero,
                        ParseTimeSpan(dto.LoadParameters.ThinkTime.MaxDelay) ?? TimeSpan.Zero)
                    : null),
            Duration = ParseTimeSpan(dto.Duration) ?? TimeSpan.Zero,
            WarmupPeriod = ParseTimeSpan(dto.WarmupPeriod) ?? TimeSpan.Zero,
            Criteria = new SuccessCriteria(
                ParseTimeSpan(dto.SuccessCriteria?.MaxResponseTime),
                dto.SuccessCriteria?.MinThroughput,
                dto.SuccessCriteria?.MaxErrorRate ?? 0.05),
            Authentication = dto.Authentication != null
                ? new AuthenticationConfig(
                    dto.Authentication.Type ?? string.Empty,
                    dto.Authentication.TokenEndpoint,
                    dto.Authentication.Credentials)
                : null
        };
    }

    private HttpMethod ParseHttpMethod(string? method)
    {
        return method?.ToUpperInvariant() switch
        {
            "GET" => HttpMethod.Get,
            "POST" => HttpMethod.Post,
            "PUT" => HttpMethod.Put,
            "DELETE" => HttpMethod.Delete,
            "PATCH" => HttpMethod.Patch,
            _ => HttpMethod.Get
        };
    }

    private RampUpType ParseRampUpType(string? type)
    {
        return type?.ToLowerInvariant() switch
        {
            "linear" => RampUpType.Linear,
            "step" => RampUpType.Step,
            "immediate" => RampUpType.Immediate,
            _ => RampUpType.Immediate
        };
    }

    private TimeSpan? ParseTimeSpan(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (TimeSpan.TryParse(value, out var result))
        {
            return result;
        }

        return null;
    }
}
