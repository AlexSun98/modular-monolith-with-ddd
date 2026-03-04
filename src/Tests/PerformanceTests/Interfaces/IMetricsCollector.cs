using CompanyName.MyMeetings.PerformanceTests.Models;

namespace CompanyName.MyMeetings.PerformanceTests.Interfaces;

public interface IMetricsCollector
{
    void RecordRequest(RequestMetric metric);

    void RecordResourceSample(ResourceSample sample);

    MetricsSummary GetSummary();

    TimeSeriesData GetTimeSeries();
}
