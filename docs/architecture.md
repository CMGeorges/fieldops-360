# Architecture initiale

## Style

Monolithe modulaire avant microservices. Les frontières `WorkOrders`, `Organizations`, `Vendors`, `Identity`, `Notifications` et `Reporting` sont explicites, mais partagent un déploiement tant qu'une charge ou une organisation d'équipe ne justifie pas une extraction.

## Chemin critique

```text
React/TypeScript
      │ HTTPS + idempotency key
      ▼
ASP.NET Core API
      │ autorisation organisation + rôle
      ▼
Application command handler
      │ charge l'agrégat + contrôle de version
      ▼
WorkOrder aggregate ──► domain events
      │                      │
      ▼                      ▼
PostgreSQL transaction   Outbox record
                             │
                             ▼
                    notifications / reporting
```

## Décisions

- **Monolithe modulaire :** réduit le coût opérationnel d'un premier pilote sans sacrifier les frontières de domaine.
- **PostgreSQL :** données transactionnelles, contraintes, JSONB pour métadonnées limitées et migrations contrôlées.
- **Outbox :** aucune notification ne doit être publiée avant la validation de la transaction métier.
- **Clé d'idempotence :** toute commande modifiant un ordre contient un identifiant unique conservé avec l'événement.
- **Concurrence optimiste :** la version de l'agrégat empêche deux répartiteurs d'écraser leurs décisions.
- **Adaptateurs :** courriel, SMS, stockage et paiement sont remplacés par des implémentations sandbox dans la démonstration.

## Menaces prioritaires

- fuite de données entre organisations;
- accès d'un contracteur à un ordre non assigné;
- lien de pièce jointe partageable trop longtemps;
- rejeu d'une commande ou d'un webhook;
- modification ou suppression d'une preuve après fermeture;
- injection de contenu dans commentaires et fichiers;
- secrets de fournisseur dans les logs.

## Contrôles attendus

- filtre d'organisation obligatoire et tests de non-régression;
- politiques d'autorisation par action, pas seulement par écran;
- URL signées à courte durée et analyse des fichiers;
- idempotence persistée et contraintes uniques;
- stockage append-only des événements d'audit;
- validation stricte et encodage de sortie;
- masquage des secrets et logs structurés corrélés.
