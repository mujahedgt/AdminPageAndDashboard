document.addEventListener("DOMContentLoaded", () => {
    try {
        loadModelStats();
        loadModelDecisions();

        const btnRetrain = document.getElementById("btnRetrain");
        const btnTrainNew = document.getElementById("btnTrainNew");

        if (btnRetrain) {
            btnRetrain.addEventListener("click", () => {
                if (confirm("Are you sure you want to retrain the model?")) {
                    retrainModel("v1.0");
                }
            });
        }

        if (btnTrainNew) {
            btnTrainNew.addEventListener("click", () => {
                const version = prompt("Enter model version (e.g., v1.1):");
                if (version && version.trim()) {
                    trainNewModel(version.trim());
                }
            });
        }
    } catch (err) {
        console.error("Models page initialization error:", err);
    }
});

async function loadModelStats() {
    try {
        const res = await fetch("/Models/GetModelStatistics");
        if (!res.ok) {
            throw new Error(`HTTP ${res.status}: ${res.statusText}`);
        }
        const data = await res.json();
        
        if (!data) {
            throw new Error("No data received");
        }

        const currentVersionEl = document.getElementById("currentVersion");
        if (currentVersionEl) {
            currentVersionEl.textContent = data.current_version || "N/A";
        }
        
        const accuracyEl = document.getElementById("accuracy");
        if (accuracyEl) {
            accuracyEl.textContent = (data.accuracy ? data.accuracy * 100 : 0).toFixed(1) + "%";
        }
        
        const lastTrainedEl = document.getElementById("lastTrained");
        if (lastTrainedEl && data.last_trained) {
            lastTrainedEl.textContent = new Date(data.last_trained).toLocaleString();
        }
    } catch (err) {
        console.error("Failed to load model statistics:", err);
        const statusEl = document.getElementById("trainingStatus");
        if (statusEl) {
            statusEl.textContent = "Error loading model statistics";
            statusEl.className = "alert alert-danger";
        }
    }
}

async function loadModelDecisions() {
    try {
        const res = await fetch("/Models/GetRequests?page=1&pageSize=20");
        if (!res.ok) {
            throw new Error(`HTTP ${res.status}: ${res.statusText}`);
        }
        const data = await res.json();
        
        // Populate table if needed
        const tbody = document.querySelector("#modelDecisionsTable tbody");
        if (tbody && data && data.data) {
            tbody.innerHTML = "";
            data.data.forEach(d => {
                const row = document.createElement("tr");
                row.innerHTML = `
                    <td>${d.request_id || 'N/A'}</td>
                    <td><code>${d.client_ip || 'N/A'}</code></td>
                    <td><span class="badge ${d.prediction ? 'bg-danger' : 'bg-success'}">${d.prediction ? 'Anomaly' : 'Normal'}</span></td>
                    <td>${d.confidence ? (d.confidence * 100).toFixed(2) + '%' : 'N/A'}</td>
                    <td>${d.user_label ? 'Yes' : 'No'}</td>
                    <td><button class="btn btn-sm btn-warning" onclick="changeLabel(${d.request_id})">Correct</button></td>
                `;
                tbody.appendChild(row);
            });
        }
    } catch (err) {
        console.error("Failed to load model decisions:", err);
    }
}

async function retrainModel(version) {
    const statusEl = document.getElementById("trainingStatus");
    
    try {
        if (statusEl) {
            statusEl.textContent = "Retraining in progress...";
            statusEl.className = "alert alert-warning";
        }
        
        const res = await fetch("/Models/RetrainModel", {
            method: "POST",
            headers: { 
                "Content-Type": "application/json",
                "X-CSRF-TOKEN": getCSRFToken()
            },
            body: JSON.stringify({ modelVersion: version })
        });
        
        if (!res.ok) {
            throw new Error(`HTTP ${res.status}: ${res.statusText}`);
        }
        
        const result = await res.json();
        
        if (statusEl) {
            statusEl.textContent = "Retraining completed successfully!";
            statusEl.className = "alert alert-success";
        }
        
        // Reload stats after retraining
        setTimeout(loadModelStats, 2000);
    } catch (err) {
        console.error("Retrain error:", err);
        if (statusEl) {
            statusEl.textContent = "Error: " + err.message;
            statusEl.className = "alert alert-danger";
        }
    }
}

async function trainNewModel(version) {
    const statusEl = document.getElementById("trainingStatus");
    
    try {
        if (statusEl) {
            statusEl.textContent = "Training new model...";
            statusEl.className = "alert alert-warning";
        }
        
        const res = await fetch("/Models/TrainModel", {
            method: "POST",
            headers: { 
                "Content-Type": "application/json",
                "X-CSRF-TOKEN": getCSRFToken()
            },
            body: JSON.stringify({ modelVersion: version, useCorrectedLabels: true })
        });
        
        if (!res.ok) {
            throw new Error(`HTTP ${res.status}: ${res.statusText}`);
        }
        
        const result = await res.json();
        
        if (statusEl) {
            statusEl.textContent = `Model v${version} trained successfully!`;
            statusEl.className = "alert alert-success";
        }
        
        setTimeout(loadModelStats, 2000);
    } catch (err) {
        console.error("Training error:", err);
        if (statusEl) {
            statusEl.textContent = "Error: " + err.message;
            statusEl.className = "alert alert-danger";
        }
    }
}

async function changeLabel(requestId) {
    try {
        const userLabel = confirm("Mark as legitimate? (OK = Legitimate, Cancel = Anomaly)");
        
        const res = await fetch("/Models/ChangeLabel", {
            method: "POST",
            headers: { 
                "Content-Type": "application/json",
                "X-CSRF-TOKEN": getCSRFToken()
            },
            body: JSON.stringify({ requestId: requestId, label: userLabel })
        });
        
        if (!res.ok) {
            throw new Error(`HTTP ${res.status}: ${res.statusText}`);
        }
        
        alert("Label updated successfully!");
        loadModelDecisions();
    } catch (err) {
        console.error("Label change error:", err);
        alert("Error updating label: " + err.message);
    }
}

function getCSRFToken() {
    const token = document.querySelector('[name="__RequestVerificationToken"]')?.value;
    if (!token) {
        console.warn("CSRF token not found");
    }
    return token || '';
}