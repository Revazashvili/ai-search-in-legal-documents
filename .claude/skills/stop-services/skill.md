---
description: Stop backend, frontend, and postgres
---

Run the following command and report the result:

```bash
lsof -ti :5081 | xargs kill -9 2>/dev/null; lsof -ti :3000 | xargs kill -9 2>/dev/null; docker compose down 2>&1; echo "All services stopped"
```
