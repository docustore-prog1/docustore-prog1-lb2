namespace DocumentManager.Data.Models
{
    public class Folder
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        // Navigation properties
        public Folder? ParentFolder { get; set; }
        public ICollection<Folder> SubFolders { get; set; } = new List<Folder>();
        public ICollection<Document> Documents { get; set; } = new List<Document>();
    }
}
