import test from "node:test"
import assert from "node:assert/strict"
import fs from "node:fs"
import path from "node:path"
import { assistantStyles } from "../lib/assistantStyles.js"

test("assistant component source keeps the launcher label available", () => {
  const source = fs.readFileSync(path.resolve("src/components/AgentAssistant.jsx"), "utf8")
  assert.match(source, /Delivery Assistant/)
})

test("assistant layering stays above other page content", () => {
  assert.ok(assistantStyles.fab.zIndex >= 5000)
  assert.ok(assistantStyles.panel.zIndex > assistantStyles.fab.zIndex)
})

test("assistant transcript and input keep readable layout constraints", () => {
  assert.equal(assistantStyles.transcript.flex, "1 1 auto")
  assert.equal(assistantStyles.transcript.minHeight, 0)
  assert.equal(assistantStyles.textarea.background, "#fff")
  assert.equal(assistantStyles.message.lineHeight, 1.45)
})

test("assistant component source keeps accessibility hooks for toggling the panel", () => {
  const source = fs.readFileSync(path.resolve("src/components/AgentAssistant.jsx"), "utf8")

  assert.match(source, /aria-expanded=\{isOpen\}/)
  assert.match(source, /aria-controls="delivery-assistant-panel"/)
  assert.match(source, /placeholder="Ask about your order, route, ETA, or assigned robot"/)
})

test("assistant component source includes quick question prompts and enter-to-send handling", () => {
  const source = fs.readFileSync(path.resolve("src/components/AgentAssistant.jsx"), "utf8")

  assert.match(source, /promptSuggestions\.map/)
  assert.match(source, /event\.key === "Enter"/)
  assert.match(source, /!event\.shiftKey/)
  assert.match(source, /messages/)
})

test("assistant component source shows where assistant replies came from", () => {
  const source = fs.readFileSync(path.resolve("src/components/AgentAssistant.jsx"), "utf8")

  assert.match(source, /Azure agent/)
  assert.match(source, /Local fallback/)
  assert.match(source, /Local assistant/)
  assert.match(source, /New chat/)
  assert.match(source, /message\.model/)
})

test("assistant component source sends the latest conversation history to the agent service", () => {
  const source = fs.readFileSync(path.resolve("src/components/AgentAssistant.jsx"), "utf8")

  assert.match(source, /const conversationHistory = \[\.\.\.messages, userMessage\]/)
  assert.match(source, /messages: conversationHistory/)
  assert.match(source, /ref=\{transcriptRef\}/)
})

test("assistant component source keeps an explicit send button and draft guard", () => {
  const source = fs.readFileSync(path.resolve("src/components/AgentAssistant.jsx"), "utf8")

  assert.match(source, /"Send"/)
  assert.match(source, /!\s*draft\.trim\(\)/)
})
