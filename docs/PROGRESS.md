# Avancement — World Quiz Race

| Phase | Contenu | Statut |
|-------|---------|--------|
| 1. Fondations | Solution, architecture, EF Core, options, layout, git | ✅ Terminée (2026-11-20) |
| 2. Données | Entités, configurations EF, migrations, seed (âges, catégories, routes, défis) | ✅ Terminée (2026-11-20) |
| 3. Moteur de jeu | Inscription, match, sélection de défis, validation, tours, progression, vainqueur | ✅ Terminée (2026-11-20) |
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
- `CLAUDE.md` existe désormais (règles permanentes, ignoré par git) ; les commentaires du code des phases 1–2 ont été
  traduits en anglais pour le respecter (commit `refactor: translate code comments to English`).

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

## Phase 3 — Moteur de jeu

### Réalisé
- **DTOs** (records, `Application/Dtos`) : `RegisterPlayerRequest`, `CreateMatchRequest`, `PlayerDto`, `MatchStateDto`
  (+ joueurs, routes, checkpoints), `ChallengeDto` (contenu public uniquement), `SubmitAnswerRequest`, `AnswerResultDto`,
  `RewardDto`, `BadgeDto`, `MatchResultDto`, `PlayerResultDto`.
- **Services** (interfaces + implémentations dans Application, une responsabilité chacun) : `IAgeGroupService`,
  `IPlayerRegistrationService`, `IMatchService`, `IDifficultyPolicy`, `IChallengeSelectionService`,
  `IAnswerValidationService` (+ 14 `IAnswerValidator`), `ITurnService` (+ `ITurnStrategy`), `IProgressionService`,
  `IWinnerService`, `IRewardService` (+ `IBadgeRule`), `IMatchStateQuery`, `IMatchResultService`, `IGameService`.
- **API** `GameApiController` (`/api/game/...`), filtre `GameExceptionFilter` (ProblemDetails 400/404/409 + `code`),
  anti-forgery par en-tête `X-CSRF-TOKEN`, enums sérialisés en texte, fichier `WorldQuizRace.Web.http`.
- **Tests** : 163 au total (122 nouveaux) — validateurs et règles en tests unitaires purs, moteur complet sur LocalDB jetable,
  API via `WebApplicationFactory`.
- Session de jeu réelle via `curl` (application lancée) avec tentatives de triche : voir le résumé de fin de phase.

### Décisions
- **Pas de modification du schéma** : le modèle de la phase 2 suffisait (pas de nouvelle migration).
- **`IApplicationDbContext` dans Application** (implémenté par le DbContext d'Infrastructure) : les services métier
  restent dans Application sans dépendre de SQL Server ; seul le package `Microsoft.EntityFrameworkCore` y est ajouté.
- **Découverte automatique des stratégies** (`IAnswerValidator`, `IBadgeRule`, `ITurnStrategy`) par réflexion dans
  `AddApplication()` : ajouter un type de défi, un badge ou un mode de tour = ajouter une classe.
- **Intégrité des soumissions** : transaction explicite ; contrôles dans l'ordre partie existante → tentative de cette partie →
  partie InProgress → tentative Pending → tour du joueur → tentative du tour courant. Une course entre deux requêtes
  simultanées est arbitrée par les `RowVersion` (`Match` est toujours modifié par une soumission) et l'index unique filtré :
  la perdante reçoit 409 `concurrent_update`. Testé en parallèle réel.
- **Défi courant idempotent** : `GET …/challenge` renvoie la tentative Pending existante (même `ServedAt`, le chronomètre
  ne repart pas). Deux GET simultanés : l'index filtré n'en garde qu'un, l'autre requête relit la tentative existante.
- **Temps limite** : `ServedAt + TimeLimit + AnswerGracePeriodSeconds (2 s)`. Au-delà : `TimedOut`, mauvaise réponse,
  pas d'avance, le tour passe. Le serveur ne fait pas expirer un défi « tout seul » : le client soumet à la fin du
  chronomètre (ou au rechargement, `RemainingSeconds` vaut alors 0).
- **Sélection des défis** : tranche d'âge du joueur, difficulté de la position visée (`CurrentPosition + 1`), `IsActive`,
  jamais un défi déjà vu dans la partie. **Repli** : difficulté voisine la plus proche (à égalité, la plus facile
  d'abord : Medium → Easy → Hard), puis, tout étant épuisé, le défi vu il y a le plus longtemps (difficulté cible d'abord).
  **Équité** : la difficulté dépend uniquement de la position, pour les deux joueurs, chacun dans sa propre tranche.
- **Validation des réponses** : réponse mal formée = mauvaise réponse (jamais de 500) ; texte insensible à la casse,
  aux accents, aux espaces et à la ponctuation autour ; nombres tolérants (`"3,5"` = `3.5`) ; ordre exact pour suites et
  classements ; ensembles pour paires, mémoire et sélections ; labyrinthe rejoué case par case (tout chemin valide accepté).
- **Récompenses** (configurables, `GameSettings:Rewards`) : bonne réponse = 100 pts, 1 ⭐, 10 pièces ; bonus rapidité
  (réponse dans la première moitié du temps) = +50 pts, +1 ⭐ ; arrivée = +50 pièces. Badges : « Premier pas »,
  « 3 sans faute », « Mi-parcours », « Explorateur du monde », « Apprenti courageux » (fin de partie, gagnant ou non).
  Les anciens badges de la phase 2 jamais attribués sont retirés par le seeder.
- **Anti-forgery** conservé malgré l'absence d'authentification : il empêche un site tiers de piloter la partie
  ouverte sur `localhost` ; cookie `SameSite=Strict`, `HttpOnly`.
- **Premier joueur** : `GameSettings:FirstPlayer` = `A`, `B` ou `Random` (hasard injectable).
- **Messages** au joueur (erreurs, résultats) en français et bienveillants ; code et identifiants en anglais.

### Hypothèses
- « Défis complétés » = défis réussis ; « défis tentés » = toutes les réponses (y compris hors délai). Les deux sont exposés.
- « Temps total » d'un joueur = somme des durées de réponse (`AnsweredAt - ServedAt`) ; durée de partie = `EndDate - StartDate`.
- Un joueur ne peut pas être dans deux parties en cours (`player_already_in_match`) ; les joueurs sont recréés à chaque
  nouvelle partie (pseudo + âge, aucune donnée persistante superflue).
- Pseudo : 2 à 20 caractères après nettoyage des espaces ; lettres, chiffres, espaces et `- _ ' .` uniquement.
- Avatars autorisés : liste configurable `GameSettings:AllowedAvatars` (fox, panda, lion, owl, turtle, penguin, rabbit,
  koala, dolphin, unicorn) ; l'avatar reste facultatif.
- Résultats disponibles uniquement pour une partie terminée (`match_not_finished` sinon).

### Problèmes connus
- Pas d'expiration côté serveur d'un défi abandonné (fermeture de l'onglet) : il sera résolu en `TimedOut` à la prochaine
  soumission du client. Un endpoint « passer son tour » pourra être ajouté en phase 4 si nécessaire.
- Le statut `Abandoned` existe mais aucun cas d'usage ne l'utilise encore.
