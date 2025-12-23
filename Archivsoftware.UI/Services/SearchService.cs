using DocumentManager.Data;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace Archivsoftware.Services
{
    public class SearchService
    {
        public List<SearchResultItem> Search(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return new List<SearchResultItem>();

            var normalized = query.Trim();

            using var db = new DocumentDbContext();

            // einfache LIKE-Suche auf PlainText
            var docs = db.Documents
                .Include(d => d.Folder)
                    .ThenInclude(f => f.ParentFolder)
                .Where(d => d.PlainText.Contains(normalized))
                .AsNoTracking()
                .ToList();

            var results = new List<SearchResultItem>();

            foreach (var d in docs)
            {
                var folderPath = BuildFolderPath(d.Folder);
                var snippet = BuildSnippet(d.PlainText, normalized, 80);

                results.Add(new SearchResultItem
                {
                    DocumentId = d.Id,
                    Title = d.Title,
                    FolderPath = folderPath,
                    Snippet = snippet
                });
            }

            return results;
        }

        private static string BuildFolderPath(DocumentManager.Data.Models.Folder folder)
        {
            var stack = new Stack<string>();
            var current = folder;
            while (current != null)
            {
                stack.Push(current.Name);
                current = current.ParentFolder;
            }

            return string.Join(" / ", stack);
        }

        private static string BuildSnippet(string text, string term, int radius)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            var idx = text.IndexOf(term, StringComparison.OrdinalIgnoreCase);
            if (idx < 0)
                return text.Length <= 2 * radius ? text : text.Substring(0, 2 * radius) + " ...";

            var start = Math.Max(0, idx - radius);
            var end = Math.Min(text.Length, idx + term.Length + radius);
            var snippet = text.Substring(start, end - start).Trim();

            if (start > 0) snippet = "... " + snippet;
            if (end < text.Length) snippet += " ...";

            return snippet;
        }
    }
}
