# Formats JSON des défis

Chaque `Challenge` porte deux documents JSON dont la forme dépend de son `Type` (`ChallengeType`) :

| Colonne | Visibilité | Rôle |
|---|---|---|
| `Content` | **PUBLIC** — envoyé au navigateur | Énoncé, options, éléments à manipuler. Ne contient **jamais** la réponse. |
| `CorrectAnswer` | **SECRET** — ne quitte jamais le serveur | Sert uniquement à la validation côté serveur. |

Ce document est le contrat entre le seed (`src/WorldQuizRace.Infrastructure/Seed/challenges.json`),
les validateurs de réponses (phase 3) et l'interface (phase 4). Il est vérifié automatiquement par
`tests/WorldQuizRace.Tests/Seed/ChallengeFormatRules.cs`.

## Règles communes

- `Content` et `CorrectAnswer` sont des **objets** JSON (contrainte SQL `ISJSON` en plus).
- `Content.prompt` (string, obligatoire) : la consigne affichée.
- `Content.visual` (string, optionnel) : emojis affichés en grand pour illustrer (ex. `"🍎🍎🍎"`). Pas d'images externes.
- Les éléments manipulables ont un `id` **unique dans leur liste** et un libellé `text` (les emojis sont autorisés dans `text`).
- **Soumission** : le navigateur envoie un objet JSON dont la forme est indiquée pour chaque type ci-dessous ;
  elle est stockée telle quelle dans `PlayerChallenge.Answer`.
- **Ordre d'affichage** : l'ordre des listes dans `Content` ne doit pas révéler la réponse
  (vérifié par les tests pour le mode `order`). L'interface peut en plus mélanger l'affichage.
- **Normalisation du texte** (`TextAnswer`, `LogicPuzzle` en mode `text`, valeurs non numériques de `SequenceCompletion`) :
  Unicode NFKD, suppression des accents, minuscules, apostrophes typographiques unifiées, espaces de début/fin
  supprimés, espaces multiples réduits à un seul, ponctuation autour de la réponse ignorée (`"La Loire !"` = `"la loire"`).
  Les valeurs de `acceptedAnswers` sont stockées **déjà normalisées**.
- **Nombres** : dans le seed, JSON numérique (point décimal). `tolerance` absente = 0. Dans une **soumission**, un nombre
  peut aussi être une chaîne : `"3,5"` = `"3.5"` = `3.5`, espaces de milliers ignorés (`"1 000"`).
- **Réponse mal formée** (pas un objet, champ manquant ou d'un autre type, identifiants inconnus) = **mauvaise réponse**,
  jamais une erreur serveur. Les validateurs sont dans `src/WorldQuizRace.Application/Answers/Validators` (une classe par type).

---

## 1. `MultipleChoice` — QCM

| | Format |
|---|---|
| Content | `{ "prompt", "visual"?, "options": [{ "id", "text" }] }` (2 à 6 options) |
| CorrectAnswer | `{ "optionId": "b" }` |
| Soumission | `{ "optionId": "b" }` |
| Validation | `optionId` soumis == `optionId` attendu |

```json
{ "prompt": "Quelle est la capitale de l'Australie ?",
  "options": [{ "id": "a", "text": "Sydney" }, { "id": "b", "text": "Canberra" }, { "id": "c", "text": "Melbourne" }] }
{ "optionId": "b" }
```

## 2. `TrueFalse` — Vrai ou faux

| | Format |
|---|---|
| Content | `{ "prompt", "visual"? }` — `prompt` est l'affirmation à juger |
| CorrectAnswer | `{ "value": false }` |
| Soumission | `{ "value": true }` (les chaînes `"true"` / `"false"` sont acceptées) |
| Validation | booléens égaux |

```json
{ "prompt": "La Lune produit sa propre lumière.", "visual": "🌕" }
{ "value": false }
```

## 3. `TextAnswer` — Réponse écrite

| | Format |
|---|---|
| Content | `{ "prompt", "visual"?, "maxLength"? }` |
| CorrectAnswer | `{ "acceptedAnswers": ["loire", "la loire"] }` (valeurs normalisées) |
| Soumission | `{ "text": "La Loire" }` |
| Validation | `normalize(text)` ∈ `acceptedAnswers` |

```json
{ "prompt": "Quel est le plus long fleuve de France ?", "maxLength": 30 }
{ "acceptedAnswers": ["loire", "la loire"] }
```

## 4. `NumericAnswer` — Réponse numérique

| | Format |
|---|---|
| Content | `{ "prompt", "visual"?, "unit"? }` — `unit` est affiché à côté du champ (« cm », « € »…) |
| CorrectAnswer | `{ "value": 12, "tolerance": 0 }` |
| Soumission | `{ "value": 12 }` |
| Validation | `abs(value - attendu) <= tolerance` |

```json
{ "prompt": "Combien y a-t-il de pommes ?", "visual": "🍎🍎🍎" }
{ "value": 3, "tolerance": 0 }
```

## 5. `DragAndDrop` — Glisser-déposer vers des zones

| | Format |
|---|---|
| Content | `{ "prompt", "items": [{ "id", "text" }], "targets": [{ "id", "text" }] }` |
| CorrectAnswer | `{ "mapping": { "itemId": "targetId", … } }` — chaque item a exactement une cible ; une cible peut recevoir plusieurs items |
| Soumission | `{ "mapping": { … } }` |
| Validation | même ensemble de clés et même cible pour chaque item |

```json
{ "prompt": "Classe chaque périphérique.",
  "items": [{ "id": "i1", "text": "⌨️ Le clavier" }, { "id": "i2", "text": "🖥️ L'écran" }],
  "targets": [{ "id": "t1", "text": "Périphérique d'entrée" }, { "id": "t2", "text": "Périphérique de sortie" }] }
{ "mapping": { "i1": "t1", "i2": "t2" } }
```

## 6. `ShapeSorting` — Tri de formes

Comme `DragAndDrop`, mais les éléments sont des formes dessinées en SVG par l'interface.

| | Format |
|---|---|
| Content | `{ "prompt", "shapes": [{ "id", "shape", "color" }], "bins": [{ "id", "text" }] }` |
| | `shape` ∈ `circle`, `square`, `triangle`, `star`, `rectangle`, `heart` ; `color` ∈ `red`, `blue`, `green`, `yellow`, `orange`, `purple` |
| CorrectAnswer | `{ "mapping": { "shapeId": "binId", … } }` |
| Soumission | `{ "mapping": { … } }` |
| Validation | identique à `DragAndDrop` |

```json
{ "prompt": "Range chaque forme dans la bonne boîte.",
  "shapes": [{ "id": "s1", "shape": "circle", "color": "red" }, { "id": "s2", "shape": "square", "color": "blue" }],
  "bins": [{ "id": "b1", "text": "Les ronds" }, { "id": "b2", "text": "Les carrés" }] }
{ "mapping": { "s1": "b1", "s2": "b2" } }
```

## 7. `ImageMatching` — Relier deux colonnes

| | Format |
|---|---|
| Content | `{ "prompt", "left": [{ "id", "text" }], "right": [{ "id", "text" }] }` |
| CorrectAnswer | `{ "pairs": { "leftId": "rightId", … } }` — bijection : chaque élément de droite est utilisé une fois |
| Soumission | `{ "pairs": { … } }` |
| Validation | paires identiques |

```json
{ "prompt": "Relie chaque animal à son petit.",
  "left": [{ "id": "l1", "text": "🐔 La poule" }, { "id": "l2", "text": "🐄 La vache" }],
  "right": [{ "id": "r1", "text": "Le veau" }, { "id": "r2", "text": "🐣 Le poussin" }] }
{ "pairs": { "l1": "r2", "l2": "r1" } }
```

## 8. `MemoryCards` — Jeu de paires

| | Format |
|---|---|
| Content | `{ "prompt", "previewSeconds"?, "cards": [{ "id", "face" }] }` — nombre pair de cartes (≥ 4) ; `face` = emoji ou texte |
| CorrectAnswer | `{ "pairs": [["c1", "c3"], ["c2", "c4"]] }` — partition de toutes les cartes en paires |
| Soumission | `{ "pairs": [["c1", "c3"], …] }` — paires trouvées par le joueur |
| Validation | même ensemble de paires (ordre indifférent, dans et entre les paires) |

> Par nature, les faces doivent être envoyées au navigateur pour être retournées : les paires « identiques »
> se devinent, et les paires « associées » (calcul ↔ résultat, pays ↔ capitale) relèvent du savoir.
> Le serveur reste seul juge de la validité des paires soumises. L'interface doit afficher les cartes
> face cachée et dans un ordre mélangé.

```json
{ "prompt": "Associe chaque calcul à son résultat.", "previewSeconds": 4,
  "cards": [{ "id": "c1", "face": "2 + 3" }, { "id": "c2", "face": "6" }, { "id": "c3", "face": "5" }, { "id": "c4", "face": "3 + 3" }] }
{ "pairs": [["c1", "c3"], ["c2", "c4"]] }
```

## 9. `SequenceCompletion` — Compléter une suite

| | Format |
|---|---|
| Content | `{ "prompt", "sequence": ["2", "4", null, "8"], "options"? }` — `null` = case à compléter (au moins une) ; `options` = choix proposés (sinon saisie libre) |
| CorrectAnswer | `{ "values": ["6"] }` — une valeur par `null`, dans l'ordre |
| Soumission | `{ "values": ["6"] }` |
| Validation | valeurs égales une à une (après `trim`) |

```json
{ "prompt": "Complète les deux nombres manquants.", "sequence": ["2", "3", "5", "7", null, "13", null] }
{ "values": ["11", "17"] }
```

## 10. `LogicPuzzle` — Énigme

Énoncé plus long, éventuellement accompagné de pseudo-code. Le format de réponse dépend de `answerType`.

| | Format |
|---|---|
| Content | `{ "prompt", "code"?, "visual"?, "unit"?, "answerType": "choice" \| "number" \| "text", "options"? }` — `code` = pseudo-code multiligne affiché en police à chasse fixe ; `options` requis si `choice` |
| CorrectAnswer | `choice` → `{ "optionId" }` ; `number` → `{ "value", "tolerance" }` ; `text` → `{ "acceptedAnswers" }` |
| Soumission | comme `MultipleChoice`, `NumericAnswer` ou `TextAnswer` selon `answerType` |
| Validation | idem au type correspondant |

```json
{ "prompt": "Que vaut s à la fin de ce programme ?", "code": "s ← 0\npour i de 1 à 5 :\n    s ← s + i", "answerType": "number" }
{ "value": 15, "tolerance": 0 }
```

## 11. `Maze` — Labyrinthe

| | Format |
|---|---|
| Content | `{ "prompt", "grid": ["S.#.", "#..#", …] }` — grille rectangulaire : `S` départ, `E` sortie, `#` mur, `.` passage (exactement un `S` et un `E`) |
| CorrectAnswer | `{ "shortestPathLength": 6 }` — longueur du plus court chemin (pour le score, vérifiée par BFS dans les tests) |
| Soumission | `{ "moves": "RDDDRR" }` — `U`, `D`, `L`, `R` (un tableau `["R","D",…]` est accepté aussi) |
| Validation | le serveur rejoue les déplacements depuis `S` : aucun mur ni sortie de grille, et la dernière case est `E`. Tout chemin valide est accepté ; un chemin optimal peut rapporter un bonus. |

> La grille est publique (il faut bien l'afficher) : la difficulté vient de la recherche du chemin et du temps limite.

```json
{ "prompt": "Aide l'explorateur à aller de S jusqu'à la sortie E.", "grid": ["S.#.", "#..#", "#.#.", "#..E"] }
{ "shortestPathLength": 6 }
```

## 12. `OrderingClassification` — Ranger ou classer

Deux modes selon `mode`.

| | Format |
|---|---|
| Content (`order`) | `{ "prompt", "mode": "order", "items": [{ "id", "text" }] }` — items affichés dans un ordre qui n'est **pas** la solution |
| CorrectAnswer (`order`) | `{ "order": ["i2", "i4", "i1", "i3"] }` |
| Soumission (`order`) | `{ "order": [ … ] }` — séquence identique attendue |
| Content (`classify`) | `{ "prompt", "mode": "classify", "items": [{ "id", "text" }], "groups": [{ "id", "text" }] }` |
| CorrectAnswer (`classify`) | `{ "mapping": { "itemId": "groupId", … } }` |
| Soumission (`classify`) | `{ "mapping": { … } }` — validation identique à `DragAndDrop` |

```json
{ "prompt": "Range ces planètes de la plus proche à la plus éloignée du Soleil.", "mode": "order",
  "items": [{ "id": "i1", "text": "Mars" }, { "id": "i2", "text": "Mercure" }, { "id": "i3", "text": "Terre" }, { "id": "i4", "text": "Vénus" }] }
{ "order": ["i2", "i4", "i3", "i1"] }
```

## 13. `MathMiniGame` — Série de calculs

| | Format |
|---|---|
| Content | `{ "prompt", "problems": [{ "id", "expression" }] }` — opérateurs `+ - × ÷` et parenthèses, priorités usuelles |
| CorrectAnswer | `{ "answers": { "p1": 15, "p2": 7 } }` |
| Soumission | `{ "answers": { "p1": 15, "p2": 7 } }` |
| Validation | **toutes** les réponses doivent être exactes (le défi compte comme une seule réponse) |

```json
{ "prompt": "Résous les 3 calculs.", "problems": [{ "id": "p1", "expression": "7 + 8" }, { "id": "p2", "expression": "12 - 5" }, { "id": "p3", "expression": "3 × 4" }] }
{ "answers": { "p1": 15, "p2": 7, "p3": 12 } }
```

## 14. `VisualChallenge` — Toucher les bons éléments

Grille d'éléments (emojis ou mots) à sélectionner : trouver l'intrus, toutes les cases qui respectent une règle…

| | Format |
|---|---|
| Content | `{ "prompt", "selection": "single" \| "multiple", "cells": [{ "id", "text" }] }` |
| CorrectAnswer | `{ "selectedIds": ["v1", "v3"] }` — exactement un id si `single` |
| Soumission | `{ "selectedIds": [ … ] }` |
| Validation | ensembles égaux (ordre indifférent) |

```json
{ "prompt": "Touche tous les insectes.", "selection": "multiple",
  "cells": [{ "id": "v1", "text": "🐜 Fourmi" }, { "id": "v2", "text": "🕷️ Araignée" }, { "id": "v3", "text": "🐝 Abeille" }] }
{ "selectedIds": ["v1", "v3"] }
```

---

## Ajouter un nouveau type

1. Ajouter la valeur à `ChallengeType` (Domain) et son temps limite dans `GameSettings:DefaultTimeLimitsSeconds`.
2. Documenter son format ici.
3. Ajouter une classe `IAnswerValidator` dans `Application/Answers/Validators` : elle est découverte automatiquement
   (aucune autre modification ; un test vérifie que chaque type a exactement un validateur).
4. Ajouter la règle de format dans `ChallengeFormatRules` (tests) et le composant d'interface (phase 4).
