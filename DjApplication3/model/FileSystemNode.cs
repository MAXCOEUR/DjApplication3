using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DjApplication3.model
{
    public class FileSystemNode : INotifyPropertyChanged
    {
        private string _name;
        private string _fullPath;
        private ObservableCollection<FileSystemNode> _children;

        public string Name
        {
            get { return _name; }
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged(nameof(Name));
                }
            }
        }

        public string FullPath
        {
            get { return _fullPath; }
            set
            {
                if (_fullPath != value)
                {
                    _fullPath = value;
                    OnPropertyChanged(nameof(FullPath));
                }
            }
        }

        public ObservableCollection<FileSystemNode> Children
        {
            get { return _children; }
            set
            {
                if (_children != value)
                {
                    _children = value;
                    OnPropertyChanged(nameof(Children));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        // Surcharge de l'opérateur ==
        public static bool operator ==(FileSystemNode left, FileSystemNode right)
        {
            // Si les deux objets sont égaux en référence
            if (ReferenceEquals(left, right))
            {
                return true;
            }

            // Si l'un des deux objets est nul
            if (left is null || right is null)
            {
                return false;
            }

            // Comparaison basée sur le FullPath (ou Name, selon votre préférence)
            return left.FullPath == right.FullPath;
        }

        // Surcharge de l'opérateur !=
        public static bool operator !=(FileSystemNode left, FileSystemNode right)
        {
            return !(left == right);
        }

        // Surcharge de Equals()
        public override bool Equals(object obj)
        {
            if (obj is FileSystemNode otherNode)
            {
                return this == otherNode;
            }
            return false;
        }

        // Surcharge de GetHashCode()
        public override int GetHashCode()
        {
            return FullPath?.GetHashCode() ?? 0;
        }

    }
}
