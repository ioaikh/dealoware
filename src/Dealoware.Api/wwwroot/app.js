/**
 * Dealoware MVP Stage C Basic UI - JavaScript Application
 * Issue #69: Basic authenticated Participant UI
 * 
 * SECURITY NOTE:
 * - UI is NOT a security boundary
 * - All authorization enforced server-side via FieldPolicy (#31)
 * - No UI-only filtering as security control
 * - Assistant invocations bind #67 hard wall (server-side gateway)
 * - Budget status is minimal cutoff display only (#68)
 * 
 * @see specs/2026-09-28__spec__spec__mvp-stage-c-basic-ui-first-party-bot-x2.md
 */

(function() {
    'use strict';

    const API_BASE = '';
    let authToken = null;
    let currentSub = null;

    // DOM Elements
    const authSection = document.getElementById('auth-section');
    const protectedSections = document.getElementById('protected-sections');
    const authStatus = document.getElementById('auth-status');

    // Initialize
    document.addEventListener('DOMContentLoaded', init);

    function init() {
        setupAuthForms();
        setupNavigation();
        setupProtectedForms();
        checkStoredAuth();
    }

    // Authentication
    function setupAuthForms() {
        document.getElementById('register-form').addEventListener('submit', handleRegister);
        document.getElementById('login-form').addEventListener('submit', handleLogin);
    }

    async function handleRegister(e) {
        e.preventDefault();
        const displayName = document.getElementById('register-display-name').value;
        const resultBox = document.getElementById('register-result');

        try {
            const response = await fetch(`${API_BASE}/auth/register`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ displayName: displayName || null })
            });

            if (!response.ok) {
                throw new Error(`Registration failed: ${response.status}`);
            }

            const data = await response.json();
            resultBox.innerHTML = `
                <p class="success">Registration successful!</p>
                <p><strong>Sub:</strong> ${escapeHtml(data.sub)}</p>
                <p><strong>API Key:</strong> <code>${escapeHtml(data.apiKey)}</code></p>
                <p class="warning">Save your API key securely. It will not be shown again.</p>
            `;
            resultBox.classList.add('success');

            setAuth(data.apiKey, data.sub);
        } catch (error) {
            resultBox.innerHTML = `<p class="error">${escapeHtml(error.message)}</p>`;
            resultBox.classList.add('error');
        }
    }

    async function handleLogin(e) {
        e.preventDefault();
        const apiKey = document.getElementById('login-api-key').value;
        const resultBox = document.getElementById('login-result');

        try {
            const response = await fetch(`${API_BASE}/auth/token`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ apiKey })
            });

            if (!response.ok) {
                if (response.status === 401) {
                    throw new Error('Invalid API key');
                }
                throw new Error(`Login failed: ${response.status}`);
            }

            const data = await response.json();
            setAuth(apiKey, data.sub);
            resultBox.innerHTML = `<p class="success">Login successful!</p>`;
            resultBox.classList.add('success');
        } catch (error) {
            resultBox.innerHTML = `<p class="error">${escapeHtml(error.message)}</p>`;
            resultBox.classList.add('error');
        }
    }

    function setAuth(apiKey, sub) {
        authToken = apiKey;
        currentSub = sub;
        localStorage.setItem('dealoware_api_key', apiKey);
        localStorage.setItem('dealoware_sub', sub);
        updateAuthUI();
        loadInitialData();
    }

    function clearAuth() {
        authToken = null;
        currentSub = null;
        localStorage.removeItem('dealoware_api_key');
        localStorage.removeItem('dealoware_sub');
        updateAuthUI();
    }

    function checkStoredAuth() {
        const storedKey = localStorage.getItem('dealoware_api_key');
        const storedSub = localStorage.getItem('dealoware_sub');
        if (storedKey && storedSub) {
            authToken = storedKey;
            currentSub = storedSub;
            updateAuthUI();
            loadInitialData();
        }
    }

    function updateAuthUI() {
        if (authToken) {
            authSection.style.display = 'none';
            protectedSections.style.display = 'block';
            authStatus.innerHTML = `
                <span>Logged in as: ${escapeHtml(currentSub)}</span>
                <button id="logout-btn">Logout</button>
            `;
            document.getElementById('logout-btn').addEventListener('click', clearAuth);
        } else {
            authSection.style.display = 'block';
            protectedSections.style.display = 'none';
            authStatus.innerHTML = '';
        }
    }

    // Navigation
    function setupNavigation() {
        document.querySelectorAll('.nav-btn').forEach(btn => {
            btn.addEventListener('click', () => {
                const section = btn.dataset.section;
                showSection(section);
                document.querySelectorAll('.nav-btn').forEach(b => b.classList.remove('active'));
                btn.classList.add('active');
            });
        });
    }

    function showSection(sectionName) {
        document.querySelectorAll('.section.protected').forEach(s => {
            s.style.display = 'none';
        });
        const section = document.getElementById(`${sectionName}-section`);
        if (section) {
            section.style.display = 'block';
            loadSectionData(sectionName);
        }
    }

    // API Helpers
    function getAuthHeaders() {
        if (!authToken) return {};
        return {
            'Authorization': `ApiKey ${authToken}`,
            'Content-Type': 'application/json'
        };
    }

    async function apiCall(endpoint, options = {}) {
        const headers = { ...getAuthHeaders(), ...(options.headers || {}) };
        const response = await fetch(`${API_BASE}${endpoint}`, {
            ...options,
            headers
        });

        if (response.status === 401) {
            clearAuth();
            throw new Error('Authentication required');
        }

        return response;
    }

    // Load Initial Data
    async function loadInitialData() {
        await loadProfile();
    }

    function loadSectionData(section) {
        switch(section) {
            case 'profile': loadProfile(); break;
            case 'artifacts': loadArtifacts(); break;
            case 'search': break;
            case 'negotiations': loadNegotiations(); break;
            case 'strategies': loadStrategies(); break;
            case 'assistant': loadAssistantCapabilities(); break;
            case 'budget': loadBudgetStatus(); break;
        }
    }

    // Profile
    async function loadProfile() {
        const display = document.getElementById('profile-display');
        try {
            const response = await apiCall('/profile');
            if (!response.ok) {
                throw new Error(`Failed to load profile: ${response.status}`);
            }
            const profile = await response.json();
            display.innerHTML = `
                <p><strong>Sub:</strong> ${escapeHtml(profile.sub)}</p>
                ${profile.displayName ? `<p><strong>Display Name:</strong> ${escapeHtml(profile.displayName)}</p>` : ''}
                ${profile.contactEmail ? `<p><strong>Contact Email:</strong> ${escapeHtml(profile.contactEmail)}</p>` : ''}
                <p><strong>Created:</strong> ${escapeHtml(profile.createdAt)}</p>
            `;
        } catch (error) {
            display.innerHTML = `<p class="error">${escapeHtml(error.message)}</p>`;
        }
    }

    // Artifacts
    async function loadArtifacts() {
        const list = document.getElementById('artifacts-list');
        try {
            const response = await apiCall('/artifacts');
            if (!response.ok) {
                throw new Error(`Failed to load artifacts: ${response.status}`);
            }
            const artifacts = await response.json();
            if (artifacts.length === 0) {
                list.innerHTML = '<p class="empty">No artifacts yet.</p>';
            } else {
                list.innerHTML = artifacts.map(a => `
                    <div class="data-item">
                        <h4>${escapeHtml(a.subjectEntity?.name || 'Unnamed')}</h4>
                        <p>${escapeHtml(a.subjectEntity?.description || '')}</p>
                        <p><strong>Intent:</strong> ${escapeHtml(a.intent || 'N/A')}</p>
                        <p><strong>ID:</strong> <code>${escapeHtml(a.id)}</code></p>
                    </div>
                `).join('');
            }
        } catch (error) {
            list.innerHTML = `<p class="error">${escapeHtml(error.message)}</p>`;
        }
    }

    // Search (Discovery)
    async function handleSearch(e) {
        e.preventDefault();
        const query = document.getElementById('search-query').value;
        const results = document.getElementById('search-results');

        try {
            const response = await apiCall(`/search/artifacts?q=${encodeURIComponent(query)}`);
            if (!response.ok) {
                throw new Error(`Search failed: ${response.status}`);
            }
            const data = await response.json();
            if (data.results.length === 0) {
                results.innerHTML = '<p class="empty">No results found.</p>';
            } else {
                results.innerHTML = data.results.map(a => `
                    <div class="data-item discoverable">
                        <h4>${escapeHtml(a.subjectEntity?.name || 'Unnamed')}</h4>
                        <p>${escapeHtml(a.subjectEntity?.description || '')}</p>
                        <p><strong>Intent:</strong> ${escapeHtml(a.intent || 'N/A')}</p>
                        <p><strong>ID:</strong> <code>${escapeHtml(a.id)}</code></p>
                    </div>
                `).join('');
            }
        } catch (error) {
            results.innerHTML = `<p class="error">${escapeHtml(error.message)}</p>`;
        }
    }

    // Negotiations
    async function loadNegotiations() {
        const list = document.getElementById('negotiations-list');
        try {
            const response = await apiCall('/negotiations');
            if (!response.ok) {
                throw new Error(`Failed to load negotiations: ${response.status}`);
            }
            const negotiations = await response.json();
            if (negotiations.length === 0) {
                list.innerHTML = '<p class="empty">No negotiations yet.</p>';
            } else {
                list.innerHTML = negotiations.map(n => `
                    <div class="data-item" onclick="window.loadNegotiationDetail('${n.id}')">
                        <h4>Negotiation: ${escapeHtml(n.id)}</h4>
                        <p><strong>Status:</strong> ${escapeHtml(n.status)}</p>
                        <p><strong>Artifact:</strong> ${escapeHtml(n.artifactId)}</p>
                        <p><strong>Created:</strong> ${escapeHtml(n.createdAt)}</p>
                    </div>
                `).join('');
            }
        } catch (error) {
            list.innerHTML = `<p class="error">${escapeHtml(error.message)}</p>`;
        }
    }

    window.loadNegotiationDetail = async function(id) {
        const detail = document.getElementById('negotiation-detail');
        try {
            const response = await apiCall(`/negotiations/${id}`);
            if (!response.ok) {
                throw new Error(`Failed to load negotiation: ${response.status}`);
            }
            const negotiation = await response.json();
            detail.style.display = 'block';
            detail.innerHTML = `
                <h3>Negotiation Details</h3>
                <p><strong>ID:</strong> ${escapeHtml(negotiation.id)}</p>
                <p><strong>Status:</strong> ${escapeHtml(negotiation.status)}</p>
                <p><strong>Artifact:</strong> ${escapeHtml(negotiation.artifactId)}</p>
                <p><strong>Caller:</strong> ${escapeHtml(negotiation.callerParticipantId)}</p>
                <p><strong>Counterparty:</strong> ${escapeHtml(negotiation.counterpartyParticipantId)}</p>
                <h4>Offers</h4>
                ${negotiation.offers && negotiation.offers.length > 0 
                    ? negotiation.offers.map(o => `
                        <div class="offer-item">
                            <p><strong>From:</strong> ${escapeHtml(o.fromParticipantId)}</p>
                            <p><strong>Amount:</strong> ${escapeHtml(o.amount)} ${escapeHtml(o.currency)}</p>
                            <p><strong>Status:</strong> ${escapeHtml(o.status)}</p>
                        </div>
                    `).join('')
                    : '<p class="empty">No offers yet.</p>'
                }
                <button onclick="document.getElementById('negotiation-detail').style.display='none'">Close</button>
            `;
        } catch (error) {
            detail.innerHTML = `<p class="error">${escapeHtml(error.message)}</p>`;
            detail.style.display = 'block';
        }
    };

    // Strategies
    async function loadStrategies() {
        const list = document.getElementById('strategies-list');
        try {
            const response = await apiCall('/strategies');
            if (!response.ok) {
                throw new Error(`Failed to load strategies: ${response.status}`);
            }
            const strategies = await response.json();
            if (strategies.length === 0) {
                list.innerHTML = '<p class="empty">No strategies yet.</p>';
            } else {
                list.innerHTML = strategies.map(s => `
                    <div class="data-item">
                        <h4>${escapeHtml(s.name || 'Unnamed Strategy')}</h4>
                        <p><strong>ID:</strong> <code>${escapeHtml(s.id)}</code></p>
                        ${s.strategyBody ? `<p><strong>Body:</strong> ${escapeHtml(s.strategyBody)}</p>` : ''}
                        <p><strong>Created:</strong> ${escapeHtml(s.createdAt)}</p>
                    </div>
                `).join('');
            }
            populateStrategyDropdown(strategies);
        } catch (error) {
            list.innerHTML = `<p class="error">${escapeHtml(error.message)}</p>`;
        }
    }

    function populateStrategyDropdown(strategies) {
        const select = document.getElementById('assistant-strategy');
        select.innerHTML = '<option value="">No Strategy</option>' +
            strategies.map(s => `<option value="${escapeHtml(s.id)}">${escapeHtml(s.name || 'Unnamed')}</option>`).join('');
    }

    // Assistant (#66 - binds #67 hard wall server-side)
    async function loadAssistantCapabilities() {
        const display = document.getElementById('assistant-capabilities');
        const toolSelect = document.getElementById('assistant-tool');

        try {
            const response = await apiCall('/assistant/capabilities');
            if (!response.ok) {
                throw new Error(`Failed to load capabilities: ${response.status}`);
            }
            const capabilities = await response.json();
            display.innerHTML = `
                <p><strong>Available Tools:</strong> ${capabilities.availableTools.length}</p>
                <p><strong>Has Strategies:</strong> ${capabilities.hasStrategies ? 'Yes' : 'No'}</p>
                ${capabilities.strategyCount > 0 ? `<p><strong>Strategy Count:</strong> ${capabilities.strategyCount}</p>` : ''}
            `;

            toolSelect.innerHTML = '<option value="">No Tool</option>' +
                capabilities.availableTools.map(t => `<option value="${escapeHtml(t)}">${escapeHtml(t)}</option>`).join('');

            await loadStrategies();
        } catch (error) {
            display.innerHTML = `<p class="error">${escapeHtml(error.message)}</p>`;
        }
    }

    async function handleAssistantInvoke(e) {
        e.preventDefault();
        const strategyId = document.getElementById('assistant-strategy').value || null;
        const toolName = document.getElementById('assistant-tool').value || null;
        const input = document.getElementById('assistant-input').value || null;
        const resultBox = document.getElementById('assistant-result');

        try {
            const response = await apiCall('/assistant/invoke', {
                method: 'POST',
                body: JSON.stringify({
                    strategyId: strategyId || undefined,
                    toolName: toolName || undefined,
                    input: input || undefined
                })
            });

            const result = await response.json();

            if (!response.ok) {
                throw new Error(result.errorMessage || `Invoke failed: ${response.status}`);
            }

            resultBox.innerHTML = `
                <p class="success">Invocation successful!</p>
                ${result.responseText ? `<p><strong>Response:</strong> ${escapeHtml(result.responseText)}</p>` : ''}
                ${result.strategyUsed ? `
                    <p><strong>Strategy Used:</strong> ${escapeHtml(result.strategyUsed.strategyName)}</p>
                ` : ''}
                ${result.toolResult ? `
                    <p><strong>Tool:</strong> ${escapeHtml(result.toolResult.toolName)}</p>
                    <p><strong>Tool Success:</strong> ${result.toolResult.success ? 'Yes' : 'No'}</p>
                ` : ''}
            `;
            resultBox.classList.remove('error');
            resultBox.classList.add('success');
        } catch (error) {
            resultBox.innerHTML = `<p class="error">${escapeHtml(error.message)}</p>`;
            resultBox.classList.remove('success');
            resultBox.classList.add('error');
        }
    }

    // Budget Status (#68 - minimal cutoff display only)
    async function loadBudgetStatus() {
        const display = document.getElementById('budget-display');
        try {
            const response = await apiCall('/budget/status');
            if (response.status === 404) {
                display.innerHTML = '<p class="empty">No budget configured.</p>';
                return;
            }
            if (!response.ok) {
                throw new Error(`Failed to load budget: ${response.status}`);
            }
            const budget = await response.json();
            const isExhausted = budget.isExhausted;
            display.innerHTML = `
                <p><strong>Budget Remaining:</strong> ${escapeHtml(budget.remainingUnits)} / ${escapeHtml(budget.totalUnits)} units</p>
                <p><strong>Status:</strong> ${isExhausted 
                    ? '<span class="error">EXHAUSTED - Operations may be denied</span>' 
                    : '<span class="success">Available</span>'}</p>
                <p class="note">Budget cutoff is enforced server-side. When exhausted, metered operations will fail.</p>
            `;
        } catch (error) {
            display.innerHTML = `<p class="error">${escapeHtml(error.message)}</p>`;
        }
    }

    // Protected Forms
    function setupProtectedForms() {
        document.getElementById('profile-form').addEventListener('submit', handleProfileUpdate);
        document.getElementById('artifact-form').addEventListener('submit', handleArtifactCreate);
        document.getElementById('search-form').addEventListener('submit', handleSearch);
        document.getElementById('strategy-form').addEventListener('submit', handleStrategyCreate);
        document.getElementById('assistant-form').addEventListener('submit', handleAssistantInvoke);
    }

    async function handleProfileUpdate(e) {
        e.preventDefault();
        const displayName = document.getElementById('profile-display-name').value || null;
        const contactEmail = document.getElementById('profile-contact-email').value || null;

        const body = {};
        if (displayName) body.displayName = displayName;
        if (contactEmail) body.contactEmail = contactEmail;

        try {
            const response = await apiCall('/profile', {
                method: 'PATCH',
                body: JSON.stringify(body)
            });
            if (!response.ok) {
                throw new Error(`Update failed: ${response.status}`);
            }
            await loadProfile();
            alert('Profile updated successfully!');
        } catch (error) {
            alert(`Error: ${error.message}`);
        }
    }

    async function handleArtifactCreate(e) {
        e.preventDefault();
        const name = document.getElementById('artifact-name').value;
        const description = document.getElementById('artifact-description').value;
        const intent = document.getElementById('artifact-intent').value;
        const locations = document.getElementById('artifact-locations').value
            .split(',').map(l => l.trim()).filter(l => l);

        try {
            const response = await apiCall('/artifacts', {
                method: 'POST',
                body: JSON.stringify({
                    subjectEntity: {
                        name,
                        description: description || null,
                        properties: []
                    },
                    intent,
                    locations: locations.length > 0 ? locations : null
                })
            });
            if (!response.ok) {
                throw new Error(`Create failed: ${response.status}`);
            }
            document.getElementById('artifact-form').reset();
            await loadArtifacts();
            alert('Artifact created successfully!');
        } catch (error) {
            alert(`Error: ${error.message}`);
        }
    }

    async function handleStrategyCreate(e) {
        e.preventDefault();
        const name = document.getElementById('strategy-name').value;
        const strategyBody = document.getElementById('strategy-body').value;

        try {
            const response = await apiCall('/strategies', {
                method: 'POST',
                body: JSON.stringify({ name, strategyBody })
            });
            if (!response.ok) {
                throw new Error(`Create failed: ${response.status}`);
            }
            document.getElementById('strategy-form').reset();
            await loadStrategies();
            alert('Strategy created successfully!');
        } catch (error) {
            alert(`Error: ${error.message}`);
        }
    }

    // Utilities
    function escapeHtml(str) {
        if (str === null || str === undefined) return '';
        const div = document.createElement('div');
        div.textContent = String(str);
        return div.innerHTML;
    }

})();
