output "gke_cluster_name" {
  description = "GKE cluster name."
  value       = google_container_cluster.primary.name
}

output "gke_location" {
  description = "GKE cluster location."
  value       = google_container_cluster.primary.location
}

output "artifact_registry_repository" {
  description = "Artifact Registry repository id."
  value       = google_artifact_registry_repository.images.repository_id
}

output "db_instance_connection_name" {
  description = "Cloud SQL instance connection name."
  value       = google_sql_database_instance.postgres.connection_name
}

output "api_service_account_email" {
  description = "GCP service account the API runs as (Workload Identity)."
  value       = google_service_account.api.email
}
