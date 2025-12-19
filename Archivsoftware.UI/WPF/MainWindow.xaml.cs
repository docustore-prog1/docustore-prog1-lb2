using DocumentManager.Data;
using DocumentManager.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using System.IO;
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

        private async void OpenFilePicker(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select files to import",
                Filter = "PDF and Word Files (*.pdf;*.docx)|*.pdf;*.docx",
                Multiselect = true,
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            };

            var result = dialog.ShowDialog();
            if (result == true)
            {
                FilePathTextBox.Text = string.Join(Environment.NewLine, dialog.FileNames);
                FilePathTextBox.IsEnabled = false;
                UploadButton.IsEnabled = false;
                UploadButton.Content = "Uploading...";

                int? parentFolderId = null;
                if (_selectedItem != null && _selectedItem.IsFolder)
                {
                    parentFolderId = _selectedItem.FolderId;
                }

                await Task.Run(() =>
                {
                    using (var backgroundDb = new DocumentDbContext())
                    {
                        Folder? dbParent = null;
                        if (parentFolderId.HasValue)
                        {
                            dbParent = backgroundDb.Folders.Find(parentFolderId.Value);
                        }

                        foreach (var filePath in dialog.FileNames)
                        {
                            ImportFile(backgroundDb, filePath, dbParent);
                        }

                        backgroundDb.SaveChanges();
                    }
                });

                UploadButton.Content = "Upload...";
                UploadButton.IsEnabled = true;
                FilePathTextBox.IsEnabled = true;
                FilePathTextBox.Text = "";

                LoadFolderTree();
            }
        }

        private void ImportFile(DocumentDbContext context, string filePath, Folder? parentFolder)
        {
            if (parentFolder == null)
            {
                // Optional: Default-Ordner anlegen oder Fehlermeldung anzeigen.
                return;
            }

            var fileInfo = new FileInfo(filePath);

            var newDoc = new Document
            {
                Title = fileInfo.Name,
                Folder = parentFolder,
                FileData = File.ReadAllBytes(filePath)
            };

            context.Documents.Add(newDoc);
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
                "Neuer Ordner"
            );

            if (string.IsNullOrWhiteSpace(name))
                return;

            Folder? parentFolder = null;

            if (_selectedItem != null && _selectedItem.IsFolder)
            {
                parentFolder = _db.Folders
                    .FirstOrDefault(f => f.Id == _selectedItem.FolderId);
            }

            // Eindeutigkeit pro Ebene prüfen
            var parentId = parentFolder?.Id;
            var exists = _db.Folders.Any(f =>
                f.Name == name &&
                ((f.ParentFolder == null && parentId == null) ||
                 (f.ParentFolder != null && f.ParentFolder.Id == parentId)));

            if (exists)
            {
                MessageBox.Show("In diesem Ordner-Level existiert bereits ein Ordner mit diesem Namen.",
                    "Ordner anlegen nicht möglich",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
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

        private void RenameMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            if (_selectedItem == null || !_selectedItem.IsFolder)
                return;

            var folder = _db.Folders
                .Include(f => f.ParentFolder)
                .FirstOrDefault(f => f.Id == _selectedItem.FolderId);

            if (folder == null)
                return;

            var currentName = folder.Name;

            var newName = Microsoft.VisualBasic.Interaction.InputBox(
                "Neuer Name des Ordners:",
                "Ordner umbenennen",
                currentName
            );

            if (string.IsNullOrWhiteSpace(newName) || newName == currentName)
                return;

            // Validierung: eindeutiger Name pro Ebene
            var parentId = folder.ParentFolder?.Id;
            var exists = _db.Folders.Any(f =>
                f.Id != folder.Id &&
                f.Name == newName &&
                ((f.ParentFolder == null && parentId == null) ||
                 (f.ParentFolder != null && f.ParentFolder.Id == parentId)));

            if (exists)
            {
                MessageBox.Show("In diesem Ordner-Level existiert bereits ein Ordner mit diesem Namen.",
                    "Umbenennen nicht möglich",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            folder.Name = newName;
            _db.SaveChanges();
            LoadFolderTree();
        }

        private void DeleteMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            if (_selectedItem == null || !_selectedItem.IsFolder)
                return;

            var confirm = MessageBox.Show(
                "Möchten Sie diesen Ordner wirklich löschen?",
                "Ordner löschen",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes)
                return;

            var folder = _db.Folders
                .Include(f => f.SubFolders)
                .Include(f => f.Documents)
                .FirstOrDefault(f => f.Id == _selectedItem.FolderId);

            if (folder == null)
                return;

            // DeleteBehavior.Restrict bei ParentFolder verlangt, dass keine SubFolders vorhanden sind[web:53].
            if (folder.SubFolders.Any())
            {
                MessageBox.Show("Ordner enthält Unterordner. Bitte zuerst Unterordner löschen.",
                    "Löschen nicht möglich",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            if (folder.Documents.Any())
            {
                var confirmDocs = MessageBox.Show(
                    "Der Ordner enthält Dokumente. Diese werden beim Löschen des Ordners mitgelöscht. Fortfahren?",
                    "Dokumente löschen",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (confirmDocs != MessageBoxResult.Yes)
                    return;
            }

            _db.Documents.RemoveRange(folder.Documents);
            _db.Folders.Remove(folder);
            _db.SaveChanges();

            LoadFolderTree();
        }
    }
}