// BotNet API client.
// Contract matches BotNetApi/Controllers/BotsController.cs from PR #37 (issue #23).
// If the API is unreachable, calls fall back to mock data so the admin app can
// be demoed without the backend deployed.

import { getAuthHeaders } from '../auth/token.js'

const baseUrl = (import.meta.env.VITE_BOTNET_API_URL ?? '').replace(/\/+$/, '')

const mockBots = [
  {
    id: 1,
    name: 'Bot-001',
    batteryLevel: 87,
    lastUpdated: '2026-05-20T18:42:11Z',
    isOnline: true,
    isServicingCustomer: true,
  },
  {
    id: 2,
    name: 'Bot-002',
    batteryLevel: 23,
    lastUpdated: '2026-05-20T18:41:58Z',
    isOnline: true,
    isServicingCustomer: false,
  },
  {
    id: 3,
    name: 'Bot-003',
    batteryLevel: 100,
    lastUpdated: '2026-05-20T18:40:02Z',
    isOnline: false,
    isServicingCustomer: false,
  },
  {
    id: 4,
    name: 'Bot-004',
    batteryLevel: 64,
    lastUpdated: '2026-05-20T18:42:09Z',
    isOnline: true,
    isServicingCustomer: false,
  },
]

async function callOrMock(path, init, mockResult) {
  if (!baseUrl) {
    return { data: mockResult, source: 'mock' }
  }
  try {
    const authHeaders = await getAuthHeaders()
    const res = await fetch(`${baseUrl}${path}`, {
      ...init,
      headers: { ...(init?.headers), ...authHeaders },
    })
    if (!res.ok) throw new Error(`HTTP ${res.status}`)
    const data = res.status === 204 ? null : await res.json()
    return { data, source: 'api' }
  } catch (err) {
    console.warn(`BotNet API unreachable (${err.message}); using mock data.`)
    return { data: mockResult, source: 'mock', error: err.message }
  }
}

export function listBots() {
  return callOrMock('/api/bots', undefined, mockBots)
}

export function getBot(id) {
  return callOrMock(
    `/api/bots/${id}`,
    undefined,
    mockBots.find((b) => b.id === id) ?? null,
  )
}

export function createBot({ name, batteryLevel = 100, isOnline = true }) {
  return callOrMock(
    '/api/bots',
    {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ name, batteryLevel, isOnline }),
    },
    {
      id: Math.max(0, ...mockBots.map((b) => b.id)) + 1,
      name,
      batteryLevel,
      lastUpdated: new Date().toISOString(),
      isOnline,
      isServicingCustomer: false,
    },
  )
}

export function updateBot(id, payload) {
  return callOrMock(
    `/api/bots/${id}`,
    {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payload),
    },
    { id, ...payload, lastUpdated: new Date().toISOString() },
  )
}

export function deleteBot(id) {
  return callOrMock(`/api/bots/${id}`, { method: 'DELETE' }, null)
}

export function rechargeBot(id) {
  return callOrMock(
    `/api/bots/${id}/recharge`,
    { method: 'PUT' },
    { ...(mockBots.find((b) => b.id === id) ?? {}), batteryLevel: 100 },
  )
}

export function updateServicingStatus(id, isServicingCustomer) {
  return callOrMock(
    `/api/bots/${id}/servicing-status`,
    {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ isServicingCustomer }),
    },
    { ...(mockBots.find((b) => b.id === id) ?? {}), isServicingCustomer },
  )
}

export const apiConfig = {
  baseUrl,
  configured: Boolean(baseUrl),
}
