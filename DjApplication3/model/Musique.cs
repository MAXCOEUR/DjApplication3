using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DjApplication3.model
{
    public class Musique
    {
        public string url;
        public string title;
        public string author;
        public Musique(string url, string title, string author)
        {
            this.url = url;
            this.title = title;
            this.author = author;
        }

        // Surcharge de l'opérateur ==
        public static bool operator ==(Musique musique1, Musique musique2)
        {
            // Si les deux objets sont null, ils sont égaux
            if (ReferenceEquals(musique1, musique2))
                return true;

            // Si l'un des objets est null, ils ne sont pas égaux
            if (musique1 is null || musique2 is null)
                return false;

            // Comparaison des propriétés title et author
            return musique1.title == musique2.title &&
                   musique1.author == musique2.author;
        }

        // Surcharge de l'opérateur !=
        public static bool operator !=(Musique musique1, Musique musique2)
        {
            return !(musique1 == musique2);
        }

        // Méthode GetHashCode pour respecter les conventions
        public override int GetHashCode()
        {
            return (url, title, author).GetHashCode();
        }

        // Méthode Equals pour respecter les conventions
        public override bool Equals(object obj)
        {
            if (obj is Musique otherMusique)
            {
                return this == otherMusique;
            }
            return false;
        }
    }

}
