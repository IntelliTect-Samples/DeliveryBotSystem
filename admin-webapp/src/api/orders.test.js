import { describe, it, expect, vi, afterEach } from 'vitest'
import { ORDER_STATUSES } from './orders.js'

describe('orders client (issue #53)', () => {
  afterEach(() => {
    vi.unstubAllEnvs()
    vi.resetModules()
  })

  it('falls back to mock data when no Order Service URL is configured', async () => {
    // Stub the env empty and re-import so the result is deterministic even when
    // a local .env.local supplies a real URL.
    vi.resetModules()
    vi.stubEnv('VITE_ORDER_SERVICE_URL', '')
    const mod = await import('./orders.js?nocache=' + Date.now())

    expect(mod.apiConfig.configured).toBe(false)

    const { data, source } = await mod.listOrders()
    expect(source).toBe('mock')
    expect(Array.isArray(data)).toBe(true)
    expect(data.length).toBeGreaterThan(0)
    // Every mock order carries the fields the admin table renders.
    for (const order of data) {
      expect(order).toHaveProperty('id')
      expect(order).toHaveProperty('customerId')
      expect(order).toHaveProperty('status')
      expect(order).toHaveProperty('createdAt')
    }
  })

  it('exposes the OrderStatus values for the status filter', () => {
    expect(ORDER_STATUSES).toContain('Pending')
    expect(ORDER_STATUSES).toContain('InTransit')
    expect(ORDER_STATUSES).toContain('Delivered')
    expect(ORDER_STATUSES).toContain('Failed')
  })
})
