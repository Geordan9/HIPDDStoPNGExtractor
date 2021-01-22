using Microsoft.WindowsAPICodePack.Dialogs;

namespace HIPDDStoPNGExtractor.Utils
{
    public static class Dialogs
    {
        public static string OpenFolderDialog(string Title, string FolderName = null)
        {
            var dlg = new CommonOpenFileDialog();
            dlg.Title = Title;
            dlg.IsFolderPicker = true;
            dlg.DefaultFileName = FolderName;

            dlg.AddToMostRecentlyUsedList = false;
            dlg.AllowNonFileSystemItems = false;
            dlg.EnsureReadOnly = false;
            dlg.EnsureValidNames = true;
            dlg.EnsurePathExists = false;
            dlg.EnsureFileExists = false;
            dlg.Multiselect = false;
            dlg.ShowPlacesList = true;

            if (dlg.ShowDialog() == CommonFileDialogResult.Ok) return dlg.FileName;

            return null;
        }
    }
}