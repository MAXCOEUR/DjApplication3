# AGENTS.md

Guide rapide pour les futurs agents qui travaillent sur DjApplication3.

## Projet

DjApplication3 est une application WinUI .NET 8 x64 pour DJ/mixage. Elle gere une bibliotheque locale, Youtube, Youtube Music, les previews, le BPM, les waveforms, plusieurs pistes audio, le crossfade, la sortie casque, les peripheriques audio et une table Hercules DjControl Instinct.

La solution est decoupee en deux projets principaux :

- `DjApplication3.Core` : modeles, services, sources de donnees, audio, MIDI, chemins runtime, repository et outils externes.
- `DjApplication3.WinUI` : application WinUI, vues XAML, controles, view models et ressources graphiques.

L'application WinUI cible actuellement `net8.0-windows10.0.19041.0`, min Windows `10.0.17763.0`, `PlatformTarget=x64`, `WindowsPackageType=None`, avec version `2.0.7.0`.

## Commandes

- Restaurer : `dotnet restore DjApplication3.sln`
- Compiler sans restaurer : `dotnet build DjApplication3.sln --no-restore`
- Lancer l'application : `dotnet run --project DjApplication3.WinUI`
- Publier et generer l'installateur Inno Setup : `SetupInno\build-installer.ps1`

Notes :

- Le script d'installateur ferme l'application si elle tourne, publie `DjApplication3.WinUI` en `Release/win-x64`, verifie les fichiers WinUI publies, puis appelle Inno Setup 6.
- Ne pas lancer de restauration ou de build long sans raison pour un changement documentaire.

## Regles De Modification

- Preserver les bindings XAML : ne pas renommer une propriete, commande, evenement ou handler utilise par XAML sans mettre a jour le XAML correspondant.
- Preferer les petits refactors lisibles aux grandes refontes.
- Reutiliser les services existants avant d'ajouter une nouvelle abstraction ou un nouveau singleton.
- Garder les longues operations de bibliotheque, Youtube, Youtube Music, BPM, waveform et telechargement en `async` quand le flux existant le permet.
- Conserver les erreurs MIDI, audio et materiel non bloquantes : la table Hercules et certains peripheriques peuvent etre absents pendant le dev.
- Ne pas changer les chemins runtime dans `AppPaths` sans verifier le setup, les cookies Youtube Music, le cache, les previews, les fichiers temporaires et l'installateur.
- Ne pas modifier le format des cookies Youtube Music, des fichiers de settings, de l'historique de lectures ou des fichiers temporaires sauf demande explicite.
- Respecter la separation actuelle : logique metier et acces donnees dans `Core`, etat UI dans les view models, interactions XAML/code-behind dans `WinUI`.
- Pour les changements UI/audio, verifier les scenarios manuels listes plus bas avant de conclure.

## Carte Du Code

- `DjApplication3.Core/Infrastructure/AppPaths.cs` : chemins runtime (`musique`, `tmp`, `preview`, `outilsExtern`, cookies, settings, historique).
- `DjApplication3.Core/Model` : modeles metier (`Musique`, `PlayListe`, `FileSystemNode`, `MusicIdentity`, `SettingsManager`).
- `DjApplication3.Core/Model/MusicIdentity.cs` : comparaison/remplacement de musiques; l'utiliser au lieu de dupliquer `title`/`author`.
- `DjApplication3.Core/Repository/MusiqueRepository.cs` : facade historique vers local, Youtube, Youtube Music, cache, BPM, waveform et previews.
- `DjApplication3.Core/DataSource` : acces local, Youtube, Youtube Music, cache, previews, BPM et waveform.
- `DjApplication3.Core/Services` : abstractions audio, bibliotheque, preview, reglages et MIDI.
- `DjApplication3.Core/Outils` : integration FFmpeg et Hercules.
- `DjApplication3.Core/outilsExtern` : binaires runtime copies en sortie (`ffmpeg`, `ffprobe`, `yt-dlp.exe`, `qjs.exe`, etc.).
- `DjApplication3.WinUI/App.xaml.cs` : theme sombre et gestion globale des exceptions WinUI.
- `DjApplication3.WinUI/MainWindow.xaml.cs` : initialisation des dossiers runtime, titre/version, maximisation et nettoyage des fichiers temporaires.
- `DjApplication3.WinUI/ViewModels/MainViewModel*.cs` : etat global de l'ecran principal, bibliotheque, navigation, decks, MIDI, previews et actions runtime.
- `DjApplication3.WinUI/ViewModels/DeckViewModel*.cs` : etat d'une piste, chargement musique, lecture, position, waveform/BPM et auto-next.
- `DjApplication3.WinUI/Views/MainView*.cs` : code-behind WinUI separe par actions, layout, evenements bibliotheque, reglages et Youtube Music.
- `DjApplication3.WinUI/Controls` : controles XAML reutilisables (`DeckControl`, `TrackBarPerso`, `WaveformControl`).
- `SetupInno` : script de publication et fichier Inno Setup pour produire `DjApplication3Setup.exe`.

## Dependances Et Runtime

- Audio : `CSCore 1.2.1.2` et `NAudio 2.3.0`.
- Metadonnees et fichiers : `TagLibSharp`, `Newtonsoft.Json`.
- Youtube : `YoutubeExplode`, `YouTubeMusicAPI`, `yt-dlp.exe`, FFmpeg et parfois cookies.
- UI : `Microsoft.WindowsAppSDK`, `Microsoft.Web.WebView2`, `WinUI.TableView`.
- Les binaires externes sont copies depuis `DjApplication3.Core/outilsExtern` vers la sortie WinUI.
- WebView2Loader est lie dans le projet WinUI; verifier le publish si WebView2 ou l'installateur est touche.

## Verification Manuelle

Apres un changement UI/audio, verifier au minimum :

- scan d'un dossier local;
- recherche et selection d'un titre;
- chargement piste gauche et piste droite;
- play, pause, stop, seek/scratch;
- crossfade, volumes de piste, EQ et volume casque;
- preview d'un titre si le flux concerne les previews;
- changement du nombre de pistes 2/3/4;
- ouverture/fermeture des options;
- selection des peripheriques audio/MIDI si disponibles;
- connexion ou deconnexion Youtube Music seulement si WebView2 et les cookies sont disponibles;
- fermeture de l'application et nettoyage des fichiers temporaires.

Pour un changement installateur, verifier :

- `dotnet publish` genere les `.xbf`, `.pri`, exe/dll et `WebView2Loader.dll`;
- `SetupInno\build-installer.ps1` produit `SetupInno\Output\DjApplication3Setup.exe`;
- les ressources, outils externes et fichiers WinUI sont bien presents dans le publish.

## Risques Connus

- `CSCore 1.2.1.2` peut produire un avertissement `NU1701` de compatibilite avec .NET 8.
- Le code a `Nullable` active, mais contient encore des avertissements historiques; ne pas transformer leur correction en refonte non demandee.
- Youtube/Youtube Music dependent du reseau, de `yt-dlp`, de FFmpeg, de `qjs.exe`, de WebView2 et parfois des cookies.
- Les cookies Youtube Music et `ytdlp_cookies.txt` sont sensibles au format et a l'emplacement.
- La table Hercules n'est pas toujours presente sur les machines de dev; conserver les chemins d'erreur existants.
- Les peripheriques audio peuvent changer d'ordre; privilegier les IDs quand le code le permet et conserver les fallbacks par index.
- Les fichiers temporaires et previews vivent sous `musique\tmp`; eviter de supprimer plus large que necessaire.
