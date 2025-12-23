using Archivsoftware;
using DocumentFormat.OpenXml.Packaging;
using DocumentManager.Data;
using DocumentManager.Data.Models;
using System.IO;
using System.Text;
using UglyToad.PdfPig;

namespace Archivsoftware.Services
{
    public class DocumentService
    {
        public void ImportFiles(IEnumerable<string> filePaths, FolderTreeItem? parentItem)
        {
            using var db = new DocumentDbContext();

            Folder? parentFolder = null;
            if (parentItem != null && parentItem.IsFolder)
            {
                parentFolder = db.Folders.FirstOrDefault(f => f.Id == parentItem.FolderId);
            }

            if (parentFolder == null)
            {
                return;
            }

            foreach (var filePath in filePaths)
            {
                var fileInfo = new FileInfo(filePath);

                var ext = fileInfo.Extension.ToLowerInvariant();
                if (ext != ".pdf" && ext != ".docx")
                {
                    continue;
                }

                var fileBytes = File.ReadAllBytes(filePath);
                var plainText = ExtractPlainText(filePath, ext, fileBytes);

                var newDoc = new Document
                {
                    Title = fileInfo.Name,
                    Folder = parentFolder,
                    FileData = fileBytes,
                    PlainText = plainText
                };

                db.Documents.Add(newDoc);
            }

            db.SaveChanges();
        }

        private string ExtractPlainText(string path, string ext, byte[] fileBytes)
        {
            if (ext == ".pdf")
            {
                return ExtractPlainTextFromPdf(path);
            }
            else if (ext == ".docx")
            {
                return ExtractPlainTextFromDocx(path);
            }

            return string.Empty;
        }

        private string ExtractPlainTextFromPdf(string path)
        {
            using var document = PdfDocument.Open(path);
            var sb = new StringBuilder();
            foreach (var page in document.GetPages())
            {
                sb.AppendLine(page.Text);
            }
            return sb.ToString();
        }

        private string ExtractPlainTextFromDocx(string path)
        {
            using var wordDoc = WordprocessingDocument.Open(path, false);
            var body = wordDoc.MainDocumentPart?.Document.Body;
            return body?.InnerText ?? string.Empty;
        }
    }
}
