locals {
  required_services = [
    "compute.googleapis.com",
    "container.googleapis.com",
    "sqladmin.googleapis.com",
    "secretmanager.googleapis.com",
    "artifactregistry.googleapis.com",
    "servicenetworking.googleapis.com",
  ]
}

resource "google_project_service" "enabled" {
  for_each           = toset(local.required_services)
  service            = each.value
  disable_on_destroy = false
}
