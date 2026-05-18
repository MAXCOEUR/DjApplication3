using DjApplication3.WinUI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DjApplication3.WinUI.Controls
{
    public sealed partial class DeckControl : UserControl
    {
        public DeckControl()
        {
            InitializeComponent();
        }

        private void Waveform_SeekRequested(object? sender, double e)
        {
            if (DataContext is DeckViewModel vm)
            {
                vm.Seek(e);
            }
        }
    }
}
