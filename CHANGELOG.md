# Changelog

All notable changes to Nagger are documented here. The format is based on
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

The morning report also carries a `schemaVersion`; a breaking change to report
items bumps it and is called out under **Changed**.

## [Unreleased]

### Added

- Recurring task templates and their instances, across REST and MCP.
- Listing endpoints `GET /tasks/one-shot` and `GET /tasks/recurring`.
- `nextDueAt` on recurring template representations, and `completedInstance` / `nextInstance` on recurring completion.
- MCP server at `/mcp` (streamable HTTP) exposing task lifecycle, listing, and morning-report tools.

### Changed

- Morning report items carry a `type` discriminator and are ordered chronologically by due timestamp.
- Morning report `schemaVersion` is `"4"`; earlier values were `"3"` (added `type`), `"2"` (added the seven-day window), and `"1"` (initial).

### Removed

- `reminderPolicy` from report items and task representations.
