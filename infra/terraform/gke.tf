resource "google_service_account" "gke_nodes" {
  account_id   = "apidiff-${var.environment}-gke"
  display_name = "APIDiff GKE nodes (${var.environment})"
}

resource "google_container_cluster" "primary" {
  name       = "apidiff-${var.environment}"
  location   = var.region
  network    = google_compute_network.vpc.id
  subnetwork = google_compute_subnetwork.subnet.id

  # Manage node pools separately from the cluster.
  remove_default_node_pool = true
  initial_node_count       = 1

  networking_mode = "VPC_NATIVE"
  ip_allocation_policy {
    cluster_secondary_range_name  = "pods"
    services_secondary_range_name = "services"
  }

  workload_identity_config {
    workload_pool = "${var.project_id}.svc.id.goog"
  }

  release_channel {
    channel = "REGULAR"
  }

  # Set to true for production clusters.
  deletion_protection = false
}

resource "google_container_node_pool" "primary" {
  name     = "primary"
  location = var.region
  cluster  = google_container_cluster.primary.name

  node_count = var.gke_node_count

  node_config {
    machine_type    = var.gke_machine_type
    service_account = google_service_account.gke_nodes.email
    oauth_scopes    = ["https://www.googleapis.com/auth/cloud-platform"]

    workload_metadata_config {
      mode = "GKE_METADATA"
    }
  }

  management {
    auto_repair  = true
    auto_upgrade = true
  }
}
