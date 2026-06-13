export const assistantStyles = {
  fab: {
    position: "fixed",
    right: "1.5rem",
    bottom: "1.5rem",
    zIndex: 5000,
    border: "none",
    borderRadius: "999px",
    padding: "0.95rem 1.2rem",
    background: "#f97316",
    color: "#fff7ed",
    fontWeight: 700,
    boxShadow: "0 14px 40px rgba(15, 23, 42, 0.35)",
    cursor: "pointer"
  },
  panel: {
    position: "fixed",
    right: "1.5rem",
    bottom: "5.5rem",
    width: "min(28rem, calc(100vw - 2rem))",
    height: "min(44rem, calc(100vh - 7rem))",
    zIndex: 5001,
    borderRadius: "24px",
    background: "#fff7ed",
    color: "#7c2d12",
    boxShadow: "0 28px 60px rgba(15, 23, 42, 0.32)",
    border: "1px solid rgba(249, 115, 22, 0.25)",
    display: "flex",
    flexDirection: "column",
    overflow: "hidden"
  },
  header: {
    display: "flex",
    justifyContent: "space-between",
    alignItems: "flex-start",
    gap: "1rem",
    padding: "1.1rem 1.1rem 0.9rem"
  },
  headerActions: {
    display: "flex",
    gap: "0.5rem",
    alignItems: "center"
  },
  kicker: {
    fontSize: "0.72rem",
    textTransform: "uppercase",
    letterSpacing: "0.12em",
    fontWeight: 700,
    color: "#ea580c",
    marginBottom: "0.3rem"
  },
  title: {
    margin: 0,
    fontSize: "1.2rem",
    lineHeight: 1.15
  },
  connectionBadge: {
    display: "inline-flex",
    marginTop: "0.45rem",
    padding: "0.2rem 0.55rem",
    borderRadius: "999px",
    background: "#fff",
    color: "#9a3412",
    fontSize: "0.75rem",
    border: "1px solid rgba(124, 45, 18, 0.12)"
  },
  closeButton: {
    border: "1px solid rgba(124, 45, 18, 0.15)",
    background: "#ffedd5",
    color: "#9a3412",
    borderRadius: "999px",
    padding: "0.45rem 0.8rem",
    cursor: "pointer"
  },
  secondaryButton: {
    border: "1px solid rgba(124, 45, 18, 0.15)",
    background: "#fff",
    color: "#9a3412",
    borderRadius: "999px",
    padding: "0.45rem 0.8rem",
    cursor: "pointer"
  },
  contextCard: {
    margin: "0 1.1rem",
    padding: "0.85rem 0.95rem",
    borderRadius: "18px",
    background: "#ffedd5",
    display: "grid",
    gap: "0.2rem",
    textAlign: "left",
    fontSize: "0.92rem"
  },
  suggestions: {
    display: "flex",
    flexWrap: "wrap",
    gap: "0.5rem",
    padding: "0.85rem 1.1rem 0"
  },
  suggestionButton: {
    border: "1px solid rgba(124, 45, 18, 0.14)",
    background: "#fff",
    color: "#9a3412",
    borderRadius: "999px",
    padding: "0.45rem 0.75rem",
    fontSize: "0.8rem",
    cursor: "pointer"
  },
  transcript: {
    display: "flex",
    flex: "1 1 auto",
    flexDirection: "column",
    gap: "0.7rem",
    padding: "1rem 1.1rem",
    overflowY: "auto",
    minHeight: 0
  },
  message: {
    borderRadius: "18px",
    padding: "0.85rem 0.95rem",
    textAlign: "left",
    lineHeight: 1.45
  },
  userMessage: {
    alignSelf: "flex-end",
    background: "#fdba74",
    color: "#7c2d12",
    maxWidth: "85%"
  },
  assistantMessage: {
    alignSelf: "flex-start",
    background: "#fff",
    color: "#7c2d12",
    border: "1px solid rgba(124, 45, 18, 0.1)",
    maxWidth: "90%"
  },
  messageRole: {
    display: "block",
    marginBottom: "0.3rem",
    fontSize: "0.75rem",
    textTransform: "uppercase",
    letterSpacing: "0.08em"
  },
  messageMeta: {
    display: "block",
    marginTop: "0.45rem",
    fontSize: "0.72rem",
    color: "#9a3412",
    opacity: 0.78
  },
  warning: {
    margin: "0 1.1rem 0.7rem",
    padding: "0.7rem 0.85rem",
    borderRadius: "14px",
    background: "#ffedd5",
    color: "#9a3412",
    textAlign: "left",
    fontSize: "0.88rem"
  },
  error: {
    margin: "0 1.1rem 0.7rem",
    padding: "0.7rem 0.85rem",
    borderRadius: "14px",
    background: "#fee2e2",
    color: "#991b1b",
    textAlign: "left",
    fontSize: "0.88rem"
  },
  form: {
    padding: "0 1.1rem 1.1rem",
    display: "grid",
    gap: "0.75rem",
    borderTop: "1px solid rgba(124, 45, 18, 0.08)",
    paddingTop: "0.9rem",
    background: "#fff7ed"
  },
  textarea: {
    width: "100%",
    minHeight: "5.25rem",
    resize: "vertical",
    borderRadius: "18px",
    border: "1px solid rgba(124, 45, 18, 0.15)",
    padding: "0.9rem 1rem",
    boxSizing: "border-box",
    font: "inherit",
    color: "#7c2d12",
    background: "#fff"
  },
  submitButton: {
    justifySelf: "flex-end",
    border: "none",
    borderRadius: "999px",
    background: "#ea580c",
    color: "#fff7ed",
    fontWeight: 700,
    padding: "0.75rem 1.15rem",
    cursor: "pointer"
  }
}
