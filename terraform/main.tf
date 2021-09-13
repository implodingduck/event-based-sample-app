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
  name     = "rg-eventbased-sampleapp-${random_string.unique.result}"
  location = var.location
  tags = {
    "managed_by" = "terraform"
  }
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

  tags = {
    "managed_by" = "terraform"
  }
}

resource "azurerm_key_vault_access_policy" "sp" {
  key_vault_id = azurerm_key_vault.kv.id
  tenant_id = data.azurerm_client_config.current.tenant_id
  object_id = data.azurerm_client_config.current.object_id
  
  key_permissions = [
    "Create",
    "Get",
    "Purge",
    "Recover",
    "Delete"
  ]

  secret_permissions = [
    "Set",
    "Purge",
    "Get",
    "List"
  ]

  certificate_permissions = [
    "Purge"
  ]

  storage_permissions = [
    "Purge"
  ]
}


resource "azurerm_key_vault_access_policy" "as1" {
  key_vault_id = azurerm_key_vault.kv.id
  tenant_id = data.azurerm_client_config.current.tenant_id
  object_id = module.func.identity_principal_id
  
  key_permissions = [
    "get",
  ]

  secret_permissions = [
    "get",
    "list"
  ]
  
}

resource "azurerm_key_vault_access_policy" "as2" {
  key_vault_id = azurerm_key_vault.kv.id
  tenant_id = data.azurerm_client_config.current.tenant_id
  object_id = module.eventapi.identity_principal_id
  
  key_permissions = [
    "get",
  ]

  secret_permissions = [
    "get",
    "list"
  ]
  
}


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

resource "azurerm_servicebus_queue" "account" {
  name                = "accountqueue"
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
    "CosmosDBConnection" = "@Microsoft.KeyVault(VaultName=${azurerm_key_vault.kv.name};SecretName=${azurerm_key_vault_secret.cosmosdbcs.name})"
    "CosmosDBDatabase"   = azurerm_cosmosdb_sql_database.db.name
    "ComsosDBCollection" = "customers"
    "EventHubName"       = "transactions"
    "EventHubConnection" = "@Microsoft.KeyVault(VaultName=${azurerm_key_vault.kv.name};SecretName=eventhubcs)"
  }
  app_identity = [
      {
          type = "SystemAssigned"
          identity_ids = null
      }
  ]
  auth_settings = [
    {
      enabled = true
      default_provider = "AzureActiveDirectory"
      unauthenticated_client_action  = "RedirectToLoginPage"
      issuer = var.api_issuer
      active_directory = [{
        client_id = var.api_client_id
        client_secret = var.api_client_secret
      }]
    }
  ]
  cors = [{
    allowed_origins = ["http://localhost:3000"]
    support_credentials = true
  }]
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
    "CosmosDBConnection" = "@Microsoft.KeyVault(VaultName=${azurerm_key_vault.kv.name};SecretName=${azurerm_key_vault_secret.cosmosdbcs.name})"
    "CosmosDBDatabase"   = azurerm_cosmosdb_sql_database.db.name
    "ComsosDBCollection" = "customers"
    "EventHubName"       = "b2caudit"
    "EventHubConnection" = "@Microsoft.KeyVault(VaultName=${azurerm_key_vault.kv.name};SecretName=eventhubcs)"
    "AppId"              = "@Microsoft.KeyVault(VaultName=${azurerm_key_vault.kv.name};SecretName=b2cappid)"
    "TenantId"           = "@Microsoft.KeyVault(VaultName=${azurerm_key_vault.kv.name};SecretName=b2ctenantid)"
    "AppSecret"          = "@Microsoft.KeyVault(VaultName=${azurerm_key_vault.kv.name};SecretName=b2cclientsecret)"
    "EventGridTopicUri"  =  azurerm_eventgrid_topic.customusers.endpoint
    "EventGridTopicKey"  = "@Microsoft.KeyVault(VaultName=${azurerm_key_vault.kv.name};SecretName=${azurerm_key_vault_secret.egkey.name})"
  }
  app_identity = [
      {
          type = "SystemAssigned"
          identity_ids = null
      }
  ]
}

resource "azurerm_cosmosdb_account" "db" {
  name                = "${local.func_name}-dba"
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  offer_type          = "Standard"
  enable_free_tier    = false # :(
  consistency_policy {
    consistency_level       = "Session"
  }
  geo_location {
    location          = "West US"
    failover_priority = 1
  }

  geo_location {
    location          = azurerm_resource_group.rg.location
    failover_priority = 0
  }
  tags         = {
    "managed_by" = "terraform"
  }
}

resource "azurerm_cosmosdb_sql_database" "db" {
  name                = "${local.func_name}-db"
  resource_group_name = azurerm_cosmosdb_account.db.resource_group_name
  account_name        = azurerm_cosmosdb_account.db.name
  throughput          = 400
}

resource "azurerm_cosmosdb_sql_container" "db" {
  name                  = "customers"
  resource_group_name   = azurerm_cosmosdb_account.db.resource_group_name
  account_name          = azurerm_cosmosdb_account.db.name
  database_name         = azurerm_cosmosdb_sql_database.db.name
  partition_key_path    = "/id"
  partition_key_version = 1
  throughput            = 400
}

resource "azurerm_cosmosdb_sql_container" "accountsdb" {
  name                  = "accounts"
  resource_group_name   = azurerm_cosmosdb_account.db.resource_group_name
  account_name          = azurerm_cosmosdb_account.db.name
  database_name         = azurerm_cosmosdb_sql_database.db.name
  partition_key_path    = "/id"
  partition_key_version = 1
  throughput            = 400
}

resource "azurerm_cosmosdb_sql_container" "transactionsdb" {
  name                  = "transactions"
  resource_group_name   = azurerm_cosmosdb_account.db.resource_group_name
  account_name          = azurerm_cosmosdb_account.db.name
  database_name         = azurerm_cosmosdb_sql_database.db.name
  partition_key_path    = "/id"
  partition_key_version = 1
  throughput            = 400
}

resource "azurerm_key_vault_secret" "cosmosdbcs" {
  depends_on = [
    azurerm_key_vault_access_policy.sp
  ]
  name         = "cosmosdbcs"
  value        = azurerm_cosmosdb_account.db.connection_strings.0 
  key_vault_id = azurerm_key_vault.kv.id
  tags         = {
    "managed_by" = "terraform"
  }
}

resource "azurerm_eventgrid_topic" "customusers" {
  name                = "eg-custom-user"
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name

  tags = {
    "managed_by" = "terraform"
  }
}

resource "azurerm_key_vault_secret" "egkey" {
  depends_on = [
    azurerm_key_vault_access_policy.sp
  ]
  name         = "egkey"
  value        = azurerm_eventgrid_topic.customusers.primary_access_key
  key_vault_id = azurerm_key_vault.kv.id
  tags         = {
    "managed_by" = "terraform"
  }
}

data "template_file" "emaillogicapp" {
  template = file("${path.module}/arm_email-logicapp.json.tmpl")
  vars = {
    "subscription_id" = var.subscription_id
    "bcc" = var.email
    "name" = "${local.func_name}-email-logicapp"
    "location" = "eastus"
    "resource_group_name" = azurerm_resource_group.rg.name
    "eventgrid_id" = azurerm_eventgrid_topic.customusers.id
  }
}

resource "azurerm_template_deployment" "logicapp" {
  name                = "${local.func_name}-email-logicapp"
  resource_group_name = azurerm_resource_group.rg.name

  template_body = data.template_file.emaillogicapp.rendered
  deployment_mode = "Incremental"
}