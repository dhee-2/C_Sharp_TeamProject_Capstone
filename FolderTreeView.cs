using System.Collections.ObjectModel;
using System.Drawing;

namespace Hcode
{
    public class FolderItem
    {
        public string Name
        {
            get; set;
        }
        public string Icon
        {
            get; set;
        }
        public ObservableCollection<object> SubItems
        {
            get; set;
        }

        public FolderItem(string name)
        {
            Name = name;
            Icon = "Resources/folder.png";
            SubItems = new ObservableCollection<object>();
        }
    }

    public class FileItem
    {
        public string Name
        {
            get; set;
        }

        public string Icon
        {
            get; set;
        }

        public FileItem(string name)
        {
            Name = name;
            Icon = "Resources/file.png";
        }
    }
}