# Ma recette terrain avant usage régulier

Je commence sur un chantier de test, sans données contractuelles. Je vérifie chaque étape sur mon téléphone et mon ordinateur.

1. Je me connecte, crée mon dossier et attends la confirmation de synchronisation ainsi que le cache « préparé » dans **Mes données**.
2. Je charge une photo et j’ouvre le dossier sur un autre appareil connecté. Je vérifie l’image originale, les notes et les dimensions.
3. Je coupe la connexion, recharge l’application, ajoute une note et une photo, puis enregistre début, pause, reprise et fin. Je vérifie que la file affiche mes opérations en attente.
4. Je rétablis le réseau et j’appuie sur la synchronisation. Je contrôle la disparition des opérations confirmées, leur présence sur l’autre appareil et la conservation des horodatages.
5. Je répète la synchronisation et je vérifie qu’aucun élément n’est dupliqué.
6. Je vérifie le CSV avec les durées attendues, le devis avec mes tarifs et le rapport avec les photos réellement incluses.
7. Je provoque un conflit entre deux appareils sur un dossier de test. Je vérifie que mon écriture locale reste conservée et que le message de conflit apparaît. Je n’utilise pas cette version pour du pointage simultané sur plusieurs appareils tant que la fusion n’est pas développée.
8. Je vérifie les refus de permission GPS, la conversion HEIC, la limite des fichiers et le refus de stockage persistant.
9. Je mesure mes parcours en réseau mobile faible et je note l’appareil, le navigateur, la taille des photos, la durée locale et la durée serveur. Je distingue une écriture locale rapide d’un téléversement terminé.

Je n’ai pas exécuté cette recette manuelle dans l’environnement de construction actuel. Les tests automatisés et la compilation ne remplacent pas cette validation terrain.
