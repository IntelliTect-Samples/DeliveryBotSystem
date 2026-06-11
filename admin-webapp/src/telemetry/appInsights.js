// Azure Monitor / Application Insights client telemetry (final feature).
//
// Gated behind VITE_APPINSIGHTS_CONNECTION_STRING (a client ingestion key, not
// a secret — baked in at build time like the API URLs). When it's unset,
// telemetry is disabled and every call here is a safe no-op, so local dev and
// the unconfigured build keep working. The SDK is dynamically imported so it
// never loads (or enters the test graph) unless telemetry is actually on.

const connectionString = import.meta.env.VITE_APPINSIGHTS_CONNECTION_STRING ?? ''

export const telemetryEnabled = Boolean(connectionString)

let appInsights = null

export async function initTelemetry() {
  if (!telemetryEnabled || appInsights) return
  const { ApplicationInsights } = await import('@microsoft/applicationinsights-web')
  appInsights = new ApplicationInsights({
    config: {
      connectionString,
      enableAutoRouteTracking: true, // SPA tab/page views
    },
  })
  appInsights.loadAppInsights()
  appInsights.trackPageView()
}

// Record a named admin action (e.g. BotRecharged). No-op until telemetry inits.
export function trackEvent(name, properties = {}) {
  if (!appInsights) return
  appInsights.trackEvent({ name }, properties)
}
