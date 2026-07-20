resource "random_password" "db" {
  length  = 24
  special = true
}

resource "google_sql_database_instance" "postgres" {
  name             = "apidiff-${var.environment}"
  database_version = "POSTGRES_16"
  region           = var.region

  depends_on = [google_service_networking_connection.sql]

  settings {
    tier = var.postgres_tier

    ip_configuration {
      ipv4_enabled    = false
      private_network = google_compute_network.vpc.id
    }

    backup_configuration {
      enabled = true
    }
  }

  # Set to true for production instances.
  deletion_protection = false
}

resource "google_sql_database" "apidiff" {
  name     = var.db_name
  instance = google_sql_database_instance.postgres.name
}

resource "google_sql_user" "apidiff" {
  name     = var.db_user
  instance = google_sql_database_instance.postgres.name
  password = random_password.db.result
}
