import test from "node:test"
import assert from "node:assert/strict"
import {
  readLatestOrder,
  subscribeToLatestOrder,
  writeLatestOrder
} from "./orderSession.js"

function createMockWindow() {
  const eventTarget = new EventTarget()
  const sessionStorageData = new Map()
  const localStorageData = new Map()

  return Object.assign(eventTarget, {
    localStorage: {
      getItem(key) {
        return localStorageData.has(key) ? localStorageData.get(key) : null
      },
      setItem(key, value) {
        localStorageData.set(key, value)
      },
      removeItem(key) {
        localStorageData.delete(key)
      }
    },
    sessionStorage: {
      getItem(key) {
        return sessionStorageData.has(key) ? sessionStorageData.get(key) : null
      },
      setItem(key, value) {
        sessionStorageData.set(key, value)
      },
      removeItem(key) {
        sessionStorageData.delete(key)
      }
    }
  })
}

test("readLatestOrder returns null outside the browser", () => {
  const originalWindow = globalThis.window
  delete globalThis.window

  try {
    assert.equal(readLatestOrder(), null)
  } finally {
    globalThis.window = originalWindow
  }
})

test("writeLatestOrder persists the latest order and readLatestOrder restores it", () => {
  const originalWindow = globalThis.window
  globalThis.window = createMockWindow()

  try {
    const order = { id: "mock-1", status: "Assigned" }
    writeLatestOrder(order)
    assert.deepEqual(readLatestOrder(), order)
  } finally {
    globalThis.window = originalWindow
  }
})

test("readLatestOrder returns null when saved session data is malformed", () => {
  const originalWindow = globalThis.window
  globalThis.window = createMockWindow()

  try {
    globalThis.window.sessionStorage.setItem("deliverybot.latestOrder", "{broken-json")
    assert.equal(readLatestOrder(), null)
  } finally {
    globalThis.window = originalWindow
  }
})

test("readLatestOrder ignores stale localStorage data from older runs", () => {
  const originalWindow = globalThis.window
  globalThis.window = createMockWindow()

  try {
    globalThis.window.localStorage.setItem("deliverybot.latestOrder", JSON.stringify({ id: "old-order" }))
    assert.equal(readLatestOrder(), null)
  } finally {
    globalThis.window = originalWindow
  }
})

test("writeLatestOrder is a safe no-op outside the browser", () => {
  const originalWindow = globalThis.window
  delete globalThis.window

  try {
    assert.doesNotThrow(() => writeLatestOrder({ id: "noop" }))
  } finally {
    globalThis.window = originalWindow
  }
})

test("subscribeToLatestOrder publishes updates and can unsubscribe cleanly", async () => {
  const originalWindow = globalThis.window
  globalThis.window = createMockWindow()

  try {
    const updates = []
    const unsubscribe = subscribeToLatestOrder((order) => updates.push(order))

    writeLatestOrder({ id: "mock-2", status: "Pending" })
    globalThis.window.dispatchEvent(new Event("storage"))

    assert.ok(updates.length >= 2)
    assert.equal(updates.at(-1).id, "mock-2")

    unsubscribe()
    const currentCount = updates.length
    globalThis.window.dispatchEvent(new Event("storage"))
    assert.equal(updates.length, currentCount)
  } finally {
    globalThis.window = originalWindow
  }
})

test("subscribeToLatestOrder returns a cleanup function outside the browser", () => {
  const originalWindow = globalThis.window
  delete globalThis.window

  try {
    const unsubscribe = subscribeToLatestOrder(() => {})
    assert.equal(typeof unsubscribe, "function")
    assert.doesNotThrow(() => unsubscribe())
  } finally {
    globalThis.window = originalWindow
  }
})
