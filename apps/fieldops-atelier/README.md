# FieldOps Atelier — mon espace chantier

Je construis une application originale qui réunit mes dossiers de chantier, mes preuves photo et mes événements de pointage. Je peux travailler depuis un navigateur sur téléphone ou ordinateur, enregistrer localement et reprendre la synchronisation lorsque le serveur répond.

Je livre une première version fonctionnelle privée. Je ne revendique ni une copie du code d’Encircle ou de Mobile-Punch, ni une équivalence complète à ces services. Je conserve mon implémentation précédente de FieldOps 360 dans le dépôt ; cette application constitue un module distinct.

## Mon parcours d’utilisation

1. Je me connecte avec ChatGPT et je crée mon chantier dans **Mes chantiers**.
2. Dans **Mes dossiers**, j’ajoute photos, pièces mesurées, notes, relevés d’humidité, objets, équipements, tâches et formulaires simples.
3. Dans **Mon pointage**, je démarre, prends une pause, reprends, enregistre un déplacement et termine ma session. Je peux joindre ma position à l’action ; je n’effectue aucun suivi continu.
4. Dans **Mon planning**, je définis mes interventions.
5. Dans **Mes devis**, je génère un brouillon depuis mes surfaces et mes heures. Je saisis mes tarifs et je valide une majoration des matériaux, initialement proposée à 20 %. Je n’ajoute pas automatiquement les taxes et je n’envoie rien au client.
6. J’exporte mes heures en CSV, mes données structurées en JSON et mes dossiers/devis en HTML imprimable en PDF. Mon export JSON ne contient pas les fichiers photo. Mes rapports intègrent les photos locales disponibles, dans la limite de 20 fichiers et de 20 Mo ; je vérifie leur présence avant partage.

## Mon architecture

| Couche | Mon choix | Garantie recherchée |
|---|---|---|
| Interface | React, TypeScript, Vinext | Interface responsive, formulaire validé |
| Stockage de l’appareil | IndexedDB séparé par compte | Dossier, opération et fichier écrits dans une transaction |
| Synchronisation | File persistante, UUID, versions | Reprise dédupliquée ; confirmation avant retrait de la file |
| Serveur | Worker, D1/SQLite | Écritures atomiques et contrôle de version |
| Photos | R2 privé | Accès filtré par compte, formats et taille contrôlés |
| Identité | Passerelle Sites / ChatGPT | Compte authentifié à chaque appel API |
| Hors connexion | Service worker, manifeste des ressources compilées | Démarrage local après préparation en ligne |

Je conserve l’horodatage capturé lors du pointage. L’ordre des événements est donné par une séquence monotone ; une reprise réseau ne change pas l’heure d’origine. Mes états de session sont vérifiés côté appareil et côté serveur. Deux démarrages concurrents ne peuvent pas ouvrir deux sessions pour le même compte.

Je bloque une écriture en cas de conflit de version et je conserve l’opération locale. Cette version ne propose pas encore d’écran de fusion manuelle. Je privilégie un seul appareil pour mon pointage ; une intervention est nécessaire si deux appareils produisent des événements incompatibles. Je peux exporter mes données en attente avant cette intervention. Je n’efface pas le stockage du navigateur lorsqu’une opération attend une confirmation.

## Mon installation

Je dispose de Node.js 22.13 ou plus récent et du gestionnaire indiqué dans `package.json`.

```bash
corepack enable
pnpm install --frozen-lockfile
pnpm typecheck
pnpm test
pnpm build
pnpm test:runtime
node scripts/prepare-offline.mjs
```

Je génère mes migrations avec `pnpm db:generate`. Je conserve les SQL dans `drizzle/` et je les applique à D1 avant de recevoir des requêtes. Sur Sites, mon manifeste `.openai/hosting.json` demande les liaisons `DB` et `BUCKET` ; le déploiement provisionne les ressources et applique les migrations. Les scripts de construction et le Worker proviennent du socle Sites/Vinext.

Pour un développement local dans un environnement standard, je lance `pnpm dev`. L’intégration Sites fournit une identité de développement uniquement dans ce contexte local. Je ne déploie pas une passerelle qui accepte des en-têtes d’identité fournis directement par un visiteur. Pour héberger ailleurs, je remplace l’authentification par un fournisseur réel et je configure D1, R2 et les protections d’origine.

## Ma vérification

Je vérifie les calculs de durée et de prix, les limites des requêtes, la déduplication, les conflits, les écritures concurrentes et l’isolation des comptes. Mes tests D1 utilisent SQLite via Miniflare ; mes tests appareil utilisent une implémentation IndexedDB de test. Je vérifie aussi les types et la compilation de production. Mes routes compilées sont exécutées dans Miniflare pour vérifier l’identité, l’origine, la validation des données et le parcours téléversement → dossier photo → lecture privée.

Je n’ai pas effectué de validation visuelle en navigateur dans cet environnement, ni de benchmark sur un réseau mobile réel. Mes tests ne démontrent donc pas une supériorité mesurée sur Mobile-Punch. Mon objectif de réactivité est obtenu par l’enregistrement local préalable ; la durée affichée dans l’interface mesure l’écriture sur mon appareil ou l’échange avec mon propre serveur.

## Mes limites actuelles

Je prépare le cache de l’application lors d’une première connexion. Un navigateur peut refuser ou supprimer son stockage ; je garde des sauvegardes et je peux demander un stockage persistant dans **Mes données**. Mes données locales ne sont pas chiffrées par l’application. Sur un appareil partagé, je supprime les données du site dans les réglages du navigateur après avoir confirmé la synchronisation.

Je conserve les photos originales en JPEG, PNG ou WebP, jusqu’à 10 Mo chacune. Je convertis mes fichiers HEIC avant import. Cette version ne traite pas encore la vidéo, les prises 360°, la photogrammétrie ou les plans automatiques.

Je retrouve le périmètre complet et mes prochaines étapes dans [docs/PERIMETRE.md](docs/PERIMETRE.md), mon protocole de recette dans [docs/RECETTE.md](docs/RECETTE.md) et mes décisions d’architecture dans [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md).
