import { useMemo, useState } from "react"
import { Link } from "react-router-dom"
import {
  formatOrderStatus,
  getOrderTypeOptions,
  submitOrder,
  summarizeItems,
  validateOrderForm
} from "../lib/orders.js"

const initialForm = {
  merchantName: "",
  deliveryAddress: "",
  customerName: "",
  phone: "",
  orderType: "water",
  notes: ""
}

export default function CreateOrder({ onOrderCreated }) {
  const [form, setForm] = useState(initialForm)
  const [errors, setErrors] = useState({})
  const [submissionState, setSubmissionState] = useState({
    isSaving: false,
    error: "",
    warning: "",
    order: null,
    source: ""
  })
  const orderOptions = useMemo(() => getOrderTypeOptions(), [])

  function updateField(field, value) {
    setForm((current) => ({
      ...current,
      [field]: value
    }))

    setErrors((current) => {
      if (!current[field]) {
        return current
      }

      const next = { ...current }
      delete next[field]
      return next
    })
  }

  async function handleSubmit(event) {
    event.preventDefault()

    const nextErrors = validateOrderForm(form)
    if (Object.keys(nextErrors).length > 0) {
      setErrors(nextErrors)
      return
    }

    setSubmissionState({
      isSaving: true,
      error: "",
      warning: "",
      order: null,
      source: ""
    })

    try {
      const result = await submitOrder(form)
      onOrderCreated?.(result.order)
      setSubmissionState({
        isSaving: false,
        error: "",
        warning: result.warning || "",
        order: result.order,
        source: result.source
      })
    } catch (error) {
      setSubmissionState({
        isSaving: false,
        error: error.message,
        warning: "",
        order: null,
        source: ""
      })

      if (error.validationErrors) {
        setErrors(error.validationErrors)
      }
    }
  }

  return (
    <div style={styles.page}>
      <div style={styles.shell}>
        <div style={styles.orderCard}>
          <h1>Create Delivery Order</h1>

          <p style={styles.subtitle}>
            Submit an order so the site can calculate a route and the assistant
            can answer questions about it.
          </p>

          <form style={styles.form} onSubmit={handleSubmit}>
            <Field
              label="Restaurant or Store"
              value={form.merchantName}
              onChange={(value) => updateField("merchantName", value)}
              placeholder="Restaurant or Store"
            />

            <Field
              label="Delivery Address"
              value={form.deliveryAddress}
              onChange={(value) => updateField("deliveryAddress", value)}
              placeholder="Delivery Address"
              error={errors.deliveryAddress}
            />

            <Field
              label="Customer Name"
              value={form.customerName}
              onChange={(value) => updateField("customerName", value)}
              placeholder="Customer Name"
              error={errors.customerName}
            />

            <Field
              label="Phone Number"
              value={form.phone}
              onChange={(value) => updateField("phone", value)}
              placeholder="Phone Number"
              error={errors.phone}
            />

            <label style={styles.field}>
              <span style={styles.label}>Delivery Item</span>
              <select
                value={form.orderType}
                onChange={(event) => updateField("orderType", event.target.value)}
                style={styles.input}
              >
                {orderOptions.map((option) => (
                  <option key={option.value} value={option.value}>
                    {option.label}
                  </option>
                ))}
              </select>
              {errors.orderType && <span style={styles.error}>{errors.orderType}</span>}
            </label>

            <label style={styles.field}>
              <span style={styles.label}>Delivery Notes</span>
              <textarea
                value={form.notes}
                onChange={(event) => updateField("notes", event.target.value)}
                placeholder="Delivery Notes"
                style={styles.textarea}
              />
            </label>

            {submissionState.error && (
              <p style={styles.errorBanner}>{submissionState.error}</p>
            )}

            <button style={styles.button} disabled={submissionState.isSaving}>
              {submissionState.isSaving ? "Submitting..." : "Submit Order"}
            </button>
          </form>
        </div>

        <div style={styles.sidePanel}>
          <h2>OSRM and Agent</h2>
          <p style={styles.sideText}>
            After you submit an order, the home page uses the saved destination
            to calculate a route and the delivery assistant uses that same order
            for status and ETA questions.
          </p>

          {submissionState.order && (
            <div style={styles.confirmationCard}>
              <h3 style={styles.confirmationTitle}>
                Order {submissionState.order.id.slice(0, 8)}
              </h3>

              <p style={styles.confirmationLine}>
                Status: {formatOrderStatus(submissionState.order.status)}
              </p>
              <p style={styles.confirmationLine}>
                Assigned Bot: {submissionState.order.assignedBotId || "Pending"}
              </p>
              <p style={styles.confirmationLine}>
                Items: {summarizeItems(submissionState.order.items)}
              </p>
              <p style={styles.confirmationLine}>
                Source: {submissionState.source === "api" ? "Order Service" : "Local preview"}
              </p>

              {submissionState.warning && (
                <p style={styles.warningBanner}>{submissionState.warning}</p>
              )}

              <Link to="/" style={styles.linkButton}>
                View Fleet Map
              </Link>
            </div>
          )}
        </div>
      </div>
    </div>
  )
}

function Field({ label, value, onChange, placeholder, error }) {
  return (
    <label style={styles.field}>
      <span style={styles.label}>{label}</span>
      <input
        type="text"
        value={value}
        onChange={(event) => onChange(event.target.value)}
        placeholder={placeholder}
        style={styles.input}
      />
      {error && <span style={styles.error}>{error}</span>}
    </label>
  )
}

const styles = {
  page: {
    minHeight: "calc(100vh - 88px)",
    backgroundColor: "#111827",
    padding: "2rem"
  },
  shell: {
    display: "grid",
    gridTemplateColumns: "minmax(0, 1.3fr) minmax(18rem, 0.8fr)",
    gap: "1.5rem"
  },
  orderCard: {
    backgroundColor: "#1e293b",
    padding: "2.5rem",
    borderRadius: "20px",
    color: "white",
    border: "1px solid #334155",
    textAlign: "left"
  },
  subtitle: {
    color: "#cbd5e1",
    marginBottom: "2rem"
  },
  form: {
    display: "flex",
    flexDirection: "column",
    gap: "1rem"
  },
  field: {
    display: "grid",
    gap: "0.4rem"
  },
  label: {
    color: "#e2e8f0",
    fontSize: "0.9rem"
  },
  input: {
    padding: "1rem",
    borderRadius: "10px",
    border: "none",
    fontSize: "1rem"
  },
  textarea: {
    padding: "1rem",
    borderRadius: "10px",
    border: "none",
    minHeight: "100px",
    fontSize: "1rem",
    fontFamily: "inherit"
  },
  button: {
    backgroundColor: "#2563eb",
    color: "white",
    border: "none",
    padding: "1rem",
    borderRadius: "10px",
    fontSize: "1rem",
    fontWeight: "bold",
    cursor: "pointer"
  },
  sidePanel: {
    backgroundColor: "#1f2937",
    border: "1px solid #334155",
    borderRadius: "20px",
    color: "white",
    padding: "1.5rem",
    textAlign: "left"
  },
  sideText: {
    color: "#cbd5e1",
    marginBottom: "1rem"
  },
  confirmationCard: {
    backgroundColor: "#111827",
    borderRadius: "12px",
    padding: "1rem",
    marginTop: "1rem"
  },
  confirmationTitle: {
    marginTop: 0
  },
  confirmationLine: {
    color: "#e2e8f0",
    marginBottom: "0.5rem"
  },
  linkButton: {
    display: "inline-block",
    marginTop: "0.75rem",
    backgroundColor: "#2563eb",
    color: "white",
    textDecoration: "none",
    padding: "0.75rem 1rem",
    borderRadius: "10px",
    fontWeight: "bold"
  },
  error: {
    color: "#fecaca",
    fontSize: "0.85rem"
  },
  errorBanner: {
    backgroundColor: "#7f1d1d",
    color: "#fee2e2",
    padding: "0.85rem 1rem",
    borderRadius: "10px"
  },
  warningBanner: {
    backgroundColor: "#422006",
    color: "#fde68a",
    padding: "0.85rem 1rem",
    borderRadius: "10px",
    marginTop: "0.75rem"
  }
}
