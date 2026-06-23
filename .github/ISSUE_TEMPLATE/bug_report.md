---
name: 🐛 Bug Report
about: Create a report to help us improve the Sales API.
title: 'bug: '
labels: bug
assignees: ''
---

**Describe the Bug**
A clear and concise description of what the bug is.

**To Reproduce**
Steps to reproduce the behavior (e.g. API payload, parameters, database state):
1. Send request `POST /api/sales` with payload ...
2. Receive response ...
3. See error ...

**Expected Behavior**
A clear and concise description of what you expected to happen.

**Actual Behavior & Error Response**
If applicable, include the JSON error payload returned by the API (which follows the standard validation/domain error schema):
```json
{
  "type": "...",
  "error": "...",
  "detail": "...",
  "status": 422,
  "errors": []
}
```

**Environment Info:**
- OS: [e.g. macOS, Ubuntu, Windows]
- .NET Version: [e.g. .NET 10.0]
- Database: [e.g. PostgreSQL 16]

**Additional Context**
Add any other context about the problem here.
