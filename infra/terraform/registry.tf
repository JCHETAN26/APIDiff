resource "google_artifact_registry_repository" "images" {
  location      = var.region
  repository_id = "apidiff"
  description   = "APIDiff container images"
  format        = "DOCKER"
  depends_on    = [google_project_service.enabled]
}
