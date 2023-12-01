# DjApplication3

DjApplication3 est une application qui peut fonctionner avec une table de mixage Hercules DjControl Instinct. Pour utiliser l'interaction, vous devez installer Wireshark et USBPcap.

## Installation de Wireshark

1. Téléchargez Wireshark depuis le site officiel : [Téléchargement de Wireshark](https://www.wireshark.org/download.html).

2. Pendant l'installation, assurez-vous de cocher l'option d'installation pour USBPcap.

## Installation des pilotes Hercules

1. Téléchargez les pilotes de la table de mixage : [Téléchargement des pilotes](https://support.hercules.com/en/product/djcontrolinstinct-en/).

2. Pour pouvoir l'utiliser, assurez-vous que le PC est sous tension.

## Configuration de DjApplication3

1. Assurez-vous que le PC est sous tension et que Wireshark et les pilotes sont installés.

2. Ouvrez les options de DjApplication3.

3. Entrez le chemin d'accès complet vers l'exécutable TShark (ligne de commande de Wireshark) dans le champ `chemin jusqu'à TShark`.

   Exemple : `C:\Program Files\Wireshark\tshark.exe`

4. Trouvez avec l'aide de Wireshark le numéro Source de la table de mixage et entrez-le dans les options dans le champ `numéro USB`.

   Exemple : `1.8`

5. Cliquez sur le bouton `lancer Hercules`. Cela lancera l'écoute du périphérique.
