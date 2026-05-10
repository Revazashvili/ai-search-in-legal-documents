---
description: Start postgres, backend, and frontend via docker compose
---

All three services run in Docker. Start with:

```bash
docker compose up -d 2>&1
```

Steps:
1. Run `docker compose up -d 2>&1` and wait for it to complete.
2. Poll `docker compose logs backend` every 5 seconds until you see `Now listening on` or `Application started`.
3. Poll `docker compose logs frontend` every 5 seconds until you see `Ready in` or `Local:`.
4. If either container exits or errors, run `docker compose logs <service>` and show the relevant lines.
5. Once both are up, print:
   - Frontend: http://localhost:3000
   - API docs: http://localhost:5081/scalar
