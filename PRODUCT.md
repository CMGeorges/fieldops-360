# FieldOps 360 — cahier produit initial

## Promesse

Chaque demande reçoit un responsable, une échéance, une preuve et une issue. Si un contracteur refuse, tarde ou ne se présente pas, le gestionnaire voit immédiatement l'écart et peut réassigner sans perdre l'historique.

## Utilisateurs

- **Demandeur** : locataire, responsable de succursale ou employé qui signale un problème.
- **Répartiteur** : qualifie, priorise et assigne la demande.
- **Contracteur** : accepte ou refuse, propose un créneau et dépose ses preuves.
- **Gestionnaire** : voit les SLA, coûts, exceptions et performances.
- **Administrateur** : configure bâtiments, catégories, règles, équipes et accès.

## Parcours critique v1

1. Le demandeur crée une requête avec lieu, catégorie, description et photos.
2. Le système calcule une priorité suggérée et une échéance SLA.
3. Le répartiteur confirme la priorité et invite un contracteur.
4. Le contracteur doit accepter ou refuser avant une limite précise.
5. Une absence de réponse déclenche une relance puis une escalade.
6. Le contracteur planifie la visite et enregistre arrivée, diagnostic et preuve.
7. Le demandeur ou le gestionnaire valide la résolution.
8. Le système ferme le dossier et produit une chronologie exportable.

## États contrôlés

`DRAFT → SUBMITTED → TRIAGED → OFFERED → ACCEPTED → SCHEDULED → IN_PROGRESS → READY_FOR_REVIEW → CLOSED`

Branches d'exception : `DECLINED`, `NO_RESPONSE`, `NO_SHOW`, `CANCELLED`, `REOPENED`.

Chaque transition exige : rôle autorisé, version courante, motif si exception et événement d'audit.

## Modèle métier minimal

- Tenant / Organization
- User / Membership / Role
- Site / Building / Unit / Asset
- WorkRequest / WorkOrder
- AssignmentOffer / Appointment
- Evidence / Comment
- SLA / EscalationRule
- AuditEvent / Notification
- Vendor / ContractorProfile

## Critères d'acceptation de la première tranche verticale

- création d'une organisation et chargement de données de démonstration;
- connexion avec trois rôles de démonstration;
- création et triage d'une requête;
- offre assignée à un contracteur;
- acceptation ou refus avec raison;
- planification et changement d'état contrôlé;
- simulation `NO_RESPONSE` et réassignation;
- chronologie complète visible;
- tests de transitions, permissions, concurrence et idempotence;
- démarrage local documenté en une commande.

## Hors périmètre de la première tranche

- paiement réel;
- SMS ou courriels réels;
- géolocalisation continue;
- application mobile native;
- intégration comptable réelle;
- stockage de renseignements personnels de clients réels.

Ces fonctions auront des interfaces d'adaptation et des implémentations sandbox afin de démontrer l'architecture sans prétendre qu'elles sont déjà certifiées pour la production.
