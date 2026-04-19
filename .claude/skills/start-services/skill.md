---
description: Start postgres (Docker), backend (.NET), and frontend (Next.js) locally
---

Start all three services. Run each in the background using the Bash tool with `run_in_background: true`:

1. **Postgres** — `docker compose up -d` (waits for healthy before proceeding)
2. **Backend** — `dotnet run --project backend/src/LegalDocumentAISearch.Api` from the repo root
3. **Frontend** — `npm run dev` from `frontend/admin/`

Steps:
1. Run `docker compose up -d 2>&1` and wait for it to complete (it's fast).
2. Run the backend in the background. Output file will be in /tmp.
3. Run the frontend in the background. Output file will be in /tmp.
4. Poll both output files every 10 seconds. Report when you see:
   - Backend ready: `Application started` or `Now listening on`
   - Frontend ready: `Ready in` or `Local:`
5. If either process errors on startup, show the relevant lines.
6. Once both are up, print:
   - Frontend: http://localhost:3000
   - API docs: http://localhost:5081/scalar
