import { describe, it, expect } from 'vitest'
import { listOrders, ORDER_STATUSES, apiConfig } from './orders.js'

describe('orders client (issue #53)', () => {
  it('falls back to mock data when no Order Service URL is configured', async () => {
    expect(apiConfig.configured).toBe(false)

    const { data, source } = await listOrders()

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
