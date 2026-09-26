<p align="center">
  <a href="DEVELOPMENT.md">Development</a> | <a href="USAGE.md">Usage</a> | <a href="CHANGELOG.md">Changelog</a> | <a href="docs/hermes-integration.md">Hermes Integration</a>
</p>

![Nagger banner](resources/repo_banner.jpg)

# Nagger

> Your assistant can talk. Nagger makes sure it remembers.

**FEEL LIKE SOMETHING IS MISSING?** Appointments slipping away? Commitments falling through the cracks? No one around to **NAG** you when you forget?

**NAGGER** IS THE SOLUTION.

Hand **NAGGER** to your personal-assistant LLM. Tell it to store your important tasks and commitments. Then let **NAGGER** deliver the daily rundown of everything coming up — computed from real state, not from vibes.

**NAGGER:** because tomorrow is too late to remember.

## What Nagger Does

Every assistant can talk. Almost none can be trusted to remember. Nagger is the dependable external memory your assistant writes to: explicit, inspectable tasks and a deterministic morning report, so it summarizes the facts instead of improvising a productivity system in prose.

Put another way: your LLM is brilliant and has the short-term memory of a goldfish. Nagger is the tank.

Here is a session where the assistant does exactly that:

> **You:** Remind me to pick up the dry cleaning on Tuesday the 25th, around 5 in the evening. Once is fine.
>
> **Assistant:** _Tool note: `create_one_shot_task` — title "Pick up dry cleaning", dueAt `2026-08-25T17:00:00+03:00`._
>
> Saved. It will show up in the morning rundown.
>
> **Tuesday 25 August — morning rundown** (_from `get_morning_report` for `2026-08-25`_):
>
> - **Pick up dry cleaning** — due today.
> - **Review the quarterly metrics deck** — due in 3 days.
>
> **You:** Picked the shirts up, that's done.
>
> **Assistant:** _Tool note: `complete_one_shot_task` — id 42._
>
> Marked complete. No half-remembering required.

## Available Now

- **One-shot tasks** with an explicit due time, for the things that happen exactly once and then haunt you.
- **Complete, pause, resume, or cancel** any task without losing its history.
- **Recurring templates** that spawn instances daily, weekly, or monthly — the gym membership of your to-do list.
- **A morning report** of due-today, overdue, and upcoming tasks for any date you ask about.
- **Everything stays local** in SQLite, because your forgotten chores are nobody else's business.
- **An MCP server** at `/mcp` (streamable HTTP) that hands your assistant the task tools and the report directly.

## Try It

Start the host and point an MCP client at `http://localhost:5246/mcp`:

```bash
dotnet run --project src/Nagger.Host
```

Every endpoint and tool is in [Usage](USAGE.md); build, test, and mutation gates are in [Development](DEVELOPMENT.md).

## Coming Next

- A shopping ledger for the things you will definitely remember at the shop. Until you do not.
- Deployment automation, so Nagger can get back to its important work: judging your follow-through.

## License

MIT — see [LICENSE](LICENSE).
