import { useEffect, useMemo, useState } from 'react'

// Reusable Add / Edit dialog for a bot.
//
// Props:
//   mode       — "create" | "edit"
//   open       — boolean
//   bot        — when mode === "edit", the bot being edited
//   onSubmit   — async ({ name, batteryLevel, isOnline, isServicingCustomer }) => result
//                returns the orchestration result so we can show partial-failure warnings
//   onClose    — () => void
export default function BotDialog({ mode, open, bot, onSubmit, onClose }) {
  const isEdit = mode === 'edit'
  const initial = useMemo(
    () => ({
      name: bot?.name ?? '',
      batteryLevel: bot?.batteryLevel ?? 100,
      isOnline: bot?.isOnline ?? true,
      isServicingCustomer: bot?.isServicingCustomer ?? false,
    }),
    [bot],
  )

  const [values, setValues] = useState(initial)
  const [submitting, setSubmitting] = useState(false)
  const [errors, setErrors] = useState({})
  const [warning, setWarning] = useState(null)

  // Reset whenever the dialog opens with a fresh bot.
  useEffect(() => {
    if (open) {
      setValues(initial)
      setErrors({})
      setWarning(null)
    }
  }, [open, initial])

  if (!open) return null

  function validate(v) {
    const next = {}
    const name = String(v.name || '').trim()
    if (!name) next.name = 'Name is required.'
    else if (name.length > 100) next.name = 'Name must be 100 characters or fewer.'

    const battery = Number(v.batteryLevel)
    if (!Number.isFinite(battery) || battery < 0 || battery > 100) {
      next.batteryLevel = 'Battery level must be between 0 and 100.'
    }
    return next
  }

  async function handleSubmit(e) {
    e.preventDefault()
    const v = {
      ...values,
      name: String(values.name).trim(),
      batteryLevel: Math.round(Number(values.batteryLevel)),
    }
    const found = validate(v)
    setErrors(found)
    if (Object.keys(found).length > 0) return

    setSubmitting(true)
    setWarning(null)
    try {
      const result = await onSubmit(v)
      // Partial failure: BotNet OK but Simulator failed
      const simResult = result?.simulator
      const botnetErr = result?.botnet?.error
      if (botnetErr) {
        setErrors({ form: botnetErr })
        return
      }
      if (simResult && !simResult.ok && !simResult.skipped) {
        setWarning(
          `Saved in BotNet registry, but simulator sync failed: ${simResult.error || 'unknown error'}.`,
        )
        // Close anyway after a beat — admin will see the warning in the page-level banner
        setTimeout(() => onClose(), 1500)
        return
      }
      onClose()
    } catch (err) {
      setErrors({ form: err?.message || 'Unexpected error.' })
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div style={styles.backdrop} onClick={onClose}>
      <div style={styles.dialog} onClick={(e) => e.stopPropagation()}>
        <header style={styles.header}>
          <h2 style={styles.title}>{isEdit ? `Edit ${bot?.name}` : 'New Bot'}</h2>
          <button style={styles.close} onClick={onClose} aria-label="Close">
            ×
          </button>
        </header>

        <form onSubmit={handleSubmit} style={styles.form}>
          <label style={styles.label}>
            <span>Name</span>
            <input
              style={styles.input}
              type="text"
              value={values.name}
              maxLength={100}
              onChange={(e) => setValues((v) => ({ ...v, name: e.target.value }))}
              autoFocus
              disabled={submitting}
            />
            {errors.name && <span style={styles.errorText}>{errors.name}</span>}
          </label>

          <label style={styles.label}>
            <span>Battery Level (0–100)</span>
            <input
              style={styles.input}
              type="number"
              min={0}
              max={100}
              value={values.batteryLevel}
              onChange={(e) =>
                setValues((v) => ({ ...v, batteryLevel: e.target.value }))
              }
              disabled={submitting}
            />
            {errors.batteryLevel && (
              <span style={styles.errorText}>{errors.batteryLevel}</span>
            )}
          </label>

          <label style={styles.checkRow}>
            <input
              type="checkbox"
              checked={values.isOnline}
              onChange={(e) => setValues((v) => ({ ...v, isOnline: e.target.checked }))}
              disabled={submitting}
            />
            <span>Online</span>
          </label>

          {isEdit && (
            <label style={styles.checkRow}>
              <input
                type="checkbox"
                checked={values.isServicingCustomer}
                onChange={(e) =>
                  setValues((v) => ({ ...v, isServicingCustomer: e.target.checked }))
                }
                disabled={submitting}
              />
              <span>Servicing a customer</span>
            </label>
          )}

          {errors.form && <div style={styles.formError}>{errors.form}</div>}
          {warning && <div style={styles.warning}>{warning}</div>}

          <footer style={styles.footer}>
            <button
              type="button"
              style={styles.cancel}
              onClick={onClose}
              disabled={submitting}
            >
              Cancel
            </button>
            <button type="submit" style={styles.submit} disabled={submitting}>
              {submitting ? 'Saving…' : isEdit ? 'Save changes' : 'Create bot'}
            </button>
          </footer>
        </form>
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
    width: 'min(420px, 92vw)',
    background: 'var(--bg-elev)',
    border: '1px solid var(--border)',
    borderRadius: '12px',
    boxShadow: '0 20px 60px rgba(0,0,0,0.45)',
    overflow: 'hidden',
  },
  header: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    padding: '0.85rem 1.25rem',
    borderBottom: '1px solid var(--border)',
  },
  title: { margin: 0, fontSize: '1.05rem' },
  close: {
    background: 'transparent',
    border: 'none',
    color: 'var(--text-dim)',
    fontSize: '1.5rem',
    lineHeight: 1,
  },
  form: {
    padding: '1.25rem',
    display: 'flex',
    flexDirection: 'column',
    gap: '0.85rem',
  },
  label: { display: 'flex', flexDirection: 'column', gap: '0.3rem' },
  input: {
    background: 'var(--bg-elev-2)',
    color: 'var(--text)',
    border: '1px solid var(--border)',
    borderRadius: '6px',
    padding: '0.5rem 0.65rem',
    fontSize: '0.95rem',
  },
  checkRow: { display: 'flex', alignItems: 'center', gap: '0.5rem' },
  errorText: { color: 'var(--bad)', fontSize: '0.8rem' },
  formError: {
    color: 'var(--bad)',
    background: 'rgba(239,68,68,0.1)',
    border: '1px solid rgba(239,68,68,0.4)',
    padding: '0.5rem 0.75rem',
    borderRadius: '6px',
    fontSize: '0.85rem',
  },
  warning: {
    color: 'var(--warn)',
    background: 'rgba(245,158,11,0.1)',
    border: '1px solid rgba(245,158,11,0.4)',
    padding: '0.5rem 0.75rem',
    borderRadius: '6px',
    fontSize: '0.85rem',
  },
  footer: {
    display: 'flex',
    justifyContent: 'flex-end',
    gap: '0.5rem',
    paddingTop: '0.5rem',
  },
  cancel: {
    background: 'transparent',
    color: 'var(--text)',
    border: '1px solid var(--border)',
    borderRadius: '8px',
    padding: '0.5rem 0.9rem',
    fontSize: '0.9rem',
  },
  submit: {
    background: 'var(--accent)',
    color: 'white',
    border: 'none',
    borderRadius: '8px',
    padding: '0.5rem 1rem',
    fontSize: '0.9rem',
  },
}
