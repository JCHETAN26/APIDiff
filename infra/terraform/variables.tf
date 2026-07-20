variable "project_id" {
  type        = string
  description = "GCP project id to deploy into."
}

variable "region" {
  type        = string
  description = "GCP region."
  default     = "us-central1"
}

variable "environment" {
  type        = string
  description = "Environment name (e.g. staging, prod)."
  default     = "staging"
}

variable "gke_node_count" {
  type        = number
  description = "Number of nodes in the primary node pool."
  default     = 2
}

variable "gke_machine_type" {
  type        = string
  description = "Machine type for GKE nodes."
  default     = "e2-standard-2"
}

variable "postgres_tier" {
  type        = string
  description = "Cloud SQL machine tier."
  default     = "db-custom-1-3840"
}

variable "db_name" {
  type        = string
  description = "Application database name."
  default     = "apidiff"
}

variable "db_user" {
  type        = string
  description = "Application database user."
  default     = "apidiff"
}
