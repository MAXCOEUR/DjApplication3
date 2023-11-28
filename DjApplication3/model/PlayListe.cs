using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DjApplication3.model
{
    public class PlayListe
    {
        public string id { get; set; }
        public string name { get; set; }
        public PlayListe(string id,string name) {
            this.id = id;
            this.name = name;
        }
    }
}
