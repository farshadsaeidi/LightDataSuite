// src/App.jsx - Complete tester for your LightDataSuite API
import React, { useState, useCallback } from 'react';
import { loadGraphData, loadKqlData } from './test';  // ✅ Only functions

function App() {
    const [accounts, setAccounts] = useState([]);
    const [error, setError] = useState("");
    const [loading, setLoading] = useState(false);
    const [activeTab, setActiveTab] = useState("kql");

    // Load GraphQL data
    const loadGraphDataComponent = useCallback(async () => {
        try {
            setLoading(true);
            setError("");
            setActiveTab("gql");
            const result = await loadGraphData(10540);
            setAccounts(result);
        } catch (err) {
            setError(err.message);
        } finally {
            setLoading(false);
        }
    }, []);

    // Load KQL data 
    const loadKqlDataComponent = useCallback(async () => {
        try {
            setLoading(true);
            setError("");
            setActiveTab("kql");
            const result = await loadKqlData(10540);
            setAccounts(result);
        } catch (err) {
            setError(err.message);
        } finally {
            setLoading(false);
        }
    }, []);

    return (
        <div style={{
            padding: 30,
            fontFamily: 'Segoe UI, system-ui, sans-serif',
            maxWidth: 1000,
            margin: '0 auto'
        }}>
            <h1 style={{ color: '#2563eb', marginBottom: 10 }}>
                🧪 LightDataSuite API Tester
            </h1>
            <p style={{ color: '#6b7280', marginBottom: 30 }}>
                Test your KQL & GraphQL endpoints. Backend: <code>https://localhost:7190</code>
            </p>

            {/* Buttons */}
            <div style={{
                display: 'flex',
                gap: 12,
                marginBottom: 30,
                flexWrap: 'wrap'
            }}>
                <button
                    onClick={loadGraphDataComponent}
                    disabled={loading}
                    style={{
                        padding: '12px 24px',
                        fontSize: 16,
                        border: 'none',
                        borderRadius: 8,
                        background: activeTab === 'gql' ? '#10b981' : '#3b82f6',
                        color: 'white',
                        cursor: loading ? 'not-allowed' : 'pointer',
                        opacity: loading ? 0.7 : 1
                    }}
                >
                    🟢 Test GraphQL 
                </button>

                <button
                    onClick={loadKqlDataComponent}
                    disabled={loading}
                    style={{
                        padding: '12px 24px',
                        fontSize: 16,
                        border: 'none',
                        borderRadius: 8,
                        background: activeTab === 'kql' ? '#10b981' : '#3b82f6',
                        color: 'white',
                        cursor: loading ? 'not-allowed' : 'pointer',
                        opacity: loading ? 0.7 : 1
                    }}
                >
                    🔵 Test KQL 
                </button>
            </div>

            {/* Status */}
            {loading && (
                <div style={{
                    padding: 16,
                    background: '#fef3c7',
                    borderRadius: 8,
                    marginBottom: 20
                }}>
                    ⏳ Loading from API...
                </div>
            )}

            {error && (
                <div style={{
                    padding: 16,
                    background: '#fee2e2',
                    borderRadius: 8,
                    borderLeft: '4px solid #ef4444',
                    marginBottom: 20
                }}>
                    ❌ <strong>Error:</strong> {error}
                </div>
            )}

            {/* Results */}
            {accounts.length > 0 ? (
                <div>
                    <div style={{
                        padding: 12,
                        background: activeTab === 'gql' ? '#dbeafe' : '#d1fae5',
                        borderRadius: 8,
                        marginBottom: 16,
                        fontWeight: 600,
                        textTransform: 'uppercase',
                        fontSize: 14
                    }}>
                        {activeTab === 'gql' ? '📊 GraphQL Result' : '🔍 KQL Result'}
                        ({accounts.length} records)
                    </div>

                    <pre style={{
                        background: '#f8fafc',
                        padding: 20,
                        borderRadius: 12,
                        fontSize: 13,
                        lineHeight: 1.5,
                        overflow: 'auto',
                        maxHeight: 600,
                        border: '1px solid #e2e8f0'
                    }}>
                        {JSON.stringify(accounts, null, 2)}
                    </pre>
                </div>
            ) : (
                <div style={{
                    padding: 40,
                    textAlign: 'center',
                    color: '#94a3b8',
                    fontStyle: 'italic'
                }}>
                    Click a button above to test the API
                </div>
            )}
        </div>
    );
}

export default App;