resource "google_secret_manager_secret" "db_connection" {
  secret_id  = "apidiff-${var.environment}-db-connection"
  depends_on = [google_project_service.enabled]

  replication {
    auto {}
  }
}

resource "google_secret_manager_secret_version" "db_connection" {
  secret = google_secret_manager_secret.db_connection.id
  secret_data = join(";", [
    "Host=${google_sql_database_instance.postgres.private_ip_address}",
    "Port=5432",
    "Database=${var.db_name}",
    "Username=${var.db_user}",
    "Password=${random_password.db.result}",
  ])
}

# Populated out of band (the value is set by an operator, not Terraform).
resource "google_secret_manager_secret" "github_webhook" {
  secret_id  = "apidiff-${var.environment}-github-webhook"
  depends_on = [google_project_service.enabled]

  replication {
    auto {}
  }
}
