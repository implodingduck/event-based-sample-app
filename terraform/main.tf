terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "=2.71.0"
    }
    random = {
      source  = "hashicorp/random"
      version = "=3.1.0"
    }
  }
  backend "azurerm" {

  }
}

provider "azurerm" {
  features {}

  subscription_id = var.subscription_id
}

locals {
  func_name = "eventbsa${random_string.unique.result}"
}

data "azurerm_client_config" "current" {}


resource "azurerm_resource_group" "rg" {
  name     = "rg-eventbased-sampleapp"
  location = var.location
}

resource "random_string" "unique" {
  length  = 8
  special = false
  upper   = false
}

resource "azurerm_key_vault" "kv" {
  name                       = "kv-${local.func_name}"
  location                   = azurerm_resource_group.rg.location
  resource_group_name        = azurerm_resource_group.rg.name
  tenant_id                  = data.azurerm_client_config.current.tenant_id
  sku_name                   = "standard"
  soft_delete_retention_days = 7
  purge_protection_enabled = false

  
}

resource "azurerm_key_vault_access_policy" "sp" {
  key_vault_id = azurerm_key_vault.kv.id
  tenant_id = data.azurerm_client_config.current.tenant_id
  object_id = data.azurerm_client_config.current.object_id
  
  key_permissions = [
    "create",
    "get",
    "purge",
    "recover",
    "delete"
  ]

  secret_permissions = [
    "set",
    "purge",
    "get",
    "list"
  ]

  certificate_permissions = [
    "purge"
  ]

  storage_permissions = [
    "purge"
  ]
  
}


# resource "azurerm_key_vault_access_policy" "as" {
#   for_each = toset([
#     module.func.identity_principal_id
#   ])
#   key_vault_id = azurerm_key_vault.kv.id
#   tenant_id = data.azurerm_client_config.current.tenant_id
#   object_id = each.key
  
#   key_permissions = [
#     "get",
#   ]

#   secret_permissions = [
#     "get",
#     "list"
#   ]
  
# }


resource "azurerm_servicebus_namespace" "customer" {
  name                = "${local.func_name}-namespace"
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  sku                 = "Standard"

  tags = {
    "managed_by" = "terraform"
  }
}

resource "azurerm_servicebus_queue" "customer" {
  name                = "customerqueue"
  resource_group_name = azurerm_resource_group.rg.name
  namespace_name      = azurerm_servicebus_namespace.customer.name

  enable_partitioning = true
}

resource "azurerm_key_vault_secret" "sbcustomercs" {
  depends_on = [
    azurerm_key_vault_access_policy.sp
  ]
  name         = "sbcustomercs"
  value        = azurerm_servicebus_namespace.customer.default_primary_connection_string
  key_vault_id = azurerm_key_vault.kv.id
  tags         = {
    "managed_by" = "terraform"
  }
}



module "func" {
  source = "github.com/implodingduck/tfmodules//functionapp"
  func_name = "${local.func_name}"
  resource_group_name = azurerm_resource_group.rg.name
  resource_group_location = azurerm_resource_group.rg.location
  working_dir = "../frontend-api"
  app_settings = {
    "FUNCTIONS_WORKER_RUNTIME" = "dotnet"
    "CustomerServiceBus" = "@Microsoft.KeyVault(VaultName=${azurerm_key_vault.kv.name};SecretName=${azurerm_key_vault_secret.sbcustomercs.name})"
  }
  app_identity = [
      {
          type = "SystemAssigned"
          identity_ids = null
      }
  ]
}

module "eventapi" {
  source = "github.com/implodingduck/tfmodules//functionapp"
  func_name = "api${local.func_name}"
  resource_group_name = azurerm_resource_group.rg.name
  resource_group_location = azurerm_resource_group.rg.location
  working_dir = "../event-api"
  app_settings = {
    "FUNCTIONS_WORKER_RUNTIME" = "dotnet"
    "CustomerServiceBus" = "@Microsoft.KeyVault(VaultName=${azurerm_key_vault.kv.name};SecretName=${azurerm_key_vault_secret.sbcustomercs.name})"
  }
  app_identity = [
      {
          type = "SystemAssigned"
          identity_ids = null
      }
  ]
}