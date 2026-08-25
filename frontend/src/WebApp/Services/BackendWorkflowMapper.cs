using Cohere.LoanProcessing.Shared.Contracts.Api.Cases;
using Cohere.LoanProcessing.WebApp.Contracts.Backend;
using Cohere.LoanProcessing.WebApp.Models;

namespace Cohere.LoanProcessing.WebApp.Services;

internal static class BackendWorkflowMapper
{
    public static void ApplyDocuments(CaseSession session, CaseDocumentsResponse documents)
    {
        session.Documents = documents.Documents
            .Select((document, index) => new DocumentRecordDto(
                $"doc-{index + 1}",
                InferDocumentType(document.FileName),
                document.FileName,
                true))
            .ToList();
        session.UpdatedAt = DateTimeOffset.UtcNow;
    }

    public static void ApplyWorkflowStatus(CaseSession session, BasicWorkflowStatusResponse status)
    {
        session.ExecutionId = status.ExecutionId;
        session.BackendStatus = status.Status;
        session.FailureReason = status.FailureReason;
        session.LastWorkflowUpdateUtc = status.LastUpdatedUtc;
        session.DocumentProcessing = AgentOutputParser.ParseDocumentProcessing(status.AgentOutputs.DocumentProcessing)
            ?? session.DocumentProcessing;
        session.Underwriting = AgentOutputParser.ParseUnderwriting(status.AgentOutputs.Underwriting)
            ?? session.Underwriting;
        session.ResponsibleAi = AgentOutputParser.ParseResponsibleAi(status.AgentOutputs.ResponsibleAi)
            ?? session.ResponsibleAi;
        session.LoanSetup = AgentOutputParser.ParseLoanSetup(status.AgentOutputs.LoanSetup)
            ?? session.LoanSetup;

        AppendWorkflowNote(session, status);
        session.UpdatedAt = DateTimeOffset.UtcNow;
    }

    public static void ApplyHumanDecision(CaseSession session, bool approved, string? notes)
    {
        session.HumanDecision = new HumanDecisionDto(approved, notes, DateTimeOffset.UtcNow);
        session.Notes.Add(new CaseNoteDto(
            approved ? "Human approval recorded." : "Human rejection recorded.",
            DateTimeOffset.UtcNow));
        session.UpdatedAt = DateTimeOffset.UtcNow;
    }

    public static CaseDetailResponse ToDetail(CaseSession session) =>
        new(
            session.SeedCase.CaseId,
            session.SeedCase.CaseId,
            MapUiStatus(session),
            MapCurrentStage(session),
            MapUiStatusLabel(session),
            BuildApplicant(session.SeedCase),
            session.Documents,
            session.DocumentProcessing,
            session.Underwriting,
            session.ResponsibleAi,
            session.LoanSetup,
            session.HumanDecision,
            BuildHumanReview(session),
            session.Notes.OrderBy(note => note.RecordedAt).ToList(),
            [],
            ResolveAllowedActions(session),
            session.CreatedAt,
            session.UpdatedAt,
            1);

    public static CaseSummaryResponse ToSummary(CaseSession session) =>
        new(
            session.SeedCase.CaseId,
            session.SeedCase.CaseId,
            MapUiStatus(session),
            MapCurrentStage(session),
            MapUiStatusLabel(session),
            BuildApplicant(session.SeedCase),
            session.CreatedAt,
            session.UpdatedAt,
            session.ExecutionId);

    public static WorkflowProgressResponse ToProgress(CaseSession session) =>
        new(
            session.SeedCase.CaseId,
            MapUiStatus(session),
            MapCurrentStage(session),
            MapUiStatusLabel(session),
            BuildSteps(session),
            ResolveAllowedActions(session));

    public static WorkflowRunStatusResponse ToRunStatus(CaseSession session) =>
        new(
            session.ExecutionId ?? string.Empty,
            session.SeedCase.CaseId,
            MapRunStatus(session.BackendStatus),
            null,
            session.ExecutionId,
            session.FailureReason,
            MapCurrentStage(session),
            session.FailureReason,
            session.CreatedAt,
            session.LastWorkflowUpdateUtc ?? session.UpdatedAt);

    public static ScenarioSummaryResponse ToScenario(SeedCaseDefinition seedCase) =>
        new(
            seedCase.CaseId,
            CaseDisplayFormatting.FormatLoanTitle(seedCase.BorrowerName, seedCase.RequestedLoanAmount),
            seedCase.Description,
            seedCase.ExpectedOutcome,
            seedCase.DemoTagline);

    private static ApplicantProfileDto BuildApplicant(SeedCaseDefinition seedCase)
    {
        decimal annualIncome = seedCase.MonthlyIncome is > 0 and var monthlyIncome
            ? monthlyIncome * 12m
            : 0m;

        return new ApplicantProfileDto(
            seedCase.BorrowerName,
            BuildEmail(seedCase),
            seedCase.OccupationTitle ?? "Employed",
            annualIncome,
            seedCase.PrimaryReason ?? "Dataset seed application",
            seedCase.RequestedLoanAmount,
            seedCase.Product ?? "Mortgage");
    }

    private static string BuildEmail(SeedCaseDefinition seedCase)
    {
        string localPart = new(seedCase.BorrowerName
            .Where(char.IsLetterOrDigit)
            .Select(char.ToLowerInvariant)
            .ToArray());

        return string.IsNullOrWhiteSpace(localPart)
            ? $"{seedCase.CaseId.ToLowerInvariant()}@example.com"
            : $"{localPart}@example.com";
    }

    private static HumanReviewContextDto? BuildHumanReview(CaseSession session)
    {
        var reasons = new List<string>();
        if (session.Underwriting?.RequiresHumanReview == true)
        {
            reasons.Add("Underwriting recommendation requires human review.");
        }

        if (session.Underwriting?.HasCriticalAnomaly == true)
        {
            reasons.Add("Critical anomaly detected during financial assessment.");
        }

        return reasons.Count == 0 ? null : new HumanReviewContextDto(reasons);
    }

    private static IReadOnlyList<string> ResolveAllowedActions(CaseSession session) =>
        session.BackendStatus switch
        {
            "NotStarted" => ["StartWorkflow"],
            "AwaitingHumanApproval" when session.HumanDecision is null => ["SubmitDecision"],
            _ => []
        };

    private static string MapUiStatus(CaseSession session) =>
        session.BackendStatus switch
        {
            "NotStarted" => "Created",
            "Pending" or "Running" => "InReview",
            "AwaitingHumanApproval" => "AwaitingHumanApproval",
            "Completed" when session.HumanDecision?.Approved == false => "Rejected",
            "Completed" => "Completed",
            "Failed" => "Failed",
            _ => "Created"
        };

    private static string MapUiStatusLabel(CaseSession session) => MapUiStatus(session);

    private static readonly string[] StagePipeline =
    [
        "DocumentProcessing",
        "Underwriting",
        "HumanDecision",
        "ResponsibleAiReview",
        "LoanSetup"
    ];

    private static string MapCurrentStage(CaseSession session) => ResolveActiveStageKey(session);

    private static string MapRunStatus(string backendStatus) =>
        backendStatus switch
        {
            "Pending" => "Queued",
            "Running" => "Running",
            "AwaitingHumanApproval" => "Succeeded",
            "Completed" => "Succeeded",
            "Failed" => "Failed",
            _ => "Queued"
        };

    private static IReadOnlyList<WorkflowStageResponse> BuildSteps(CaseSession session)
    {
        return
        [
            BuildStep(session, "Document Review", "DocumentProcessing",
                BuildDocumentProcessingSummary(session.DocumentProcessing)),
            BuildStep(session, "Financial Assessment", "Underwriting",
                BuildUnderwritingSummary(session.Underwriting)),
            BuildStep(session, "Human Review", "HumanDecision", session.HumanDecision?.Notes),
            BuildStep(session, "Fairness Review", "ResponsibleAiReview",
                BuildResponsibleAiSummary(session.ResponsibleAi)),
            BuildStep(session, "Account Setup", "LoanSetup",
                BuildLoanSetupSummary(session.LoanSetup))
        ];
    }

    private static string? BuildDocumentProcessingSummary(DocumentProcessingResultDto? result)
    {
        if (result is null)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(result.Decision) && !string.IsNullOrWhiteSpace(result.Summary))
        {
            return $"{result.Decision}. {result.Summary}";
        }

        if (!string.IsNullOrWhiteSpace(result.Decision))
        {
            return result.Decision;
        }

        return result.Summary;
    }

    private static string? BuildUnderwritingSummary(UnderwritingResultDto? result)
    {
        if (result is null)
        {
            return null;
        }

        string decision = result.Decision ?? result.Recommendation;
        if (!string.IsNullOrWhiteSpace(result.Summary))
        {
            return $"{decision}: {result.Summary}";
        }

        return decision;
    }

    private static string? BuildResponsibleAiSummary(ResponsibleAiResultDto? result)
    {
        if (result is null)
        {
            return null;
        }

        string decision = result.Decision ?? (result.Passed ? "Passed" : "Escalated");
        if (!string.IsNullOrWhiteSpace(result.Summary))
        {
            return $"{decision}. {result.Summary}";
        }

        return decision;
    }

    private static string? BuildLoanSetupSummary(LoanSetupResultDto? result)
    {
        if (result is null)
        {
            return null;
        }

        if (result.RequiresAdditionalInformation)
        {
            return result.Decision ?? "Additional information required";
        }

        if (!string.IsNullOrWhiteSpace(result.Decision) && !string.Equals(result.Status, "Completed", StringComparison.OrdinalIgnoreCase))
        {
            return result.Decision;
        }

        return result.SetupSummary;
    }

    private static WorkflowStageResponse BuildStep(
        CaseSession session,
        string name,
        string stageKey,
        string? summary)
    {
        string executionStatus = ResolveExecutionStatus(session, stageKey, summary);
        return new WorkflowStageResponse(name, stageKey, executionStatus, summary);
    }

    private static string ResolveActiveStageKey(CaseSession session) =>
        session.BackendStatus switch
        {
            "NotStarted" => "None",
            "Pending" => "DocumentProcessing",
            "Running" => ResolveRunningActiveStageKey(session),
            "AwaitingHumanApproval" => "HumanDecision",
            "Completed" => "Completed",
            "Failed" => "Failed",
            _ => "None"
        };

    private static string ResolveRunningActiveStageKey(CaseSession session)
    {
        if (session.DocumentProcessing is null || !session.DocumentProcessing.IsComplete)
        {
            return "DocumentProcessing";
        }

        if (session.Underwriting is null)
        {
            return "Underwriting";
        }

        if (session.HumanDecision is null)
        {
            return "Underwriting";
        }

        if (!session.HumanDecision.Approved)
        {
            return "HumanDecision";
        }

        if (session.ResponsibleAi is null)
        {
            return "ResponsibleAiReview";
        }

        if (session.LoanSetup is null || !IsLoanSetupTerminal(session.LoanSetup))
        {
            return "LoanSetup";
        }

        return "Completed";
    }

    private static string ResolveExecutionStatus(CaseSession session, string stageKey, string? summary)
    {
        bool hasData = HasStageData(session, stageKey);
        string activeKey = ResolveActiveStageKey(session);
        int stageIndex = GetStageIndex(stageKey);
        int activeIndex = GetStageIndex(activeKey);

        if (session.BackendStatus == "AwaitingHumanApproval" && stageKey == "HumanDecision")
        {
            return "Blocked";
        }

        if (session.BackendStatus == "Completed")
        {
            return hasData ? ResolveCompletedExecutionStatus(stageKey, summary) : "NotStarted";
        }

        if (session.BackendStatus == "Failed")
        {
            string failedStage = ResolveRunningActiveStageKey(session);
            int failedIndex = GetStageIndex(failedStage);

            if (string.Equals(stageKey, failedStage, StringComparison.OrdinalIgnoreCase))
            {
                return "Failed";
            }

            if (hasData && stageIndex >= 0 && failedIndex >= 0 && stageIndex < failedIndex)
            {
                return "Succeeded";
            }

            return "NotStarted";
        }

        if (session.BackendStatus is "Running" or "Pending")
        {
            if (string.Equals(stageKey, activeKey, StringComparison.OrdinalIgnoreCase))
            {
                return "InProgress";
            }

            if (hasData && activeIndex >= 0 && stageIndex >= 0 && stageIndex < activeIndex)
            {
                return "Succeeded";
            }

            return "NotStarted";
        }

        return hasData ? "Succeeded" : "NotStarted";
    }

    private static bool HasStageData(CaseSession session, string stageKey) => stageKey switch
    {
        "DocumentProcessing" => session.DocumentProcessing is not null,
        "Underwriting" => session.Underwriting is not null,
        "HumanDecision" => session.HumanDecision is not null,
        "ResponsibleAiReview" => session.ResponsibleAi is not null,
        "LoanSetup" => session.LoanSetup is not null,
        _ => false
    };

    private static int GetStageIndex(string stageKey)
    {
        for (int i = 0; i < StagePipeline.Length; i++)
        {
            if (string.Equals(StagePipeline[i], stageKey, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }

    private static bool IsLoanSetupTerminal(LoanSetupResultDto setup) =>
        setup.Status is "Completed" or "Failed" or "ActionRequired";

    private static string ResolveCompletedExecutionStatus(string stageKey, string? summary)
    {
        if (stageKey == "LoanSetup"
            && summary?.Contains("additional information", StringComparison.OrdinalIgnoreCase) == true)
        {
            return "Blocked";
        }

        if (stageKey == "ResponsibleAiReview"
            && summary?.Contains("not supported", StringComparison.OrdinalIgnoreCase) == true)
        {
            return "Blocked";
        }

        if (stageKey == "Underwriting"
            && (summary?.StartsWith("Reject", StringComparison.OrdinalIgnoreCase) == true
                || summary?.StartsWith("Deny", StringComparison.OrdinalIgnoreCase) == true))
        {
            return "Succeeded";
        }

        return "Succeeded";
    }

    private static void AppendWorkflowNote(CaseSession session, BasicWorkflowStatusResponse status)
    {
        string? message = status.Status switch
        {
            "Running" when session.DocumentProcessing is not null && session.Underwriting is null =>
                "Document processing completed.",
            "Running" when session.Underwriting is not null && session.HumanDecision is null =>
                "Underwriting recommendation available.",
            "AwaitingHumanApproval" => "Awaiting human approval.",
            "Completed" when session.LoanSetup is not null => "Loan setup completed.",
            "Failed" => status.FailureReason ?? "Workflow failed.",
            _ => null
        };

        if (message is null || session.Notes.Any(note => note.Message.Equals(message, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        session.Notes.Add(new CaseNoteDto(message, status.LastUpdatedUtc));
    }

    private static string InferDocumentType(string fileName)
    {
        string normalized = fileName.Replace('-', '_').ToLowerInvariant();
        return normalized switch
        {
            _ when normalized.Contains("loan_application") => "application",
            _ when normalized.Contains("identity") => "identity",
            _ when normalized.Contains("paystub") => "income_proof",
            _ when normalized.Contains("employment") => "employment_verification",
            _ when normalized.Contains("bank_statement") => "bank_statement",
            _ when normalized.Contains("credit") => "credit_report",
            _ when normalized.Contains("appraisal") => "appraisal",
            _ when normalized.Contains("property") => "property_information",
            _ => "supporting_document"
        };
    }

    private static bool IsRequiredDocument(string fileName)
    {
        _ = fileName;
        return false;
    }
}
