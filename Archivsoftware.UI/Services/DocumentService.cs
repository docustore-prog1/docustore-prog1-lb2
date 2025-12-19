using DocumentManager.Data;
using DocumentManager.Data.Models;
using System.IO;

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

                var newDoc = new Document
                {
                    Title = fileInfo.Name,
                    Folder = parentFolder,
                    FileData = File.ReadAllBytes(filePath)
                };

                db.Documents.Add(newDoc);
            }

            db.SaveChanges();
        }
    }
}
