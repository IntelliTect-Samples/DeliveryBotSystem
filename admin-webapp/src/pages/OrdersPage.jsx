import { useCallback, useEffect, useMemo, useState } from 'react'
import { listOrders, apiConfig, ORDER_STATUSES } from '../api/orders.js'

// Color a status pill by where the order is in its lifecycle.
function statusColor(status) {
  switch (status) {
    case 'Delivered':
      return { fg: 'var(--ok)', bg: 'rgba(34,197,94,0.15)', bd: 'rgba(34,197,94,0.4)' }
    case 'InTransit':
    case 'Assigned':
      return { fg: 'var(--accent)', bg: 'rgba(37,99,235,0.15)', bd: 'rgba(37,99,235,0.4)' }
    case 'Pending':
      return { fg: 'var(--warn)', bg: 'rgba(245,158,11,0.15)', bd: 'rgba(245,158,11,0.4)' }
    case 'Cancelled':
    case 'Failed':
      return { fg: 'var(--bad)', bg: 'rgba(239,68,68,0.15)', bd: 'rgba(239,68,68,0.4)' }
    default:
      return { fg: 'var(--text-dim)', bg: 'rgba(148,163,184,0.15)', bd: 'rgba(148,163,184,0.3)' }
  }
}

function formatTime(iso) {
  if (!iso) return '—'
  const d = new Date(iso)
  if (Number.isNaN(d.getTime())) return iso
  return d.toLocaleString()
}

// CustomerId is stored as "Name:Phone" by the Order Service; show the name.
function customerName(customerId) {
  if (!customerId) return '—'
  return customerId.split(':')[0] || customerId
}

function shortId(id) {
  return id ? String(id).slice(0, 8) : '—'
}

export default function OrdersPage() {
  const [orders, setOrders] = useState([])
  const [loading, setLoading] = useState(true)
  const [source, setSource] = useState('mock')
  const [statusFilter, setStatusFilter] = useState('All')

  const refresh = useCallback(async () => {
    setLoading(true)
    const { data, source } = await listOrders()
    setOrders(Array.isArray(data) ? data : [])
    setSource(source)
    setLoading(false)
  }, [])

  useEffect(() => {
    refresh()
  }, [refresh])

  const visible = useMemo(() => {
    if (statusFilter === 'All') return orders
    return orders.filter((o) => o.status === statusFilter)
  }, [orders, statusFilter])

  const live = source === 'api' && apiConfig.configured

  return (
    <section style={styles.section}>
      <header style={styles.header}>
        <div>
          <h1 style={styles.h1}>Order Status</h1>
          <p style={styles.sub}>Read-only view of current and recent customer orders.</p>
        </div>
        <div style={styles.headerRight}>
          <span
            style={{
              ...styles.badge,
              background: live ? 'rgba(34,197,94,0.15)' : 'rgba(245,158,11,0.15)',
              color: live ? 'var(--ok)' : 'var(--warn)',
              borderColor: live ? 'rgba(34,197,94,0.4)' : 'rgba(245,158,11,0.4)',
            }}
          >
            Orders: {live ? 'Live' : 'Mock'}
          </span>
          <label style={styles.filterLabel}>
            Status:{' '}
            <select
              style={styles.select}
              value={statusFilter}
              onChange={(e) => setStatusFilter(e.target.value)}
            >
              <option value="All">All</option>
              {ORDER_STATUSES.map((s) => (
                <option key={s} value={s}>
                  {s}
                </option>
              ))}
            </select>
          </label>
          <button style={styles.secondaryBtn} onClick={refresh} disabled={loading}>
            {loading ? 'Refreshing…' : 'Refresh'}
          </button>
        </div>
      </header>

      <div style={styles.tableWrap}>
        <table style={styles.table}>
          <thead>
            <tr>
              <th style={styles.th}>Order ID</th>
              <th style={styles.th}>Customer</th>
              <th style={styles.th}>Assigned Bot</th>
              <th style={styles.th}>Status</th>
              <th style={styles.th}>Created</th>
            </tr>
          </thead>
          <tbody>
            {loading && visible.length === 0 && (
              <tr>
                <td style={styles.td} colSpan={5}>Loading orders…</td>
              </tr>
            )}
            {!loading && visible.length === 0 && (
              <tr>
                <td style={styles.td} colSpan={5}>
                  {orders.length === 0
                    ? 'No orders yet.'
                    : `No orders with status "${statusFilter}".`}
                </td>
              </tr>
            )}
            {visible.map((order) => {
              const c = statusColor(order.status)
              return (
                <tr key={order.id}>
                  <td style={styles.td}>
                    <code style={styles.code} title={order.id}>{shortId(order.id)}</code>
                  </td>
                  <td style={styles.td} title={order.customerId}>{customerName(order.customerId)}</td>
                  <td style={styles.td}>{order.assignedBotId || <span style={{ color: 'var(--text-dim)' }}>Unassigned</span>}</td>
                  <td style={styles.td}>
                    <span style={{ ...styles.pill, color: c.fg, background: c.bg, borderColor: c.bd }}>
                      {order.status}
                    </span>
                  </td>
                  <td style={styles.td}>{formatTime(order.createdAt)}</td>
                </tr>
              )
            })}
          </tbody>
        </table>
      </div>

      <footer style={styles.footer}>
        <div>
          Order Service:{' '}
          <code style={styles.code}>
            {apiConfig.configured ? apiConfig.baseUrl : '(unset — mock data)'}
          </code>
        </div>
        <div>
          Showing {visible.length} of {orders.length} order{orders.length === 1 ? '' : 's'}
        </div>
      </footer>
    </section>
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
  headerRight: { display: 'flex', alignItems: 'center', gap: '0.75rem', flexWrap: 'wrap' },
  h1: { margin: 0, fontSize: '1.75rem' },
  sub: { margin: '0.25rem 0 0', color: 'var(--text-dim)' },
  filterLabel: { color: 'var(--text-dim)', fontSize: '0.9rem' },
  select: {
    background: 'var(--bg-elev)',
    color: 'var(--text)',
    border: '1px solid var(--border)',
    padding: '0.4rem 0.6rem',
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
