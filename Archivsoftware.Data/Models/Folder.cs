using System.ComponentModel.DataAnnotations;
using System.Reflection.Metadata;

namespace DocumentManager.Data.Models
{
    public class Folder
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public int? ParentFolderId { get; set; }

        // Navigation properties
        public Folder? ParentFolder { get; set; }
        public ICollection<Folder> SubFolders { get; set; } = new List<Folder>();
        public ICollection<Document> Documents { get; set; } = new List<Document>();
    }
}
