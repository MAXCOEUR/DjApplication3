# AGENTS.md

Guide rapide pour les futurs agents qui travaillent sur DjApplication3.

## Projet

DjApplication3 est une application WinUI .NET 8 pour DJ/mixage. Elle gere une bibliotheque locale, Youtube, Youtube Music, plusieurs pistes audio, le crossfade, la sortie casque et une table Hercules DjControl Instinct.

La solution est decoupee en deux projets principaux :

- `DjApplication3.Core` : modeles, services, sources de donnees, audio, MIDI, chemins runtime et outils externes.
- `DjApplication3.WinUI` : vues WinUI, controles XAML, view models et ressources graphiques.

## Commandes

- Compiler sans restaurer : `dotnet build DjApplication3.sln --no-restore`
- Restaurer si necessaire : `dotnet restore DjApplication3.sln`
- Lancer l'application : `dotnet run --project DjApplication3.WinUI`

## Regles De Modification

- Preserver les bindings XAML : ne pas renommer une propriete ou un handler public/prive utilise par XAML sans mettre a jour le XAML correspondant.
- Preferer les petits refactors lisibles aux grandes refontes.
- Garder les longues operations de bibliotheque en `async` avec `CancellationToken` quand le flux existant le permet.
- Eviter d'ajouter de nouveaux singletons; reutiliser les services et helpers existants.
- Ne pas changer les chemins runtime dans `AppPaths` sans verifier le setup, les cookies Youtube Music, le cache et l'installateur.
- Ne pas modifier le format des cookies Youtube Music ou des fichiers temporaires sauf demande explicite.
- Le materiel Hercules peut ne pas etre branche pendant le dev : les erreurs MIDI doivent rester non bloquantes.

## Carte Du Code

- `DjApplication3.WinUI/ViewModels/MainViewModel*.cs` : etat global de l'ecran principal, bibliotheque, navigation, decks, MIDI et actions runtime.
- `DjApplication3.WinUI/ViewModels/DeckViewModel*.cs` : etat d'une piste, chargement musique, lecture, position, waveform/BPM et auto-next.
- `DjApplication3.WinUI/Views/MainView*.cs` : code-behind WinUI separe par evenements bibliotheque, options, Youtube Music et helpers UI.
- `DjApplication3.Core/Model/MusicIdentity.cs` : comparaison/remplacement de musiques; l'utiliser au lieu de dupliquer `title`/`author`.
- `DjApplication3.Core/Services` : abstractions audio, bibliotheque, reglages et MIDI.
- `DjApplication3.Core/DataSource` : acces local, Youtube, Youtube Music, cache, BPM et waveform.

## Verification Manuelle

Apres un changement UI/audio, verifier au minimum :

- scan d'un dossier local;
- recherche et selection d'un titre;
- chargement piste gauche et piste droite;
- play, pause, stop, seek/scratch;
- crossfade et volume casque;
- changement du nombre de pistes 2/3/4;
- ouverture/fermeture des options;
- selection des peripheriques audio/MIDI si disponibles;
- connexion ou deconnexion Youtube Music seulement si WebView2 et les cookies sont disponibles.

## Risques Connus

- `CSCore 1.2.1.2` produit un avertissement `NU1701` de compatibilite avec .NET 8.
- Le code historique contient des avertissements nullable; ne pas transformer leur correction en refonte non demandee.
- Youtube/Youtube Music dependent du reseau, de `yt-dlp`, de FFmpeg, de WebView2 et parfois des cookies.
- La table Hercules n'est pas toujours presente sur les machines de dev; conserver les chemins d'erreur existants.
