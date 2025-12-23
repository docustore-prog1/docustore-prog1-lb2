namespace Archivsoftware
{
    public class SearchResultItem
    {
        public int DocumentId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string FolderPath { get; set; } = string.Empty;
        public string Snippet { get; set; } = string.Empty;
    }
}