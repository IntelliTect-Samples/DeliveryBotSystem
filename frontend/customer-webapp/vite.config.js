import { defineConfig, loadEnv } from 'vite'
import react from '@vitejs/plugin-react'
import { fileURLToPath } from 'node:url'

function trimTrailingSlash(value) {
  return typeof value === "string" ? value.replace(/\/+$/, "") : ""
}

function escapeRegExp(value) {
  return value.replace(/[.*+?^${}()|[\]\\]/g, "\\$&")
}

function createProxyConfig(prefix, target, fallbackTarget) {
  const resolvedTarget = trimTrailingSlash(target) || fallbackTarget

  if (!resolvedTarget) {
    return undefined
  }

  const parsed = new URL(resolvedTarget)
  const origin = `${parsed.protocol}//${parsed.host}`
  const basePath = trimTrailingSlash(parsed.pathname)
  const prefixPattern = new RegExp(`^${escapeRegExp(prefix)}`)

  return {
    target: origin,
    changeOrigin: true,
    secure: false,
    rewrite: (path) => `${basePath}${path.replace(prefixPattern, "")}`
  }
}

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, fileURLToPath(new URL('.', import.meta.url)), "")
  const simulatorTarget = trimTrailingSlash(env.VITE_SIMULATOR_API_BASE)
  const orderServiceTarget = trimTrailingSlash(
    env.VITE_ORDER_SERVICE_URL || env.VITE_ORDER_SERVICE_API_BASE
  )
  const agentTarget = trimTrailingSlash(env.VITE_AGENT_API_URL)

  return {
    plugins: [react()],
    base: '/',
    server: {
      proxy: {
        '/api/simulator': createProxyConfig(
          '/api/simulator',
          simulatorTarget,
          'http://localhost:5099'
        ),
        '/api/order-service': createProxyConfig(
          '/api/order-service',
          orderServiceTarget
        ),
        '/api/agent': createProxyConfig(
          '/api/agent',
          agentTarget
        )
      }
    }
  }
})
