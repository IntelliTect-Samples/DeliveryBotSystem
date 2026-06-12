const STORAGE_KEY = "deliverybot.latestOrder"
const UPDATE_EVENT = "deliverybot:order-updated"

function getStorage() {
  if (typeof window === "undefined") {
    return null
  }

  return window.sessionStorage || null
}

export function readLatestOrder() {
  const storage = getStorage()

  if (!storage) {
    return null
  }

  try {
    const stored = storage.getItem(STORAGE_KEY)
    return stored ? JSON.parse(stored) : null
  } catch {
    return null
  }
}

export function writeLatestOrder(order) {
  const storage = getStorage()

  if (!storage) {
    return
  }

  storage.setItem(STORAGE_KEY, JSON.stringify(order))
  window.dispatchEvent(new Event(UPDATE_EVENT))
}

export function subscribeToLatestOrder(callback) {
  if (typeof window === "undefined") {
    return () => {}
  }

  const handleUpdate = () => callback(readLatestOrder())
  window.addEventListener(UPDATE_EVENT, handleUpdate)
  window.addEventListener("storage", handleUpdate)

  return () => {
    window.removeEventListener(UPDATE_EVENT, handleUpdate)
    window.removeEventListener("storage", handleUpdate)
  }
}
