namespace Archivsoftware
{
    public class FolderTreeItem
    {
        public int? FolderId { get; }
        public int? DocumentId { get; }
        public string Name { get; }

        public bool IsFolder => FolderId.HasValue;
        public bool IsDocument => DocumentId.HasValue;

        public List<FolderTreeItem> Children { get; } = new();

        public FolderTreeItem(int? folderId, int? documentId, string name)
        {
            FolderId = folderId;
            DocumentId = documentId;
            Name = name;
        }

        public override string ToString() => Name;
    }
}