# Contributing to FulcrumEngine 🛠️

Thanks for considering contributing to **FulcrumEngine**! This guide explains how to report issues, propose changes, and submit pull requests in a clean, community-friendly way.

## Code of Conduct 🙂

By participating, you agree to follow our **Code of Conduct**: be respectful, constructive, and professional in all discussions. Harassment, hate speech, or personal attacks are not tolerated.

## Quick Start ✅

* Check existing **Issues** and **Pull Requests** before opening a new one.
* Prefer small, focused PRs over huge “mega changes”.
* Keep discussions technical and actionable.

---

## Ways to Contribute

### 1) Report Bugs 🐞

When filing a bug report, include:

* Expected behavior vs actual behavior
* Reproduction steps (minimal)
* Logs / stack traces (if any)
* OS + hardware + runtime (.NET version)
* If relevant: GPU/driver + graphics backend details

### 2) Suggest Features 💡

A good feature request includes:

* The problem it solves
* Proposed solution (high-level)
* Alternatives considered
* Scope estimate (small/medium/large)

### 3) Improve Docs 📚

Docs improvements are welcome:

* Fix typos, clarify usage, add examples
* Keep docs consistent with current engine behavior
* If you change behavior, update docs in the same PR

---

## Before You Start

### Check Project Direction

If your change is large (architecture changes, new subsystem, major refactor), open an issue first to align on:

* goals
* API shape
* maintainability expectations

### Supported Changes

✅ Bug fixes, performance improvements, stability improvements
✅ Docs + examples
✅ Tests
✅ Refactors that reduce complexity
⚠️ Breaking public API changes require discussion first

---

## Development Setup

### Requirements

* **.NET SDK**: use the version specified by the repository (or latest stable if not specified)
* Git
* A C# IDE (Rider / Visual Studio recommended)

### Build

* Clone the repo
* Restore dependencies
* Build the solution

If the repo contains CI workflows, your PR is expected to pass the same checks locally when possible.

---

## Branching & Workflow

### Branching

* Create a branch from the default branch (usually `main`)
* Naming suggestions:

  * `fix/<short-description>`
  * `feat/<short-description>`
  * `docs/<short-description>`
  * `refactor/<short-description>`

### Commit Style

* Use clear, descriptive commits
* Prefer conventional-ish prefixes (recommended):

  * `fix: ...`
  * `feat: ...`
  * `docs: ...`
  * `refactor: ...`
  * `test: ...`
  * `chore: ...`

Example:

* `fix: prevent null reference in mesh submission`
* `docs: clarify scene serialization format`

---

## Coding Standards

### General

* Keep changes minimal and readable
* Avoid unnecessary abstractions
* Prefer explicit, maintainable code over clever code
* Don’t introduce new dependencies unless there’s a strong reason

### C# Style

* Follow common .NET naming conventions (PascalCase types/methods, camelCase locals)
* Use `var` when the type is obvious
* Prefer `readonly` where applicable
* Keep public APIs well-documented

### Engine Architecture Expectations

* Keep subsystem boundaries clean (rendering/scene/audio/input/etc.)
* Avoid circular dependencies between core modules
* If you add a feature, include at least:

  * documentation update
  * minimal test coverage where feasible

---

## Testing

* Add tests for bug fixes when practical
* Ensure existing tests pass
* If the engine uses integration/sample projects, ensure your change doesn’t break them

If tests are hard to add, explain why in the PR description and provide a manual verification checklist.

---

## Documentation Expectations

If you change public behavior or APIs:

* Update README/docs
* Update examples (if any)
* Note breaking changes clearly

---

## Pull Request Guidelines ✅

### PR Checklist

Include in your PR description:

* What changed and why
* How to test (steps)
* Any breaking changes
* Any performance impact

### Keep PRs Focused

* One PR = one theme
* Don’t mix refactors with unrelated features
* If you must, split into multiple PRs

### Review Notes

* Maintainers may request changes for style, architecture, or clarity
* Be responsive and respectful in review discussions

---

## Security

If you discover a vulnerability or security-sensitive issue:

* **Do not open a public issue**
* Instead, contact the maintainers privately (use the security contact method if the repo provides one)

---

## Licensing

By contributing, you agree that your contributions will be licensed under the repository’s license. If you include third-party code:

* Ensure it’s compatible with the project license
* Include attribution and license text where required

---

## Getting Help

* Open an issue with clear context
* Provide logs and reproduction steps when relevant
* If you’re unsure whether a change is welcome, ask first 🙂

---

Thanks for helping improve **FulcrumEngine** ❤️

- This article was written by ChatGPT.
