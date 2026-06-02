import { describe, it, expect } from 'vitest'
import { authEnabled, ADMIN_GROUP_ID } from './authConfig.js'

describe('authConfig (issue #54)', () => {
  it('disables auth when the Entra env vars are blank', () => {
    // The scaffold ships with blank VITE_ENTRA_* so the app runs open until an
    // app registration exists. This guards against accidentally shipping a
    // half-configured auth setup that locks everyone out.
    expect(authEnabled).toBe(false)
    expect(ADMIN_GROUP_ID).toBe('')
  })
})
