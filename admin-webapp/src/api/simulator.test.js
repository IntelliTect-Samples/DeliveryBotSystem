import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { toBotId } from './simulator.js'

describe('toBotId', () => {
  it('lowercases the input', () => {
    expect(toBotId('Bot-001')).toBe('bot-001')
  })

  it('replaces whitespace with hyphens', () => {
    expect(toBotId('Bot 42')).toBe('bot-42')
  })

  it('strips characters that are not alphanumeric or hyphens', () => {
    expect(toBotId('Bot_42!@#')).toBe('bot42')
  })

  it('handles empty / null / undefined', () => {
    expect(toBotId('')).toBe('')
    expect(toBotId(null)).toBe('')
    expect(toBotId(undefined)).toBe('')
  })

  it('trims surrounding whitespace', () => {
    expect(toBotId('  Bot-9  ')).toBe('bot-9')
  })
})

describe('simulator client behavior', () => {
  beforeEach(() => {
    vi.stubGlobal('fetch', vi.fn())
  })
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('skips the call when VITE_SIMULATOR_API_URL is unset', async () => {
    // Re-import with no env set
    vi.resetModules()
    vi.stubEnv('VITE_SIMULATOR_API_URL', '')
    const mod = await import('./simulator.js?nocache=' + Date.now())
    const result = await mod.listSimulatorBots()
    expect(result.ok).toBe(false)
    expect(result.skipped).toBe(true)
    expect(globalThis.fetch).not.toHaveBeenCalled()
  })
})
