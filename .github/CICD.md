# CI/CD Pipeline Documentation

## Overview

Trunk-based development with 2-week sprints ending on Tuesdays.

## Pipeline Architecture

```
workflows/
├── build.yml           # Reusable: Build & test
├── deploy.yml          # Reusable: Deploy to environment
├── pr-validation.yml   # PR checks
├── ci-cd.yml          # Main branch → QA
├── release.yml        # Sprint + Production (unified)
└── hotfix.yml         # Emergency fixes
```

## Flow Diagram

```
┌─────────────────────────────────────────────────────────────────────┐
│                         DEVELOPMENT                                  │
├─────────────────────────────────────────────────────────────────────┤
│                                                                      │
│   Feature Branch ──► PR ──► pr-validation.yml ──► Review ──► Merge  │
│                              (build, test, security)                 │
│                                                                      │
└──────────────────────────────────┬──────────────────────────────────┘
                                   │
                                   ▼
┌─────────────────────────────────────────────────────────────────────┐
│                         CONTINUOUS INTEGRATION                       │
├─────────────────────────────────────────────────────────────────────┤
│                                                                      │
│   Push to main ──► ci-cd.yml ──► Build ──► Deploy QA (automatic)    │
│                                                                      │
└──────────────────────────────────┬──────────────────────────────────┘
                                   │
                                   ▼
┌─────────────────────────────────────────────────────────────────────┐
│                         SPRINT RELEASE (Tuesday)                     │
├─────────────────────────────────────────────────────────────────────┤
│                                                                      │
│   release.yml (type: sprint)                                         │
│        │                                                             │
│        ├──► Create Tag (sprint-25.02)                               │
│        ├──► Build                                                    │
│        ├──► Deploy QA                                                │
│        ├──► Deploy STAGE                                             │
│        └──► Create GitHub Release                                    │
│                                                                      │
└──────────────────────────────────┬──────────────────────────────────┘
                                   │
                                   ▼
┌─────────────────────────────────────────────────────────────────────┐
│                         PRODUCTION RELEASE (Monthly)                 │
├─────────────────────────────────────────────────────────────────────┤
│                                                                      │
│   release.yml (type: production)                                     │
│        │                                                             │
│        ├──► Select existing tag (sprint-25.02)                      │
│        ├──► Pre-deployment checklist                                 │
│        ├──► ⏸️  APPROVAL GATE                                        │
│        ├──► Deploy PROD (same artifact)                             │
│        └──► Update prod tag                                          │
│                                                                      │
└─────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────┐
│                         HOTFIX (Emergency)                           │
├─────────────────────────────────────────────────────────────────────┤
│                                                                      │
│   hotfix.yml                                                         │
│        │                                                             │
│        ├──► Build & Tag (hotfix-YYYYMMDD.N)                         │
│        ├──► Deploy QA                                                │
│        ├──► ⏸️  QA Validation                                        │
│        ├──► Deploy STAGE (can skip for Critical)                    │
│        └──► Deploy PROD                                              │
│                                                                      │
└─────────────────────────────────────────────────────────────────────┘
```

## Workflows

| Workflow | Trigger | Purpose |
|----------|---------|---------|
| `pr-validation.yml` | PR to main | Build, test, security scan |
| `ci-cd.yml` | Push to main | Build → Deploy QA |
| `release.yml` | Manual | Sprint release OR Production release |
| `hotfix.yml` | Manual | Emergency production fixes |

## Release Workflow Usage

### Sprint Release (Every Tuesday)
```
Actions → Release → Run workflow
├── release-type: sprint
├── sprint-number: 25.02
└── release-notes: (optional)
```

### Production Release (Monthly)
```
Actions → Release → Run workflow
├── release-type: production
├── tag: sprint-25.02
└── change-ticket: CHG0012345 (optional)
```

## Environments

| Environment | Auto-deploy | Approval Required |
|-------------|-------------|-------------------|
| QA | Yes (on merge) | No |
| Stage | Sprint release | No |
| Prod | Production release | Yes |

### GitHub Setup

1. Go to **Settings → Environments**
2. Create: `qa`, `stage`, `prod`, `qa-validation`
3. For `prod`: Add required reviewers

## Versioning

| Type | Format | Example |
|------|--------|---------|
| CI Build | `1.0.{run}` | `1.0.42` |
| Sprint | `1.0.0-sprint.{YY.SS}` | `1.0.0-sprint.25.02` |
| Hotfix | `1.0.0-hotfix.{YYYYMMDD}.{N}` | `1.0.0-hotfix.20250115.1` |

## Tags

| Tag | Purpose |
|-----|---------|
| `sprint-YY.SS` | Sprint release point |
| `hotfix-YYYYMMDD.N` | Hotfix release point |
| `prod` | Current production (moves) |

## Build Once, Deploy All

```
Build creates artifact
       │
       ▼
┌─────────────────────────────────────────┐
│  calculator-1.0.0-sprint.25.02          │
│  ├── Calculator.dll                     │
│  ├── build-info.json  ◄── metadata      │
│  └── ...                                │
└─────────────────────────────────────────┘
       │
       ├──► QA    (same artifact)
       ├──► Stage (same artifact)
       └──► Prod  (same artifact)
```

## Sprint Calendar

```
Week 1
├── Mon: Sprint starts
├── Tue-Fri: PRs merged → auto-deploy to QA

Week 2
├── Mon: Stabilization
├── Tue: Sprint ends
│        └── Run: release.yml (type: sprint)
│            Creates: sprint-25.02
│            Deploys: QA + Stage
│
│   Later (monthly):
│        └── Run: release.yml (type: production)
│            Uses: sprint-25.02
│            Deploys: Prod (with approval)
```

## Rollback

To rollback production:
```
Actions → Release → Run workflow
├── release-type: production
├── tag: sprint-25.01  ◄── previous sprint tag
```
