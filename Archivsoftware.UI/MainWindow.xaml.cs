using DocumentManager.Data;
using DocumentManager.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Windows;

namespace Archivsoftware
{
    public partial class MainWindow : Window
    {
        private readonly DocumentDbContext _db;
        private FolderTreeItem? _selectedItem;

        public MainWindow()
        {
            InitializeComponent();
            _db = new DocumentDbContext();
            LoadFolderTree();
        }

        private void OpenFilePicker(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog
            {
                Title = "Select Files and Folders",
                IsFolderPicker = true,
                Multiselect = true,
                InitialDirectory = Environment.GetFolderPath(
                    Environment.SpecialFolder.MyDocuments
                )
            };
            dialog.ShowDialog();

        }

        private void LoadFolderTree()
        {
            var folders = _db.Folders
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
                var rootNode = BuildFolderNode(root);
                rootItems.Add(rootNode);
            }

            FolderTree.ItemsSource = rootItems;
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

        private void FolderTree_OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            _selectedItem = e.NewValue as FolderTreeItem;
        }

        private void NewFolderButton_OnClick(object sender, RoutedEventArgs e)
        {
            var name = Microsoft.VisualBasic.Interaction.InputBox(
                "Name des neuen Ordners:",
                "Neuer Ordner",
                "Neuer Ordner");

            if (string.IsNullOrWhiteSpace(name))
                return;

            Folder? parentFolder = null;

            if (_selectedItem != null && _selectedItem.IsFolder)
            {
                parentFolder = _db.Folders
                    .FirstOrDefault(f => f.Id == _selectedItem.FolderId);
            }

            var newFolder = new Folder
            {
                Name = name,
                ParentFolder = parentFolder
            };

            _db.Folders.Add(newFolder);
            _db.SaveChanges();

            LoadFolderTree();
        }
    }
}
