# CI/CD Pipeline Documentation

## Overview

Trunk-based development with 2-week sprints. Build once, deploy to all environments.

## Pipeline Architecture

```
workflows/
├── build.yml           # Reusable: Build & test
├── deploy.yml          # Reusable: Deploy to environment
├── pr-validation.yml   # PR checks
├── ci-cd.yml          # Merge to main → QA (automatic)
├── release.yml        # Sprint/Hotfix → Build → Tag → QA → GitHub Release
├── deploy-stage.yml   # Download → Stage
└── deploy-prod.yml    # Download → Prod (with approval)
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
│                    RELEASE (Manual - Sprint End)                         │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│   release.yml (type: sprint)                                             │
│        │                                                                 │
│        ├───► Build from main                                             │
│        ├───► Create tag: main-sprint-54                                  │
│        ├───► Deploy QA                                                   │
│        └───► Upload artifact.zip to GitHub Release                       │
│                                                                          │
└────────────────────────────────────┬────────────────────────────────────┘
                                     │
                                     ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                    DEPLOY STAGE (Manual)                                 │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│   deploy-stage.yml                                                       │
│        │                                                                 │
│        ├───► Download artifact.zip from GitHub Release                   │
│        └───► Deploy Stage (same artifact)                                │
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
│ 1. Create hotfix branch from tag    │
│    git checkout -b hotfix/INC001 main-sprint-54
└─────────────────────────────────────┘
         │
         ▼
┌─────────────────────────────────────┐
│ 2. Fix, commit, push                │
│    git push -u origin hotfix/INC001 │
└─────────────────────────────────────┘
         │
         ▼
┌─────────────────────────────────────┐
│ 3. Run Release workflow             │
│    type: hotfix                     │
│    sprint-number: 54                │
│    hotfix-number: 1                 │
│    base-tag: main-sprint-54         │
│    hotfix-branch: hotfix/INC001     │
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
| `release.yml` | Manual | Sprint or Hotfix release |
| `deploy-stage.yml` | Manual | Download → Stage |
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

### Hotfix Release
```
Actions → Release → Run workflow
├── release-type: hotfix
├── sprint-number: 54
├── hotfix-number: 1
├── base-tag: main-sprint-54
└── hotfix-branch: hotfix/INC001-fix
```

### Deploy to Stage
```
Actions → Deploy Stage → Run workflow
└── tag: main-sprint-54
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
