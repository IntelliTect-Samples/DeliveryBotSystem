import { useEffect, useMemo, useRef, useState } from "react"
import {
  buildAgentContext,
  getAssistantPromptSuggestions,
  sendAgentMessage
} from "../lib/agent.js"
import { assistantStyles } from "../lib/assistantStyles.js"
import { formatOrderStatus } from "../lib/orders.js"

const starterMessages = [
  {
    id: "welcome",
    role: "assistant",
    text: "Ask about your route, ETA, latest order, or assigned robot.",
    source: "local"
  }
]

export default function AgentAssistant({ latestOrder, route }) {
  const [isOpen, setIsOpen] = useState(false)
  const [messages, setMessages] = useState(starterMessages)
  const [draft, setDraft] = useState("")
  const [isSending, setIsSending] = useState(false)
  const [error, setError] = useState("")
  const [lastWarning, setLastWarning] = useState("")
  const [connectionInfo, setConnectionInfo] = useState({
    source: "local",
    model: null
  })
  const context = useMemo(
    () => buildAgentContext(latestOrder, route),
    [latestOrder, route]
  )
  const promptSuggestions = useMemo(
    () => getAssistantPromptSuggestions(context),
    [context]
  )
  const nextMessageIdRef = useRef(1)
  const transcriptRef = useRef(null)

  useEffect(() => {
    if (!transcriptRef.current) {
      return
    }

    transcriptRef.current.scrollTop = transcriptRef.current.scrollHeight
  }, [messages, isSending])

  function createMessageId(prefix) {
    const nextValue = nextMessageIdRef.current
    nextMessageIdRef.current += 1
    return `${prefix}-${nextValue}`
  }

  async function sendMessageText(message) {
    const trimmed = message.trim()
    if (!trimmed || isSending) {
      return
    }

    const userMessage = {
      id: createMessageId("user"),
      role: "user",
      text: trimmed
    }
    const conversationHistory = [...messages, userMessage]

    setMessages(conversationHistory)
    setDraft("")
    setIsSending(true)
    setError("")
    setLastWarning("")

    try {
      const response = await sendAgentMessage(trimmed, context, {
        messages: conversationHistory
      })
      setConnectionInfo({
        source: response.source,
        model: response.model || null
      })

      setMessages((current) => [
        ...current,
        {
          id: createMessageId("assistant"),
          role: "assistant",
          text: response.reply,
          source: response.source,
          model: response.model || null
        }
      ])
      setLastWarning(response.warning || "")
    } catch (sendError) {
      setError(sendError.message)
    } finally {
      setIsSending(false)
    }
  }

  async function handleSend(event) {
    event.preventDefault()
    await sendMessageText(draft)
  }

  function resetConversation() {
    setMessages(starterMessages)
    setDraft("")
    setError("")
    setLastWarning("")
    setConnectionInfo({
      source: "local",
      model: null
    })
  }

  return (
    <>
      <button
        type="button"
        onClick={() => setIsOpen((current) => !current)}
        style={assistantStyles.fab}
        aria-expanded={isOpen}
        aria-controls="delivery-assistant-panel"
      >
        Delivery Assistant
      </button>

      {isOpen && (
        <aside id="delivery-assistant-panel" style={assistantStyles.panel}>
          <div style={assistantStyles.header}>
            <div>
              <p style={assistantStyles.kicker}>Customer support</p>
              <h2 style={assistantStyles.title}>Delivery Assistant</h2>
              <span style={assistantStyles.connectionBadge}>
                {formatAssistantSource(connectionInfo.source, connectionInfo.model)}
              </span>
            </div>

            <div style={assistantStyles.headerActions}>
              <button
                type="button"
                onClick={resetConversation}
                style={assistantStyles.secondaryButton}
              >
                New chat
              </button>

              <button
                type="button"
                onClick={() => setIsOpen(false)}
                style={assistantStyles.closeButton}
                aria-label="Close delivery assistant"
              >
                Close
              </button>
            </div>
          </div>

          <div style={assistantStyles.contextCard}>
            {latestOrder ? (
              <>
                <strong>Latest order</strong>
                <span>{latestOrder.id.slice(0, 8)}</span>
                <span>{formatOrderStatus(latestOrder.status)}</span>
                <span>{latestOrder.assignedBotId || "Waiting for bot assignment"}</span>
              </>
            ) : (
              <span>No saved order yet. Place an order to personalize the assistant.</span>
            )}
          </div>

          <div style={assistantStyles.suggestions}>
            {promptSuggestions.map((prompt) => (
              <button
                key={prompt}
                type="button"
                onClick={() => sendMessageText(prompt)}
                style={assistantStyles.suggestionButton}
                disabled={isSending}
              >
                {prompt}
              </button>
            ))}
          </div>

          <div ref={transcriptRef} style={assistantStyles.transcript}>
            {messages.map((message) => (
              <article
                key={message.id}
                style={{
                  ...assistantStyles.message,
                  ...(message.role === "user"
                    ? assistantStyles.userMessage
                    : assistantStyles.assistantMessage)
                }}
              >
                <strong style={assistantStyles.messageRole}>
                  {message.role === "user" ? "You" : "Assistant"}
                </strong>
                <p>{message.text}</p>
                {message.role === "assistant" && message.source && (
                  <span style={assistantStyles.messageMeta}>
                    {formatAssistantSource(message.source, message.model)}
                  </span>
                )}
              </article>
            ))}

            {isSending && (
              <article style={{ ...assistantStyles.message, ...assistantStyles.assistantMessage }}>
                <strong style={assistantStyles.messageRole}>Assistant</strong>
                <p>Thinking through your delivery details...</p>
              </article>
            )}
          </div>

          {lastWarning && <p style={assistantStyles.warning}>{lastWarning}</p>}
          {error && <p style={assistantStyles.error}>{error}</p>}

          <form onSubmit={handleSend} style={assistantStyles.form}>
            <textarea
              value={draft}
              onChange={(event) => setDraft(event.target.value)}
              onKeyDown={(event) => {
                if (event.key === "Enter" && !event.shiftKey) {
                  event.preventDefault()
                  void sendMessageText(draft)
                }
              }}
              placeholder="Ask about your order, route, ETA, or assigned robot"
              style={assistantStyles.textarea}
              rows={3}
            />

            <button
              type="submit"
              style={assistantStyles.submitButton}
              disabled={isSending || !draft.trim()}
            >
              {isSending ? "Sending..." : "Send"}
            </button>
          </form>
        </aside>
      )}
    </>
  )
}

function formatAssistantSource(source, model) {
  if (source === "api" && model) {
    return `Azure agent · ${model}`
  }

  if (source === "api") {
    return "Azure agent"
  }

  if (source === "fallback") {
    return "Local fallback"
  }

  return "Local assistant"
}
