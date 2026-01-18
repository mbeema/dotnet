# Secrets, Variables & Environments Guide

## Overview

GitHub provides three levels of configuration:

| Type | Syntax | Use For | Encrypted |
|------|--------|---------|-----------|
| **Secrets** | `${{ secrets.NAME }}` | Sensitive data (passwords, tokens, keys) | Yes |
| **Variables** | `${{ vars.NAME }}` | Non-sensitive config (URLs, names, regions) | No |
| **Environment Variables** | `env:` block | Workflow-level constants | No |

## 1. Repository Secrets

### Location
```
GitHub → Repository → Settings → Secrets and variables → Actions → Secrets
```

### How to Add
1. Click **New repository secret**
2. Enter name (e.g., `AZURE_CREDENTIALS`)
3. Enter value
4. Click **Add secret**

### Usage in Workflow
```yaml
steps:
  - name: Deploy
    run: ./deploy.sh
    env:
      API_KEY: ${{ secrets.API_KEY }}

  # Or directly in commands (masked in logs)
  - name: Login
    run: az login --service-principal -u ${{ secrets.AZURE_CLIENT_ID }}
```

### Common Secrets
```
AZURE_CREDENTIALS       # Azure service principal JSON
AWS_ACCESS_KEY_ID       # AWS credentials
AWS_SECRET_ACCESS_KEY
DOCKER_USERNAME         # Container registry
DOCKER_PASSWORD
KUBE_CONFIG            # Kubernetes config (base64)
SSH_PRIVATE_KEY        # Server access
SLACK_WEBHOOK_URL      # Notifications
SONAR_TOKEN            # Code quality
```

## 2. Repository Variables

### Location
```
GitHub → Repository → Settings → Secrets and variables → Actions → Variables
```

### How to Add
1. Click **New repository variable**
2. Enter name (e.g., `AZURE_REGION`)
3. Enter value (e.g., `eastus`)
4. Click **Add variable**

### Usage in Workflow
```yaml
steps:
  - name: Deploy
    run: |
      echo "Deploying to ${{ vars.AZURE_REGION }}"
      az webapp deploy --name ${{ vars.APP_NAME }}
```

### Common Variables
```
AZURE_REGION           # eastus, westus2
AZURE_SUBSCRIPTION_ID  # Non-secret subscription ID
AWS_REGION             # us-east-1
CONTAINER_REGISTRY     # ghcr.io/myorg, myacr.azurecr.io
K8S_NAMESPACE          # production, staging
APP_NAME               # my-calculator-app
DOMAIN                 # example.com
```

## 3. Environment-Specific Configuration

### Create Environments
```
GitHub → Repository → Settings → Environments → New environment
```

Create: `qa`, `stage`, `prod`

### Environment Secrets (Override Repository Secrets)
```
Settings → Environments → [env] → Environment secrets
```

Each environment can have its own secrets:
```
qa:
  - DATABASE_URL → qa-database.example.com
  - API_KEY → qa-api-key-xxx

stage:
  - DATABASE_URL → stage-database.example.com
  - API_KEY → stage-api-key-xxx

prod:
  - DATABASE_URL → prod-database.example.com
  - API_KEY → prod-api-key-xxx
```

### Environment Variables (Override Repository Variables)
```
Settings → Environments → [env] → Environment variables
```

```
qa:
  - SERVER_URL → https://qa.example.com
  - LOG_LEVEL → debug

prod:
  - SERVER_URL → https://example.com
  - LOG_LEVEL → warning
```

### Usage in Workflow
```yaml
jobs:
  deploy:
    runs-on: ubuntu-latest
    environment: prod  # ← This activates prod secrets/variables

    steps:
      - name: Deploy
        run: |
          # Uses prod-specific values automatically
          echo "URL: ${{ vars.SERVER_URL }}"
        env:
          DB_URL: ${{ secrets.DATABASE_URL }}
```

## 4. Environment Protection Rules

### Location
```
Settings → Environments → [env] → Protection rules
```

### Options

| Rule | Description |
|------|-------------|
| **Required reviewers** | Must approve before deployment |
| **Wait timer** | Delay before deployment (e.g., 30 min) |
| **Deployment branches** | Restrict which branches can deploy |

### Setup for Production
```
Environment: prod
├── Required reviewers: @devops-team, @tech-lead
├── Wait timer: 30 minutes (optional)
└── Deployment branches: main, hotfix-*
```

## 5. Workflow-Level Environment Variables

### Global (entire workflow)
```yaml
env:
  DOTNET_VERSION: '10.0.x'
  SOLUTION_FILE: 'DotnetCICD.slnx'

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - run: echo $DOTNET_VERSION  # 10.0.x
```

### Job-Level
```yaml
jobs:
  build:
    runs-on: ubuntu-latest
    env:
      BUILD_CONFIG: Release

    steps:
      - run: dotnet build -c $BUILD_CONFIG
```

### Step-Level
```yaml
steps:
  - name: Build
    env:
      NODE_ENV: production
    run: npm run build
```

## 6. Passing Secrets to Reusable Workflows

### Caller Workflow
```yaml
jobs:
  deploy:
    uses: ./.github/workflows/deploy.yml
    with:
      environment: prod
    secrets: inherit  # ← Pass all secrets
```

### Or Explicit
```yaml
jobs:
  deploy:
    uses: ./.github/workflows/deploy.yml
    with:
      environment: prod
    secrets:
      DEPLOY_TOKEN: ${{ secrets.DEPLOY_TOKEN }}
      SSH_KEY: ${{ secrets.SSH_KEY }}
```

### Reusable Workflow Definition
```yaml
on:
  workflow_call:
    inputs:
      environment:
        type: string
    secrets:
      DEPLOY_TOKEN:
        required: true
      SSH_KEY:
        required: false
```

## 7. Complete Example Setup

### Repository Secrets (shared across all environments)
```
SLACK_WEBHOOK_URL = https://hooks.slack.com/xxx
SONAR_TOKEN = xxx
DOCKER_USERNAME = myuser
DOCKER_PASSWORD = xxx
```

### Repository Variables (shared defaults)
```
CONTAINER_REGISTRY = ghcr.io/myorg
APP_NAME = calculator
```

### Environment: qa
```
Secrets:
  DATABASE_URL = Server=qa-db.example.com;Database=calc_qa;...
  API_KEY = qa-key-xxx

Variables:
  SERVER_HOST = qa.example.com
  LOG_LEVEL = debug
  REPLICAS = 1
```

### Environment: stage
```
Secrets:
  DATABASE_URL = Server=stage-db.example.com;Database=calc_stage;...
  API_KEY = stage-key-xxx

Variables:
  SERVER_HOST = stage.example.com
  LOG_LEVEL = info
  REPLICAS = 2
```

### Environment: prod
```
Protection:
  Required reviewers: @devops-lead, @cto
  Wait timer: 15 minutes

Secrets:
  DATABASE_URL = Server=prod-db.example.com;Database=calc_prod;...
  API_KEY = prod-key-xxx

Variables:
  SERVER_HOST = example.com
  LOG_LEVEL = warning
  REPLICAS = 5
```

## 8. Best Practices

### DO
```
✅ Use secrets for: passwords, tokens, connection strings, private keys
✅ Use variables for: URLs, regions, app names, feature flags
✅ Use environment-specific overrides for different configs per env
✅ Use secrets: inherit for reusable workflows
✅ Rotate secrets regularly
✅ Use least-privilege service accounts
```

### DON'T
```
❌ Don't hardcode secrets in workflow files
❌ Don't echo/print secrets (they're masked but still risky)
❌ Don't use secrets in workflow names or job names
❌ Don't commit secrets to repository (use .gitignore)
❌ Don't share secrets across unrelated repositories
```

## 9. Debugging

### Check if Secret Exists
```yaml
- name: Check secret
  run: |
    if [ -z "${{ secrets.MY_SECRET }}" ]; then
      echo "::error::MY_SECRET is not set"
      exit 1
    fi
    echo "Secret is configured"
```

### List Available Variables
```yaml
- name: Show variables
  run: |
    echo "CONTAINER_REGISTRY: ${{ vars.CONTAINER_REGISTRY }}"
    echo "APP_NAME: ${{ vars.APP_NAME }}"
```

### View in Actions Log
- Secrets are automatically masked as `***`
- Variables are shown in plain text
- Use `ACTIONS_STEP_DEBUG=true` secret for verbose logging

## 10. Quick Reference

```yaml
# Secrets (sensitive)
${{ secrets.API_KEY }}
${{ secrets.DATABASE_URL }}

# Variables (non-sensitive)
${{ vars.APP_NAME }}
${{ vars.REGION }}

# GitHub context
${{ github.actor }}
${{ github.sha }}
${{ github.ref_name }}

# Environment variables
env:
  MY_VAR: value
  FROM_SECRET: ${{ secrets.SECRET }}
  FROM_VAR: ${{ vars.VARIABLE }}
```
