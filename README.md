# FieldOps 360

Je construis mon environnement de travail pour coordonner les interventions, documenter mes chantiers et conserver mes événements de pointage.

## Mon application FieldOps Atelier

Je dispose maintenant d’un module web distinct dans [`apps/fieldops-atelier`](apps/fieldops-atelier). Je peux créer mes dossiers, conserver mes photos, saisir pièces/relevés/inventaire/équipements, planifier mes interventions, enregistrer mes heures et préparer un brouillon de devis depuis les surfaces et le pointage.

Je sauvegarde d’abord sur l’appareil, puis je synchronise avec contrôle de version et déduplication. Je conserve les changements refusés et je n’efface pas les horodatages d’origine. Une version privée est publiée ; je n’ouvre pas les dossiers au public.

Je retrouve mon installation, mes garanties et mes limites dans le [guide du module](apps/fieldops-atelier/README.md). Je vérifie 30 tests automatisés : calculs, stockage local, synchronisation D1 et routes compilées. Je n’ai pas encore exécuté la recette visuelle sur téléphone ni mesuré une supériorité sur Mobile-Punch.

Je ne revendique pas une équivalence complète avec Encircle ou Mobile-Punch : mes accès d’équipe, la paie, les signatures, les plans automatiques et la fusion multi-appareils restent à développer. Mon [périmètre](apps/fieldops-atelier/docs/PERIMETRE.md) distingue ce qui fonctionne et ce qui reste à réaliser.

## Mon noyau de domaine .NET

Je conserve dans `src/FieldOps.Domain` le noyau de coordination des demandes d’intervention, avec son offre horodatée, son acceptation explicite, ses délais, sa réassignation et ses événements. Je n’ai pas encore relié ce noyau C# au nouveau module web.

Mon parcours prévu reste : demande → triage → offre → acceptation/refus → planification → intervention → validation → fermeture. Je garde les cas d’absence de réponse, d’absence terrain et de réouverture dans le modèle.

Pour vérifier ce noyau, je dispose du SDK .NET 10 et je lance :

```bash
dotnet test tests/FieldOps.Domain.Tests/FieldOps.Domain.Tests.csproj
```

Je retrouve son cadrage initial dans [PRODUCT.md](PRODUCT.md) et [docs/architecture.md](docs/architecture.md). Ces documents décrivent le noyau précédent, pas une intégration achevée au module Atelier.
