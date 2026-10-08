# Mes décisions d’ingénierie

## Mon écriture locale précède le réseau

Je sauvegarde le document, l’opération à envoyer et son éventuel fichier photo dans une transaction IndexedDB. Je confirme l’enregistrement dans l’interface après la fin de cette transaction. Je peux ensuite échanger avec le serveur sans bloquer la saisie. Je n’annonce pas une confirmation distante tant que le serveur n’a pas répondu.

## Ma reprise possède un identifiant stable

Je garde le même UUID lors d’une reprise. Le serveur conserve le reçu et une empreinte du contenu normalisé. Il refuse la réutilisation du même identifiant avec un autre contenu. Je retire de ma file locale seulement les opérations explicitement confirmées.

## Mon contrôle de version est atomique

Je conditionne l’insertion du reçu à la version attendue. Je groupe reçu, document et état de pointage dans un lot transactionnel D1. Une concurrence entre deux écritures produit une seule modification acceptée. Je conserve l’écriture refusée sur l’appareil plutôt que d’écraser les données du serveur.

## Mon pointage est un journal

Je représente les actions par des événements immuables, avec horodatage d’origine et séquence. Je n’édite pas les événements pour corriger une feuille de temps. Une correction future devra être une action approuvée et traçable. Je distingue les segments de travail, de pause et de déplacement. Je refuse une session démarrée deux fois et un horodatage antérieur au précédent.

## Mes fichiers restent privés

Je vérifie le compte et le chantier avant téléversement, puis le document photo avant lecture. Je limite le corps en lecture progressive, contrôle MIME et signature de format, et utilise une création conditionnelle R2 pour éviter de remplacer un objet concurrent. Je n’expose pas d’URL publique de stockage.

## Mes limites et évolutions

Je rapatrie les dossiers par pages de 200 éléments et je protège les documents ayant une modification locale en attente. La synchronisation actuelle relit l’inventaire complet ; une grande volumétrie demandera un journal incrémental avec curseur serveur et mesures de charge. Je n’affirme pas une capacité illimitée.

Je prépare les ressources compilées et une page hors connexion sans identité personnelle dans le cache de démarrage. Je conserve les dossiers dans IndexedDB, séparés par compte, et je ne cache pas les réponses API dans le service worker.

Je dois encore développer la fusion manuelle, les sauvegardes serveur et leur restauration, la supervision, les règles de partage d’équipe et la recette multi-appareils. Je garde ces limites explicites dans le périmètre livré.
