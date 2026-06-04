import { describe, it, expect, vi, afterEach } from 'vitest'

// authEnabled is derived from env, so stub the env and re-import for a
// deterministic result regardless of any local .env.local.
describe('authConfig (issue #54)', () => {
  afterEach(() => {
    vi.unstubAllEnvs()
    vi.resetModules()
  })

  it('disables auth when the Entra env vars are blank', async () => {
    vi.resetModules()
    vi.stubEnv('VITE_ENTRA_CLIENT_ID', '')
    vi.stubEnv('VITE_ENTRA_TENANT_ID', '')
    const mod = await import('./authConfig.js?nocache=' + Date.now())
    expect(mod.authEnabled).toBe(false)
  })

  it('enables auth when both client and tenant IDs are set', async () => {
    vi.resetModules()
    vi.stubEnv('VITE_ENTRA_CLIENT_ID', 'test-client-id')
    vi.stubEnv('VITE_ENTRA_TENANT_ID', 'test-tenant-id')
    const mod = await import('./authConfig.js?nocache=' + Date.now())
    expect(mod.authEnabled).toBe(true)
  })
})
