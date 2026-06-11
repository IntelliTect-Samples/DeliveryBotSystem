// Orchestration layer for admin operations that must affect both BotNet
// (the device registry, source of truth) and the Robot Simulator
// (the runtime simulated bot).
//
// Pattern: BotNet is called first. If BotNet succeeds, the simulator is
// called best-effort. Partial failures are surfaced to the caller with a
// `botnet`/`simulator` shape so the UI can show specific warnings.

import {
  createBot as botnetCreate,
  updateBot as botnetUpdate,
  deleteBot as botnetDelete,
  listBots as botnetList,
  rechargeBot as botnetRecharge,
  updateServicingStatus as botnetServicing,
} from './bots.js'

import {
  toBotId,
  createSimulatorBot,
  updateSimulatorBot,
  deleteSimulatorBot,
  listSimulatorBots,
  simulatorConfig,
} from './simulator.js'

export async function listBots() {
  return botnetList()
}

// Live fleet view: the BotNet registry enriched with the simulator's live
// runtime telemetry (power level, status, location), matched by botId. Both
// calls run concurrently and the simulator side is best-effort — if it's
// unreachable, bots come back with `telemetry: null` and the registry view
// still renders.
export async function listBotsWithTelemetry() {
  const [botnetResult, simResult] = await Promise.all([
    botnetList(),
    listSimulatorBots(),
  ])

  const simBots = simResult?.ok && Array.isArray(simResult.data) ? simResult.data : []
  const byBotId = new Map(simBots.map((b) => [b.botId, b]))

  const data = (Array.isArray(botnetResult.data) ? botnetResult.data : []).map((bot) => {
    const t = byBotId.get(toBotId(bot.name))
    return {
      ...bot,
      telemetry: t
        ? { powerLevel: t.powerLevel, status: t.status, location: t.currentLocation }
        : null,
    }
  })

  return {
    data,
    source: botnetResult.source,
    simulatorReachable: Boolean(simResult?.ok),
  }
}

export async function registerBot({ name, batteryLevel = 100, isOnline = true }) {
  const botnetResult = await botnetCreate({ name, batteryLevel, isOnline })

  // BotNet failed — nothing to sync.
  if (botnetResult.source !== 'api' && botnetResult.source !== 'mock') {
    return { botnet: botnetResult, simulator: null }
  }
  if (botnetResult.error) {
    return { botnet: botnetResult, simulator: null }
  }

  // BotNet succeeded. Try to create a simulator bot using `name` as the bridge.
  const botId = toBotId(botnetResult.data?.name ?? name)
  const simResult = await createSimulatorBot({ botId })

  return { botnet: botnetResult, simulator: simResult, botId }
}

export async function modifyBot(id, name, payload) {
  const botnetResult = await botnetUpdate(id, payload)
  if (!simulatorConfig.configured) {
    return { botnet: botnetResult, simulator: { ok: false, skipped: true } }
  }

  const botId = toBotId(name)
  // Map BotNet batteryLevel (0-100 int) to simulator powerLevel (0-100 double).
  const simPayload = {}
  if (typeof payload.batteryLevel === 'number') {
    simPayload.powerLevel = payload.batteryLevel
  }
  if (Object.keys(simPayload).length === 0) {
    return { botnet: botnetResult, simulator: { ok: true, skipped: true } }
  }

  const simResult = await updateSimulatorBot(botId, simPayload)
  return { botnet: botnetResult, simulator: simResult }
}

export async function removeBot(id, name) {
  const botnetResult = await botnetDelete(id)
  if (botnetResult.error) {
    return { botnet: botnetResult, simulator: null }
  }
  const botId = toBotId(name)
  const simResult = await deleteSimulatorBot(botId)
  return { botnet: botnetResult, simulator: simResult }
}

// #51 Quick-action: recharge. BotNet sets the battery to 100; mirror that to
// the simulator's powerLevel so the runtime bot reflects the recharge.
export async function rechargeBot(id, name) {
  const botnetResult = await botnetRecharge(id)
  if (botnetResult.error) {
    return { botnet: botnetResult, simulator: null }
  }
  if (!simulatorConfig.configured) {
    return { botnet: botnetResult, simulator: { ok: false, skipped: true } }
  }
  const botId = toBotId(name)
  const simResult = await updateSimulatorBot(botId, { powerLevel: 100 })
  return { botnet: botnetResult, simulator: simResult }
}

// #51 Quick-action: toggle servicing status. BotNet is the source of truth.
// The simulator's UpdateBotRequest exposes no settable status field (BotStatus
// is managed internally by the simulation), so this is a BotNet-only write.
export async function setServicingStatus(id, isServicingCustomer) {
  const botnetResult = await botnetServicing(id, isServicingCustomer)
  return {
    botnet: botnetResult,
    simulator: { ok: true, skipped: true, reason: 'Simulator has no settable status field' },
  }
}

export { simulatorConfig }
