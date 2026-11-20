# PROJECT: WORLD QUIZ RACE
## Educational Multiplayer Game – .NET

You are an experienced Senior .NET Developer, Software Architect, UI/UX Designer, and Game Developer.

Your mission is to analyze, design, implement, and improve an educational competitive web game called **World Quiz Race**.

The project must be developed using the **.NET ecosystem**, with clean architecture, maintainable code, an attractive interface, and a complete playable experience.

Do not just provide suggestions or code examples. Work directly on the existing project, implement the features, and verify that the application works.

---

# 1. PROJECT OVERVIEW

World Quiz Race is an educational competitive game designed for children aged 5 to 15.

The main idea is to combine:
- Education and learning.
- General knowledge.
- Logical reasoning.
- Problem-solving.
- Interactive mini-games.
- Competition between two players.
- A world map-based racing experience.

Unlike a traditional quiz application, players must advance across an interactive world map by successfully completing questions and challenges.

The game must be fun, colorful, intuitive, and suitable for children.

## Main objective

Two players compete on the same computer.

Each player starts from an opposite extremity of a world map.

Both players follow separate but equally long predefined routes.

Their routes converge at a common final destination located approximately in the center of the map.

The first player who successfully completes all the required challenges and reaches the final destination wins.

**Important:** This is a racing game, not a territory-conquest game. Players do not capture or own countries.

---

# 2. TECHNOLOGY REQUIREMENTS

Use the .NET ecosystem.

Preferred technologies:

- Backend: ASP.NET Core with C#.
- Frontend: ASP.NET Core MVC or Razor Pages, depending on the existing project structure.
- Database: SQL Server.
- ORM: Entity Framework Core.
- Frontend interactions: HTML, CSS, JavaScript.
- Interactive map: SVG or another suitable web-based mapping approach.
- UI animations: CSS and JavaScript.
- Optional: Bootstrap or another suitable responsive UI framework.

Do not introduce unnecessary technologies.

For the first version, the game runs locally on the same computer. SignalR is not required.

If the existing project already has a structure, framework version, database, or implementation, inspect it and work with it instead of replacing everything.

---

# 3. PLAYER REGISTRATION

At the beginning of each game, display a registration screen for two players.

Each player must provide:

- Player name or nickname.
- Age.
- Optional avatar selection.

Example:

Player A:
- Name: Adam
- Age: 9

Player B:
- Name: Lina
- Age: 12

After registration, the system automatically determines the appropriate age group for each player.

The game must support four age groups:

1. 5–7 years.
2. 8–10 years.
3. 11–12 years.
4. 13–15 years.

Players can have different ages and therefore different age groups.

The system must adapt the questions and challenges individually to each player.

Do not force both players to receive identical questions if their ages are different.

---

# 4. WORLD MAP AND RACING SYSTEM

This is the central feature of the application.

Create an attractive, colorful, interactive world map.

The map must contain two symmetrical racing routes.

## Starting positions

- Player A starts from one extremity of the world map.
- Player B starts from the opposite extremity.
- Both routes lead toward the same central destination.
- The final destination must be positioned approximately in the middle of the map.

## Route requirements

- Both routes must have exactly the same number of checkpoints.
- Both players must travel the same logical distance.
- Each route contains 15 checkpoints by default.
- The final checkpoint is shared.
- The routes must be visually distinguishable using different colors.
- Each checkpoint must have a visual representation.
- Each player must have an avatar or marker showing their current position.
- The map must update visually after every successful challenge.

The two routes can represent different geographical regions, countries, or continents.

The exact countries and geographical paths can be configured later.

**Important:** Do not create unequal routes. The two players must have the same number of steps and equivalent difficulty.

## Map interface

The interface should show:

- The world map.
- Player A's starting point.
- Player B's starting point.
- The common final destination.
- Both racing paths.
- All checkpoints.
- Each player's current position.
- Completed checkpoints.
- Remaining checkpoints.
- A progress indicator for each player.

Use an attractive illustrated world map suitable for children.

Avoid a basic administrative-looking map. The experience should resemble an educational adventure or board game.

---

# 5. GAMEPLAY AND TURN MANAGEMENT

The first version must support two players on the same computer.

Recommended default gameplay: alternating turns.

Example:

1. Player A receives a challenge.
2. Player A answers.
3. If the answer is correct, Player A advances one checkpoint.
4. If the answer is incorrect, Player A stays in the same position.
5. Player B receives a challenge.
6. The process continues until one player reaches the finish line.

Each successful challenge advances the player by exactly one checkpoint.

A wrong answer must not make the player move backward.

The turn system should be configurable so that simultaneous or other game modes can be added later.

## Important gameplay rules

- Players always follow their predefined routes.
- Each player progresses independently.
- The same checkpoint count is required for both players.
- Players cannot skip checkpoints.
- A player cannot advance without successfully completing the challenge.
- The game must track the number of attempts, correct answers, wrong answers, and elapsed time.
- The first player to reach the final destination wins.

The game must prevent inconsistent states, such as advancing twice after one correct answer.

---

# 6. EDUCATIONAL CHALLENGES AND AGE ADAPTATION

This is another essential part of the project.

The platform must not be limited to traditional multiple-choice questions.

It must support different challenge types and interactive educational games.

## Age group 1: Children aged 5–7

Focus on simple visual and interactive activities.

Examples:
- Shape sorting: circles, squares, triangles.
- Color recognition.
- Animal and habitat matching.
- Counting objects.
- Image recognition.
- Simple memory games.
- Matching identical pictures.
- Simple puzzles.

## Age group 2: Children aged 8–10

Focus on basic knowledge and logical reasoning.

Examples:
- Basic mathematics.
- Mental calculations.
- General knowledge.
- Missing number sequences.
- Simple riddles.
- Sorting objects by multiple criteria.
- Memory challenges.
- Basic geography.
- Science questions.

## Age group 3: Children aged 11–12

Focus on reasoning and problem-solving.

Examples:
- Mathematical problems.
- Logical puzzles.
- Science questions.
- Geography and history.
- Mazes.
- Pattern recognition.
- Classification problems.
- Interactive reasoning exercises.

## Age group 4: Teenagers aged 13–15

Focus on more advanced intellectual challenges.

Examples:
- Advanced mathematics.
- Algorithmic reasoning.
- Basic programming.
- Scientific knowledge.
- Complex logical puzzles.
- Geography and history.
- Technology and computing.
- Problem-solving scenarios.

## Challenge types to support

Implement a flexible challenge system capable of supporting:

1. Multiple-choice questions (QCM).
2. True or false.
3. Text-based answers.
4. Numeric answers.
5. Drag and drop.
6. Shape sorting.
7. Image matching.
8. Memory cards.
9. Sequence completion.
10. Logical puzzles.
11. Mazes.
12. Ordering and classification.
13. Mathematical mini-games.
14. Interactive visual challenges.

The system must be extensible so that new challenge types can be added without redesigning the entire application.

---

# 7. QUESTION SELECTION AND FAIRNESS

The game must select challenges according to the player's age group.

For example:

Player A is 9 years old:
- Receives challenges designed for ages 8–10.

Player B is 13 years old:
- Receives challenges designed for ages 13–15.

Both players must face equivalent relative difficulty.

The fact that one player is older must not automatically provide a competitive advantage.

## Difficulty levels

Within each age group, support:

- Easy.
- Medium.
- Hard.

The system should select challenges according to configurable difficulty rules.

Avoid repeating the same question during a single match.

Questions should be selected randomly from the available challenges, while respecting:
- Age group.
- Difficulty.
- Challenge type.
- Availability.
- Previous questions already used in the current match.

The game should support configurable timers for different challenge types.

---

# 8. WINNING CONDITIONS AND GAME RESULTS

The game ends when the first player reaches the final checkpoint.

Display an attractive victory screen.

It should include:

- Winner's name and avatar.
- Winner's final position.
- Total elapsed time.
- Number of correct answers.
- Number of incorrect answers.
- Accuracy percentage.
- Number of challenges completed.
- Final map showing both players' positions.

The losing player should also receive a positive summary encouraging learning.

Introduce a reward system that can be extended later:

- Stars.
- Coins.
- Badges.
- Achievements.
- Player statistics.

Do not make the experience discouraging for children.

---

# 9. DATABASE DESIGN

Use SQL Server and Entity Framework Core.

Design a clean relational database.

Consider the following entities:

## Player
- Id
- Name
- Age
- AgeGroupId
- Avatar
- CreatedAt

## AgeGroup
- Id
- Name
- MinimumAge
- MaximumAge

## Challenge
- Id
- Title
- Description
- ChallengeType
- AgeGroupId
- Difficulty
- CategoryId
- Content
- CorrectAnswer
- TimeLimit
- IsActive

## Category
- Id
- Name
- Description

## Route
- Id
- Name
- Description
- TotalCheckpoints
- IsActive

## Checkpoint
- Id
- RouteId
- Position
- Name
- Country
- MapCoordinates
- IsFinal

## Match
- Id
- PlayerAId
- PlayerBId
- RouteId
- StartDate
- EndDate
- WinnerId
- Status

## MatchPlayer
- Id
- MatchId
- PlayerId
- CurrentCheckpoint
- CorrectAnswers
- WrongAnswers
- Score
- ElapsedTime

## PlayerChallenge
- Id
- MatchId
- PlayerId
- ChallengeId
- CheckpointPosition
- IsCorrect
- Answer
- AttemptDate

Adjust the schema when necessary.

Use appropriate relationships, constraints, indexes, and migrations.

Do not duplicate data unnecessarily.

---

# 10. APPLICATION ARCHITECTURE

Organize the project into clear layers.

Suggested structure:

- Domain: entities and business rules.
- Application: services, DTOs, and use cases.
- Infrastructure: database and Entity Framework Core.
- Web: controllers, views, frontend assets.

Avoid putting all business logic inside controllers.

Create dedicated services for:

- Player registration.
- Match creation.
- Age group determination.
- Challenge selection.
- Answer validation.
- Turn management.
- Player progression.
- Winner determination.
- Match results.

Business rules must be validated on the server side.

Never trust the frontend to decide whether an answer is correct or whether a player can advance.

---

# 11. USER INTERFACE AND DESIGN

The interface must be modern, playful, colorful, and child-friendly.

Design the following screens:

1. Home page.
2. New game screen.
3. Player registration.
4. Avatar selection.
5. World map and route presentation.
6. Challenge screen.
7. Answer feedback.
8. Player progression.
9. Victory screen.
10. Match summary.

## Design guidelines

- Use attractive colors.
- Use rounded cards and buttons.
- Use readable typography.
- Add smooth animations.
- Display avatars and progress indicators.
- Make the map the main visual element.
- Use clear visual feedback for correct and incorrect answers.
- Make the application responsive.
- Keep navigation simple and intuitive.

The experience should feel like a real educational video game, not a conventional school examination website.

---

# 12. SECURITY AND DATA VALIDATION

Even though this is a local two-player game, implement proper validation.

- Validate player ages between 5 and 15.
- Validate all submitted answers on the server.
- Prevent unauthorized progression.
- Prevent duplicate submissions for the same turn.
- Prevent players from skipping checkpoints.
- Validate challenge availability.
- Handle invalid or missing data.
- Avoid exposing correct answers in frontend data before submission.

Since the target audience includes children, collect only the information necessary to play.

Use nicknames rather than requiring real names.

Do not implement public messaging or unnecessary collection of personal information.

---

# 13. DEVELOPMENT METHODOLOGY

Before writing or modifying code, follow these steps.

## Phase 1: Existing project analysis

Inspect the complete existing project.

Identify:
- .NET version.
- Project structure.
- Existing dependencies.
- Existing database configuration.
- Existing entities.
- Existing controllers.
- Existing views.
- Current implementation status.

Do not overwrite existing work without understanding it.

Explain the existing architecture briefly.

## Phase 2: Architecture and database

Define the required entities and relationships.

Implement the database using EF Core.

Create and apply migrations.

Seed initial data:
- Four age groups.
- Initial categories.
- Example routes.
- 15 checkpoints per route.
- A useful collection of sample challenges for every age group.

## Phase 3: Core game functionality

Implement:
- Player registration.
- Match creation.
- Age group identification.
- Route initialization.
- Challenge selection.
- Answer validation.
- Turn management.
- Player progression.
- Winner determination.

## Phase 4: Interactive interface

Implement:
- Home page.
- Registration screens.
- Interactive world map.
- Player markers.
- Checkpoints.
- Challenge interfaces.
- Progress indicators.
- Victory screen.

## Phase 5: Testing

Test the complete game.

At minimum, verify:

1. Two players can register successfully.
2. Different player ages are handled correctly.
3. Each player receives age-appropriate challenges.
4. Both routes have equal checkpoint counts.
5. Both routes lead to the same final destination.
6. A correct answer advances the player exactly one checkpoint.
7. An incorrect answer does not advance the player.
8. The turn order is respected.
9. A player cannot skip a checkpoint.
10. The winner is correctly determined.
11. Match results are saved correctly.
12. The application works without runtime errors.

---

# 14. IMPORTANT INSTRUCTIONS FOR THE AI CODING AGENT

- Work directly on the existing project.
- Do not generate a completely separate project unless necessary.
- Do not delete existing files without justification.
- Do not replace working features unnecessarily.
- Do not leave important features as TODO comments or mock implementations.
- Use clean, readable, maintainable C# code.
- Follow SOLID principles where appropriate.
- Use dependency injection.
- Use asynchronous operations when appropriate.
- Use DTOs for communication between application layers.
- Handle exceptions and validation properly.
- Keep the frontend and backend responsibilities clearly separated.
- Explain important implementation decisions.
- Build and run the application after implementation.
- Fix compilation and runtime errors before declaring completion.

If a requirement is ambiguous, choose a reasonable implementation, document the assumption, and keep it configurable.

Do not attempt to implement every advanced feature at once. Prioritize a complete, functional MVP.

---

# 15. EXPECTED FINAL RESULT

The final result must be a working .NET web application where:

1. Two players register on the same computer.
2. Each player's age determines their educational challenge level.
3. Both players start from opposite extremities of an interactive world map.
4. Both follow separate, equally long routes.
5. Both routes converge at a shared destination in the center.
6. Each successful challenge advances the corresponding player by one checkpoint.
7. Players compete until one reaches the final destination.
8. The application announces the winner and displays match statistics.

**The priority is to deliver a polished, playable, functional educational racing game, not just a static design or a collection of disconnected screens.**

Start by inspecting the existing repository and presenting a short implementation plan. Then proceed with implementation phase by phase.