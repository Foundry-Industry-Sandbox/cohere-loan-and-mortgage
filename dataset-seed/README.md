# Dataset seed

## Purpose

Demo-ready synthetic loan and mortgage cases consumed by the API, MCP tools, and frontend. Not required to run data-generation scripts for a standard demo.

## Dataset contents

| Path | Description |
|---|---|
| `cases/case-01` … `case-20` | Self-contained demo scenarios |
| `cases/catalog.json` | Auto-generated case metadata for the UI |
| `policies/` | Underwriting and governance policy text for RAG seeding |

Each case folder:

```text
cases/case-01/
  ingest/                       borrower-upload documents (workflow start)
  fabric-pre-requisite-data/    bronze JSON for MCP tools
  README.md
```

## File formats

| Format | Location | Use |
|---|---|---|
| `.txt` | `cases/*/ingest/` | Workflow start documents |
| `.json` | `cases/*/fabric-pre-requisite-data/` | Structured bronze data for MCP |
| `.txt` | `policies/` | Policy knowledge for search index seed |

## Schema

Bronze JSON follows schemas documented under `data-generation/corpus/bronze/*/SCHEMA.md`.

Policy files follow [`policies/SCHEMA.md`](policies/SCHEMA.md).

## Data relationships

- Each `case-NN` maps to a legacy application id `APP-NNN` (see per-case `README.md`).
- `ingest/` files represent borrower uploads at workflow start.
- `fabric-pre-requisite-data/` mirrors bronze layers (`01_application` … `07_collateral`) for MCP retrieval.
- Shared policies apply across cases.

## How the backend uses this data

| File | Description | Used by |
|---|---|---|
| `cases/*/ingest/*` | Uploaded loan documents | API workflow start, document processing agent |
| `cases/*/fabric-pre-requisite-data/**/*.json` | Case bronze data | MCP case data adapters |
| `cases/catalog.json` | Case titles and expected outcomes | Frontend catalog |
| `policies/general_policy.txt` | Primary policy seed source | MCP policy index seed job |
| `policies/*.txt` | Additional policy corpora | Policy retrieval tools |

## Regenerating the dataset

See [`../data-generation/README.md`](../data-generation/README.md). After regeneration, rebuild and redeploy containers that embed `dataset-seed/`.

## Limitations

- All data is synthetic and for demonstration only.
- Twenty cases ship by default; adding cases requires rebuild and redeploy.
- Workflow state is not persisted in the dataset.

## Highlight cases

| Case | Borrower | Expected outcome |
|---|---|---|
| `case-01` | Olivia Bennett | approve |
| `case-13` | Emily Reed | deny |
| `case-15` | Sofia Alvarez | deny |
| `case-17` | Amelia Price | manual_review |
| `case-18` | William Cooper | manual_review |
