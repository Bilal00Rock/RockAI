# Copilot Instructions

## Project Guidelines
- For test-project work, inspect actual production APIs first; do not assume APIs, create speculative abstractions, or modify production behavior solely for test convenience. Keep common test projects limited to genuinely reusable builders, fixtures, fakes, and helpers; start with a small meaningful test set and always restore, build, verify test discovery, run all tests, and report exact results.

## General Guidelines
- Use the general Application-layer interface name **IAIEndpoints** because it is intended to support endpoint definitions for multiple AI providers, not only Ollama.
- For MAUI chat streaming implementation, use `await Task.Run(async () => { ... })` around AI stream generation to keep the UI responsive; retain explicit MainThread dispatch for bound UI updates.