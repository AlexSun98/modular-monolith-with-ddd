namespace CompanyName.MyMeetings.PerformanceTests.Reporting;

public static class HtmlReportTemplate
{
    public static string GetTemplate() => @"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Performance Test Report - {{ScenarioName}}</title>
    <script src=""https://cdn.jsdelivr.net/npm/chart.js@4.4.0/dist/chart.umd.min.js""></script>
    <style>
        body {
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, Cantarell, sans-serif;
            margin: 0;
            padding: 20px;
            background-color: #f5f5f5;
        }
        .container {
            max-width: 1200px;
            margin: 0 auto;
            background-color: white;
            padding: 30px;
            border-radius: 8px;
            box-shadow: 0 2px 4px rgba(0,0,0,0.1);
        }
        h1 {
            color: #333;
            border-bottom: 3px solid #4CAF50;
            padding-bottom: 10px;
        }
        .status {
            display: inline-block;
            padding: 5px 15px;
            border-radius: 4px;
            font-weight: bold;
            margin-left: 10px;
        }
        .status.passed {
            background-color: #4CAF50;
            color: white;
        }
        .status.failed {
            background-color: #f44336;
            color: white;
        }
        .status.error {
            background-color: #ff9800;
            color: white;
        }
        .summary {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
            gap: 20px;
            margin: 30px 0;
        }
        .metric-card {
            background-color: #f9f9f9;
            padding: 20px;
            border-radius: 6px;
            border-left: 4px solid #4CAF50;
        }
        .metric-label {
            font-size: 14px;
            color: #666;
            margin-bottom: 5px;
        }
        .metric-value {
            font-size: 28px;
            font-weight: bold;
            color: #333;
        }
        .metric-unit {
            font-size: 14px;
            color: #999;
        }
        .chart-container {
            margin: 30px 0;
            padding: 20px;
            background-color: #f9f9f9;
            border-radius: 6px;
        }
        .chart-title {
            font-size: 18px;
            font-weight: bold;
            margin-bottom: 15px;
            color: #333;
        }
        canvas {
            max-height: 300px;
        }
        .errors {
            margin: 30px 0;
        }
        .error-item {
            background-color: #ffebee;
            padding: 15px;
            margin: 10px 0;
            border-radius: 4px;
            border-left: 4px solid #f44336;
        }
        .timestamp {
            color: #666;
            font-size: 12px;
        }
    </style>
</head>
<body>
    <div class=""container"">
        <h1>
            {{ScenarioName}}
            <span class=""status {{StatusClass}}"">{{Status}}</span>
        </h1>
        
        <div class=""timestamp"">
            Test Duration: {{StartTime}} to {{EndTime}} ({{Duration}})
        </div>

        <div class=""summary"">
            <div class=""metric-card"">
                <div class=""metric-label"">Total Requests</div>
                <div class=""metric-value"">{{TotalRequests}}</div>
            </div>
            <div class=""metric-card"">
                <div class=""metric-label"">Throughput</div>
                <div class=""metric-value"">{{Throughput}} <span class=""metric-unit"">req/s</span></div>
            </div>
            <div class=""metric-card"">
                <div class=""metric-label"">Error Rate</div>
                <div class=""metric-value"">{{ErrorRate}}<span class=""metric-unit"">%</span></div>
            </div>
            <div class=""metric-card"">
                <div class=""metric-label"">P50 Response Time</div>
                <div class=""metric-value"">{{P50}} <span class=""metric-unit"">ms</span></div>
            </div>
            <div class=""metric-card"">
                <div class=""metric-label"">P95 Response Time</div>
                <div class=""metric-value"">{{P95}} <span class=""metric-unit"">ms</span></div>
            </div>
            <div class=""metric-card"">
                <div class=""metric-label"">P99 Response Time</div>
                <div class=""metric-value"">{{P99}} <span class=""metric-unit"">ms</span></div>
            </div>
        </div>

        <div class=""chart-container"">
            <div class=""chart-title"">Throughput Over Time</div>
            <canvas id=""throughputChart""></canvas>
        </div>

        <div class=""chart-container"">
            <div class=""chart-title"">Response Time Over Time</div>
            <canvas id=""responseTimeChart""></canvas>
        </div>

        <div class=""chart-container"">
            <div class=""chart-title"">Error Rate Over Time</div>
            <canvas id=""errorRateChart""></canvas>
        </div>

        {{ErrorsSection}}
    </div>

    <script>
        const timeSeriesData = {{TimeSeriesData}};

        // Throughput Chart
        new Chart(document.getElementById('throughputChart'), {
            type: 'line',
            data: {
                labels: timeSeriesData.labels,
                datasets: [{
                    label: 'Requests/sec',
                    data: timeSeriesData.throughput,
                    borderColor: '#4CAF50',
                    backgroundColor: 'rgba(76, 175, 80, 0.1)',
                    tension: 0.4
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                scales: {
                    y: {
                        beginAtZero: true
                    }
                }
            }
        });

        // Response Time Chart
        new Chart(document.getElementById('responseTimeChart'), {
            type: 'line',
            data: {
                labels: timeSeriesData.labels,
                datasets: [{
                    label: 'Avg Response Time (ms)',
                    data: timeSeriesData.responseTime,
                    borderColor: '#2196F3',
                    backgroundColor: 'rgba(33, 150, 243, 0.1)',
                    tension: 0.4
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                scales: {
                    y: {
                        beginAtZero: true
                    }
                }
            }
        });

        // Error Rate Chart
        new Chart(document.getElementById('errorRateChart'), {
            type: 'line',
            data: {
                labels: timeSeriesData.labels,
                datasets: [{
                    label: 'Error Rate (%)',
                    data: timeSeriesData.errorRate,
                    borderColor: '#f44336',
                    backgroundColor: 'rgba(244, 67, 54, 0.1)',
                    tension: 0.4
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                scales: {
                    y: {
                        beginAtZero: true,
                        max: 100
                    }
                }
            }
        });
    </script>
</body>
</html>";
}
