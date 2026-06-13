import test from "node:test"
import assert from "node:assert/strict"
import {
  buildAgentContext,
  buildAgentPayload,
  buildLocalReply,
  getAssistantPromptSuggestions,
  mapConversationHistory,
  normalizeAgentResponse,
  sendAgentMessage
} from "./agent.js"

const latestOrder = {
  id: "12345678-1111-2222-3333-abcdefabcdef",
  status: "Assigned",
  assignedBotId: "bot-001",
  deliveryAddress: "123 Main St, Spokane, WA",
  items: [
    { itemId: "water", quantity: 1 },
    { itemId: "chips", quantity: 2 }
  ]
}

const route = {
  distanceMeters: 2400,
  durationSeconds: 780,
  source: "osrm"
}

test("buildAgentContext exposes the latest order and route summary", () => {
  const context = buildAgentContext(latestOrder, route)

  assert.equal(context.latestOrder.assignedBotId, "bot-001")
  assert.equal(context.latestOrder.itemsSummary, "water x1, chips x2")
  assert.equal(context.route.distance, "2.4 km")
  assert.equal(context.route.eta, "13 min")
})

test("buildAgentContext handles missing order and route data", () => {
  const context = buildAgentContext(null, null)

  assert.equal(context.latestOrder, null)
  assert.equal(context.route.available, false)
})

test("getAssistantPromptSuggestions adapts to the available context", () => {
  assert.deepEqual(getAssistantPromptSuggestions(buildAgentContext(null, null)), [
    "What can you help with?",
    "How do routes work?",
    "What happens after I place an order?"
  ])

  assert.deepEqual(getAssistantPromptSuggestions(buildAgentContext(latestOrder, null)), [
    "What is my order status?",
    "Which robot is assigned?",
    "Why is there no route?",
    "What items are in my order?"
  ])

  assert.deepEqual(getAssistantPromptSuggestions(buildAgentContext(latestOrder, route)), [
    "What is my order status?",
    "Which robot is assigned?",
    "What is the ETA?",
    "Where is the delivery going?",
    "Summarize my delivery"
  ])
})

test("buildAgentPayload trims the message and includes context", () => {
  const context = buildAgentContext(latestOrder, route)
  const payload = buildAgentPayload("  status update  ", context, [
    { role: "assistant", text: "Hello there" },
    { role: "user", text: "What is my order?" }
  ])

  assert.equal(payload.message, "status update")
  assert.equal(payload.context.latestOrder.status, "Assigned")
  assert.equal(payload.history.length, 2)
})

test("mapConversationHistory keeps the latest non-empty chat turns", () => {
  const history = mapConversationHistory([
    { role: "system", text: "ignore" },
    { role: "assistant", text: "First" },
    { role: "user", text: "Second" },
    { role: "assistant", text: "  " },
    { role: "user", text: "Third" }
  ])

  assert.deepEqual(history, [
    { role: "assistant", text: "First" },
    { role: "user", text: "Second" },
    { role: "user", text: "Third" }
  ])
})

test("mapConversationHistory keeps only the latest eight non-empty turns", () => {
  const history = mapConversationHistory([
    { role: "assistant", text: "1" },
    { role: "user", text: "2" },
    { role: "assistant", text: "3" },
    { role: "user", text: "4" },
    { role: "assistant", text: "5" },
    { role: "user", text: "6" },
    { role: "assistant", text: "7" },
    { role: "user", text: "8" },
    { role: "assistant", text: "9" }
  ])

  assert.deepEqual(history, [
    { role: "user", text: "2" },
    { role: "assistant", text: "3" },
    { role: "user", text: "4" },
    { role: "assistant", text: "5" },
    { role: "user", text: "6" },
    { role: "assistant", text: "7" },
    { role: "user", text: "8" },
    { role: "assistant", text: "9" }
  ])
})

test("buildLocalReply answers ETA questions without repeating distance", () => {
  const context = buildAgentContext(latestOrder, route)
  const reply = buildLocalReply("What is the eta?", context)

  assert.match(reply, /13 min/i)
  assert.doesNotMatch(reply, /2.4 km/i)
})

test("buildLocalReply answers route distance questions distinctly", () => {
  const context = buildAgentContext(latestOrder, route)
  const reply = buildLocalReply("How far is the route?", context)

  assert.match(reply, /2.4 km/i)
  assert.doesNotMatch(reply, /13 min/i)
})

test("buildLocalReply answers route questions with both distance and ETA", () => {
  const context = buildAgentContext(latestOrder, route)
  const reply = buildLocalReply("Show me the route details", context)

  assert.match(reply, /2.4 km/i)
  assert.match(reply, /13 min/i)
})

test("buildLocalReply answers destination questions distinctly", () => {
  const context = buildAgentContext(latestOrder, route)
  const reply = buildLocalReply("Where is the delivery going?", context)

  assert.match(reply, /123 Main St/i)
})

test("buildLocalReply answers latest order questions with the current order details", () => {
  const context = buildAgentContext(latestOrder, route)
  const reply = buildLocalReply("What is my order?", context)

  assert.match(reply, /Assigned/i)
  assert.match(reply, /bot-001/i)
})

test("buildLocalReply can list order items", () => {
  const context = buildAgentContext(latestOrder, route)
  const reply = buildLocalReply("What items are in my order?", context)

  assert.match(reply, /water x1/i)
  assert.match(reply, /chips x2/i)
})

test("buildLocalReply can summarize the active delivery", () => {
  const context = buildAgentContext(latestOrder, route)
  const reply = buildLocalReply("Summarize my delivery", context)

  assert.match(reply, /12345678/i)
  assert.match(reply, /13 min/i)
  assert.match(reply, /123 Main St/i)
})

test("buildLocalReply can provide the order number", () => {
  const context = buildAgentContext(latestOrder, route)
  const reply = buildLocalReply("What is my order number?", context)

  assert.match(reply, /12345678/i)
})

test("buildLocalReply answers assigned bot questions directly", () => {
  const context = buildAgentContext(latestOrder, route)
  const reply = buildLocalReply("Which robot is assigned?", context)

  assert.match(reply, /bot-001/i)
})

test("buildLocalReply explains when no robot is assigned yet", () => {
  const context = buildAgentContext(
    {
      ...latestOrder,
      assignedBotId: null
    },
    route
  )
  const reply = buildLocalReply("Which robot is assigned?", context)

  assert.match(reply, /does not have an assigned robot yet/i)
})

test("buildLocalReply answers help questions with supported topics", () => {
  const context = buildAgentContext(latestOrder, route)
  const reply = buildLocalReply("help", context)

  assert.match(reply, /ETA/i)
  assert.match(reply, /destination/i)
  assert.match(reply, /order number/i)
  assert.match(reply, /ordered items/i)
})

test("buildLocalReply answers greeting questions with a concise intro", () => {
  const context = buildAgentContext(latestOrder, route)
  const reply = buildLocalReply("hello", context)

  assert.match(reply, /help with order/i)
  assert.match(reply, /route/i)
})

test("buildLocalReply explains why route details are unavailable", () => {
  const context = buildAgentContext(latestOrder, null)
  const reply = buildLocalReply("Why is there no route?", context)

  assert.match(reply, /no route/i)
  assert.match(reply, /supported demo destinations/i)
})

test("buildLocalReply can describe the route source", () => {
  const context = buildAgentContext(latestOrder, route)
  const reply = buildLocalReply("What is the route source?", context)

  assert.match(reply, /OSRM/i)
})

test("buildLocalReply explains when no ETA is available", () => {
  const context = buildAgentContext(latestOrder, null)
  const reply = buildLocalReply("When will it arrive?", context)

  assert.match(reply, /ETA is not available/i)
})

test("buildLocalReply explains what happens next when a route exists", () => {
  const context = buildAgentContext(latestOrder, route)
  const reply = buildLocalReply("What happens next?", context)

  assert.match(reply, /assigned to a robot/i)
  assert.match(reply, /ETA/i)
})

test("normalizeAgentResponse reads several common response shapes", () => {
  assert.equal(normalizeAgentResponse({ reply: "one" }), "one")
  assert.equal(normalizeAgentResponse({ message: "one-b" }), "one-b")
  assert.equal(normalizeAgentResponse({ content: "one-c" }), "one-c")
  assert.equal(normalizeAgentResponse({ answer: "two" }), "two")
  assert.equal(
    normalizeAgentResponse({ choices: [{ message: { content: "three" } }] }),
    "three"
  )
})

test("sendAgentMessage uses the local responder when no agent service URL is configured", async () => {
  const context = buildAgentContext(latestOrder, route)
  const result = await sendAgentMessage("What is the order status?", context)

  assert.equal(result.source, "local")
  assert.match(result.reply, /Assigned/i)
})

test("sendAgentMessage returns the API reply when the agent service succeeds", async () => {
  const context = buildAgentContext(latestOrder, route)
  const requests = []
  const result = await sendAgentMessage("What is the eta?", context, {
    agentApiUrl: "https://agent.example.com",
    messages: [
      { role: "assistant", text: "How can I help?" }
    ],
    fetchImpl: async (url, options) => {
      requests.push({ url, options })
      return {
        ok: true,
        async json() {
          return {
            reply: "The current ETA is about 13 min.",
            model: "gpt-4o-mini"
          }
        }
      }
    }
  })

  assert.equal(result.source, "api")
  assert.equal(result.reply, "The current ETA is about 13 min.")
  assert.equal(result.model, "gpt-4o-mini")
  assert.equal(requests[0].url, "https://agent.example.com/chat")
  assert.match(requests[0].options.body, /"message":"What is the eta\?"/)
  assert.match(requests[0].options.body, /"history"/)
  assert.match(requests[0].options.body, /How can I help\?/)
})

test("sendAgentMessage falls back to the local responder when the agent service fails", async () => {
  const context = buildAgentContext(latestOrder, route)
  const result = await sendAgentMessage("Which robot is assigned?", context, {
    agentApiUrl: "https://agent.example.com",
    fetchImpl: async () => ({
      ok: false,
      status: 502,
      async json() {
        return {}
      }
    })
  })

  assert.equal(result.source, "fallback")
  assert.match(result.reply, /bot-001/i)
  assert.match(result.warning, /502/)
})

test("sendAgentMessage uses structured error details from the agent service", async () => {
  const context = buildAgentContext(latestOrder, route)
  const result = await sendAgentMessage("Which robot is assigned?", context, {
    agentApiUrl: "https://agent.example.com",
    fetchImpl: async () => ({
      ok: false,
      status: 502,
      async json() {
        return {
          detail: "Azure OpenAI returned HTTP 404."
        }
      }
    })
  })

  assert.equal(result.source, "fallback")
  assert.match(result.warning, /404/)
})

test("sendAgentMessage falls back when the agent service returns an empty reply", async () => {
  const context = buildAgentContext(latestOrder, route)
  const result = await sendAgentMessage("What is the eta?", context, {
    agentApiUrl: "https://agent.example.com",
    fetchImpl: async () => ({
      ok: true,
      async json() {
        return {}
      }
    })
  })

  assert.equal(result.source, "fallback")
  assert.match(result.reply, /13 min/i)
  assert.match(result.warning, /empty response/i)
})

test("sendAgentMessage gives a sensible starter reply before any order exists", async () => {
  const context = buildAgentContext(null, null)
  const result = await sendAgentMessage("What can you do?", context)

  assert.equal(result.source, "local")
  assert.match(result.reply, /place an order/i)
})
