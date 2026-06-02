// Robot Simulator API client.
// Contract from RobotSimulator/src/DeliveryBot.RobotSimulator.Api/Program.cs (PR #38).
// Falls back to a no-op when VITE_SIMULATOR_API_URL is unset so the admin app
// still works against BotNet alone.

const baseUrl = (import.meta.env.VITE_SIMULATOR_API_URL ?? '').replace(/\/+$/, '')

// Spokane city center, used as the default location for newly registered bots.
const DEFAULT_LOCATION = { latitude: 47.6588, longitude: -117.4260 }
const DEFAULT_MODEL = 'DeliveryBot-V1'

// Turn a BotNet bot name into a simulator botId.
// Simulator botIds are lowercase strings (e.g. "bot-001") per its docs.
export function toBotId(name) {
  return String(name || '')
    .toLowerCase()
    .trim()
    .replace(/\s+/g, '-')
    .replace(/[^a-z0-9-]/g, '')
}

async function call(path, init) {
  if (!baseUrl) {
    return { ok: false, skipped: true, reason: 'Simulator URL not configured' }
  }
  try {
    const res = await fetch(`${baseUrl}${path}`, init)
    if (!res.ok) {
      let detail = `HTTP ${res.status}`
      try {
        const body = await res.text()
        if (body) detail += `: ${body}`
      } catch {
        /* ignore */
      }
      return { ok: false, status: res.status, error: detail }
    }
    const data = res.status === 204 ? null : await res.json()
    return { ok: true, data }
  } catch (err) {
    return { ok: false, error: err.message }
  }
}

export function listSimulatorBots() {
  return call('/bots')
}

export function getSimulatorBot(botId) {
  return call(`/bots/${encodeURIComponent(botId)}`)
}

export function createSimulatorBot({
  botId,
  model = DEFAULT_MODEL,
  currentLocation = DEFAULT_LOCATION,
}) {
  return call('/bots', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ botId, model, currentLocation }),
  })
}

export function updateSimulatorBot(botId, payload) {
  return call(`/bots/${encodeURIComponent(botId)}`, {
    method: 'PATCH',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload),
  })
}

export function deleteSimulatorBot(botId) {
  return call(`/bots/${encodeURIComponent(botId)}`, { method: 'DELETE' })
}

export const simulatorConfig = {
  baseUrl,
  configured: Boolean(baseUrl),
  defaults: { model: DEFAULT_MODEL, location: DEFAULT_LOCATION },
}
