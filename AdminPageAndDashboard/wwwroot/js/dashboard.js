let routingChart, trendChart;

// Read dashboard configuration from server
const dashboardConfig = {
    refreshIntervalMs: parseInt(document.querySelector('[data-refresh-interval]')?.getAttribute('data-refresh-interval') || '5000'),
    maxRequests: parseInt(document.querySelector('[data-max-requests]')?.getAttribute('data-max-requests') || '100')
};

document.addEventListener("DOMContentLoaded", function () {
    try {
        initializeCharts();
        loadDashboard();
        
        // Use configuration from appsettings.json
        const refreshInterval = dashboardConfig.refreshIntervalMs;
        if (refreshInterval > 0) {
            setInterval(loadDashboard, refreshInterval);
        }
    } catch (err) {
        console.error("Dashboard initialization error:", err);
        showError("Failed to initialize dashboard");
    }
});

function loadDashboard() {
    try {
        // TODO: Uncomment when services are running
        // loadStats();
        // loadCharts();
        // loadRecentRequests();
        
        console.log("Dashboard services disabled - waiting for services to be running");
    } catch (err) {
        console.error("Dashboard initialization error:", err);
    }
}

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

        updateStats(data);
        updateRecentRequests(data.recentRequests);
        updateCharts(data);
    } catch (err) {
        console.error("Failed to load dashboard data:", err);
        showError(`Dashboard error: ${err.message}`);
    }
}

function updateStats(data) {
    if (!data || !data.statistics) {
        console.warn("Statistics data is missing");
        return;
    }
    
    try {
        document.getElementById("totalRequests").textContent = data.statistics.total_requests || 0;
        document.getElementById("anomalyCount").textContent = data.statistics.anomaly_count || 0;
        document.getElementById("legitimateCount").textContent = data.statistics.legitimate_count || 0;
        document.getElementById("anomalyRate").textContent = (data.statistics.anomaly_rate || 0).toFixed(1) + "%";
    } catch (err) {
        console.error("Error updating stats:", err);
    }
}

function updateRecentRequests(requests) {
    const tbody = document.getElementById("recentRequests");
    if (!tbody) return;
    
    if (!requests || requests.length === 0) {
        tbody.innerHTML = "<tr><td colspan='5' class='text-center text-muted'>No requests found</td></tr>";
        return;
    }

    try {
        tbody.innerHTML = "";

        // Limit to MaxRequestsToDisplay from config
        const maxDisplay = dashboardConfig.maxRequests;
        const limitedRequests = requests.slice(0, maxDisplay);

        limitedRequests.forEach(r => {
            const row = document.createElement("tr");
            const timestamp = r.timestamp ? new Date(r.timestamp).toLocaleTimeString() : 'N/A';
            
            row.innerHTML = `
                <td>${timestamp}</td>
                <td><code>${r.client_ip || 'N/A'}</code></td>
                <td>${r.endpoint || 'N/A'}</td>
                <td><span class="badge ${r.is_anomaly ? 'bg-danger' : 'bg-success'}">
                    ${r.is_anomaly ? 'Anomaly' : 'Normal'}
                </span></td>
                <td>${r.confidence ? (r.confidence * 100).toFixed(1) + '%' : 'N/A'}</td>
            `;
            tbody.appendChild(row);
        });
    } catch (err) {
        console.error("Error updating requests table:", err);
        tbody.innerHTML = `<tr><td colspan="5" class="text-danger">Error loading requests</td></tr>`;
    }
}

function initializeCharts() {
    try {
        const ctx1 = document.getElementById('routingChart');
        if (!ctx1) {
            console.warn("routingChart element not found");
            return;
        }
        
        const chartCtx1 = ctx1.getContext('2d');
        routingChart = new Chart(chartCtx1, {
            type: 'doughnut',
            data: { 
                labels: ['Honeypot', 'Real System'], 
                datasets: [{ 
                    data: [0, 0], 
                    backgroundColor: ['#e74c3c', '#27ae60'],
                    borderColor: ['#c0392b', '#229954'],
                    borderWidth: 2
                }] 
            },
            options: { 
                responsive: true,
                maintainAspectRatio: true,
                plugins: {
                    legend: { position: 'bottom' },
                    tooltip: { enabled: true }
                }
            }
        });

        const ctx2 = document.getElementById('trendChart');
        if (!ctx2) {
            console.warn("trendChart element not found");
            return;
        }
        
        const chartCtx2 = ctx2.getContext('2d');
        trendChart = new Chart(chartCtx2, {
            type: 'line',
            data: { 
                labels: ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'], 
                datasets: [{ 
                    label: 'Anomalies', 
                    data: [0, 0, 0, 0, 0, 0, 0], 
                    borderColor: '#e74c3c', 
                    backgroundColor: 'rgba(231, 76, 60, 0.1)',
                    tension: 0.3,
                    fill: true,
                    pointRadius: 4,
                    pointBackgroundColor: '#e74c3c',
                    pointBorderColor: '#fff',
                    pointBorderWidth: 2
                }] 
            },
            options: { 
                responsive: true,
                maintainAspectRatio: true,
                plugins: {
                    legend: { position: 'bottom' }
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        min: 0
                    }
                }
            }
        });
    } catch (err) {
        console.error("Error initializing charts:", err);
        showError("Failed to initialize charts");
    }
}

function updateCharts(data) {
    if (!data || !data.statistics) {
        console.warn("Statistics data is missing for chart update");
        return;
    }

    try {
        const breakdown = data.statistics.routing_breakdown;
        if (routingChart && breakdown) {
            routingChart.data.datasets[0].data = [
                breakdown?.honeypot?.count || 0,
                breakdown?.real_system?.count || 0
            ];
            routingChart.update('none');
        }
    } catch (err) {
        console.error("Error updating charts:", err);
    }
}

function showError(message) {
    const alertHtml = `
        <div class="alert alert-danger alert-dismissible fade show" role="alert">
            <i class="fas fa-exclamation-circle"></i> ${message}
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        </div>
    `;
    const container = document.querySelector('.container');
    if (container) {
        container.insertAdjacentHTML('afterbegin', alertHtml);
    }
}