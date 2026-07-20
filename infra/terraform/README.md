# infra/terraform

GCP provisioning for APIDiff: enabled APIs, VPC + subnet (VPC-native ranges),
GKE cluster + node pool (Workload Identity), Cloud SQL for PostgreSQL (private
IP), Artifact Registry, Secret Manager secrets, and the application service
account + IAM.

## Usage

```bash
cp terraform.tfvars.example terraform.tfvars   # set project_id, region, …
terraform init
terraform plan
terraform apply
```

CI validates formatting and configuration on every change
(`.github/workflows/infra.yml`): `terraform fmt -check`, `init -backend=false`,
`terraform validate`.

## Notes

- `deletion_protection` is `false` for easy teardown; set it `true` for prod.
- The database password is generated and stored in Secret Manager
  (`apidiff-<env>-db-connection`); the GitHub webhook secret
  (`apidiff-<env>-github-webhook`) is created empty and populated out of band.
- Configure a remote state backend (GCS) before using this beyond experiments.
