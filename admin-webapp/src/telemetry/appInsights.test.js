import { describe, it, expect, vi, afterEach } from 'vitest'
import { trackEvent } from './appInsights.js'

describe('appInsights telemetry (final: Azure Monitor)', () => {
  afterEach(() => {
    vi.unstubAllEnvs()
    vi.resetModules()
  })

  it('is disabled when no connection string is configured', async () => {
    vi.resetModules()
    vi.stubEnv('VITE_APPINSIGHTS_CONNECTION_STRING', '')
    const mod = await import('./appInsights.js?nocache=' + Date.now())
    expect(mod.telemetryEnabled).toBe(false)
  })

  it('enables telemetry when a connection string is set', async () => {
    vi.resetModules()
    vi.stubEnv('VITE_APPINSIGHTS_CONNECTION_STRING', 'InstrumentationKey=test;IngestionEndpoint=https://x/')
    const mod = await import('./appInsights.js?nocache=' + Date.now())
    expect(mod.telemetryEnabled).toBe(true)
  })

  it('trackEvent is a safe no-op before telemetry initializes', () => {
    expect(() => trackEvent('TestEvent', { a: 1 })).not.toThrow()
  })
})
