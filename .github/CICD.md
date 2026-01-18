# CI/CD Pipeline Documentation

## Overview

Trunk-based development with 2-week sprints. Build once, deploy anywhere.

## Pipeline Architecture

```
workflows/
├── build.yml                 # Reusable: Build & test
├── pr-validation.yml         # PR checks
├── ci-cd.yml                # Push to main → Build → QA (continuous)
├── release.yml              # Build → Tag → GitHub Release (manual + scheduled)
├── deploy.yml               # Deploy any release to any environment
└── create-hotfix-branch.yml # Create hotfix branch from tag
```

## Flow Diagram

```
┌─────────────────────────────────────────────────────────────────────────┐
│                              DEVELOPMENT                                 │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│   Feature Branch ───► PR ───► pr-validation.yml ───► Merge to main      │
│                                                                          │
└────────────────────────────────────┬────────────────────────────────────┘
                                     │
                                     ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                         CI/CD (Automatic)                                │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│   Push to main ───► ci-cd.yml ───► Build ───► Deploy QA                 │
│                                                                          │
└─────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────┐
│                    RELEASE (Manual or Scheduled)                         │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│   release.yml                                                            │
│        │                                                                 │
│        ├───► Build from main (or hotfix branch)                          │
│        ├───► Create tag: v1.54.0                                         │
│        └───► Upload artifact.zip to GitHub Release                       │
│                                                                          │
│   ⏰ Scheduled: Every Tuesday 6 PM UTC (sprint end)                      │
│                                                                          │
└────────────────────────────────────┬────────────────────────────────────┘
                                     │
                                     ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                    DEPLOY (Manual - Any Environment)                     │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│   deploy.yml                                                             │
│        │                                                                 │
│        ├───► Select tag (e.g., v1.54.0)                                  │
│        ├───► Select environment (QA / Stage / Prod)                      │
│        ├───► Download artifact from GitHub Release                       │
│        ├───► Deploy to selected environment                              │
│        └───► (Prod only) Update 'prod' tag + require approval            │
│                                                                          │
└─────────────────────────────────────────────────────────────────────────┘
```

## Flexible Deployment Scenarios

| Scenario | How |
|----------|-----|
| Sprint release to all envs | release.yml → deploy.yml (QA) → deploy.yml (Stage) → deploy.yml (Prod) |
| Direct to Stage | release.yml → deploy.yml (Stage) |
| Hotfix to Prod | release.yml (hotfix) → deploy.yml (Prod) |
| Rollback | deploy.yml with previous tag |
| Test in QA | deploy.yml (QA) with any tag |

## Hotfix Flow

```
Production Bug Found (running v1.54.0)
         │
         ▼
┌─────────────────────────────────────┐
│ 1. Create hotfix branch             │
│    Run: create-hotfix-branch.yml    │
│    - base-tag: v1.54.0              │
│    - incident-id: INC001234         │
│    - description: fix-login-bug     │
│    Creates: hotfix/INC001234-fix-login-bug
└─────────────────────────────────────┘
         │
         ▼
┌─────────────────────────────────────┐
│ 2. Checkout, fix, commit, push      │
│    git fetch origin                 │
│    git checkout hotfix/INC001234-fix-login-bug
│    # make fixes                     │
│    git add . && git commit -m "fix: ..."
│    git push                         │
└─────────────────────────────────────┘
         │
         ▼
┌─────────────────────────────────────┐
│ 3. Run Release workflow             │
│    type: hotfix                     │
│    sprint-number: 54                │
│    hotfix-number: 1                 │
│    base-tag: v1.54.0                │
│    hotfix-branch: hotfix/INC001234-fix-login-bug
│    Creates: v1.54.1                 │
└─────────────────────────────────────┘
         │
         ▼
┌─────────────────────────────────────┐
│ 4. Deploy to any environment        │
│    Run: deploy.yml                  │
│    - tag: v1.54.1                   │
│    - environment: prod (or stage)   │
└─────────────────────────────────────┘
         │
         ▼
┌─────────────────────────────────────┐
│ 5. Cherry-pick to main              │
│    git checkout main                │
│    git cherry-pick <sha>            │
│    git push                         │
└─────────────────────────────────────┘
```

## Tag Naming (SemVer)

| Type | Format | Example |
|------|--------|---------|
| Sprint | `v1.{sprint}.0` | `v1.54.0` |
| Hotfix | `v1.{sprint}.{patch}` | `v1.54.1` |
| Production | `prod` | Points to current prod release |

## Workflows

| Workflow | Trigger | Purpose |
|----------|---------|---------|
| `pr-validation.yml` | PR to main | Build, test, security |
| `ci-cd.yml` | Push to main | Build → QA (continuous) |
| `release.yml` | Manual / Scheduled | Build → Tag → GitHub Release |
| `deploy.yml` | Manual | Deploy any release to any environment |
| `create-hotfix-branch.yml` | Manual | Create hotfix branch from tag |

## Scheduled Release

The release workflow runs automatically every **Tuesday at 6 PM UTC** (sprint end).

To modify the schedule, edit the cron expression in `release.yml`:
```yaml
schedule:
  - cron: '0 18 * * 2'  # Every Tuesday at 18:00 UTC
```

## Environment Setup

### GitHub Environments

Create these in **Settings → Environments**:

| Environment | Protection | Approvers |
|-------------|------------|-----------|
| `qa` | None | None |
| `stage` | None | None |
| `prod` | Required reviewers | Add team members |

## Quick Reference

### Sprint Release
```
Actions → Release → Run workflow
├── release-type: sprint
├── sprint-number: 54
└── release-notes: Sprint 54 release
→ Creates: v1.54.0
```

### Deploy to Any Environment
```
Actions → Deploy → Run workflow
├── tag: v1.54.0
├── environment: qa / stage / prod
└── change-ticket: CHG0012345 (for prod)
```

### Create Hotfix Branch
```
Actions → Create Hotfix Branch → Run workflow
├── base-tag: v1.54.0
├── incident-id: INC001234
└── description: fix-login-bug
→ Creates: hotfix/INC001234-fix-login-bug
```

### Hotfix Release
```
Actions → Release → Run workflow
├── release-type: hotfix
├── sprint-number: 54
├── hotfix-number: 1
├── base-tag: v1.54.0
└── hotfix-branch: hotfix/INC001234-fix-login-bug
→ Creates: v1.54.1
```

### Rollback
```
Actions → Deploy → Run workflow
├── tag: v1.53.0  ← Previous release tag
└── environment: prod
```
