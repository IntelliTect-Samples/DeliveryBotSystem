import { appConfig } from "./config.js"
import { formatDistance, formatDuration } from "./osrm.js"
import { formatOrderStatus, summarizeItems } from "./orders.js"

export function buildAgentContext(latestOrder, route) {
  return {
    latestOrder: latestOrder
      ? {
          id: latestOrder.id,
          status: latestOrder.status,
          assignedBotId: latestOrder.assignedBotId,
          deliveryAddress: latestOrder.deliveryAddress,
          itemsSummary: summarizeItems(latestOrder.items),
          shortId: latestOrder.id?.slice(0, 8) || ""
        }
      : null,
    route: route
      ? {
          distance: formatDistance(route.distanceMeters),
          eta: formatDuration(route.durationSeconds),
          source: route.source,
          available: true
        }
      : {
          available: false
        }
  }
}

export function mapConversationHistory(messages = []) {
  return messages
    .filter((message) => message?.role === "user" || message?.role === "assistant")
    .map((message) => ({
      role: message.role,
      text: message.text?.trim() || ""
    }))
    .filter((message) => message.text)
    .slice(-8)
}

export function buildAgentPayload(message, context, messages = []) {
  return {
    message: message.trim(),
    context,
    history: mapConversationHistory(messages)
  }
}

function matchesWord(prompt, pattern) {
  return new RegExp(`\\b${pattern}\\b`, "i").test(prompt)
}

function matchesAny(prompt, patterns) {
  return patterns.some((pattern) => prompt.includes(pattern))
}

export function getAssistantPromptSuggestions(context) {
  if (!context.latestOrder) {
    return [
      "What can you help with?",
      "How do routes work?",
      "What happens after I place an order?"
    ]
  }

  if (!context.route?.available) {
    return [
      "What is my order status?",
      "Which robot is assigned?",
      "Why is there no route?",
      "What items are in my order?"
    ]
  }

  return [
    "What is my order status?",
    "Which robot is assigned?",
    "What is the ETA?",
    "Where is the delivery going?",
    "Summarize my delivery"
  ]
}

export function buildLocalReply(message, context) {
  const prompt = message.toLowerCase()
  const latestOrder = context.latestOrder
  const route = context.route
  const routeAvailable = route?.available
  const isGreeting = matchesWord(prompt, "hello") || matchesWord(prompt, "hi") || matchesWord(prompt, "hey")
  const asksWhatItDoes = prompt.includes("what can you do") || prompt.includes("what do you do")
  const wantsStatus = matchesAny(prompt, ["status", "delivery status", "progress"])
  const wantsBot = matchesWord(prompt, "bot") || matchesWord(prompt, "robot") || prompt.includes("assigned to")
  const wantsEta = matchesWord(prompt, "eta") || matchesAny(prompt, ["how long", "arrival", "arrive"])
  const wantsDistance = matchesAny(prompt, ["distance", "far", "mile", "kilometer", "kilometre"])
  const wantsRoute = matchesWord(prompt, "route") || matchesAny(prompt, ["path", "directions"])
  const wantsLocation = matchesAny(prompt, ["where", "destination", "address", "going"])
  const wantsRouteSource = prompt.includes("source")
  const wantsItems = matchesAny(prompt, ["item", "order contain", "what did i order", "what is in my order"])
  const wantsSummary = matchesAny(prompt, ["summary", "summarize", "what do you know", "overview"])
  const wantsOrderId = matchesAny(prompt, ["order id", "order number", "order code"])
  const wantsDeliveryFlow = matchesAny(prompt, ["what happens", "next", "next step", "what now"])

  if (isGreeting) {
    return latestOrder
      ? `Hi. I can help with order ${latestOrder.id.slice(0, 8)}. Ask about status, route, ETA, or destination.`
      : "Hi. Place an order and I can help with route, ETA, and delivery questions."
  }

  if (wantsRouteSource && route) {
    return `The route source is ${route.source === "osrm" ? "OSRM" : "the local fallback route"}.`
  }

  if (wantsOrderId && latestOrder) {
    return `The latest order number is ${latestOrder.shortId || latestOrder.id.slice(0, 8)}.`
  }

  if (wantsBot && latestOrder) {
    return latestOrder.assignedBotId
      ? `The assigned robot is ${latestOrder.assignedBotId}.`
      : "The latest order does not have an assigned robot yet."
  }

  if (wantsItems && latestOrder) {
    return latestOrder.itemsSummary
      ? `The latest order includes ${latestOrder.itemsSummary}.`
      : "The latest order does not list any items yet."
  }

  if (wantsStatus && latestOrder) {
    return `Your latest order is ${formatOrderStatus(latestOrder.status)}${latestOrder.assignedBotId ? ` with ${latestOrder.assignedBotId}` : ""}.`
  }

  if ((prompt.includes("my order") || prompt.includes("latest order")) && latestOrder) {
    return `Your latest order is ${formatOrderStatus(latestOrder.status)}${latestOrder.assignedBotId ? ` and assigned to ${latestOrder.assignedBotId}` : ""}.`
  }

  if (wantsSummary && latestOrder) {
    return routeAvailable
      ? `Order ${latestOrder.shortId || latestOrder.id.slice(0, 8)} is ${formatOrderStatus(latestOrder.status)}${latestOrder.assignedBotId ? ` with ${latestOrder.assignedBotId}` : ""}, headed to ${latestOrder.deliveryAddress}, and the current ETA is ${route.eta}.`
      : `Order ${latestOrder.shortId || latestOrder.id.slice(0, 8)} is ${formatOrderStatus(latestOrder.status)}${latestOrder.assignedBotId ? ` with ${latestOrder.assignedBotId}` : ""}, headed to ${latestOrder.deliveryAddress}. A route is not available yet.`
  }

  if (wantsDeliveryFlow && latestOrder) {
    return routeAvailable
      ? "The order is saved, assigned to a robot, and the route is available for ETA and distance updates."
      : "The order is saved. Once a supported destination and robot route are available, the assistant can report ETA and distance."
  }

  if (wantsEta && routeAvailable) {
    return `The current ETA is about ${route.eta}.`
  }

  if (wantsEta && !routeAvailable && latestOrder) {
    return "There is no active route yet, so an ETA is not available."
  }

  if (wantsDistance && routeAvailable) {
    return `The route distance is about ${route.distance}.`
  }

  if (wantsDistance && !routeAvailable && latestOrder) {
    return "There is no active route yet, so the distance is not available."
  }

  if (wantsRoute && routeAvailable) {
    return `The route is active from the assigned robot to the delivery destination. It covers about ${route.distance} and the ETA is ${route.eta}.`
  }

  if (wantsRoute && !routeAvailable && latestOrder) {
    return "There is no route to show yet. In local mode, only supported demo destinations can be mapped."
  }

  if (wantsLocation && latestOrder?.deliveryAddress) {
    return `The current delivery is headed to ${latestOrder.deliveryAddress}.`
  }

  if (prompt.includes("help") || asksWhatItDoes) {
    return latestOrder
      ? "You can ask about order status, assigned bot, route distance, ETA, destination, order number, or ordered items."
      : "Place an order and then ask about status, route distance, ETA, destination, or how the delivery assistant works."
  }

  return latestOrder
    ? `I can help with order ${latestOrder.id.slice(0, 8)}. Ask about status, route, or ETA.`
    : "I can help once you place an order. Ask about route details or how the delivery assistant works."
}

export function normalizeAgentResponse(data) {
  return data?.reply ||
    data?.message ||
    data?.content ||
    data?.answer ||
    data?.choices?.[0]?.message?.content ||
    ""
}

async function readAgentError(response) {
  try {
    const data = await response.json()
    return data?.detail || data?.title || `Agent service returned HTTP ${response.status}.`
  } catch {
    return `Agent service returned HTTP ${response.status}.`
  }
}

export async function sendAgentMessage(message, context, options = {}) {
  const fetchImpl = options.fetchImpl || fetch
  const agentApiUrl = options.agentApiUrl || appConfig.agentApiUrl
  const payload = buildAgentPayload(message, context, options.messages)

  if (!agentApiUrl) {
    return {
      reply: buildLocalReply(message, context),
      source: "local",
      model: null
    }
  }

  try {
    const response = await fetchImpl(`${agentApiUrl}/chat`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json"
      },
      body: JSON.stringify(payload)
    })

    if (!response.ok) {
      throw new Error(await readAgentError(response))
    }

    const data = await response.json()
    const reply = normalizeAgentResponse(data)

    if (!reply) {
      throw new Error("Agent service returned an empty response.")
    }

    return {
      reply,
      source: "api",
      model: data?.model || null
    }
  } catch (error) {
    return {
      reply: buildLocalReply(message, context),
      source: "fallback",
      warning: error.message,
      model: null
    }
  }
}
