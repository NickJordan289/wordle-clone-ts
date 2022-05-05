# Wordle Multiplayer - Typescript
[![.github/workflows/azure-webapps-node.yml](https://github.com/NickJordan289/wordle-multiplayer-ts/actions/workflows/azure-webapps-node.yml/badge.svg)](https://github.com/NickJordan289/wordle-multiplayer-ts/actions/workflows/azure-webapps-node.yml)

Multiplayer implementation of the wordle
- Inspired by Jabrils https://www.youtube.com/watch?v=kxCPmSB2OgA

Play at https://app-wordle-multiplayer-dev-001.azurewebsites.net/

## Frontend
- Azure App Service
- Node v16
- React

## Backend
- Azure Function
- Web PubSub

## Terraform
- [System Key Provisioner (External Data Source)](https://github.com/NickJordan289/wordle-multiplayer-ts/blob/main/terraform/get_system_key.sh)
- App Service
- Function App
- Ap Service plan x2
- Application Insights
- Storage account
- Web PubSub Service

## Actions
- [Terraform + Build + Deploy](https://github.com/NickJordan289/wordle-multiplayer-ts/actions/workflows/azure-webapps-node.yml)

## Configuration
Actions Secrets
| Name | Description | Type |
| --------------- | --------------- | --------------- |
| ARM_CLIENT_ID | Service Principal Client ID | String |
| ARM_CLIENT_SECRET | Service Principal Secret | String |
| ARM_SUBSCRIPTION_ID | Subscription where this is being deployed | String |
| ARM_TENANT_ID | Tenant where this is being deployed | String |
| AZURE_CREDENTIALS | Service Principal Credentials JSON | JSON |
| AZURE_FUNCTIONAPP_PUBLISH_PROFILE | Downloaded Publish Profile for Function App | XML |
| AZURE_WEBAPP_PUBLISH_PROFILE | Downloaded Publish Profile for App Service | XML |
| TF_VAR_BACKEND_KEY | Access key to backend storage account| String |