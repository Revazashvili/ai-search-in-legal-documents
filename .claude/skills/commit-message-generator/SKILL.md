---
description: Generates a concise commit message based on staged changes
---

## Staged changes

!`git diff --cached`

## Instructions

Generate a commit message for the changes above following these rules:

1. **Subject line** — one short sentence (max 72 chars), imperative mood, no period. Summarize *what* changed and *why* at a high level.
2. **Body (optional)** — if the change is non-trivial, add a blank line after the subject, then bullet points highlighting the main features or areas affected. Keep each bullet concise.
3. **Never** include co-author lines, AI attribution, or any mention of tools used to generate the message.
4. Focus on the intent and impact of the change, not on low-level implementation details.
5. Always use conventional commit prefixes: `feat:`, `fix:`, `refactor:`, `chore:`, `docs:`, `test:`.

Output only the raw commit message — no explanation, no markdown fences.
