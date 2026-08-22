## 1. Documentation

- [ ] 1.1 Create `docs/hermes-integration.md` covering build, run (systemd user unit with `Restart=always` + `WorkingDirectory`), SQLite data location and `Nagger__DatabasePath` override, Hermes `mcp_servers` wiring, and the stale MCP tool-discovery restart pitfall; verify the file exists and contains each of those sections
- [ ] 1.2 Link `docs/hermes-integration.md` from the MCP Server section of `USAGE.md`; verify the link text matches the file name and the file exists

## 2. Verification

- [ ] 2.1 Run `openspec validate document-hermes-mcp-setup --strict` and verify the change validates
- [ ] 2.2 Verify the documented build command, endpoint, tool list, and config keys match `Program.cs`, the launch profile, and the MCP tool table in `USAGE.md`
