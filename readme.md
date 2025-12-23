# Archivsoftware

Archivsoftware ist eine WPF-Desktopanwendung zur Verwaltung und Volltextsuche von Dokumenten (PDF/DOCX) auf Basis einer SQL Server LocalDB-Datenbank.  
Dokumente werden als BLOB gespeichert, ihr Plaintext wird extrahiert und für die Suche in der Datenbank abgelegt.

## Features

- Hierarchische Ordnerverwaltung  
  - Ordner anlegen, umbenennen, verschieben und löschen  
  - Eindeutige Ordnernamen pro Ebene

- Dateiimport  
  - Manueller Import von PDF/DOCX über die GUI  
  - Automatischer Import über einen konfigurierbaren Watcher (überwachter Ordner)  
  - Speicherung der Dateien als BLOB und zusätzliche Ablage des Plaintext-Inhalts

- Volltextsuche  
  - Suche über den gespeicherten Plaintext (Stichwörter, einfache Phrasen)  
  - Trefferliste mit Titel, Ordnerpfad und Text-Snippet  
  - Detailansicht des Dokumentinhalts (Plaintext)

- WPF-GUI  
  - Hauptfenster mit Ordnerbaum, Trefferliste und Detailansicht  
  - Nicht-blockierender Dateiimport im Hintergrund

## Technologie-Stack

- .NET / WPF (Desktop-Anwendung)
- Entity Framework Core (Code-First, SQL Server LocalDB)
- C# (Serviceschicht für Ordner, Dokumente, Suche und Watcher)
- FileSystemWatcher für automatischen Dateiimport

## Getting Started

### Voraussetzungen

- Visual Studio 2022 (oder neuer) mit .NET Desktop Development
- SQL Server Express LocalDB

### Projekt bauen und starten

1. Repository klonen:
git clone <URL-zum-Repo>

text
2. Solution in Visual Studio öffnen.
3. Sicherstellen, dass das WPF-Projekt als Startprojekt gesetzt ist.
4. Build: `Build` → `Build Solution`.
5. Starten:
   - Entweder per grünem „Start“-Button in Visual Studio,
   - oder die erzeugte EXE im `bin\<Debug|Release>\`-Ordner ausführen.

Die Datenbank `ArchivSoftwareDB` wird bei Bedarf automatisch via EF-Migrationen erzeugt.

## Bedienung (Kurzüberblick)

- **Ordner anlegen:**  
  Im Ordnerbaum den gewünschten Elternordner auswählen → „Neuer Ordner“ klicken.

- **Ordner umbenennen / löschen / verschieben:**  
  Rechtsklick auf einen Ordner im Baum → Kontextmenü nutzen.

- **Dateien manuell importieren:**  
  „Upload…“ klicken, PDF/DOCX auswählen, Zielordner im Baum wählen → Dateien werden in diesen Ordner importiert.

- **Watcher konfigurieren:**  
  Zielordner im Baum auswählen → „Watcher-Ordner wählen“ → Dateisystemordner wählen.  
  Neue PDF/DOCX in diesem Ordner werden automatisch importiert und im Baum angezeigt.

- **Volltextsuche:**  
  Suchbegriff eingeben → „Suchen“ klicken → Treffer
