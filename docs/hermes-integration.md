# Hermes MCP Integration

Nagger can run as a local MCP task server that Hermes connects to over
streamable HTTP. This guide covers building the host, running it server-style
under systemd, where the SQLite data lands, and wiring it into Hermes.

## 1. Build

Build the host with a framework-dependent publish, which assumes the .NET 10
runtime is installed:

```bash
dotnet publish src/Nagger.Host -c Release -o .release
```

On a machine without the .NET 10 runtime, produce a self-contained build that
bundles the runtime instead:

```bash
dotnet publish src/Nagger.Host -c Release -o .release --self-contained
```

The published host is `.release/Nagger.Host` (or `dotnet .release/Nagger.Host.dll`
for the framework-dependent build).

## 2. Run

Without a launch profile the host listens on `http://127.0.0.1:5000`, and the
MCP endpoint is served at `/mcp`. For a one-off test run:

```bash
dotnet .release/Nagger.Host.dll
```

For persistent, server-style operation use a systemd user unit. Create
`~/.config/systemd/user/nagger.service`:

```ini
[Unit]
Description=Nagger MCP task server

[Service]
Type=simple
Restart=always
WorkingDirectory=/home/pi/.local/share/nagger
ExecStart=/home/pi/.release/Nagger.Host.dll

[Install]
WantedBy=default.target
```

`Restart=always` keeps the service alive across crashes; `WorkingDirectory` is
explicit so the process (and its SQLite file) lives in a known place. Start and
enable it with:

```bash
systemctl --user daemon-reload
systemctl --user enable --now nagger
```

## 3. SQLite data location

Nagger stores tasks in SQLite. The default database path is `nagger.db`,
resolved relative to the process working directory, so with the unit above the
data lands at `/home/pi/.local/share/nagger/nagger.db`.

To redirect the database to another path, set the `Nagger__DatabasePath`
environment variable in the unit:

```ini
[Service]
Environment=Nagger__DatabasePath=/home/pi/.local/share/nagger/nagger.db
```

The report timezone is set with `Nagger__TimeZone` (default
`Europe/Helsinki`); report dates are calculated in that timezone, not UTC.

## 4. Hermes wiring

Add Nagger as an HTTP MCP server in `~/.hermes/config.yaml` on the machine
running Hermes (typically the same machine, since Nagger listens on localhost
only):

```yaml
mcp_servers:
  nagger:
    url: "http://127.0.0.1:5000/mcp"
```

## 5. Stale MCP tool discovery

Hermes discovers MCP tools when its gateway starts. If you wire Nagger up, or
rebuild and restart it with new tools, restart the Hermes gateway afterwards;
otherwise Hermes keeps calling the previously discovered tool set and the new
or changed tools will not be available.

```bash
systemctl --user restart hermes
```
