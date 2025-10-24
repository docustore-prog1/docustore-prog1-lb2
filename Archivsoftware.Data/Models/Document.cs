﻿using System.ComponentModel.DataAnnotations;

namespace DocumentManager.Data.Models
{
    public class Document
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public byte[] FileData { get; set; } = [];

        public string PlainText { get; set; } = string.Empty;

        public int FolderId { get; set; }

        public Folder Folder { get; set; } = null!;
    }
}
