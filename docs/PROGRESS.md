# Avancement — World Quiz Race

| Phase | Contenu | Statut |
|-------|---------|--------|
| 1. Fondations | Solution, architecture, EF Core, options, layout, git | ✅ Terminée (2026-11-20) |
| 2. Données | Entités, configurations EF, migrations, seed (âges, catégories, routes, défis) | ✅ Terminée (2026-11-20) |
| 3. Moteur de jeu | Inscription, match, sélection de défis, validation, tours, progression, vainqueur | ⏳ À faire |
| 4. Interface | Inscription/avatars, carte SVG, écrans de défi, victoire | ⏳ À faire |
| 5. Intégration & tests | Tests d'intégration, scénarios du §13 du cahier des charges | ⏳ À faire |

## Phase 1 — Fondations

### Réalisé
- Solution `WorldQuizRace.slnx` (.NET 10), `global.json` : SDK 10.0.401, `rollForward: latestFeature`.
- Projets et références (dépendances orientées vers le Domain) :
  - `Domain` ← `Application` ← `Infrastructure` ; `Web` → `Application` + `Infrastructure` ; `Tests` → tous.
- EF Core 10.0.15 (SqlServer, Design, Tools) ; `dotnet-ef` 10.0.15 en outil **local** (`dotnet-tools.json`).
- `WorldQuizRaceDbContext` vide, enregistré par `AddInfrastructure(configuration)` ; base `WorldQuizRaceDb` sur LocalDB.
- `AddApplication()` : options `GameSettings` liées à la configuration et validées au démarrage.
- Enums du Domain : `Difficulty`, `ChallengeType` (14 types du cahier des charges), `TurnMode`, `PlayerSlot`.
- Layout ludique (Nunito / Baloo 2 + repli système), palette en variables CSS, accueil provisoire.
- Template nettoyé (Privacy, CSS scopé du layout, page d'erreur réécrite pour les enfants).
- `.vscode/launch.json` + `tasks.json` (F5, build, test, watch).
- 8 tests xUnit sur la validation de `GameSettings`, dont un qui valide le vrai `appsettings.json`.

### Décisions
- **Options dans Application** : `GameSettings` pilote les règles de jeu, ce n'est pas un détail d'infrastructure.
- **Validation par un seul `IValidateOptions<GameSettings>`** plutôt que des DataAnnotations :
  les règles croisées (couverture exacte des positions 1..N sans trou ni chevauchement,
  un temps limite pour *chaque* `ChallengeType`) ne s'expriment pas en attributs.
- **Enums sérialisés par nom** dans la configuration (`"Alternating"`, `"A"`, `"Easy"`, `"Maze"`) : lisible et sûr.
- **`TurnMode` ne contient que `Alternating`** : on n'expose pas un mode configurable qui n'est pas implémenté.
- **Versions de packages figées** (10.0.15) plutôt que flottantes, pour des builds reproductibles.
- **Manifeste d'outils à la racine** (`dotnet-tools.json`) : emplacement par défaut du SDK 10.
- **Migrations dans Infrastructure** (`MigrationsAssembly`), Web sert de projet de démarrage pour `dotnet ef`.
- **`public partial class Program`** exposé pour les futurs tests d'intégration (`WebApplicationFactory`).

### Hypothèses
- Temps limites par défaut (s) : vrai/faux 20, QCM 30, réponses courtes 40–45, jeux interactifs 45–60,
  mémoire et énigmes logiques 90, labyrinthe 120. Bornes acceptées : 5–600 s. Ajustables sans recompiler.
- Nombre de checkpoints accepté : 2 à 50 (15 par défaut).
- Les polices sont chargées depuis Google Fonts ; hors connexion, la police système prend le relais.
- Bouton « Nouvelle partie » volontairement désactivé jusqu'à la phase 4.

### Points d'attention
- `CLAUDE.md` à la racine est un **dossier vide**, pas un fichier (règle Git appliquée : message de commit seul, sans mention d'IA).

## Phase 2 — Données

### Réalisé
- **Domain** (aucune dépendance à EF Core) : `AgeGroup`, `Player`, `Category`, `Challenge`, `Route`, `Checkpoint`,
  `Match`, `MatchPlayer`, `PlayerChallenge`, `Badge`, `PlayerBadge` ; enums `ChallengeType` (14 types, `VisualChallenge`
  remplace `InteractiveVisual`), `Difficulty`, `MatchStatus`, `PlayerSlot`, `AttemptStatus`, `TurnMode` ;
  `MapProjection` (projection équirectangulaire vers le viewBox 1000×500).
- **Mapping EF Core** : une `IEntityTypeConfiguration<T>` par entité (`Infrastructure/Persistence/Configurations`),
  enums stockés en texte, longueurs issues des constantes des entités.
- **Migration `InitialCreate`** appliquée sur LocalDB (`Infrastructure/Persistence/Migrations`).
- **Seed idempotent** (`DatabaseSeeder`) exécuté au démarrage après `MigrateAsync()` : 4 tranches d'âge, 10 catégories,
  5 badges, 2 routes × 15 checkpoints, 180 défis. Second passage : 0 ligne écrite (vérifié par test).
- **docs/CHALLENGE_FORMATS.md** : format de `Content`, `CorrectAnswer` et de la soumission pour chacun des 14 types.
- **Tests** : 41 au total. Fichiers de seed (routes, comptages, formats, réponses calculables) et intégration LocalDB
  (idempotence, CHECK, index filtré, concurrence optimiste, cascades).

### Schéma — décisions
- **Plus de `PlayerAId`/`PlayerBId`/`RouteId` sur `Match`** : les joueurs, leur emplacement A/B et leur route sont portés
  par `MatchPlayer` (unique `(MatchId, Slot)` et `(MatchId, PlayerId)`) ; rien n'est dupliqué.
- **`PlayerChallenge` → `MatchPlayer` par clé étrangère composite `(MatchId, PlayerId)`** (clé alternative) : impossible
  d'enregistrer une tentative pour un joueur qui ne fait pas partie de la partie.
- **Un seul chemin de cascade** : `Matches → MatchPlayers → PlayerChallenges` (et `Routes → Checkpoints`,
  `Players → PlayerBadges`). Toutes les autres relations sont en `NO ACTION` : aucun chemin multiple refusé par SQL Server,
  et une tranche d'âge, une catégorie, un défi ou un joueur référencé ne peut pas disparaître par accident.
- **Index unique filtré** `UX_PlayerChallenges_OnePendingPerPlayer` (`WHERE [Status] = N'Pending'`) : au plus un défi en
  attente par joueur et par partie, garanti par SQL Server même en cas de double clic.
- **CHECK** : âge 5–15, `CurrentPosition` 0–15, `Position`/`CheckpointPosition` 1–15, compteurs ≥ 0,
  `Correct + Wrong ≤ Attempts`, `MapX`/`MapY` dans le viewBox, cohérence d'état d'une tentative
  (Pending ⇔ ni résultat ni date de réponse), `ISJSON` sur `Content`/`CorrectAnswer`, temps limite 5–600 s.
- **`RowVersion`** (rowversion SQL) sur `Match` et `MatchPlayer` : une seconde écriture concurrente lève
  `DbUpdateConcurrencyException` (testé).
- **Clés naturelles pour le seed** : `Challenge.Code` (ex. `G2-M-07`), `Route.Code` (`WEST`/`EAST`), `Badge.Code`,
  `Category.Name`, `AgeGroup.MinimumAge`, tous uniques.
- **Pas de colonne `AttemptDate`** : remplacée par `ServedAt` / `AnsweredAt`. **Pas de temps écoulé stocké** :
  il se calcule à partir de `Match.StartDate/EndDate` et des dates des tentatives.
- **`Score` / `Stars` / `Coins`** : compteurs simples sur `MatchPlayer` ; `Badge` + `PlayerBadge` pour
  les récompenses extensibles (un badge obtenu une seule fois par joueur).
- **Dates en `DateTimeOffset`** (UTC à l'écriture).

### Seed — décisions
- Fichiers **embarqués comme ressources** (`LogicalName` fixe) : rien à copier, fonctionne après `dotnet publish`.
- `routes.json` stocke **latitude/longitude réelles** ; `MapX`/`MapY` sont calculés par `MapProjection` : cohérence
  garantie avec la projection. La destination est déclarée **une seule fois** et ajoutée en position 15 de chaque route.
- Le seeder **refuse de démarrer** si le nombre d'étapes d'une route ≠ `GameSettings:CheckpointsPerRoute`,
  si les positions ont un trou, si un code est dupliqué ou si une catégorie/tranche est inconnue.
- Un défi ou une route retiré des fichiers est **désactivé** (`IsActive = false`), jamais supprimé.
- JSON stocké compact ; les caractères sensibles en HTML (`< > & ' "`) restent échappés.

### Hypothèses
- Route A = `WEST` (Anchorage → … → Istanbul → Le Caire), route B = `EAST` (Auckland → … → Amman → Le Caire).
  Longueurs visuelles dans le viewBox : 788,9 et 787,8 (écart < 0,2 %). L'affectation route ↔ joueur se fera en phase 3.
- Le Caire (x ≈ 587, y ≈ 167) est « à peu près au centre » horizontalement ; la carte étant équirectangulaire,
  il est plus haut que le centre géométrique.
- La contrainte CHECK fixe la position maximale à 15 (`SchemaConstants.MaxRoutePosition`) : passer
  `CheckpointsPerRoute` à une autre valeur demande d'adapter cette constante, `routes.json` et une migration
  (le seeder bloque le démarrage tant que ce n'est pas cohérent).
- Pas d'unicité SQL sur (partie, joueur, défi) : avec 15 défis par difficulté, un enfant qui se trompe souvent pourrait
  épuiser le stock. La non-répétition sera gérée par le service de sélection (phase 3), avec repli si le stock est épuisé.
- `MemoryCards` et `Maze` exposent nécessairement leurs faces / leur grille au navigateur ; la validation reste côté serveur
  (voir docs/CHALLENGE_FORMATS.md).
- Textes en français ; emojis uniquement (aucune image externe) ; pas d'emojis drapeaux (mal rendus sous Windows).
- Quelques réponses texte acceptent des variantes raisonnables (« chat », « le chat », « chaton »…).
