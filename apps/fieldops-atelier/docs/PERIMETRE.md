# Mon périmètre fonctionnel

Je me sers des fonctions publiées d’[Encircle](https://www.getencircle.com/) et de [Mobile-Punch](https://mobilepunch.com/) pour établir ma liste de comparaison. Je produis ma propre application et ma propre interface. Je n’ai accès ni à leur code privé, ni à leurs journaux, ni à leurs services internes.

| Fonction que je vise | Mon état actuel | Ce qu’il me reste à compléter |
|---|---|---|
| Dossiers, clients, adresses, états | Implémenté | Équipe et règles de rôles |
| Photos originales et téléchargement privé | Implémenté, 10 Mo/fichier | HEIC, vidéo, 360°, annotations |
| Pièces et dimensions | Implémenté | Plan relié entre pièces, génération automatique |
| Notes et tâches | Implémenté | Assignation d’équipe et notifications |
| Humidité et température | Saisie manuelle implémentée | Courbes, références, intégration instruments |
| Inventaire d’objets et équipements | Implémenté | Étiquettes, lots et historique de mouvements |
| Formulaires | Champs simples implémentés | Modèles riches, logique et signatures |
| Rapport et devis | HTML imprimable implémenté | PDF serveur, modèles, pièces jointes complètes |
| Début/fin, pauses et déplacement | Implémenté, événements immuables | Corrections approuvées et audit de paie |
| Position au pointage | Optionnelle, ponctuelle | Règles d’affectation géographique |
| Planning | Interventions datées implémentées | Équipes, disponibilités, récurrence |
| Travail hors connexion | Stockage et reprise implémentés | Recette réelle sur iOS/Android et fusion des conflits |
| Dossier → brouillon de devis | Surfaces/heures, prix manuels, majoration ajustable | Bibliothèque de tarifs, taxes et validation commerciale |
| Paie et calculs CCQ | À construire | Connecteurs et validation métier |
| Messagerie d’équipe | À construire | Accès partagé, notifications, médias |
| Analyse IA et estimation automatique | À construire | Évaluation, traçabilité et validation humaine |
| Imports depuis les produits existants | À construire | Formats d’export fournis et mappings |
| Performance et disponibilité | Architecture locale préalable, tests ciblés | Mesures mobiles, charge, supervision et sauvegardes/restauration |

## Ma fonctionnalité intégrée

Je relie mon dossier à mon devis : les surfaces saisies et les heures enregistrées préremplissent un brouillon. Je complète les tarifs, puis je valide mes quantités et ma majoration des matériaux. Je ne présente pas ce principe comme inédit sur le marché ; il s’agit de mon adaptation au flux de travail de cette application.

## Ma suite de développement

Je priorise une recette terrain sur deux téléphones, une gestion explicite des conflits multi-appareils, puis les sauvegardes avec restauration vérifiée et la supervision. Je complète ensuite les accès d’équipe et les corrections de temps avant d’ajouter des connecteurs de paie. Je traite les fonctions automatiques avancées après validation des relevés et des calculs.

Je ne peux pas déterminer la cause de la lenteur du service Mobile-Punch sans des mesures et des journaux pertinents. Je peux mesurer et améliorer mon application, et comparer les parcours avec des essais contrôlés.
