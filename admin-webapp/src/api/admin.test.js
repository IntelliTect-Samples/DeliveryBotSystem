import { describe, it, expect, vi, beforeEach } from 'vitest'

vi.mock('./bots.js', () => ({
  createBot: vi.fn(),
  updateBot: vi.fn(),
  deleteBot: vi.fn(),
  listBots: vi.fn(),
}))

vi.mock('./simulator.js', () => ({
  toBotId: (name) => String(name || '').toLowerCase().replace(/\s+/g, '-'),
  createSimulatorBot: vi.fn(),
  updateSimulatorBot: vi.fn(),
  deleteSimulatorBot: vi.fn(),
  simulatorConfig: { baseUrl: 'http://sim', configured: true },
}))

const bots = await import('./bots.js')
const sim = await import('./simulator.js')
const admin = await import('./admin.js')

describe('registerBot (issue #49)', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('creates in BotNet first, then in the simulator', async () => {
    bots.createBot.mockResolvedValue({
      source: 'api',
      data: { id: 5, name: 'Bot-005', batteryLevel: 100, isOnline: true },
    })
    sim.createSimulatorBot.mockResolvedValue({ ok: true, data: { botId: 'bot-005' } })

    const result = await admin.registerBot({ name: 'Bot-005', batteryLevel: 100 })

    expect(bots.createBot).toHaveBeenCalledOnce()
    expect(sim.createSimulatorBot).toHaveBeenCalledWith({ botId: 'bot-005' })
    expect(result.botnet.data.id).toBe(5)
    expect(result.simulator.ok).toBe(true)
  })

  it('surfaces simulator failure as a partial-failure result', async () => {
    bots.createBot.mockResolvedValue({
      source: 'api',
      data: { id: 6, name: 'Bot-006' },
    })
    sim.createSimulatorBot.mockResolvedValue({ ok: false, error: '409 Conflict' })

    const result = await admin.registerBot({ name: 'Bot-006' })

    expect(result.botnet.data.id).toBe(6)
    expect(result.simulator.ok).toBe(false)
    expect(result.simulator.error).toContain('409')
  })

  it('does not call the simulator if BotNet errored', async () => {
    bots.createBot.mockResolvedValue({
      source: 'api',
      data: null,
      error: '400 Bad Request',
    })

    const result = await admin.registerBot({ name: '' })

    expect(sim.createSimulatorBot).not.toHaveBeenCalled()
    expect(result.botnet.error).toBe('400 Bad Request')
    expect(result.simulator).toBeNull()
  })
})

describe('removeBot (issue #52)', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('deletes from BotNet, then from the simulator using the name as bridge', async () => {
    bots.deleteBot.mockResolvedValue({ source: 'api', data: null })
    sim.deleteSimulatorBot.mockResolvedValue({ ok: true, data: null })

    const result = await admin.removeBot(5, 'Bot-005')

    expect(bots.deleteBot).toHaveBeenCalledWith(5)
    expect(sim.deleteSimulatorBot).toHaveBeenCalledWith('bot-005')
    expect(result.botnet.data).toBeNull()
    expect(result.simulator.ok).toBe(true)
  })

  it('skips the simulator call when BotNet errored', async () => {
    bots.deleteBot.mockResolvedValue({ source: 'api', error: '404 Not Found' })

    const result = await admin.removeBot(99, 'ghost')

    expect(sim.deleteSimulatorBot).not.toHaveBeenCalled()
    expect(result.botnet.error).toBe('404 Not Found')
    expect(result.simulator).toBeNull()
  })
})

describe('modifyBot (issue #50)', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('updates BotNet and maps batteryLevel → powerLevel on the simulator', async () => {
    bots.updateBot.mockResolvedValue({ source: 'api', data: { id: 5, batteryLevel: 42 } })
    sim.updateSimulatorBot.mockResolvedValue({ ok: true, data: {} })

    const result = await admin.modifyBot(5, 'Bot-005', {
      name: 'Bot-005',
      batteryLevel: 42,
      isOnline: true,
      isServicingCustomer: false,
    })

    expect(bots.updateBot).toHaveBeenCalledOnce()
    expect(sim.updateSimulatorBot).toHaveBeenCalledWith('bot-005', { powerLevel: 42 })
    expect(result.simulator.ok).toBe(true)
  })

  it('skips simulator if there are no mappable fields to update', async () => {
    bots.updateBot.mockResolvedValue({ source: 'api', data: { id: 5 } })

    const result = await admin.modifyBot(5, 'Bot-005', {
      name: 'Bot-005',
      isOnline: false,
      isServicingCustomer: false,
    })

    expect(sim.updateSimulatorBot).not.toHaveBeenCalled()
    expect(result.simulator.skipped).toBe(true)
  })
})
