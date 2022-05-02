terraform {
  # backend "local" {}
  backend "azurerm" {
    resource_group_name  = "wordle-multiplayer"
    storage_account_name = "stterraformbackendwordle"
    container_name       = "terraform-state"
    key                  = "terraform.tfstate"
    #access_key = "" # being imported via environment variable
  }
  required_providers {
    azurerm = {
      source = "hashicorp/azurerm"
      version = "3.3.0"
    }
  }
}

provider "azurerm" {
  features {}
}
