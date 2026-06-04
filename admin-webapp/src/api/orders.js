// Order Service API client (issue #53).
// Contract from OrderService/OrderService/Controllers/OrdersController.cs.
// GET /api/orders (no customerId) returns all orders for the admin view.
// Falls back to clearly-labeled mock data when VITE_ORDER_SERVICE_URL is unset
// so the admin app can be demoed without the Order Service deployed.

import { getAuthHeaders } from '../auth/token.js'

const baseUrl = (import.meta.env.VITE_ORDER_SERVICE_URL ?? '').replace(/\/+$/, '')

// Mirrors the OrderStatus enum in OrderService/Models/OrderStatus.cs.
export const ORDER_STATUSES = [
  'Pending',
  'Assigned',
  'InTransit',
  'Delivered',
  'Cancelled',
  'Failed',
]

const mockOrders = [
  {
    id: '7c1f0a2e-1111-4a01-9b01-0a1b2c3d4e01',
    customerId: 'Jane Doe:509-555-0101',
    assignedBotId: 'bot-001',
    status: 'InTransit',
    deliveryAddress: '123 Main St, Spokane, WA',
    destination: { latitude: 47.6588, longitude: -117.426 },
    items: [{ itemId: 'beverage', quantity: 2 }],
    createdAt: '2026-06-01T18:42:11Z',
  },
  {
    id: '7c1f0a2e-2222-4a02-9b02-0a1b2c3d4e02',
    customerId: 'Carlos Reyes:509-555-0188',
    assignedBotId: 'bot-002',
    status: 'Delivered',
    deliveryAddress: '900 Riverside Ave, Spokane, WA',
    destination: { latitude: 47.6601, longitude: -117.42 },
    items: [{ itemId: 'food', quantity: 1 }],
    createdAt: '2026-06-01T17:05:48Z',
  },
  {
    id: '7c1f0a2e-3333-4a03-9b03-0a1b2c3d4e03',
    customerId: 'Priya Singh:509-555-0143',
    assignedBotId: null,
    status: 'Pending',
    deliveryAddress: '55 W Boone Ave, Spokane, WA',
    destination: { latitude: 47.668, longitude: -117.41 },
    items: [{ itemId: 'package', quantity: 1 }],
    createdAt: '2026-06-01T19:12:02Z',
  },
  {
    id: '7c1f0a2e-4444-4a04-9b04-0a1b2c3d4e04',
    customerId: 'Sam Park:509-555-0120',
    assignedBotId: 'bot-003',
    status: 'Failed',
    deliveryAddress: '1200 N Division St, Spokane, WA',
    destination: { latitude: 47.67, longitude: -117.409 },
    items: [{ itemId: 'beverage', quantity: 3 }],
    createdAt: '2026-05-31T22:30:15Z',
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
    console.warn(`Order Service unreachable (${err.message}); using mock data.`)
    return { data: mockResult, source: 'mock', error: err.message }
  }
}

export function listOrders() {
  return callOrMock('/api/orders', undefined, mockOrders)
}

export const apiConfig = {
  baseUrl,
  configured: Boolean(baseUrl),
}
