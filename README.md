# 🌍 World Quiz Race

Jeu éducatif de course pour deux joueurs de 5 à 15 ans, sur le même ordinateur.
Chaque joueur part d'une extrémité d'une carte du monde illustrée et suit sa propre route
(15 checkpoints par défaut) vers une destination commune au centre. Chaque défi réussi
fait avancer d'un checkpoint ; le premier arrivé gagne. Les défis (QCM, calcul, mémoire,
tri de formes, labyrinthes…) s'adaptent à la tranche d'âge de chaque joueur.

Le cahier des charges complet est dans [docs/CAHIER_DES_CHARGES.md](docs/CAHIER_DES_CHARGES.md),
l'avancement dans [docs/PROGRESS.md](docs/PROGRESS.md).

## Stack

| Élément            | Choix                                              |
|--------------------|----------------------------------------------------|
| Runtime            | .NET 10 (LTS), SDK fixé par `global.json`          |
| Web                | ASP.NET Core MVC, Razor, Bootstrap 5               |
| Données            | SQL Server LocalDB + Entity Framework Core 10      |
| Tests              | xUnit                                              |
| Architecture       | Clean Architecture (Domain / Application / Infrastructure / Web) |

```
WorldQuizRace.slnx
├── src/
│   ├── WorldQuizRace.Domain          # Entités, enums, règles métier (aucune dépendance)
│   ├── WorldQuizRace.Application     # Services, DTO, options de jeu (→ Domain)
│   ├── WorldQuizRace.Infrastructure  # EF Core, DbContext, migrations (→ Application, Domain)
│   └── WorldQuizRace.Web             # Contrôleurs MVC, vues, assets (→ Application, Infrastructure)
└── tests/
    └── WorldQuizRace.Tests           # Tests xUnit (→ tous les projets)
```

## Prérequis

- [SDK .NET 10.0.401](https://dotnet.microsoft.com/download/dotnet/10.0) ou une version 10.0.4xx ultérieure
- SQL Server Express **LocalDB** (installé avec Visual Studio, ou via l'installeur SQL Server Express → « LocalDB »).
  Vérifier avec `sqllocaldb info` (doit lister `MSSQLLocalDB`).
- Au choix : **VS Code** + extension **C# Dev Kit**, ou **Visual Studio 2026**.

## Premier lancement (après clonage)

```bash
dotnet tool restore          # installe dotnet-ef (outil local, version figée)
dotnet build WorldQuizRace.slnx
dotnet test WorldQuizRace.slnx
```

## Lancer l'application

### VS Code
1. Ouvrir le dossier racine du dépôt.
2. Appuyer sur **F5** (configuration « World Quiz Race (Web) ») : build, démarrage sous débogueur
   et ouverture du navigateur sur https://localhost:7243.
3. Autres tâches : `Ctrl+Shift+B` (build), *Terminal → Run Task → test / watch*.

### Visual Studio
1. Ouvrir `WorldQuizRace.slnx`.
2. Définir **WorldQuizRace.Web** comme projet de démarrage, profil `https`, puis **F5**.

### Ligne de commande
```bash
dotnet run --project src/WorldQuizRace.Web --launch-profile https
```

> Au premier lancement en HTTPS, faire confiance au certificat de dev : `dotnet dev-certs https --trust`.

## Configuration du jeu

La section `GameSettings` de `src/WorldQuizRace.Web/appsettings.json` règle le nombre de checkpoints,
le mode de tour, le premier joueur, la difficulté par position et le temps limite par type de défi.
Elle est **validée au démarrage** : une configuration incohérente (trou dans les règles de difficulté,
type de défi sans temps limite…) empêche l'application de démarrer, avec un message explicite.

## Base de données (EF Core)

Base `WorldQuizRaceDb` sur LocalDB. **Au démarrage, l'application applique les migrations en attente puis
synchronise les données initiales** (tranches d'âge, catégories, badges, routes et défis) : rien à faire à la main
pour jouer. Ce comportement se désactive avec `"Database": { "InitializeOnStartup": false }`.

`dotnet-ef` est un outil **local** (version figée dans `dotnet-tools.json`) : lancer `dotnet tool restore` une fois après clonage.
Toutes les commandes se lancent depuis la racine du dépôt :

```bash
# Créer une migration après une modification du modèle (Domain / Configurations)
dotnet ef migrations add <Nom> --project src/WorldQuizRace.Infrastructure --startup-project src/WorldQuizRace.Web --output-dir Persistence/Migrations

# Appliquer les migrations sans lancer l'application
dotnet ef database update --project src/WorldQuizRace.Infrastructure --startup-project src/WorldQuizRace.Web

# Voir le SQL généré / lister les migrations
dotnet ef migrations script --project src/WorldQuizRace.Infrastructure --startup-project src/WorldQuizRace.Web
dotnet ef migrations list   --project src/WorldQuizRace.Infrastructure --startup-project src/WorldQuizRace.Web

# Repartir d'une base vide (elle sera recréée et ré-alimentée au prochain démarrage)
dotnet ef database drop --force --project src/WorldQuizRace.Infrastructure --startup-project src/WorldQuizRace.Web
```

Dans Visual Studio, l'équivalent se fait dans la *Console du Gestionnaire de package* (projet par défaut :
WorldQuizRace.Infrastructure) : `Add-Migration <Nom> -OutputDir Persistence/Migrations`, puis `Update-Database`.

> Arrêter l'application avant de lancer `dotnet ef` ou `dotnet build` : sinon le build échoue sur des DLL verrouillées.

## Données initiales et défis

- `src/WorldQuizRace.Infrastructure/Seed/challenges.json` : 180 défis (4 tranches × 3 difficultés × 15), 14 types.
- `src/WorldQuizRace.Infrastructure/Seed/routes.json` : 2 routes de 15 étapes et la destination commune (Le Caire).
- Ces fichiers sont **embarqués dans la DLL** (fonctionnent après publication). Pour ajouter ou corriger un défi :
  modifier le JSON puis relancer l'application ; la synchronisation est idempotente (clé : `code`).
  Un défi retiré du fichier est désactivé, jamais supprimé.
- Le format JSON de chaque type de défi est décrit dans [docs/CHALLENGE_FORMATS.md](docs/CHALLENGE_FORMATS.md) ;
  `dotnet test` vérifie chaque défi (format, labyrinthes résolubles, résultats des calculs).

## API de jeu (`/api/game`)

Le moteur de jeu tourne entièrement côté serveur ; le JavaScript (phase 4) appellera ces endpoints.

| Méthode | Route | Rôle |
|---|---|---|
| GET | `/api/game/antiforgery` | Pose le cookie anti-forgery et renvoie le jeton à mettre dans l'en-tête `X-CSRF-TOKEN` |
| POST | `/api/game/matches` | Inscrit les deux joueurs et crée la partie (201) |
| GET | `/api/game/matches/{matchId}` | État : joueurs, routes, checkpoints, positions, tour, statistiques |
| GET | `/api/game/matches/{matchId}/players/{playerId}/challenge` | Défi courant du joueur dont c'est le tour (même défi si on recharge) |
| POST | `/api/game/matches/{matchId}/answers` | Soumet la réponse ; renvoie correct/incorrect, explication, nouvelle position, récompenses, état |
| GET | `/api/game/matches/{matchId}/result` | Résultats de fin de partie (409 tant qu'elle n'est pas terminée) |

Les erreurs métier sont des `application/problem+json` avec un champ `code` stable
(`not_your_turn`, `attempt_already_answered`, `match_not_in_progress`, `validation_failed`…) :
400 = données invalides, 404 = introuvable, 409 = règle du jeu ou conflit. Les POST sans jeton anti-forgery reçoivent 400.

Exemples prêts à l'emploi : `src/WorldQuizRace.Web/WorldQuizRace.Web.http` (extension **REST Client** de VS Code,
ou directement dans Visual Studio). Lancer l'application, puis envoyer les requêtes dans l'ordre.

## Tests

```bash
dotnet test WorldQuizRace.slnx
```

Les tests d'intégration créent une base LocalDB temporaire (`WorldQuizRaceDb_Tests_<guid>`) et la suppriment à la fin.
