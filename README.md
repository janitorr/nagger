<p align="center">
  <a href="DEVELOPMENT.md">Development</a> | <a href="USAGE.md">Usage</a> | <a href="docs/hermes-integration.md">Hermes Integration</a>
</p>

![Nagger banner](resources/repo_banner.jpg)

# Nagger

**FEEL LIKE SOMETHING IS MISSING?** Appointments slipping away? Commitments falling through the cracks? No one around to **NAG** you when you forget?

**NAGGER** IS THE SOLUTION.

Hand **NAGGER** to your personal-assistant LLM. Tell it to store your important tasks and notifications. Then let **NAGGER** deliver the daily reminder rundown of everything coming up.

**NAGGER:** because tomorrow is too late to remember.

## What Nagger Does

Nagger gives your personal-assistant LLM a dependable place to put the things you cannot afford to half-remember. It stores tasks as explicit, inspectable state and produces a deterministic morning rundown, so your assistant can summarize facts instead of inventing a productivity system from vibes.

Here is a session where the assistant does exactly that:

> **You:** Remind me to pick up the dry cleaning on Tuesday the 25th, around 5 in the evening. Once is fine.
>
> **Assistant:** _Tool note: `create_one_shot_task` — title "Pick up dry cleaning", dueAt `2026-08-25T17:00:00+03:00`, reminderPolicy `once`._
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

- Create active one-shot tasks with an explicit due time and reminder policy.
- Complete, pause, resume, or cancel tasks without losing their history.
- Keep task data locally in SQLite.
- Generate a morning report of due-today, overdue, and upcoming tasks for any requested date.
- Create recurring templates that spawn instances on a daily, weekly, or monthly cadence.
- Connect assistant clients over streamable HTTP to an MCP server at `/mcp` with tools for task management and the morning report.

## Coming Next

- Reminder delivery and weekly follow-ups until a task is actually done.
- A shopping ledger for the things you will definitely remember at the shop. Until you do not.
- Deployment automation, so Nagger can get back to its important work: judging your follow-through.
