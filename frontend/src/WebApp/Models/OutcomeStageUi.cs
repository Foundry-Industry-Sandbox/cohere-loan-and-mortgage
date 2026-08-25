using Cohere.LoanProcessing.Shared.Contracts.Api.Cases;

namespace Cohere.LoanProcessing.WebApp.Models;

public sealed record OutcomeStepMetric(string Label, string StatusLabel, string StatusCssClass);

public static class OutcomeStageUi
{
    public static OutcomeStepMetric Map(
        string stageKey,
        string metricLabel,
        WorkflowStageResponse? step,
        CaseDetailResponse caseDetail)
    {
        string executionStatus = step?.ExecutionStatus ?? "NotStarted";
        var (statusLabel, statusCssClass) = ResolveDisplay(stageKey, executionStatus, step, caseDetail);
        return new OutcomeStepMetric(metricLabel, statusLabel, statusCssClass);
    }

    private static (string Label, string CssClass) ResolveDisplay(
        string stageKey,
        string executionStatus,
        WorkflowStageResponse? step,
        CaseDetailResponse caseDetail)
    {
        if (executionStatus == "Succeeded")
        {
            return ResolveSucceededDisplay(stageKey, caseDetail);
        }

        string label = WorkflowStageUi.ToLabel(executionStatus, step?.Name, step?.Summary);
        string cssClass = executionStatus switch
        {
            "InProgress" => "outcome-in-progress",
            "Failed" => "outcome-danger",
            "Blocked" => "outcome-warn",
            "Skipped" => "outcome-muted",
            _ => "outcome-muted"
        };

        return (label, cssClass);
    }

    private static (string Label, string CssClass) ResolveSucceededDisplay(
        string stageKey,
        CaseDetailResponse caseDetail) =>
        stageKey switch
        {
            "DocumentProcessing" when caseDetail.DocumentProcessing is not null =>
                caseDetail.DocumentProcessing.IsComplete
                    ? ("Complete", "outcome-ok")
                    : ("Incomplete", "outcome-warn"),
            "HumanDecision" when caseDetail.HumanDecision is not null =>
                caseDetail.HumanDecision.Approved
                    ? ("Approved", "outcome-ok")
                    : ("Rejected", "outcome-danger"),
            "ResponsibleAiReview" when caseDetail.ResponsibleAi is not null =>
                caseDetail.ResponsibleAi.Passed
                    ? ("Passed", "outcome-ok")
                    : ("Escalated", "outcome-warn"),
            "LoanSetup" when caseDetail.LoanSetup is not null =>
                ResolveLoanSetupDisplay(caseDetail.LoanSetup),
            _ => ("Completed", "outcome-ok")
        };

    private static (string Label, string CssClass) ResolveLoanSetupDisplay(LoanSetupResultDto loanSetup)
    {
        string label = WorkflowStageUi.LoanSetupStatusLabel(loanSetup);
        string cssClass = loanSetup.Status switch
        {
            "Failed" => "outcome-danger",
            "ActionRequired" => "outcome-warn",
            _ => "outcome-ok"
        };

        return (label, cssClass);
    }
}
