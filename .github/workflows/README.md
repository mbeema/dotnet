# CI/CD Pipeline Documentation

## Overview

A streamlined 3-workflow CI/CD pipeline designed for efficiency, automation, and minimal maintenance.

```
┌──────────────────────────────────────────────────────────────────────────────┐
│                           PIPELINE ARCHITECTURE                               │
├──────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│   DEVELOPER ACTIONS          AUTOMATED WORKFLOWS          ENVIRONMENTS       │
│   ─────────────────          ───────────────────          ────────────       │
│                                                                              │
│   ┌─────────────┐            ┌─────────────────┐                             │
│   │  Create PR  │ ─────────► │     ci.yml      │ ────────► Validation Only   │
│   └─────────────┘            │  Build + Test   │                             │
│                              │  Security Scan  │                             │
│   ┌─────────────┐            └─────────────────┘                             │
│   │  Merge PR   │ ─────────► │     ci.yml      │ ────────► QA (auto)         │
│   └─────────────┘            └─────────────────┘                             │
│                                                                              │
│   ┌─────────────┐            ┌─────────────────┐                             │
│   │  Release    │ ─────────► │   release.yml   │ ────────► Stage (auto)      │
│   │  (Manual)   │            │  Auto-version   │                             │
│   └─────────────┘            │  Create Tag     │                             │
│                              │  GitHub Release │                             │
│   ┌─────────────┐            └─────────────────┘                             │
│   │  Deploy     │ ─────────► │   deploy.yml    │ ────────► QA/Stage/Prod     │
│   │  (Manual)   │            │  From Release   │                             │
│   └─────────────┘            │  Slack Notify   │                             │
│                              └─────────────────┘                             │
│                                                                              │
└──────────────────────────────────────────────────────────────────────────────┘
```

---

## Workflows Summary

| Workflow | File | Trigger | Purpose |
|----------|------|---------|---------|
| **CI** | `ci.yml` | PR, Push | Build, test, security scan, deploy to QA |
| **Release** | `release.yml` | Manual, Schedule | Create versioned releases with auto-tagging |
| **Deploy** | `deploy.yml` | Manual | Deploy any release to any environment |

---

## Version Scheme

```
v{major}.{sprint}.{patch}

┌─────────────┬─────────────┬─────────────────────────────────┐
│   Version   │    Type     │          Description            │
├─────────────┼─────────────┼─────────────────────────────────┤
│  v1.54.0    │   Sprint    │  Sprint 54 release              │
│  v1.54.1    │   Hotfix    │  First hotfix for Sprint 54     │
│  v1.54.2    │   Hotfix    │  Second hotfix for Sprint 54    │
│  v1.55.0    │   Sprint    │  Sprint 55 release              │
└─────────────┴─────────────┴─────────────────────────────────┘

* Versions are AUTO-CALCULATED from existing tags
* No manual version input required
```

---

## Workflow Details

### 1. CI Workflow (`ci.yml`)

**Purpose:** Continuous Integration - validates code quality on every change

```
Triggers:
  • Pull Request → main     : Build + Test + Security (validation only)
  • Push → main             : Build + Test + Security + Deploy to QA
  • Push → hotfix/**        : Build + Test + Security + Deploy to QA
```

**Jobs:**

| Job | Description | Condition |
|-----|-------------|-----------|
| `build` | Restore, build, test, publish | Always |
| `security` | Vulnerability scan, outdated packages | Always |
| `deploy-qa` | Deploy artifact to QA environment | Push only |
| `summary` | Pipeline summary report | Always |

**Run Name Examples:**
- `PR #42 - feature/user-auth`
- `CI - main`
- `CI - hotfix/INC123-login-fix`

---

### 2. Release Workflow (`release.yml`)

**Purpose:** Create versioned releases for deployment

```
Triggers:
  • Manual dispatch         : On-demand sprint or hotfix release
  • Schedule (Tue 6 PM UTC) : Bi-weekly automated sprint release
```

**Inputs:**

| Input | Required | Description |
|-------|----------|-------------|
| `type` | Yes | `sprint` or `hotfix` |
| `sprint-number` | No | Sprint number (auto-detects if empty) |
| `hotfix-branch` | Hotfix only | e.g., `hotfix/INC123-fix-login` |
| `notes` | No | Release notes |

**Auto-Version Logic:**

```
Sprint Release:
  Last tag: v1.54.0  →  New: v1.55.0 (increment sprint)

Hotfix Release:
  Last tag: v1.54.1  →  New: v1.54.2 (increment patch)
```

**Jobs:**

| Job | Description |
|-----|-------------|
| `prepare` | Calculate version, validate inputs |
| `build` | Build and test from source branch |
| `release` | Create Git tag + GitHub Release |
| `deploy-stage` | Auto-trigger deploy to Stage |
| `summary` | Pipeline summary |

**Run Name Examples:**
- `Sprint Release 55`
- `Sprint Release (auto)`
- `Hotfix Release - hotfix/INC123-fix`
- `Scheduled Sprint Release`

---

### 3. Deploy Workflow (`deploy.yml`)

**Purpose:** Deploy releases to any environment

```
Trigger: Manual dispatch only
```

**Inputs:**

| Input | Required | Description |
|-------|----------|-------------|
| `tag` | Yes | Release tag (e.g., `v1.55.0`) or `latest` |
| `environment` | Yes | `qa`, `stage`, or `prod` |
| `ticket` | Recommended | Change ticket for audit trail |

**Jobs:**

| Job | Description | Condition |
|-----|-------------|-----------|
| `validate` | Verify release exists | Always |
| `deploy` | Download artifact, deploy, smoke test | Always |
| `post-deploy` | Update `prod` tag, create merge PR | Prod only |
| `update-release` | Add deployment status to release | Always |
| `notify` | Slack notification | Prod only |
| `summary` | Deployment summary | Always |

**Run Name Examples:**
- `Deploy v1.55.0 to stage`
- `Deploy v1.55.1 to prod`

---

## Common Scenarios

### Sprint Release (End of Sprint)

```bash
# Option 1: GitHub UI
Actions → Release → Run workflow
  Type: sprint
  Sprint number: (leave empty for auto-increment)

# Option 2: CLI
gh workflow run release.yml -f type=sprint
```

**What happens:**
1. Auto-calculates next version (e.g., v1.55.0 → v1.56.0)
2. Builds and tests from `main`
3. Creates Git tag and GitHub Release
4. Auto-deploys to Stage
5. Ready for manual prod deployment

---

### Hotfix Flow (Production Bug)

```
Step 1: Create hotfix branch
─────────────────────────────
git checkout -b hotfix/INC123-login-fix v1.55.0
git push -u origin hotfix/INC123-login-fix

Step 2: Fix and push (auto-deploys to QA)
─────────────────────────────────────────
git commit -am "fix: resolve login timeout"
git push

Step 3: Create hotfix release
─────────────────────────────
Actions → Release → Run workflow
  Type: hotfix
  Hotfix branch: hotfix/INC123-login-fix

Step 4: Deploy to prod
──────────────────────
Actions → Deploy → Run workflow
  Tag: v1.55.1 (or "latest")
  Environment: prod
  Ticket: INC123

Step 5: Merge to main
─────────────────────
PR auto-created → Review → Merge
```

---

### Rollback

```bash
# Deploy a previous working version
Actions → Deploy → Run workflow
  Tag: v1.55.0  (previous version)
  Environment: prod
  Ticket: INC456-rollback
```

---

## Environment Setup

### GitHub Environments

Create in **Settings → Environments**:

| Environment | Protection | Purpose |
|-------------|------------|---------|
| `qa` | None | Testing |
| `stage` | Optional reviewers | Pre-production |
| `prod` | **Required reviewers** | Production |

**Production Protection Rules:**
```
Settings → Environments → prod → Configure

☑ Required reviewers: Add approvers
☑ Deployment branches: main, hotfix/**
```

---

### Required Secrets

**Settings → Secrets and variables → Actions**

| Secret | Purpose |
|--------|---------|
| `SLACK_WEBHOOK_URL` | Prod deployment notifications |

**Optional (for Azure deployment):**

| Secret | Purpose |
|--------|---------|
| `AZURE_CREDENTIALS` | Azure service principal |
| `AZURE_WEBAPP_NAME` | App Service name |
| `AZURE_RESOURCE_GROUP` | Resource group |

---

### Branch Protection

**Settings → Branches → main**

```
☑ Require pull request before merging
☑ Require status checks to pass:
  • Build & Test
  • Security Scan
☑ Require branches to be up to date
```

---

## CLI Quick Reference

```bash
# ─────────────────────────────────────────────────────────────
# RELEASES
# ─────────────────────────────────────────────────────────────

# Sprint release (auto-version)
gh workflow run release.yml -f type=sprint

# Sprint release (specific number)
gh workflow run release.yml -f type=sprint -f sprint-number=56

# Hotfix release
gh workflow run release.yml -f type=hotfix -f hotfix-branch=hotfix/INC123-fix

# ─────────────────────────────────────────────────────────────
# DEPLOYMENTS
# ─────────────────────────────────────────────────────────────

# Deploy to QA
gh workflow run deploy.yml -f tag=v1.55.0 -f environment=qa

# Deploy to Stage
gh workflow run deploy.yml -f tag=v1.55.0 -f environment=stage

# Deploy to Prod
gh workflow run deploy.yml -f tag=v1.55.0 -f environment=prod -f ticket=CHG123

# Deploy latest release
gh workflow run deploy.yml -f tag=latest -f environment=prod -f ticket=CHG123

# ─────────────────────────────────────────────────────────────
# UTILITIES
# ─────────────────────────────────────────────────────────────

# List releases
gh release list

# View release details
gh release view v1.55.0

# List workflow runs
gh run list --workflow=ci.yml

# View run details
gh run view <run-id>

# List hotfix branches
git ls-remote --heads origin 'hotfix/*'
```

---

## Troubleshooting

| Issue | Cause | Solution |
|-------|-------|----------|
| "Tag already exists" | Re-running same sprint | Delete release/tag or use next sprint |
| "Branch not found" | Typo in hotfix branch | Verify: `git ls-remote origin 'hotfix/*'` |
| "No releases found" | Using `latest` with no releases | Create a release first |
| "prod.prod.1" version error | `prod` tag picked up | Fixed: only version tags (v#.#.#) used |
| Slack not working | Invalid webhook | Verify `SLACK_WEBHOOK_URL` secret |
| Prod deployed without approval | Environment not configured | Set up prod environment protection |
| PR creation failed | Permissions | Enable: Settings → Actions → Allow PRs |

---

## Security Best Practices

| Practice | Implementation |
|----------|----------------|
| Never commit secrets | Use GitHub Secrets |
| Limit secret access | Environment-specific secrets |
| Require approvals | Prod environment protection |
| Audit trail | Deployment status in releases |
| Dependency scanning | Security job on every build |
| Branch protection | PR reviews + status checks |

---

## Deployment Tracking

Each deployment is recorded in the GitHub Release:

```
### Deployments
| Environment | Deployed By | Timestamp |
|-------------|-------------|-----------|
| stage | github-actions[bot] | 2026-01-18 16:26 UTC |
| prod | mbeema | 2026-01-18 16:32 UTC |
```

---

## Architecture Benefits

| Feature | Benefit |
|---------|---------|
| **3 workflows only** | Simple, easy to maintain |
| **Auto-versioning** | No manual errors, consistent tags |
| **Single deploy source** | GitHub Releases = single source of truth |
| **Environment gates** | Prod requires approval |
| **Hotfix automation** | Auto-PR to merge back to main |
| **Audit trail** | Deployment history in releases |
| **Rollback ready** | Deploy any previous tag |
