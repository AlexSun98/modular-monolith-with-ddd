using CompanyName.MyMeetings.PerformanceTests.Models;

namespace CompanyName.MyMeetings.PerformanceTests.Interfaces;

public interface IReportGenerator
{
    Task GenerateHtmlReportAsync(TestResult result, string outputPath);

    Task GenerateJsonReportAsync(TestResult result, string outputPath);
}
