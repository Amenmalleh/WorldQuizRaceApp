# Avancement — World Quiz Race

| Phase | Contenu | Statut |
|-------|---------|--------|
| 1. Fondations | Solution, architecture, EF Core, options, layout, git | ✅ Terminée (2026-11-20) |
| 2. Données | Entités, configurations EF, migrations, seed (âges, catégories, routes, défis) | ⏳ À faire |
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
- `CLAUDE.md` à la racine est actuellement un **dossier vide**, pas un fichier.
- `.claude/setting.json` n'est pas lu par Claude Code (le nom attendu est `settings.json`).
