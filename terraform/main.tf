resource "azurerm_resource_group" "rg" {
  location = "australiaeast"
  name     = "wordle-multiplayer"
}

resource "azurerm_web_pubsub" "wps" {
  name                = "wps-wordle-multiplayer-dev"
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  sku                 = "Free_F1"
  depends_on = [
    azurerm_resource_group.rg,
  ]
}

resource "azurerm_web_pubsub_hub" "psh" {
  name          = "sample_funcchat"
  web_pubsub_id = azurerm_web_pubsub.wps.id
  event_handler {
    system_events      = ["connect", "connected", "disconnected"]
    url_template       = "http://${azurerm_function_app.func.default_hostname}/runtime/webhooks/webpubsub?Code=${data.external.system_key_provisioner.result.key}"
    user_event_pattern = "*"
  }
  depends_on = [
    azurerm_web_pubsub.wps,
    data.external.system_key_provisioner,
  ]
}

resource "azurerm_app_service" "app" {
  name                = "app-wordle-multiplayer-dev-001"
  app_service_plan_id = azurerm_app_service_plan.asp-web.id
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  depends_on = [
    azurerm_app_service_plan.asp-web,
  ]
}

resource "azurerm_app_service_custom_hostname_binding" "web-host" {
  app_service_name    = "app-wordle-multiplayer-dev-001"
  hostname            = "app-wordle-multiplayer-dev-001.azurewebsites.net"
  resource_group_name = azurerm_resource_group.rg.name
  depends_on = [
    azurerm_app_service.app,
  ]
}


resource "azurerm_app_service_plan" "asp-web" {
  name                = "asp-wordle-multiplayer-dev"
  kind                = "linux"
  location            = azurerm_resource_group.rg.location
  reserved            = true
  resource_group_name = azurerm_resource_group.rg.name
  sku {
    size = "B1"
    tier = "Basic"
  }
  depends_on = [
    azurerm_resource_group.rg,
  ]
}

resource "azurerm_function_app" "func" {
  name                       = "func-wordle-multiplayer-dev"
  app_service_plan_id        = azurerm_app_service_plan.asp-func.id
  enable_builtin_logging     = false
  location                   = azurerm_resource_group.rg.location
  resource_group_name        = azurerm_resource_group.rg.name
  storage_account_access_key = azurerm_storage_account.store1.primary_access_key
  storage_account_name       = azurerm_storage_account.store1.name
  tags = {
    "hidden-link: /app-insights-instrumentation-key" = azurerm_application_insights.appi-func.instrumentation_key
    "hidden-link: /app-insights-resource-id"         = azurerm_application_insights.appi-func.id
  }
  version = "~3"
  depends_on = [
    azurerm_app_service_plan.asp-func,
  ]
}

data "external" "system_key_provisioner" {
  program = ["powershell", "Set-ExecutionPolicy Bypass -Scope Process -Force; ./GetSystemKey.ps1"]
  query = {
    funcId = azurerm_function_app.func.id
  }
}

resource "azurerm_storage_account" "store1" {
  account_kind             = "Storage"
  account_replication_type = "LRS"
  account_tier             = "Standard"
  location                 = azurerm_resource_group.rg.location
  name                     = "stwordlemultiplayer001"
  resource_group_name      = azurerm_resource_group.rg.name
  depends_on = [
    azurerm_resource_group.rg,
  ]
}

resource "azurerm_app_service_plan" "asp-func" {
  name                = "ASP-wordlemultiplayer-8d7a"
  kind                = "functionapp"
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  sku {
    size = "Y1"
    tier = "Dynamic"
  }
  depends_on = [
    azurerm_resource_group.rg,
  ]
}

resource "azurerm_app_service_custom_hostname_binding" "func-host" {
  app_service_name    = "func-wordle-multiplayer-dev"
  hostname            = "func-wordle-multiplayer-dev.azurewebsites.net"
  resource_group_name = azurerm_resource_group.rg.name
  depends_on = [
    azurerm_function_app.func,
  ]
}

resource "azurerm_application_insights" "appi-func" {
  name                = "func-wordle-multiplayer-dev"
  application_type    = "web"
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  sampling_percentage = 0
  depends_on = [
    azurerm_resource_group.rg,
  ]
}