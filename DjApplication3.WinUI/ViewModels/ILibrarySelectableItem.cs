using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DjApplication3.WinUI.ViewModels
{
    public interface ILibrarySelectableItem
    {
        bool IsSelected { get; set; }
        bool IsOpened { get; set; }
    }
}
