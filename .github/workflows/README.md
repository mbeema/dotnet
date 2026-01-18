# CI/CD Pipeline Documentation

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                              CI/CD ARCHITECTURE                                  │
├─────────────────────────────────────────────────────────────────────────────────┤
│                                                                                  │
│  TRIGGERS              WORKFLOWS                 ENVIRONMENTS                    │
│  ────────              ─────────                 ────────────                    │
│                                                                                  │
│  PR → main ──────────► ci.yml ────────────────► (validation only)               │
│                        ├─ Build & Test                                          │
│                        └─ Security Scan                                         │
│                                                                                  │
│  Push → main ────────► ci.yml ────────────────► QA (auto-deploy)                │
│  Push → hotfix/* ────► ├─ Build & Test                                          │
│                        ├─ Security Scan                                         │
│                        └─ Deploy to QA                                          │
│                                                                                  │
│  Manual ─────────────► release.yml ───────────► GitHub Release + Stage          │
│  Schedule (Tue 6PM) ─► ├─ Auto-version                                          │
│                        ├─ Build & Test                                          │
│                        ├─ Create Tag                                            │
│                        ├─ Create Release                                        │
│                        └─ Auto-deploy to Stage                                  │
│                                                                                  │
│  Manual ─────────────► deploy.yml ────────────► QA / Stage / Prod               │
│                        ├─ Download from Release                                 │
│                        ├─ Deploy                                                │
│                        ├─ Smoke Test                                            │
│                        ├─ Slack Notification (prod)                             │
│                        └─ Auto-create PR (hotfix to main)                       │
│                                                                                  │
└─────────────────────────────────────────────────────────────────────────────────┘
```

---

## Workflow Files

| File | Purpose | Trigger |
|------|---------|---------|
| `ci.yml` | Build, test, security scan, deploy to QA | PR, Push to main/hotfix |
| `release.yml` | Create versioned releases | Manual, Schedule |
| `deploy.yml` | Deploy releases to environments | Manual |

---

## Version Scheme

```
v{major}.{sprint}.{patch}

Examples:
  v1.54.0  → Sprint 54 release
  v1.54.1  → Hotfix 1 for Sprint 54
  v1.54.2  → Hotfix 2 for Sprint 54
  v1.55.0  → Sprint 55 release
```

**Versions are auto-calculated** - no manual input required.

---

## Setup Guide

### 1. GitHub Repository Settings

#### Environments

Create three environments in **Settings → Environments**:

| Environment | Purpose | Configuration |
|-------------|---------|---------------|
| `qa` | Testing | No protection needed |
| `stage` | Pre-production | Optional: required reviewers |
| `prod` | Production | **Required**: protection rules |

**Production Environment Protection:**
```
Settings → Environments → prod → Configure

☑ Required reviewers: Add 1-2 approvers
☑ Wait timer: 0-5 minutes (optional)
☑ Deployment branches: Selected branches
   - main
   - hotfix/**
```

#### Branch Protection

```
Settings → Branches → Add rule → main

☑ Require a pull request before merging
☑ Require status checks to pass
  - Build & Test (ci.yml)
  - Security Scan (ci.yml)
☑ Require branches to be up to date
```

---

### 2. Secrets Configuration

Navigate to **Settings → Secrets and variables → Actions**

#### Required Secrets

| Secret | Description | Where Used |
|--------|-------------|------------|
| `SLACK_WEBHOOK_URL` | Slack incoming webhook URL | `deploy.yml` (prod notifications) |

#### Optional Secrets (for actual deployment)

| Secret | Description | Where Used |
|--------|-------------|------------|
| `AZURE_CREDENTIALS` | Azure service principal JSON | `deploy.yml` |
| `AZURE_WEBAPP_NAME` | Azure App Service name | `deploy.yml` |
| `AZURE_RESOURCE_GROUP` | Azure resource group | `deploy.yml` |

**How to create Slack Webhook:**
1. Go to https://api.slack.com/apps
2. Create New App → From scratch
3. Incoming Webhooks → Activate
4. Add New Webhook to Workspace
5. Copy the webhook URL

---

### 3. Environment Variables

#### Workflow-Level Variables

Set in **Settings → Secrets and variables → Actions → Variables**

| Variable | Description | Example |
|----------|-------------|---------|
| `DOTNET_VERSION` | .NET SDK version | `8.0.x` |
| `APP_NAME` | Application name | `calculator` |

#### Using Variables in Workflows

```yaml
env:
  # Repository variables (set in GitHub UI)
  DOTNET_VERSION: ${{ vars.DOTNET_VERSION }}

  # Hardcoded in workflow
  DOTNET_NOLOGO: true
```

#### Environment-Specific Variables

Set per-environment in **Settings → Environments → [env] → Environment variables**

| Environment | Variable | Value |
|-------------|----------|-------|
| qa | `APP_URL` | `https://qa.example.com` |
| stage | `APP_URL` | `https://stage.example.com` |
| prod | `APP_URL` | `https://prod.example.com` |

---

## Workflow Details

### ci.yml - Continuous Integration

**Triggers:**
- Pull request to `main`
- Push to `main` or `hotfix/**`

**Jobs:**

| Job | Description | Runs On |
|-----|-------------|---------|
| `build` | Build & test application | All triggers |
| `security` | Scan for vulnerabilities | All triggers |
| `deploy-qa` | Deploy to QA environment | Push only |
| `summary` | Generate pipeline summary | Always |

**Outputs:**
- Build artifact (7-day retention)
- Test results
- Security scan report

---

### release.yml - Release Creation

**Triggers:**
- Manual dispatch
- Schedule: Tuesday 6 PM UTC (bi-weekly)

**Inputs:**

| Input | Required | Description |
|-------|----------|-------------|
| `type` | Yes | `sprint` or `hotfix` |
| `sprint` | No | Sprint number (for hotfix) |
| `source` | No | Source branch (auto-detected) |
| `notes` | No | Release notes |

**Auto-Version Calculation:**

```
Sprint Release:
  Last tag: v1.54.0
  New tag:  v1.55.0  (minor + 1)

Hotfix Release:
  Sprint: 54
  Last tag: v1.54.1
  New tag:  v1.54.2  (patch + 1)
```

**Jobs:**

| Job | Description |
|-----|-------------|
| `prepare` | Calculate version, validate inputs |
| `build` | Build & test application |
| `release` | Create tag and GitHub Release |
| `deploy-stage` | Auto-deploy to Stage environment |
| `summary` | Generate pipeline summary |

**Outputs:**
- Git tag
- GitHub Release with artifact
- Changelog
- Stage deployment

---

### deploy.yml - Deployment

**Triggers:**
- Manual dispatch only

**Inputs:**

| Input | Required | Description |
|-------|----------|-------------|
| `tag` | Yes | Release tag or `latest` |
| `environment` | Yes | `qa`, `stage`, or `prod` |
| `ticket` | No | Change ticket (recommended for prod) |

**Jobs:**

| Job | Description | Condition |
|-----|-------------|-----------|
| `validate` | Validate tag exists | Always |
| `deploy` | Deploy to environment | Always |
| `post-deploy` | Update prod tag, create PR | Prod only |
| `notify` | Slack notification | Prod only |
| `update-release` | Add deployment status | Always |
| `summary` | Generate summary | Always |

---

## Common Scenarios

### Sprint Release

```bash
# Automatic - just run the workflow
Actions → Release → Run workflow
  Type: sprint
  (everything else auto-detected)
```

### Hotfix Flow

```bash
# 1. Create hotfix branch from release tag
git checkout -b hotfix/INC123-fix-login v1.54.0
git push -u origin hotfix/INC123-fix-login

# 2. Make fix and push (auto-deploys to QA)
git commit -am "fix: resolve login issue"
git push

# 3. Create release (auto-calculates version)
Actions → Release → Run workflow
  Type: hotfix
  Sprint: 54
  (branch auto-detected, version auto-calculated)

# 4. Deploy to prod
Actions → Deploy → Run workflow
  Tag: v1.54.1 (or "latest")
  Environment: prod
  Ticket: INC123

# 5. PR auto-created to merge hotfix to main
# Review and merge the PR
```

### Rollback

```bash
# Deploy a previous version
Actions → Deploy → Run workflow
  Tag: v1.54.0  (previous working version)
  Environment: prod
  Ticket: INC456
```

---

## Customization

### Adding Actual Deployment

Replace the dummy deployment in `deploy.yml`:

```yaml
- name: Deploy
  run: |
    # Azure App Service
    az webapp deploy \
      --resource-group ${{ secrets.AZURE_RESOURCE_GROUP }} \
      --name ${{ secrets.AZURE_WEBAPP_NAME }} \
      --src-path ./deploy \
      --type zip

    # Or Azure CLI login first
    az login --service-principal \
      -u ${{ secrets.AZURE_CLIENT_ID }} \
      -p ${{ secrets.AZURE_CLIENT_SECRET }} \
      --tenant ${{ secrets.AZURE_TENANT_ID }}
```

### Adding Smoke Tests

Replace the dummy smoke test:

```yaml
- name: Smoke test
  run: |
    # Health check
    curl -f https://${{ inputs.environment }}.example.com/health

    # API test
    response=$(curl -s https://${{ inputs.environment }}.example.com/api/status)
    if [[ "$response" != *"ok"* ]]; then
      echo "Smoke test failed!"
      exit 1
    fi
```

### Adding More Notifications

```yaml
# Microsoft Teams
- name: Notify Teams
  uses: jdcargile/ms-teams-notification@v1.4
  with:
    github-token: ${{ github.token }}
    ms-teams-webhook-uri: ${{ secrets.TEAMS_WEBHOOK_URL }}
    notification-summary: "Deployed to prod"

# Email (via SendGrid)
- name: Send Email
  uses: dawidd6/action-send-mail@v3
  with:
    server_address: smtp.sendgrid.net
    username: apikey
    password: ${{ secrets.SENDGRID_API_KEY }}
    subject: "Deployment Complete"
    to: team@example.com
```

---

## Troubleshooting

### Common Issues

| Issue | Cause | Solution |
|-------|-------|----------|
| "Tag already exists" | Version collision | Check existing tags, workflow auto-increments |
| "Branch not found" | Typo in branch name | List branches: `git branch -r \| grep hotfix` |
| "No releases found" | Using "latest" with no releases | Create a release first |
| Slack not working | Invalid webhook | Verify `SLACK_WEBHOOK_URL` secret |
| Prod deploy without approval | Environment not configured | Set up environment protection |

### Debugging

```bash
# View workflow runs
gh run list --workflow=ci.yml

# View specific run
gh run view <run-id>

# Download logs
gh run view <run-id> --log

# Re-run failed workflow
gh run rerun <run-id>
```

---

## Security Best Practices

1. **Never commit secrets** - Use GitHub Secrets
2. **Limit secret access** - Use environment-specific secrets
3. **Require approvals** - Enable environment protection for prod
4. **Audit deployments** - Review deployment history in releases
5. **Scan dependencies** - Security job runs on every build
6. **Branch protection** - Require PR reviews and status checks

---

## Quick Reference

### Workflow URLs

```
CI:      https://github.com/{owner}/{repo}/actions/workflows/ci.yml
Release: https://github.com/{owner}/{repo}/actions/workflows/release.yml
Deploy:  https://github.com/{owner}/{repo}/actions/workflows/deploy.yml
```

### CLI Commands

```bash
# Trigger workflows via CLI
gh workflow run release.yml -f type=sprint
gh workflow run release.yml -f type=hotfix -f sprint=54
gh workflow run deploy.yml -f tag=v1.54.0 -f environment=prod -f ticket=INC123

# List releases
gh release list

# View release
gh release view v1.54.0
```
