using Unity.ProjectAuditor.Editor;
using UnityEngine;

namespace GoatDescent.EditorTools
{
    public static class ProjectAuditorCI
    {
        public static void AuditAndExport()
        {
            string reportPath = Application.dataPath + "/../ProjectAuditReport.projectauditor";
            var projectAuditor = new ProjectAuditor();
            var report = projectAuditor.Audit();
            report.Save(reportPath);

            var codeIssues = report.FindByCategory(IssueCategory.Code);
            var settingsIssues = report.FindByCategory(IssueCategory.ProjectSetting);
            var textures = report.FindByCategory(IssueCategory.Texture);
            var meshes = report.FindByCategory(IssueCategory.Mesh);
            Debug.Log($"PROJECT_AUDITOR_OK code={codeIssues.Count} settings={settingsIssues.Count} textures={textures.Count} meshes={meshes.Count} report={reportPath}");
        }
    }
}