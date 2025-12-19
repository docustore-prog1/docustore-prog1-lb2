using DocumentManager.Data;
using DocumentManager.Data.Models;
using Microsoft.EntityFrameworkCore;


namespace Archivsoftware.Services
{
    public class FolderService
    {
        public List<FolderTreeItem> GetFolderTree()
        {
            using var db = new DocumentDbContext();

            var folders = db.Folders
                .Include(f => f.ParentFolder)
                .Include(f => f.SubFolders)
                .Include(f => f.Documents)
                .AsNoTracking()
                .ToList();

            var rootFolders = folders
                .Where(f => f.ParentFolder == null)
                .ToList();

            var rootItems = new List<FolderTreeItem>();
            foreach (var root in rootFolders)
            {
                rootItems.Add(BuildFolderNode(root));
            }

            return rootItems;
        }

        public Folder? CreateFolder(string name, FolderTreeItem? parentItem)
        {
            using var db = new DocumentDbContext();

            Folder? parentFolder = null;
            if (parentItem != null && parentItem.IsFolder)
            {
                parentFolder = db.Folders.FirstOrDefault(f => f.Id == parentItem.FolderId);
            }

            var parentId = parentFolder?.Id;
            var exists = db.Folders.Any(f =>
                f.Name == name &&
                ((f.ParentFolder == null && parentId == null) ||
                 (f.ParentFolder != null && f.ParentFolder.Id == parentId)));

            if (exists)
            {
                return null;
            }

            var newFolder = new Folder
            {
                Name = name,
                ParentFolder = parentFolder
            };

            db.Folders.Add(newFolder);
            db.SaveChanges();

            return newFolder;
        }

        public bool RenameFolder(FolderTreeItem selectedItem, string newName)
        {
            if (selectedItem == null || !selectedItem.IsFolder)
                return false;

            using var db = new DocumentDbContext();

            var folder = db.Folders
                .Include(f => f.ParentFolder)
                .FirstOrDefault(f => f.Id == selectedItem.FolderId);

            if (folder == null)
                return false;

            var parentId = folder.ParentFolder?.Id;
            var exists = db.Folders.Any(f =>
                f.Id != folder.Id &&
                f.Name == newName &&
                ((f.ParentFolder == null && parentId == null) ||
                 (f.ParentFolder != null && f.ParentFolder.Id == parentId)));

            if (exists)
                return false;

            folder.Name = newName;
            db.SaveChanges();
            return true;
        }

        public bool DeleteFolder(FolderTreeItem selectedItem)
        {
            if (selectedItem == null || !selectedItem.IsFolder)
                return false;

            using var db = new DocumentDbContext();

            var folder = db.Folders
                .Include(f => f.SubFolders)
                .Include(f => f.Documents)
                .FirstOrDefault(f => f.Id == selectedItem.FolderId);

            if (folder == null)
                return false;

            if (folder.SubFolders.Any())
            {
                return false;
            }

            db.Documents.RemoveRange(folder.Documents);
            db.Folders.Remove(folder);
            db.SaveChanges();

            return true;
        }

        private FolderTreeItem BuildFolderNode(Folder folder)
        {
            var node = new FolderTreeItem(folder.Id, null, folder.Name);

            foreach (var child in folder.SubFolders)
            {
                node.Children.Add(BuildFolderNode(child));
            }

            foreach (var doc in folder.Documents)
            {
                node.Children.Add(new FolderTreeItem(null, doc.Id, doc.Title));
            }

            return node;
        }
    }
}
