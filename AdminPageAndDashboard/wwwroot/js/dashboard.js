let routingChart, trendChart;

// Read dashboard configuration from server
const dashboardConfig = {
    refreshIntervalMs: parseInt(document.querySelector('[data-refresh-interval]')?.getAttribute('data-refresh-interval') || '5000'),
    maxRequests: parseInt(document.querySelector('[data-max-requests]')?.getAttribute('data-max-requests') || '100')
};

document.addEventListener("DOMContentLoaded", function () {
    try {
        initializeCharts();
        loadDashboardData();
        const refreshInterval = dashboardConfig.refreshIntervalMs;
        if (refreshInterval > 0) {
            setInterval(loadDashboardData, refreshInterval);
        }
    } catch (err) {
        console.error("Dashboard initialization error:", err);
        showError("Failed to initialize dashboard");
    }
});

async function loadDashboardData() {
    try {
        const response = await fetch("/Dashboard/GetDashboardData");
        if (!response.ok) {
            throw new Error(`HTTP ${response.status}: ${response.statusText}`);
        }
        const data = await response.json();
        if (!data) {
            throw new Error("No data received from server");
        }

        updateStats(data.statistics);
        updateRecentRequests(data.recentRequests?.data || []);
        updateCharts(data.chart_data, data.statistics?.routing_breakdown);

    } catch (err) {
        console.error("Failed to load dashboard data:", err);
        showError(`Dashboard error: ${err.message}`);
    }
}

function updateStats(stats) {
    if (!stats) return;

    document.getElementById("totalRequests").textContent = stats.total_requests ?? 0;
    document.getElementById("anomalyCount").textContent = stats.anomaly_count ?? 0;
    document.getElementById("legitimateCount").textContent = stats.legitimate_count ?? 0;
    document.getElementById("anomalyRate").textContent =
        ((stats.anomaly_rate ?? 0) * 100).toFixed(1) + "%";
}

function updateRecentRequests(requests) {
    const tbody = document.getElementById("recentRequests");
    if (!tbody) return;

    if (!Array.isArray(requests) || requests.length === 0) {
        tbody.innerHTML = `
            <tr>
                <td colspan="5" class="text-center text-muted py-4">
                    <i class="fas fa-inbox fa-2x mb-2 d-block"></i>
                    No requests found
                </td>
            </tr>`;
        return;
    }

    tbody.innerHTML = "";
    const maxDisplay = dashboardConfig.maxRequests;

    requests.slice(0, maxDisplay).forEach(r => {
        const row = document.createElement("tr");
        const time = r.timestamp
            ? new Date(r.timestamp).toLocaleString()
            : "N/A";
        const confidence = typeof r.confidence === "number"
            ? (Math.abs(r.confidence) * 100).toFixed(1) + "%"
            : "N/A";

        row.innerHTML = `
            <td><small>${time}</small></td>
            <td><code class="text-primary">${r.client_ip || 'N/A'}</code></td>
            <td><span class="text-muted">${r.endpoint || 'N/A'}</span></td>
            <td>
                <span class="badge ${r.is_anomaly ? "bg-danger" : "bg-success"}">
                    <i class="fas ${r.is_anomaly ? "fa-exclamation-triangle" : "fa-check-circle"} me-1"></i>
                    ${r.is_anomaly ? "Anomaly" : "Normal"}
                </span>
            </td>
            <td><strong>${confidence}</strong></td>
        `;
        tbody.appendChild(row);
    });
}

function initializeCharts() {
    try {
        // Routing Doughnut Chart
        const ctx1 = document.getElementById('routingChart');
        if (ctx1) {
            routingChart = new Chart(ctx1, {
                type: 'doughnut',
                data: {
                    labels: ['Honeypot', 'Real System'],
                    datasets: [{
                        data: [0, 0],
                        backgroundColor: ['#dc3545', '#28a745'],
                        borderColor: ['#fff', '#fff'],
                        borderWidth: 3
                    }]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    plugins: {
                        legend: { position: 'bottom' },
                        tooltip: {
                            callbacks: {
                                label: function (context) {
                                    const label = context.label || '';
                                    const value = context.parsed || 0;
                                    const total = context.dataset.data.reduce((a, b) => a + b, 0);
                                    const percentage = total > 0 ? ((value / total) * 100).toFixed(1) : 0;
                                    return `${label}: ${value} (${percentage}%)`;
                                }
                            }
                        }
                    }
                }
            });
        }

        // Trend Line Chart - Now with TWO lines: Anomalies + Legitimate
        const ctx2 = document.getElementById('trendChart');
        if (ctx2) {
            trendChart = new Chart(ctx2, {
                type: 'line',
                data: {
                    labels: ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'],
                    datasets: [
                        {
                            label: 'Anomalies Detected',
                            data: [0, 0, 0, 0, 0, 0, 0],
                            borderColor: '#dc3545',
                            backgroundColor: 'rgba(220, 53, 69, 0.1)',
                            tension: 0.4,
                            fill: true,
                            pointRadius: 5,
                            pointBackgroundColor: '#dc3545'
                        },
                        {
                            label: 'Legitimate Traffic',
                            data: [0, 0, 0, 0, 0, 0, 0],
                            borderColor: '#28a745',
                            backgroundColor: 'rgba(40, 167, 69, 0.1)',
                            tension: 0.4,
                            fill: true,
                            pointRadius: 5,
                            pointBackgroundColor: '#28a745'
                        }
                    ]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    interaction: { mode: 'index', intersect: false },
                    plugins: {
                        legend: { position: 'bottom' },
                        tooltip: {
                            backgroundColor: 'rgba(0, 0, 0, 0.8)',
                            callbacks: {
                                label: function (context) {
                                    return `${context.dataset.label}: ${context.parsed.y}`;
                                }
                            }
                        }
                    },
                    scales: {
                        y: {
                            beginAtZero: true,
                            ticks: { stepSize: 1 }
                        },
                        x: { grid: { display: false } }
                    }
                }
            });
        }
    } catch (err) {
        console.error("Error initializing charts:", err);
        showError("Failed to initialize charts");
    }
}

function updateCharts(chartData, routingBreakdown) {
    // Update Routing Doughnut Chart
    if (routingChart && chartData?.routingBreakdown) {
        const rb = chartData.routingBreakdown;
        routingChart.data.labels = rb.labels || ['Honeypot', 'Real System'];
        routingChart.data.datasets[0].data = rb.data || [0, 0];
        routingChart.update('active');
    }
    // Fallback to stats if chart endpoint fails
    else if (routingChart && routingBreakdown) {
        const honeypot = routingBreakdown.honeypot?.count ?? 0;
        const real = routingBreakdown.real_system?.count ?? 0;
        routingChart.data.datasets[0].data = [honeypot, real];
        routingChart.update('active');
    }

    // Update Trend Line Chart
    if (trendChart && chartData) {
        // Use server-provided labels and order
        if (chartData.anomalyTrend?.labels) {
            trendChart.data.labels = chartData.anomalyTrend.labels;
        }

        if (chartData.anomalyTrend?.data) {
            trendChart.data.datasets[0].data = chartData.anomalyTrend.data;
        }

        if (chartData.legitimateTrends?.data) {
            trendChart.data.datasets[1].data = chartData.legitimateTrends.data;
        }

        trendChart.update('active');
    }
}

function showError(message) {
    const alertHtml = `
        <div class="alert alert-danger alert-dismissible fade show shadow-sm" role="alert">
            <i class="fas fa-exclamation-circle me-2"></i>
            <strong>Error:</strong> ${message}
            <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
        </div>
    `;
    const container = document.querySelector('.container') || document.body;
    container.insertAdjacentHTML('afterbegin', alertHtml);

    setTimeout(() => {
        const alert = container.querySelector('.alert');
        if (alert) {
            alert.classList.remove('show');
            setTimeout(() => alert.remove(), 300);
        }
    }, 6000);
}