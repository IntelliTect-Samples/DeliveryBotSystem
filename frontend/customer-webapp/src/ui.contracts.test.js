import test from "node:test"
import assert from "node:assert/strict"
import fs from "node:fs"
import path from "node:path"
import { assistantStyles } from "./lib/assistantStyles.js"

test("assistant panel stays above the floating launcher and page content", () => {
  assert.ok(assistantStyles.panel.zIndex > assistantStyles.fab.zIndex)
})

test("home view keeps the assigned label inline instead of overlaying card content", () => {
  const homeSource = fs.readFileSync(
    path.resolve("src/pages/Home.jsx"),
    "utf8"
  )

  assert.match(homeSource, /assignedLabel:\s*\{[^}]*display:\s*"inline-block"/)
  assert.doesNotMatch(homeSource, /assignedLabel:\s*\{[^}]*position:/)
})

test("home route cards and map canvas keep readable default sizing", () => {
  const homeSource = fs.readFileSync(
    path.resolve("src/pages/Home.jsx"),
    "utf8"
  )

  assert.match(homeSource, /routeCard:\s*\{[^}]*borderRadius:\s*"8px"/)
  assert.match(homeSource, /mapCanvas:\s*\{[^}]*minHeight:\s*"360px"/)
})

test("home keeps the map section and route summary labels in the page source", () => {
  const homeSource = fs.readFileSync(
    path.resolve("src/pages/Home.jsx"),
    "utf8"
  )

  assert.match(homeSource, /aria-label="Robot location map"/)
  assert.match(homeSource, /OSRM Route/)
  assert.match(homeSource, /Latest Order/)
})
