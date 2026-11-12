# Static Application Security Testing (SAST)

SIMS is scanned with **[Semgrep](https://semgrep.dev/)** using the `--config auto`
ruleset (community C#, security-audit and secrets rules).

## Running the scan

> Requires Docker Desktop to be running.

```bash
# From the repository root
docker run --rm -v "${PWD}:/src" returntocorp/semgrep semgrep --config auto /src
```

PowerShell (Windows):

```powershell
docker run --rm -v "${PWD}:/src" returntocorp/semgrep semgrep --config auto /src
```

Produce a machine-readable report:

```bash
docker run --rm -v "${PWD}:/src" returntocorp/semgrep \
  semgrep --config auto --sarif --output /src/semgrep-report.sarif /src
```

Files excluded from the scan are listed in [`.semgrepignore`](../.semgrepignore)
(`bin/`, `obj/`, `node_modules/`, generated `Migrations/`).

## Latest results

| Metric | Value |
|--------|-------|
| Scan date | 2026-11-12 |
| Ruleset | `--config auto` (1074 rules) |
| Findings (ERROR) | **0** |
| Findings (WARNING) | **2** |
| Report | `semgrep-report.sarif` (generated locally, git-ignored) |

### Findings

| # | Rule | File | Severity | Assessment |
|---|------|------|----------|------------|
| 1–2 | `generic.secrets…detected-bcrypt-hash` | `Data/SimsDbContext.cs` | WARNING | **Accepted (false positive).** These are the two **demo seed** BCrypt hashes for `admin` / `analyst`. They are not production credentials and are overwritten on first real deployment. No live secret is exposed. |

**Conclusion:** no exploitable issues (0 errors). The only warnings are the
intentional, documented demo password hashes in the seed data.

## Mitigations already in place (addressing common SAST categories)

| Category | Mitigation in SIMS |
|----------|--------------------|
| SQL Injection | All data access via EF Core (parameterised queries); no raw SQL. |
| Weak password storage | Passwords hashed with **BCrypt** (work factor 12); never returned by the API. |
| Sensitive data exposure | Users are projected to `UserResponseDto`; `PasswordHash` is never serialised. |
| Input validation | DataAnnotations + `[ApiController]` automatic 400 responses; CVE format enforced by regex. |
| Hardcoded secrets | DB/Redis credentials overridable via environment variables / `.env`. |
