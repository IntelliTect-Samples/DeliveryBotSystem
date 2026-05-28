import { useState } from 'react'

// Generic confirm dialog. Caller controls open state and supplies onConfirm,
// which may return a Promise. While the promise is pending the buttons are
// disabled so accidental double-confirms can't fire.
export default function ConfirmDialog({
  open,
  title,
  message,
  confirmLabel = 'Confirm',
  cancelLabel = 'Cancel',
  danger = false,
  onConfirm,
  onClose,
}) {
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState(null)
  const [warning, setWarning] = useState(null)

  if (!open) return null

  async function handleConfirm() {
    setBusy(true)
    setError(null)
    setWarning(null)
    try {
      const result = await onConfirm()
      const simResult = result?.simulator
      if (result?.botnet?.error) {
        setError(result.botnet.error)
        return
      }
      if (simResult && !simResult.ok && !simResult.skipped) {
        setWarning(
          `Removed from BotNet registry, but simulator delete failed: ${simResult.error || 'unknown error'}.`,
        )
        setTimeout(() => onClose(), 1500)
        return
      }
      onClose()
    } catch (err) {
      setError(err?.message || 'Unexpected error.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <div style={styles.backdrop} onClick={busy ? undefined : onClose}>
      <div style={styles.dialog} onClick={(e) => e.stopPropagation()}>
        <h2 style={styles.title}>{title}</h2>
        <p style={styles.message}>{message}</p>
        {error && <div style={styles.error}>{error}</div>}
        {warning && <div style={styles.warning}>{warning}</div>}
        <footer style={styles.footer}>
          <button style={styles.cancel} onClick={onClose} disabled={busy}>
            {cancelLabel}
          </button>
          <button
            style={{ ...styles.confirm, ...(danger ? styles.confirmDanger : {}) }}
            onClick={handleConfirm}
            disabled={busy}
          >
            {busy ? 'Working…' : confirmLabel}
          </button>
        </footer>
      </div>
    </div>
  )
}

const styles = {
  backdrop: {
    position: 'fixed',
    inset: 0,
    background: 'rgba(0,0,0,0.55)',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    zIndex: 100,
  },
  dialog: {
    width: 'min(400px, 92vw)',
    background: 'var(--bg-elev)',
    border: '1px solid var(--border)',
    borderRadius: '12px',
    padding: '1.25rem',
    boxShadow: '0 20px 60px rgba(0,0,0,0.45)',
  },
  title: { margin: '0 0 0.5rem', fontSize: '1.05rem' },
  message: { margin: '0 0 1rem', color: 'var(--text-dim)' },
  error: {
    color: 'var(--bad)',
    background: 'rgba(239,68,68,0.1)',
    border: '1px solid rgba(239,68,68,0.4)',
    padding: '0.5rem 0.75rem',
    borderRadius: '6px',
    fontSize: '0.85rem',
    marginBottom: '0.75rem',
  },
  warning: {
    color: 'var(--warn)',
    background: 'rgba(245,158,11,0.1)',
    border: '1px solid rgba(245,158,11,0.4)',
    padding: '0.5rem 0.75rem',
    borderRadius: '6px',
    fontSize: '0.85rem',
    marginBottom: '0.75rem',
  },
  footer: { display: 'flex', justifyContent: 'flex-end', gap: '0.5rem' },
  cancel: {
    background: 'transparent',
    color: 'var(--text)',
    border: '1px solid var(--border)',
    borderRadius: '8px',
    padding: '0.5rem 0.9rem',
    fontSize: '0.9rem',
  },
  confirm: {
    background: 'var(--accent)',
    color: 'white',
    border: 'none',
    borderRadius: '8px',
    padding: '0.5rem 1rem',
    fontSize: '0.9rem',
  },
  confirmDanger: {
    background: 'var(--bad)',
  },
}
