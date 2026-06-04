import { useCallback, useEffect, useState } from 'react'
import {
  listBots,
  registerBot,
  modifyBot,
  removeBot,
  rechargeBot,
  setServicingStatus,
  simulatorConfig,
} from '../api/admin.js'
import { apiConfig as botnetConfig } from '../api/bots.js'
import BotDialog from '../components/BotDialog.jsx'
import ConfirmDialog from '../components/ConfirmDialog.jsx'

function batteryColor(level) {
  if (level >= 60) return 'var(--ok)'
  if (level >= 25) return 'var(--warn)'
  return 'var(--bad)'
}

function formatTime(iso) {
  if (!iso) return '—'
  const d = new Date(iso)
  if (Number.isNaN(d.getTime())) return iso
  return d.toLocaleString()
}

export default function BotsPage() {
  const [bots, setBots] = useState([])
  const [loading, setLoading] = useState(true)
  const [source, setSource] = useState('mock')
  const [busyId, setBusyId] = useState(null)

  const [dialog, setDialog] = useState({ open: false, mode: 'create', bot: null })
  const [deleteTarget, setDeleteTarget] = useState(null)
  const [banner, setBanner] = useState(null)

  const refresh = useCallback(async () => {
    setLoading(true)
    const { data, source } = await listBots()
    setBots(Array.isArray(data) ? data : [])
    setSource(source)
    setLoading(false)
  }, [])

  useEffect(() => {
    refresh()
  }, [refresh])

  // #51 Quick-action: recharge (double-writes battery=100 to BotNet + simulator)
  async function onRecharge(bot) {
    setBusyId(bot.id)
    const result = await rechargeBot(bot.id, bot.name)
    const sim = result?.simulator
    if (!result?.botnet?.error && sim && !sim.ok && !sim.skipped) {
      setBanner({
        tone: 'warn',
        text: `Bot #${bot.id} recharged in BotNet, but simulator sync failed: ${sim.error || 'unknown error'}.`,
      })
    } else {
      setBanner(null)
    }
    await refresh()
    setBusyId(null)
  }

  // #51 Quick-action: toggle servicing status (BotNet-only — see admin.js)
  async function onToggleServicing(bot) {
    setBusyId(bot.id)
    await setServicingStatus(bot.id, !bot.isServicingCustomer)
    await refresh()
    setBusyId(null)
  }

  // #49 Add a new bot
  async function handleCreate(values) {
    const result = await registerBot(values)
    if (!result?.botnet?.error) {
      const sim = result?.simulator
      if (sim && !sim.ok && !sim.skipped) {
        setBanner({
          tone: 'warn',
          text: `Bot "${values.name}" registered in BotNet, but simulator sync failed: ${sim.error || 'unknown error'}.`,
        })
      } else {
        setBanner(null)
      }
      await refresh()
    }
    return result
  }

  // #50 Update bot configuration
  async function handleEdit(values) {
    const id = dialog.bot.id
    const result = await modifyBot(id, values.name, values)
    if (!result?.botnet?.error) {
      const sim = result?.simulator
      if (sim && !sim.ok && !sim.skipped) {
        setBanner({
          tone: 'warn',
          text: `Bot #${id} updated in BotNet, but simulator sync failed: ${sim.error || 'unknown error'}.`,
        })
      } else {
        setBanner(null)
      }
      await refresh()
    }
    return result
  }

  // #52 Remove a bot
  async function handleDelete() {
    if (!deleteTarget) return { botnet: { error: 'no target' } }
    const result = await removeBot(deleteTarget.id, deleteTarget.name)
    if (!result?.botnet?.error) {
      const sim = result?.simulator
      if (sim && !sim.ok && !sim.skipped) {
        setBanner({
          tone: 'warn',
          text: `Bot #${deleteTarget.id} removed from BotNet, but simulator delete failed: ${sim.error || 'unknown error'}.`,
        })
      } else {
        setBanner(null)
      }
      await refresh()
    }
    return result
  }

  return (
    <section style={styles.section}>
      <header style={styles.header}>
        <div>
          <h1 style={styles.h1}>Bot Management</h1>
          <p style={styles.sub}>
            Add, configure, and inspect the delivery bot fleet.
          </p>
        </div>
        <div style={styles.headerRight}>
          <DataSourceBadge source={source} botnet={botnetConfig} sim={simulatorConfig} />
          <button
            style={styles.primaryBtn}
            onClick={() => setDialog({ open: true, mode: 'create', bot: null })}
          >
            + New Bot
          </button>
          <button style={styles.secondaryBtn} onClick={refresh} disabled={loading}>
            {loading ? 'Refreshing…' : 'Refresh'}
          </button>
        </div>
      </header>

      {banner && (
        <div
          style={{
            ...styles.banner,
            ...(banner.tone === 'warn' ? styles.bannerWarn : styles.bannerInfo),
          }}
        >
          <span>{banner.text}</span>
          <button style={styles.bannerClose} onClick={() => setBanner(null)}>
            ×
          </button>
        </div>
      )}

      <div style={styles.tableWrap}>
        <table style={styles.table}>
          <thead>
            <tr>
              <th style={styles.th}>ID</th>
              <th style={styles.th}>Name</th>
              <th style={styles.th}>Battery</th>
              <th style={styles.th}>Status</th>
              <th style={styles.th}>Servicing Customer</th>
              <th style={styles.th}>Last Updated</th>
              <th style={styles.th}>Actions</th>
            </tr>
          </thead>
          <tbody>
            {loading && bots.length === 0 && (
              <tr>
                <td style={styles.td} colSpan={7}>Loading bots…</td>
              </tr>
            )}
            {!loading && bots.length === 0 && (
              <tr>
                <td style={styles.td} colSpan={7}>
                  No bots registered. Click <strong>+ New Bot</strong> to add one.
                </td>
              </tr>
            )}
            {bots.map((bot) => (
              <tr key={bot.id}>
                <td style={styles.td}>#{bot.id}</td>
                <td style={styles.td}>{bot.name}</td>
                <td style={styles.td}>
                  <span style={{ color: batteryColor(bot.batteryLevel), fontWeight: 600 }}>
                    {bot.batteryLevel}%
                  </span>
                </td>
                <td style={styles.td}>
                  <StatusPill ok={bot.isOnline} okLabel="Online" offLabel="Offline" />
                </td>
                <td style={styles.td}>
                  <StatusPill
                    ok={bot.isServicingCustomer}
                    okLabel="Active"
                    offLabel="Idle"
                  />
                </td>
                <td style={styles.td}>{formatTime(bot.lastUpdated)}</td>
                <td style={styles.td}>
                  <div style={styles.actions}>
                    <button
                      style={styles.actionBtn}
                      onClick={() => setDialog({ open: true, mode: 'edit', bot })}
                      disabled={busyId === bot.id}
                    >
                      Edit
                    </button>
                    <button
                      style={styles.actionBtn}
                      onClick={() => onRecharge(bot)}
                      disabled={busyId === bot.id || bot.batteryLevel === 100}
                    >
                      Recharge
                    </button>
                    <button
                      style={styles.actionBtn}
                      onClick={() => onToggleServicing(bot)}
                      disabled={busyId === bot.id}
                    >
                      {bot.isServicingCustomer ? 'Mark Idle' : 'Mark Active'}
                    </button>
                    <button
                      style={{ ...styles.actionBtn, ...styles.dangerBtn }}
                      onClick={() => setDeleteTarget(bot)}
                      disabled={busyId === bot.id}
                    >
                      Delete
                    </button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <footer style={styles.footer}>
        <div>
          BotNet:{' '}
          <code style={styles.code}>
            {botnetConfig.configured ? botnetConfig.baseUrl : '(unset — mock data)'}
          </code>
        </div>
        <div>
          Simulator:{' '}
          <code style={styles.code}>
            {simulatorConfig.configured ? simulatorConfig.baseUrl : '(unset — sync disabled)'}
          </code>
        </div>
      </footer>

      <BotDialog
        mode={dialog.mode}
        open={dialog.open}
        bot={dialog.bot}
        onSubmit={dialog.mode === 'create' ? handleCreate : handleEdit}
        onClose={() => setDialog({ open: false, mode: 'create', bot: null })}
      />

      <ConfirmDialog
        open={Boolean(deleteTarget)}
        title={deleteTarget ? `Delete ${deleteTarget.name}?` : ''}
        message={
          deleteTarget
            ? `Bot #${deleteTarget.id} will be removed from BotNet and the simulator. This cannot be undone.`
            : ''
        }
        confirmLabel="Delete"
        danger
        onConfirm={handleDelete}
        onClose={() => setDeleteTarget(null)}
      />
    </section>
  )
}

function DataSourceBadge({ source, botnet, sim }) {
  const liveBotnet = source === 'api' && botnet.configured
  const simOn = sim.configured
  return (
    <div style={{ display: 'flex', gap: '0.4rem' }}>
      <span
        style={{
          ...styles.badge,
          background: liveBotnet ? 'rgba(34,197,94,0.15)' : 'rgba(245,158,11,0.15)',
          color: liveBotnet ? 'var(--ok)' : 'var(--warn)',
          borderColor: liveBotnet ? 'rgba(34,197,94,0.4)' : 'rgba(245,158,11,0.4)',
        }}
      >
        BotNet: {liveBotnet ? 'Live' : 'Mock'}
      </span>
      <span
        style={{
          ...styles.badge,
          background: simOn ? 'rgba(34,197,94,0.15)' : 'rgba(148,163,184,0.15)',
          color: simOn ? 'var(--ok)' : 'var(--text-dim)',
          borderColor: simOn ? 'rgba(34,197,94,0.4)' : 'rgba(148,163,184,0.3)',
        }}
      >
        Simulator: {simOn ? 'Live' : 'Offline'}
      </span>
    </div>
  )
}

function StatusPill({ ok, okLabel, offLabel }) {
  return (
    <span
      style={{
        ...styles.pill,
        background: ok ? 'rgba(34,197,94,0.15)' : 'rgba(148,163,184,0.15)',
        color: ok ? 'var(--ok)' : 'var(--text-dim)',
        borderColor: ok ? 'rgba(34,197,94,0.4)' : 'rgba(148,163,184,0.3)',
      }}
    >
      {ok ? okLabel : offLabel}
    </span>
  )
}

const styles = {
  section: { padding: '0 2rem 2rem' },
  header: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'flex-end',
    gap: '1rem',
    marginBottom: '1rem',
    flexWrap: 'wrap',
  },
  headerRight: { display: 'flex', alignItems: 'center', gap: '0.5rem', flexWrap: 'wrap' },
  h1: { margin: 0, fontSize: '1.75rem' },
  sub: { margin: '0.25rem 0 0', color: 'var(--text-dim)' },
  primaryBtn: {
    background: 'var(--accent)',
    color: 'white',
    border: 'none',
    padding: '0.5rem 1rem',
    borderRadius: '8px',
    fontSize: '0.9rem',
  },
  secondaryBtn: {
    background: 'transparent',
    color: 'var(--text)',
    border: '1px solid var(--border)',
    padding: '0.5rem 1rem',
    borderRadius: '8px',
    fontSize: '0.9rem',
  },
  banner: {
    padding: '0.75rem 1rem',
    borderRadius: '8px',
    border: '1px solid',
    marginBottom: '1rem',
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    gap: '0.5rem',
  },
  bannerWarn: {
    color: 'var(--warn)',
    background: 'rgba(245,158,11,0.1)',
    borderColor: 'rgba(245,158,11,0.4)',
  },
  bannerInfo: {
    color: 'var(--text)',
    background: 'rgba(37,99,235,0.1)',
    borderColor: 'rgba(37,99,235,0.4)',
  },
  bannerClose: {
    background: 'transparent',
    border: 'none',
    color: 'inherit',
    fontSize: '1.25rem',
    lineHeight: 1,
  },
  tableWrap: {
    background: 'var(--bg-elev)',
    border: '1px solid var(--border)',
    borderRadius: '12px',
    overflow: 'hidden',
  },
  table: { width: '100%', borderCollapse: 'collapse' },
  th: {
    textAlign: 'left',
    padding: '0.75rem 1rem',
    background: 'var(--bg-elev-2)',
    color: 'var(--text-dim)',
    fontWeight: 500,
    fontSize: '0.85rem',
    textTransform: 'uppercase',
    letterSpacing: '0.04em',
    borderBottom: '1px solid var(--border)',
  },
  td: {
    padding: '0.75rem 1rem',
    borderBottom: '1px solid var(--border)',
    fontSize: '0.95rem',
  },
  actions: { display: 'flex', gap: '0.35rem', flexWrap: 'wrap' },
  actionBtn: {
    background: 'transparent',
    color: 'var(--text)',
    border: '1px solid var(--border)',
    padding: '0.3rem 0.65rem',
    borderRadius: '6px',
    fontSize: '0.85rem',
  },
  dangerBtn: {
    color: 'var(--bad)',
    borderColor: 'rgba(239,68,68,0.4)',
  },
  badge: {
    padding: '0.25rem 0.65rem',
    border: '1px solid',
    borderRadius: '999px',
    fontSize: '0.78rem',
    fontWeight: 500,
  },
  pill: {
    padding: '0.2rem 0.6rem',
    border: '1px solid',
    borderRadius: '999px',
    fontSize: '0.8rem',
  },
  footer: {
    marginTop: '1rem',
    fontSize: '0.8rem',
    color: 'var(--text-dim)',
    display: 'flex',
    gap: '1.5rem',
    flexWrap: 'wrap',
  },
  code: {
    background: 'var(--bg-elev-2)',
    border: '1px solid var(--border)',
    padding: '0.15rem 0.4rem',
    borderRadius: '4px',
    fontFamily: 'ui-monospace, Consolas, monospace',
    fontSize: '0.78rem',
  },
}
