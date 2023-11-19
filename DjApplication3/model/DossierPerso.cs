using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DjApplication3.model
{
    internal class DossierPerso
    {
        public DossierPerso? parent { get; set; }
        public string Text { get; set; }
        public List<DossierPerso> Children { get; set; }

        public DossierPerso(DossierPerso parent, string text)
        {
            this.parent = parent;
            Text = text;
            Children = new List<DossierPerso>();
        }
        public DossierPerso(string text)
        {
            this.parent = null;
            Text = text;
            Children = new List<DossierPerso>();
        }
        public string getPath()
        {
            if (parent != null)
            {
                return Path.Combine(parent.getPath(), Text);
            }
            else
            {
                return Text;
            }
            
        }
    }
}
