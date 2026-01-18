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
│        ├───► Create tag: v1.54.0                                         │
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
└─────────────────────────────────────┘
         │
         ├───► Build from hotfix branch
         ├───► Create tag: v1.54.1
         ├───► Deploy QA
         └───► GitHub Release
         │
         ▼
┌─────────────────────────────────────┐
│ 4. Deploy Stage (in release.yml)    │
│    tag: v1.54.1                     │
└─────────────────────────────────────┘
         │
         ▼
┌─────────────────────────────────────┐
│ 5. Deploy Prod                      │
│    tag: v1.54.1                     │
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
Actions → Release & Deploy → Run workflow
├── release-type: sprint
├── sprint-number: 54
└── release-notes: Sprint 54 release
→ Creates: v1.54.0
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
Actions → Release & Deploy → Run workflow
├── release-type: hotfix
├── sprint-number: 54
├── hotfix-number: 1
├── base-tag: v1.54.0
└── hotfix-branch: hotfix/INC001234-fix-login-bug
→ Creates: v1.54.1
```

### Deploy to Prod
```
Actions → Deploy Prod → Run workflow
├── tag: v1.54.0
└── change-ticket: CHG0012345
```

### Rollback
```
Actions → Deploy Prod → Run workflow
└── tag: v1.53.0  ← Previous release tag
```
