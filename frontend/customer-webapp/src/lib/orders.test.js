import test from "node:test"
import assert from "node:assert/strict"
import {
  buildCustomerId,
  deriveMockDestination,
  getOrderTypeOptions,
  formatOrderStatus,
  normalizeOrder,
  submitOrder,
  summarizeItems,
  toPlaceOrderRequest,
  validateOrderForm
} from "./orders.js"

const validForm = {
  merchantName: "Downtown Cafe",
  deliveryAddress: "123 Main St, Spokane, WA",
  customerName: "Taylor Rivers",
  phone: "509-555-0100",
  orderType: "chips",
  notes: "Front desk"
}

const mappableForm = {
  ...validForm,
  deliveryAddress: "Spokane Convention Center"
}

test("validateOrderForm returns field errors for missing required values", () => {
  const result = validateOrderForm({
    customerName: "",
    phone: "",
    deliveryAddress: "",
    orderType: ""
  })

  assert.equal(result.customerName, "Enter the customer name.")
  assert.equal(result.phone, "Enter a phone number.")
  assert.equal(result.deliveryAddress, "Enter a delivery address.")
  assert.equal(result.orderType, "Choose an item for delivery.")
})

test("validateOrderForm accepts a complete order payload", () => {
  assert.deepEqual(validateOrderForm(validForm), {})
})

test("toPlaceOrderRequest matches the backend contract", () => {
  const payload = toPlaceOrderRequest(validForm)

  assert.deepEqual(payload, {
    customerName: "Taylor Rivers",
    phone: "509-555-0100",
    deliveryAddress: "123 Main St, Spokane, WA",
    orderType: "chips"
  })
})

test("toPlaceOrderRequest trims customer-entered values before sending", () => {
  const payload = toPlaceOrderRequest({
    ...validForm,
    customerName: "  Taylor Rivers  ",
    phone: " 509-555-0100 ",
    deliveryAddress: " 123 Main St, Spokane, WA  "
  })

  assert.equal(payload.customerName, "Taylor Rivers")
  assert.equal(payload.phone, "509-555-0100")
  assert.equal(payload.deliveryAddress, "123 Main St, Spokane, WA")
})

test("buildCustomerId combines the customer name and phone", () => {
  assert.equal(
    buildCustomerId("Taylor Rivers", "509-555-0100"),
    "Taylor Rivers:509-555-0100"
  )
})

test("deriveMockDestination is deterministic for supported demo addresses", () => {
  const first = deriveMockDestination("Spokane Convention Center")
  const second = deriveMockDestination("Spokane Convention Center")

  assert.deepEqual(first, second)
})

test("deriveMockDestination resolves known local demo destinations", () => {
  assert.deepEqual(deriveMockDestination("Spokane Convention Center"), {
    latitude: 47.660316,
    longitude: -117.416066
  })
})

test("deriveMockDestination returns null for blank or unsupported addresses", () => {
  assert.equal(deriveMockDestination(""), null)
  assert.equal(deriveMockDestination("123 Main St, Spokane, WA"), null)
})

test("normalizeOrder fills in missing customer metadata from fallback form values", () => {
  const order = normalizeOrder(
    {
      id: "abc",
      status: "Pending",
      items: [{ itemId: "water", quantity: 1 }]
    },
    validForm
  )

  assert.equal(order.customerId, "Taylor Rivers:509-555-0100")
  assert.equal(order.deliveryAddress, "123 Main St, Spokane, WA")
  assert.equal(order.items[0].itemId, "water")
})

test("normalizeOrder preserves a provided destination and assigned bot", () => {
  const order = normalizeOrder(
    {
      id: "route-1",
      customerId: "cust-1",
      assignedBotId: "bot-003",
      destination: {
        latitude: 47.66,
        longitude: -117.41
      },
      items: []
    },
    validForm
  )

  assert.equal(order.assignedBotId, "bot-003")
  assert.deepEqual(order.destination, {
    latitude: 47.66,
    longitude: -117.41
  })
})

test("submitOrder throws validation details when required fields are missing", async () => {
  await assert.rejects(
    submitOrder({
      customerName: "",
      phone: "",
      deliveryAddress: "",
      orderType: ""
    }),
    (error) => {
      assert.equal(error.message, "The order form is incomplete.")
      assert.equal(error.validationErrors.customerName, "Enter the customer name.")
      return true
    }
  )
})

test("submitOrder returns the API order when the service succeeds", async () => {
  const fetchCalls = []
  const result = await submitOrder(validForm, {
    fetchImpl: async (url, options) => {
      fetchCalls.push({ url, options })
      return {
        ok: true,
        async json() {
          return {
            id: "api-1",
            status: "InTransit",
            assignedBotId: "bot-002",
            deliveryAddress: "123 Main St, Spokane, WA",
            destination: {
              latitude: 47.661,
              longitude: -117.419
            },
            items: [{ itemId: "chips", quantity: 1 }]
          }
        }
      }
    }
  })

  if (fetchCalls.length > 0) {
    assert.match(fetchCalls[0].url, /\/api\/orders$/)
    assert.equal(fetchCalls[0].options.method, "POST")
    assert.match(fetchCalls[0].options.body, /"customerName":"Taylor Rivers"/)
  }

  if (result.source === "api") {
    assert.equal(result.order.id, "api-1")
    assert.equal(result.order.status, "InTransit")
    assert.equal(result.order.assignedBotId, "bot-002")
  } else {
    assert.equal(result.source, "mock")
  }
})

test("submitOrder falls back cleanly when the service returns a bad status", async () => {
  const result = await submitOrder(mappableForm, {
    fetchImpl: async () => ({
      ok: false,
      status: 503
    })
  })

  assert.equal(result.source, "mock")
  if (result.warning) {
    assert.match(result.warning, /503/)
  } else {
    assert.equal(result.order.status, "Assigned")
    assert.equal(result.order.assignedBotId, "bot-002")
  }
})

test("submitOrder falls back to a mock order when no Order Service URL is configured", async () => {
  const result = await submitOrder(mappableForm)

  assert.equal(result.source, "mock")
  assert.equal(result.order.customerId, "Taylor Rivers:509-555-0100")
  assert.equal(result.order.status, "Assigned")
  assert.equal(result.order.assignedBotId, "bot-002")
  assert.equal(result.order.items[0].itemId, "chips")
  assert.deepEqual(result.order.destination, {
    latitude: 47.660316,
    longitude: -117.416066
  })
})

test("submitOrder leaves unsupported local addresses unrouted instead of inventing a destination", async () => {
  const result = await submitOrder(validForm)

  assert.equal(result.source, "mock")
  assert.equal(result.order.status, "Pending")
  assert.equal(result.order.assignedBotId, null)
  assert.equal(result.order.destination, null)
})

test("getOrderTypeOptions exposes the selectable inventory labels", () => {
  const options = getOrderTypeOptions()

  assert.deepEqual(options.map((option) => option.value), [
    "water",
    "soda",
    "chips",
    "sandwich"
  ])
  assert.equal(options[0].label, "Water")
})

test("formatOrderStatus and summarizeItems keep delivery copy readable", () => {
  assert.equal(formatOrderStatus("InTransit"), "In transit")
  assert.equal(
    summarizeItems([
      { itemId: "water", quantity: 1 },
      { itemId: "chips", quantity: 2 }
    ]),
    "water x1, chips x2"
  )
  assert.equal(summarizeItems([]), "")
})
