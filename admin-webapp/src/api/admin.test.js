import { describe, it, expect, vi, beforeEach } from 'vitest'

vi.mock('./bots.js', () => ({
  createBot: vi.fn(),
  updateBot: vi.fn(),
  deleteBot: vi.fn(),
  listBots: vi.fn(),
  rechargeBot: vi.fn(),
  updateServicingStatus: vi.fn(),
}))

vi.mock('./simulator.js', () => ({
  toBotId: (name) => String(name || '').toLowerCase().replace(/\s+/g, '-'),
  createSimulatorBot: vi.fn(),
  updateSimulatorBot: vi.fn(),
  deleteSimulatorBot: vi.fn(),
  listSimulatorBots: vi.fn(),
  simulatorConfig: { baseUrl: 'http://sim', configured: true },
}))

const bots = await import('./bots.js')
const sim = await import('./simulator.js')
const admin = await import('./admin.js')

describe('listBotsWithTelemetry (live monitoring)', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('merges simulator telemetry into BotNet bots by botId', async () => {
    bots.listBots.mockResolvedValue({
      source: 'api',
      data: [
        { id: 1, name: 'bot-001', batteryLevel: 80 },
        { id: 2, name: 'bot-002', batteryLevel: 50 },
      ],
    })
    sim.listSimulatorBots.mockResolvedValue({
      ok: true,
      data: [{ botId: 'bot-001', powerLevel: 79.4, status: 1, currentLocation: { latitude: 47.6, longitude: -117.4 } }],
    })

    const result = await admin.listBotsWithTelemetry()

    expect(result.source).toBe('api')
    expect(result.simulatorReachable).toBe(true)
    expect(result.data[0].telemetry).toEqual({
      powerLevel: 79.4,
      status: 1,
      location: { latitude: 47.6, longitude: -117.4 },
    })
    // bot-002 has no simulator match → telemetry is null.
    expect(result.data[1].telemetry).toBeNull()
  })

  it('returns bots without telemetry when the simulator is unreachable', async () => {
    bots.listBots.mockResolvedValue({ source: 'api', data: [{ id: 1, name: 'bot-001' }] })
    sim.listSimulatorBots.mockResolvedValue({ ok: false, skipped: true })

    const result = await admin.listBotsWithTelemetry()

    expect(result.simulatorReachable).toBe(false)
    expect(result.data[0].telemetry).toBeNull()
  })
})

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

describe('rechargeBot (issue #51)', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('recharges in BotNet, then sets the simulator powerLevel to 100', async () => {
    bots.rechargeBot.mockResolvedValue({ source: 'api', data: { id: 5, batteryLevel: 100 } })
    sim.updateSimulatorBot.mockResolvedValue({ ok: true, data: {} })

    const result = await admin.rechargeBot(5, 'Bot-005')

    expect(bots.rechargeBot).toHaveBeenCalledWith(5)
    expect(sim.updateSimulatorBot).toHaveBeenCalledWith('bot-005', { powerLevel: 100 })
    expect(result.botnet.data.batteryLevel).toBe(100)
    expect(result.simulator.ok).toBe(true)
  })

  it('surfaces a simulator failure as a partial-failure result', async () => {
    bots.rechargeBot.mockResolvedValue({ source: 'api', data: { id: 6 } })
    sim.updateSimulatorBot.mockResolvedValue({ ok: false, error: '404 Not Found' })

    const result = await admin.rechargeBot(6, 'Bot-006')

    expect(result.simulator.ok).toBe(false)
    expect(result.simulator.error).toContain('404')
  })

  it('does not call the simulator if BotNet recharge errored', async () => {
    bots.rechargeBot.mockResolvedValue({ source: 'api', error: '404 Not Found' })

    const result = await admin.rechargeBot(99, 'ghost')

    expect(sim.updateSimulatorBot).not.toHaveBeenCalled()
    expect(result.simulator).toBeNull()
  })
})

describe('setServicingStatus (issue #51)', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('writes to BotNet and skips the simulator (no settable status field)', async () => {
    bots.updateServicingStatus.mockResolvedValue({
      source: 'api',
      data: { id: 5, isServicingCustomer: true },
    })

    const result = await admin.setServicingStatus(5, true)

    expect(bots.updateServicingStatus).toHaveBeenCalledWith(5, true)
    expect(sim.updateSimulatorBot).not.toHaveBeenCalled()
    expect(result.botnet.data.isServicingCustomer).toBe(true)
    expect(result.simulator.skipped).toBe(true)
  })
})
