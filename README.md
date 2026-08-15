# FieldOps 360

Coordination fiable des demandes d'intervention entre demandeurs, gestionnaires et contracteurs.

> État actuel : cadrage produit et noyau de domaine. Le projet n'est pas encore présenté comme prêt pour une exploitation client.

## Pourquoi ce produit

Les outils existants centralisent généralement les demandes et les horaires. FieldOps 360 met l'accent sur la chaîne de responsabilité lorsqu'une intervention traverse plusieurs entreprises : offre horodatée, acceptation explicite, délai de réponse, absence, réassignation et preuve complète.

## Première tranche verticale

`Demande → triage → offre → acceptation/refus → planification → intervention → validation → fermeture`

Les chemins `NO_RESPONSE`, `NO_SHOW` et `REOPENED` font partie du modèle dès la première version; ils ne sont pas traités comme de simples notes.

## Structure prévue

```text
src/
  FieldOps.Domain/          règles métier et agrégats
  FieldOps.Application/     cas d'utilisation et ports
  FieldOps.Infrastructure/  PostgreSQL, stockage, notifications
  FieldOps.Api/             API ASP.NET Core
web/                        interface React/TypeScript
tests/                      tests unitaires, intégration et E2E
docs/                       architecture, menaces et décisions
```

## Démarrage du noyau de domaine

Prérequis : SDK .NET 10.

```bash
dotnet test tests/FieldOps.Domain.Tests/FieldOps.Domain.Tests.csproj
```

## Sécurité et données

- données de démonstration uniquement;
- aucun secret dans le dépôt;
- isolation par organisation imposée à chaque commande;
- transitions contrôlées et versionnées;
- journal d'événements append-only au niveau du domaine.

Voir [PRODUCT.md](PRODUCT.md) et [l'architecture](docs/architecture.md).
