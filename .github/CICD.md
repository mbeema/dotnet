# CI/CD Pipeline Documentation

## Overview

Trunk-based development with 2-week sprints. Build once, deploy to all environments.

## Pipeline Architecture

```
workflows/
├── build.yml                 # Reusable: Build & test
├── pr-validation.yml         # PR checks
├── ci-cd.yml                # Merge to main → QA (automatic)
├── release.yml              # Sprint/Hotfix → Build → Tag → QA → Stage → GitHub Release
├── deploy-prod.yml          # Download → Prod (with approval)
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
│                    RELEASE & DEPLOY (Manual - Sprint End)                │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│   release.yml (type: sprint)                                             │
│        │                                                                 │
│        ├───► Build from main                                             │
│        ├───► Create tag: main-sprint-54                                  │
│        ├───► Deploy QA                                                   │
│        ├───► Deploy Stage                                                │
│        └───► Upload artifact.zip to GitHub Release                       │
│                                                                          │
└────────────────────────────────────┬────────────────────────────────────┘
                                     │
                                     ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                    DEPLOY PROD (Manual + Approval)                       │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│   deploy-prod.yml                                                        │
│        │                                                                 │
│        ├───► Download artifact.zip from GitHub Release                   │
│        ├───► ⏸️ Environment Approval                                     │
│        ├───► Deploy Prod (same artifact)                                 │
│        └───► Update 'prod' tag                                           │
│                                                                          │
└─────────────────────────────────────────────────────────────────────────┘
```

## Hotfix Flow

```
Production Bug Found (running main-sprint-54)
         │
         ▼
┌─────────────────────────────────────┐
│ 1. Create hotfix branch             │
│    Run: create-hotfix-branch.yml    │
│    - base-tag: main-sprint-54       │
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
│    base-tag: main-sprint-54         │
│    hotfix-branch: hotfix/INC001234-fix-login-bug
└─────────────────────────────────────┘
         │
         ├───► Build from hotfix branch
         ├───► Create tag: hotfix-sprint-54-1
         ├───► Deploy QA
         └───► GitHub Release
         │
         ▼
┌─────────────────────────────────────┐
│ 4. Deploy Stage                     │
│    tag: hotfix-sprint-54-1          │
└─────────────────────────────────────┘
         │
         ▼
┌─────────────────────────────────────┐
│ 5. Deploy Prod                      │
│    tag: hotfix-sprint-54-1          │
└─────────────────────────────────────┘
         │
         ▼
┌─────────────────────────────────────┐
│ 6. Cherry-pick to main              │
│    git checkout main                │
│    git cherry-pick <sha>            │
│    git push                         │
└─────────────────────────────────────┘
```

## Tag Naming

| Type | Format | Example |
|------|--------|---------|
| Sprint | `main-sprint-{number}` | `main-sprint-54` |
| Hotfix | `hotfix-sprint-{sprint}-{hotfix}` | `hotfix-sprint-54-1` |
| Production | `prod` | Points to current prod release |

## Workflows

| Workflow | Trigger | Purpose |
|----------|---------|---------|
| `pr-validation.yml` | PR to main | Build, test, security |
| `ci-cd.yml` | Push to main | Build → QA (automatic) |
| `create-hotfix-branch.yml` | Manual | Create hotfix branch from tag |
| `release.yml` | Manual | Sprint/Hotfix → QA → Stage |
| `deploy-prod.yml` | Manual | Download → Prod |

## Build Once, Deploy All

```
Release Workflow (Sprint or Hotfix)
         │
         ▼
┌─────────────────────┐
│       BUILD         │ ← Runs once
└─────────────────────┘
         │
         ▼
┌─────────────────────┐
│    GitHub Release   │ ← artifact.zip stored
│    + artifact.zip   │
└─────────────────────┘
         │
         ├───► QA    (deploy in release workflow)
         │
         ▼
┌─────────────────────┐
│   deploy-stage.yml  │ ← Downloads same artifact.zip
└─────────────────────┘
         │
         ▼
┌─────────────────────┐
│   deploy-prod.yml   │ ← Downloads same artifact.zip
└─────────────────────┘
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
```

### Create Hotfix Branch
```
Actions → Create Hotfix Branch → Run workflow
├── base-tag: main-sprint-54
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
├── base-tag: main-sprint-54
└── hotfix-branch: hotfix/INC001234-fix-login-bug
```

### Deploy to Prod
```
Actions → Deploy Prod → Run workflow
├── tag: main-sprint-54
└── change-ticket: CHG0012345
```

### Rollback
```
Actions → Deploy Prod → Run workflow
└── tag: main-sprint-53  ← Previous release tag
```
