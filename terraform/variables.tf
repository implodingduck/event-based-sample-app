variable "subscription_id" {
  type = string
  sensitive = true
}

variable "resource_group_name" {
  type = string
}

variable "storage_account_name" {
  type = string
  sensitive = true
}

variable "key" {
  type = string
}

variable "container_name" {
  type = string
}

variable "location" {
  type = string
  default = "East US"
}

variable "email" {
  type = string
}

variable "api_issuer" {
  type = string
  sensitive = true
}

variable "api_client_id" {
  type = string
  sensitive = true
}

variable "api_client_secret" {
  type = string
  sensitive = true
}