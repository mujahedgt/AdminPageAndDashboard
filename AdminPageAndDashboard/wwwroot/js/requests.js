let currentPage = 1;
const pageSize = 20;

document.addEventListener("DOMContentLoaded", () => {
    try {
        loadRequests(currentPage);

        const btnSearch = document.getElementById("btnSearch");
        const searchIp = document.getElementById("searchIp");

        if (btnSearch) {
            btnSearch.addEventListener("click", () => {
                currentPage = 1;
                loadRequests(currentPage);
            });
        }

        if (searchIp) {
            searchIp.addEventListener("keypress", (e) => {
                if (e.key === "Enter") {
                    currentPage = 1;
                    loadRequests(currentPage);
                }
            });
        }
    } catch (err) {
        console.error("Requests page initialization error:", err);
    }
});

async function loadRequests(page) {
    try {
        const searchIp = document.getElementById("searchIp");
        const ip = searchIp ? searchIp.value.trim() : '';
        
        let url = `/Requests/GetRequests?page=${page}&pageSize=${pageSize}`;
        if (ip) url += `&clientIp=${encodeURIComponent(ip)}`;

        console.log("Fetching from URL:", url);
        const res = await fetch(url);
        
        console.log("Response status:", res.status);
        
        if (!res.ok) {
            const errorData = await res.json().catch(() => ({ error: res.statusText }));
            throw new Error(`HTTP ${res.status}: ${errorData.error || res.statusText}`);
        }

        const data = await res.json();
        
        console.log("Data received:", data);
        
        if (!data) {
            throw new Error("No data received from server");
        }

        // Check for API error in response
        if (data.error) {
            renderError(data.error);
            renderTable(data.data || []);
            renderPagination(data.totalPages || 0, page);
            return;
        }

        renderTable(data.data || []);
        renderPagination(data.totalPages || 1, page);
        currentPage = page;
    } catch (err) {
        console.error("Failed to load requests:", err);
        renderError(err.message);
    }
}

function renderTable(requests) {
    const tbody = document.querySelector("#requestsTable tbody");
    if (!tbody) return;

    tbody.innerHTML = "";

    if (!requests || requests.length === 0) {
        tbody.innerHTML = `<tr><td colspan="8" class="text-center text-muted">No requests found</td></tr>`;
        return;
    }

    try {
        requests.forEach(r => {
            const row = document.createElement("tr");
            const timestamp = r.timestamp ? new Date(r.timestamp).toLocaleString() : 'N/A';
            
            row.innerHTML = `
                <td>${timestamp}</td>
                <td><code>${r.client_ip || 'N/A'}</code></td>
                <td>${r.endpoint || 'N/A'}</td>
                <td><span class="badge bg-secondary">${r.method || 'N/A'}</span></td>
                <td>
                    <span class="badge ${r.routed_to === 'honeypot' ? 'bg-danger' : 'bg-success'}">
                        ${r.routed_to === 'honeypot' ? 'Honeypot' : 'Real System'}
                    </span>
                </td>
                <td>
                    <span class="badge ${r.is_anomaly ? 'bg-danger' : 'bg-success'}">
                        ${r.is_anomaly ? 'Yes' : 'No'}
                    </span>
                </td>
                <td>${r.confidence ? (r.confidence * 100).toFixed(2) + '%' : 'N/A'}</td>
                <td>
                    <a href="/Requests/Details/${r.request_id}" class="btn btn-sm btn-info" title="View details">
                        <i class="fas fa-eye"></i>
                    </a>
                </td>
            `;
            tbody.appendChild(row);
        });
    } catch (err) {
        console.error("Error rendering table:", err);
        tbody.innerHTML = `<tr><td colspan="8" class="text-danger">Error rendering table: ${err.message}</td></tr>`;
    }
}

function renderPagination(totalPages, currentPageNum) {
    const ul = document.getElementById("pagination");
    if (!ul) return;

    ul.innerHTML = "";

    if (totalPages <= 1) return;

    try {
        // Previous button
        const prevLi = document.createElement("li");
        prevLi.className = `page-item ${currentPageNum === 1 ? 'disabled' : ''}`;
        const prevLink = document.createElement("a");
        prevLink.className = "page-link";
        prevLink.href = "#";
        prevLink.textContent = "Previous";
        prevLink.addEventListener("click", (e) => {
            e.preventDefault();
            if (currentPageNum > 1) loadRequests(currentPageNum - 1);
        });
        prevLi.appendChild(prevLink);
        ul.appendChild(prevLi);

        // Page numbers
        for (let i = 1; i <= totalPages; i++) {
            const li = document.createElement("li");
            li.className = `page-item ${i === currentPageNum ? 'active' : ''}`;
            const link = document.createElement("a");
            link.className = "page-link";
            link.href = "#";
            link.textContent = i;
            link.addEventListener("click", (e) => {
                e.preventDefault();
                loadRequests(i);
            });
            li.appendChild(link);
            ul.appendChild(li);
        }

        // Next button
        const nextLi = document.createElement("li");
        nextLi.className = `page-item ${currentPageNum === totalPages ? 'disabled' : ''}`;
        const nextLink = document.createElement("a");
        nextLink.className = "page-link";
        nextLink.href = "#";
        nextLink.textContent = "Next";
        nextLink.addEventListener("click", (e) => {
            e.preventDefault();
            if (currentPageNum < totalPages) loadRequests(currentPageNum + 1);
        });
        nextLi.appendChild(nextLink);
        ul.appendChild(nextLi);
    } catch (err) {
        console.error("Error rendering pagination:", err);
    }
}

function renderError(message) {
    const tbody = document.querySelector("#requestsTable tbody");
    if (tbody) {
        tbody.innerHTML = `<tr><td colspan="8" class="alert alert-warning m-2"><i class="fas fa-exclamation-triangle"></i> ${message}</td></tr>`;
    }
    
    // Log error to console for debugging
    console.warn("Table error:", message);
}