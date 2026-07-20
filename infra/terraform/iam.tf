# Application service account for the API, bound to the in-cluster Kubernetes
# service account via Workload Identity.
resource "google_service_account" "api" {
  account_id   = "apidiff-${var.environment}-api"
  display_name = "APIDiff API (${var.environment})"
}

resource "google_project_iam_member" "api_sql_client" {
  project = var.project_id
  role    = "roles/cloudsql.client"
  member  = "serviceAccount:${google_service_account.api.email}"
}

resource "google_secret_manager_secret_iam_member" "api_db_connection" {
  secret_id = google_secret_manager_secret.db_connection.id
  role      = "roles/secretmanager.secretAccessor"
  member    = "serviceAccount:${google_service_account.api.email}"
}

resource "google_secret_manager_secret_iam_member" "api_github_webhook" {
  secret_id = google_secret_manager_secret.github_webhook.id
  role      = "roles/secretmanager.secretAccessor"
  member    = "serviceAccount:${google_service_account.api.email}"
}

# Let the Kubernetes SA apidiff/apidiff-api impersonate the GCP SA.
resource "google_service_account_iam_member" "api_workload_identity" {
  service_account_id = google_service_account.api.name
  role               = "roles/iam.workloadIdentityUser"
  member             = "serviceAccount:${var.project_id}.svc.id.goog[apidiff/apidiff-api]"
}
