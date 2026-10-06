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

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Category,Severity,DescriptorId,Description,Path,Line");
            foreach (var issue in codeIssues)
            {
                sb.AppendLine(string.Join(",",
                    IssueCategory.Code.ToString(),
                    issue.Severity.ToString(),
                    issue.Id.ToString(),
                    Sanitize(issue.Description),
                    Sanitize(issue.RelativePath),
                    issue.Line.ToString()));
            }
            System.IO.File.WriteAllText(Application.dataPath + "/../ProjectAuditDetails.csv", sb.ToString());

            Debug.Log($"PROJECT_AUDITOR_OK code={codeIssues.Count} settings={settingsIssues.Count} textures={textures.Count} meshes={meshes.Count} report={reportPath}");
        }

        private static string Sanitize(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            return text.Replace("\"", "'").Replace("\r", " ").Replace("\n", " ");
        }
    }
}