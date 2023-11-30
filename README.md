# DjApplication3

DjApplication3 est une application conçue pour fonctionner avec une table de mixage Hercules DjControl Instinct. Pour utiliser pleinement les fonctionnalités de l'application, vous devez installer Wireshark et USBPcap.

## Installation de Wireshark

1. Téléchargez Wireshark depuis le site officiel : [Téléchargement de Wireshark](https://www.wireshark.org/download.html).

2. Pendant l'installation, assurez-vous de cocher l'option d'installation pour USBPcap.

## Configuration de DjApplication3

1. Ouvrez les options de DjApplication3.

2. Recherchez la section des options liées à Wireshark.

3. Entrez le chemin d'accès complet vers l'exécutable TShark (ligne de commande de Wireshark) dans le champ prévu.

   Exemple : `C:\Program Files\Wireshark\tshark.exe`

4. Trouver avec l'aide de wireShark le numero Source de la table et rentrer la dans les option 

   Exemple : `1.8`

5. Le pc doit etre sous tantion et avoir les pilote de la table de mixage : [Téléchargement des pilotes ](https://support.hercules.com/en/product/djcontrolinstinct-en/).
