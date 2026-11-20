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

```bash
dotnet ef migrations add <Nom> --project src/WorldQuizRace.Infrastructure --startup-project src/WorldQuizRace.Web
dotnet ef database update      --project src/WorldQuizRace.Infrastructure --startup-project src/WorldQuizRace.Web
```
