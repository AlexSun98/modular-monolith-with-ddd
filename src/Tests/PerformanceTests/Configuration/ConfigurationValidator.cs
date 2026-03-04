using CompanyName.MyMeetings.PerformanceTests.Interfaces;

namespace CompanyName.MyMeetings.PerformanceTests.Configuration;

public class ConfigurationValidator
{
    public ValidationResult Validate(ITestConfiguration config)
    {
        var errors = new List<string>();

        // Validate scenario name
        if (string.IsNullOrWhiteSpace(config.ScenarioName))
        {
            errors.Add("ScenarioName is required");
        }

        // Validate endpoints
        if (config.Endpoints == null || config.Endpoints.Length == 0)
        {
            errors.Add("At least one endpoint is required");
        }
        else
        {
            for (int i = 0; i < config.Endpoints.Length; i++)
            {
                var endpoint = config.Endpoints[i];
                if (string.IsNullOrWhiteSpace(endpoint.Url))
                {
                    errors.Add($"Endpoint[{i}].Url is required");
                }
                else if (!Uri.TryCreate(endpoint.Url, UriKind.RelativeOrAbsolute, out _))
                {
                    errors.Add($"Endpoint[{i}].Url is not a valid URL: {endpoint.Url}");
                }
            }
        }

        // Validate load parameters
        if (config.LoadParams == null)
        {
            errors.Add("LoadParams is required");
        }
        else
        {
            if (config.LoadParams.VirtualUsers <= 0)
            {
                errors.Add("LoadParams.VirtualUsers must be greater than 0");
            }

            if (config.LoadParams.RampUpDuration < TimeSpan.Zero)
            {
                errors.Add("LoadParams.RampUpDuration cannot be negative");
            }

            // Validate think time if present
            if (config.LoadParams.ThinkTime != null)
            {
                if (config.LoadParams.ThinkTime.MinDelay < TimeSpan.Zero)
                {
                    errors.Add("LoadParams.ThinkTime.MinDelay cannot be negative");
                }

                if (config.LoadParams.ThinkTime.MaxDelay < TimeSpan.Zero)
                {
                    errors.Add("LoadParams.ThinkTime.MaxDelay cannot be negative");
                }

                if (config.LoadParams.ThinkTime.MaxDelay < config.LoadParams.ThinkTime.MinDelay)
                {
                    errors.Add("LoadParams.ThinkTime.MaxDelay must be greater than or equal to MinDelay");
                }
            }
        }

        // Validate duration
        if (config.Duration < TimeSpan.FromSeconds(10))
        {
            errors.Add("Duration must be at least 10 seconds");
        }

        if (config.Duration > TimeSpan.FromMinutes(60))
        {
            errors.Add("Duration cannot exceed 60 minutes");
        }

        // Validate warmup period
        if (config.WarmupPeriod < TimeSpan.Zero)
        {
            errors.Add("WarmupPeriod cannot be negative");
        }

        if (config.WarmupPeriod >= config.Duration)
        {
            errors.Add("WarmupPeriod must be less than Duration");
        }

        // Validate success criteria
        if (config.Criteria == null)
        {
            errors.Add("SuccessCriteria is required");
        }
        else
        {
            if (config.Criteria.MaxResponseTime.HasValue && config.Criteria.MaxResponseTime.Value < TimeSpan.Zero)
            {
                errors.Add("SuccessCriteria.MaxResponseTime cannot be negative");
            }

            if (config.Criteria.MinThroughput.HasValue && config.Criteria.MinThroughput.Value < 0)
            {
                errors.Add("SuccessCriteria.MinThroughput cannot be negative");
            }

            if (config.Criteria.MaxErrorRate < 0 || config.Criteria.MaxErrorRate > 1)
            {
                errors.Add("SuccessCriteria.MaxErrorRate must be between 0 and 1");
            }
        }

        return new ValidationResult(errors.Count == 0, errors);
    }
}

public record ValidationResult(bool IsValid, List<string> Errors);
