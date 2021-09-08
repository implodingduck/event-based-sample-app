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

module "func" {
  source = "github.com/implodingduck/tfmodules//functionapp"
  func_name = "${local.func_name}"
  resource_group_name = azurerm_resource_group.rg.name
  resource_group_location = azurerm_resource_group.rg.location
  working_dir = "../frontend-api"
  app_settings = {
    "FUNCTIONS_WORKER_RUNTIME" = "dotnet"
  }
  app_identity = [
      {
          type = "SystemAssigned"
          identity_ids = null
      }
  ]
}