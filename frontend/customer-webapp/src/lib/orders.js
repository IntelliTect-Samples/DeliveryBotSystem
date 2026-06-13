import { appConfig } from "./config.js"

const DEFAULT_ORDER_TIMEOUT_MS = 8000

const DEMO_DESTINATIONS = [
  {
    matchers: [
      "spokane convention center",
      "spokane conv center",
      "334 w spokane falls blvd"
    ],
    coordinates: {
      latitude: 47.660316,
      longitude: -117.416066
    }
  },
  {
    matchers: [
      "riverfront park",
      "507 n howard st"
    ],
    coordinates: {
      latitude: 47.664051,
      longitude: -117.419442
    }
  },
  {
    matchers: [
      "spokane arena",
      "720 w mallon ave"
    ],
    coordinates: {
      latitude: 47.667248,
      longitude: -117.422354
    }
  }
]

const ORDER_TYPES = {
  water: { label: "Water", itemId: "water" },
  soda: { label: "Soda", itemId: "soda" },
  chips: { label: "Chips", itemId: "chips" },
  sandwich: { label: "Sandwich", itemId: "sandwich" }
}

export function getOrderTypeOptions() {
  return Object.entries(ORDER_TYPES).map(([value, entry]) => ({
    value,
    label: entry.label
  }))
}

export function validateOrderForm(form) {
  const errors = {}

  if (!form.customerName?.trim()) {
    errors.customerName = "Enter the customer name."
  }

  if (!form.phone?.trim()) {
    errors.phone = "Enter a phone number."
  }

  if (!form.deliveryAddress?.trim()) {
    errors.deliveryAddress = "Enter a delivery address."
  }

  if (!form.orderType || !ORDER_TYPES[form.orderType]) {
    errors.orderType = "Choose an item for delivery."
  }

  return errors
}

export function toPlaceOrderRequest(form) {
  return {
    customerName: form.customerName.trim(),
    phone: form.phone.trim(),
    deliveryAddress: form.deliveryAddress.trim(),
    orderType: form.orderType
  }
}

export function buildCustomerId(customerName, phone) {
  return `${customerName.trim()}:${phone.trim()}`
}

function normalizeItems(items = []) {
  return items.map((item) => ({
    itemId: item.itemId,
    quantity: item.quantity
  }))
}

export function deriveMockDestination(address = "") {
  const trimmed = address.trim()

  if (!trimmed) {
    return null
  }

  const normalized = trimmed.toLowerCase()
  const match = DEMO_DESTINATIONS.find((entry) =>
    entry.matchers.some((matcher) => normalized.includes(matcher))
  )

  return match?.coordinates || null
}

export function normalizeOrder(rawOrder, fallbackMeta = {}) {
  const knownDestination = rawOrder.destination || deriveMockDestination(
    rawOrder.deliveryAddress || fallbackMeta.deliveryAddress || ""
  )

  return {
    id: rawOrder.id,
    customerId: rawOrder.customerId || buildCustomerId(
      fallbackMeta.customerName || "Customer",
      fallbackMeta.phone || "Unknown"
    ),
    assignedBotId: rawOrder.assignedBotId || null,
    status: rawOrder.status || "Pending",
    deliveryAddress: rawOrder.deliveryAddress || fallbackMeta.deliveryAddress || "",
    destination: knownDestination,
    items: normalizeItems(rawOrder.items),
    createdAt: rawOrder.createdAt || new Date().toISOString(),
    notes: fallbackMeta.notes || "",
    merchantName: fallbackMeta.merchantName || ""
  }
}

function createMockOrder(form) {
  const customerId = buildCustomerId(form.customerName, form.phone)
  const orderType = ORDER_TYPES[form.orderType] || ORDER_TYPES.water
  const destination = deriveMockDestination(form.deliveryAddress)
  const resolvedBotId = destination ? "bot-002" : null

  return normalizeOrder(
    {
      id: `mock-${Date.now()}`,
      customerId,
      assignedBotId: resolvedBotId,
      status: resolvedBotId ? "Assigned" : "Pending",
      deliveryAddress: form.deliveryAddress,
      destination,
      items: [
        {
          itemId: orderType.itemId,
          quantity: 1
        }
      ],
      createdAt: new Date().toISOString()
    },
    form
  )
}

async function fetchWithTimeout(fetchImpl, url, options, timeoutMs) {
  if (typeof AbortController === "undefined") {
    return fetchImpl(url, options)
  }

  const controller = new AbortController()
  const timer = setTimeout(() => controller.abort(), timeoutMs)

  try {
    return await fetchImpl(url, {
      ...options,
      signal: controller.signal
    })
  } finally {
    clearTimeout(timer)
  }
}

export async function submitOrder(form, options = {}) {
  const fetchImpl = options.fetchImpl || fetch
  const timeoutMs = options.timeoutMs || DEFAULT_ORDER_TIMEOUT_MS
  const validationErrors = validateOrderForm(form)

  if (Object.keys(validationErrors).length > 0) {
    const error = new Error("The order form is incomplete.")
    error.validationErrors = validationErrors
    throw error
  }

  const payload = toPlaceOrderRequest(form)

  if (!appConfig.orderServiceUrl) {
    return {
      order: createMockOrder(form),
      source: "mock"
    }
  }

  try {
    const response = await fetchWithTimeout(
      fetchImpl,
      `${appConfig.orderServiceUrl}/api/orders`,
      {
        method: "POST",
        headers: {
          "Content-Type": "application/json"
        },
        body: JSON.stringify(payload)
      },
      timeoutMs
    )

    if (!response.ok) {
      throw new Error(`Order Service returned HTTP ${response.status}.`)
    }

    const data = await response.json()

    return {
      order: normalizeOrder(data, form),
      source: "api"
    }
  } catch (error) {
    const warning = error?.name === "AbortError"
      ? "Order Service request timed out."
      : error.message

    return {
      order: createMockOrder(form),
      source: "mock",
      warning
    }
  }
}

export function formatOrderStatus(status) {
  return status === "InTransit" ? "In transit" : status
}

export function summarizeItems(items = []) {
  return items
    .map((item) => `${item.itemId} x${item.quantity}`)
    .join(", ")
}
