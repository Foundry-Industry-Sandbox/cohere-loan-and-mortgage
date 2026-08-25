# Data model

## Entities

| Entity | Description |
|---|---|
| **Case** | Demo loan application (`case-01` … `case-20`) |
| **Application** | Legacy id (`APP-001` … `APP-020`) linked to a case |
| **Borrower** | Synthetic applicant profile in bronze JSON and ingest docs |
| **Ingest document** | Flat text (and optional pdf/png) files uploaded at workflow start |
| **Bronze record** | Structured JSON per domain layer (application, identity, income, etc.) |
| **Policy** | Underwriting rule text indexed for RAG retrieval |
| **Workflow execution** | In-memory run identified by `executionId` |
| **Agent output** | Strict JSON from each Foundry agent stage |

## Relationships

```text
Case (case-NN)
  |-- 1:1 Application (APP-NNN)
  |-- 1:N Ingest documents (ingest/)
  |-- 1:N Bronze records (fabric-pre-requisite-data/<layer>/)
  |
Policies (shared)
  |-- indexed into Azure AI Search policy index
```

Scenario metadata and expected outcomes are defined in `data-generation/scripts/scenarios.py` and reflected in `cases/catalog.json`.

## File schemas

Bronze layers (per `data-generation/corpus/bronze/*/SCHEMA.md`):

| Layer | Folder | Example file |
|---|---|---|
| Application | `01_application` | `APP-001.json` |
| Identity | `02_identity` | `APP-001_id.json` |
| Income | `03_income` | `APP-001_paystub_1.json` |
| Employment | `04_employment` | `APP-001_voe.json` |
| Banking | `05_banking` | `APP-001_statement_1.json` |
| Credit | `06_credit` | `APP-001_credit.json` |
| Collateral | `07_collateral` | `APP-001_appraisal.json` |

Ingest documents typically include: `loan_application.txt`, `identity.txt`, `paystub_*.txt`, `bank_statement_*.txt`, `employment_verification.txt`, `appraisal.txt`.

## Example records

**Catalog entry** (`cases/catalog.json`):

```json
{
  "caseId": "case-01",
  "title": "Olivia Bennett",
  "context": {
    "borrower": "Olivia Bennett",
    "expectedDecision": "approve",
    "primaryReason": "meets_credit_income_collateral_policy"
  }
}
```

**Policy snippet** (`policies/general_policy.txt`):

```txt
Policy Ref: UW-100
Rule: approve if credit score >= 700
Threshold: 700
Action: approve
```

## How agents use the data

| Agent | Primary data sources |
|---|---|
| Document processing | `ingest/` documents, indexed evidence, customer context from bronze |
| Underwriting | Application profile, credit/income/collateral bronze, policies, evidence search |
| Responsible AI | Policies, human decision context, underwriting output |
| Loan setup | Prior agent JSON outputs, setup draft tooling |

MCP tools abstract data access so the same tool contracts work in local (`dataset-seed`) or Fabric mode.
