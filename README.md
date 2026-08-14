# Program Designer API

A REST API for defining and validating education program structures (steps, groups, prerequisites), built for the octo education Full Stack Developer coding challenge.

## Tech Stack

- .NET 8 (C#), ASP.NET Core Web API
- xUnit for testing
- In-memory storage (no database)

## Setup Instructions

**Requirements:** [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

```bash
git clone https://github.com/hazem-eltahan/program-designer-api
cd program-designer-api
dotnet build
dotnet run --project ProgramDesigner.Api
```

The console output will show the URL the API is listening on (e.g. `http://localhost:5250` — the exact port varies by machine). Open `<that-url>/swagger` in a browser to explore and test the endpoints interactively.

To run the tests:

```bash
dotnet test
```

## Data Model

The core building blocks — steps and groups — are modeled using a **composite pattern**, so the same tree structure can represent a program of any shape and any nesting depth:

```
ProgramNode (abstract)
├── Id, Name, PrerequisiteId
├── Step : ProgramNode
│   └── StepType
└── Group : ProgramNode
    ├── Rule (InOrder | Choice)
    ├── ChoiceCount (only used when Rule == Choice)
    └── Children (List<ProgramNode>)
```

A **program** is just the top-level `Group` — there's no separate "Program" wrapper class, since a `Group` already has everything a program needs (a name, an id, and an ordered/choice-based set of children).

### Why a separate DTO layer

The API's JSON request/response shape is **not** the same as the internal domain model. Clients build a program in one request, meaning nested nodes need to reference each other (e.g. a step's prerequisite pointing at a group defined earlier in the same payload) before the server has assigned any real IDs.

To solve this, incoming JSON uses a parallel set of DTOs (`ProgramNodeDto` / `StepDto` / `GroupDto`) where:
- `RefId` (string) is a **client-chosen temporary label**, unique only within that one request
- `PrerequisiteRef` (string, optional) references another node's `RefId` within the same payload

On `POST /programs`, a `ProgramNodeConverter` does a two-pass conversion:
1. Walks the DTO tree once, generating a real `Guid` for every node and recording `RefId → Guid`
2. Walks it again, building real `Step`/`Group` domain objects, resolving every `PrerequisiteRef` to its corresponding real `Guid`

This keeps the "real" domain model clean (no leftover client-side labels) while still letting clients express internal cross-references in a single request.

### Polymorphic JSON

Since `Children` is a `List<ProgramNode>` (or `List<ProgramNodeDto>`) and the base type is abstract, both hierarchies use `System.Text.Json`'s built-in polymorphism support (`[JsonPolymorphic]` / `[JsonDerivedType]`) with a `"type": "Step" | "Group"` discriminator field in the JSON, so a mixed list of steps and groups deserializes into the correct concrete C# types automatically.

### Storage

Storage is a single in-memory `Dictionary<Guid, Group>`, registered as a **singleton** service (`IProgramStore` / `InMemoryProgramStore`) so it persists across requests. Only whole program roots are stored as dictionary entries — nested nodes live only inside their parent's `Children` list, never as separate top-level entries — so there's never ambiguity about whether a given `Guid` refers to a program root vs. some arbitrary nested node.

## API

### `POST /programs`

Creates a program from a JSON tree of groups/steps. All `Id`s are generated server-side; the client references nodes within the same request using string `refId` / `prerequisiteRef` fields (see Data Model above). Returns the full created program, including all resolved server-generated IDs.

### `GET /programs/:id`

Returns the full program structure for the given id, or `404` if it doesn't exist.

### `POST /programs/:id/validate`

Runs validation on the stored program and returns:

```json
{
  "isValid": true,
  "impossiblePrerequisites": [
    { "code": "IMPOSSIBLE_PREREQUISITE", "description": "..." }
  ],
  "reachabilityWarnings": [
    { "code": "POTENTIALLY_UNREACHABLE", "description": "..." }
  ]
}
```

`isValid` reflects **impossible prerequisites only** — reachability warnings do not affect it, per the spec ("valid but dangerous").

## Validation Logic (Part 2)

Both checks are built on the same primitive: for any node, compute its **path from the root** (the list of ancestors from the top-level group down to that node).

**Impossible prerequisites** — for a node `X` with a prerequisite on `Y`, compute both nodes' root paths and find the index where the paths first diverge:
- If the paths never diverge (one is a prefix of the other) → `Y` is an ancestor or descendant of `X` → **rejected** (a node can't depend on itself or its own container, and can't depend on something it contains).
- Otherwise, at the divergence point, `Y` and `X` sit under a shared parent as siblings. If that parent is a `Choice` group, the two branches are mutually exclusive → **rejected**. If it's `InOrder`, the prerequisite is valid only if `Y`'s branch comes *before* `X`'s branch in the children list.

**Reachability warnings** — for the *target* of a prerequisite, walk its root path and check every ancestor along the way: if any ancestor is a `Choice` group, the branch leading to the target isn't guaranteed for every participant → **warning** (not a rejection, since the prerequisite itself is still structurally sound).

**Design note:** reachability is currently computed relative to the *whole program tree* from the root, not relative to the dependent node's own branch. In the Computer Science scenario, this means `AI Capstone`'s prerequisite on `Electives` is flagged as a warning, even though both nodes live inside the same `AI` branch — because `Electives` still sits beneath the `Major` choice group, which not every participant will enter. This was a deliberate choice given the spec's wording ("a prerequisite depends on something inside a choice group that the participant might never pick"), though a stricter, branch-relative interpretation is a reasonable alternative.

## Tests

`ProgramDesigner.Tests/ValidationServiceTests.cs` covers the four required scenarios:

- Full Computer Science scenario has no impossible prerequisites
- A direct prerequisite cycle is rejected
- A prerequisite depending on a choice branch generates a warning, not a rejection
- A self-referencing prerequisite is rejected

## Design Decisions

- **`Choice` group's `N` (`ChoiceCount`) is validated at creation time** (`1 ≤ N ≤ M`, where M is the number of children), rather than at validation time — treated as basic structural input validation rather than a program-design concern.
- **All IDs are server-generated.** Clients never supply their own `Guid`s; they reference nodes within a single request via a temporary `refId` string instead.
- **No bonus simulation endpoint** was implemented (out of scope for time spent on the required parts).

## AI Tool Usage

This project was built with the assistance of Claude (Anthropic), used as a pairing/tutoring partner throughout — for talking through the data model design (composite pattern, DTO/domain separation, polymorphic JSON), reasoning through the reachability algorithm before writing code, reviewing and debugging code, and drafting this README. Core design decisions and the final implementation were driven and written by me; Claude was used primarily to explain concepts, review my code, and catch bugs (e.g. `new Guid()` vs `Guid.NewGuid()`, missing `return` paths, incorrect loop conditions) rather than to generate the solution wholesale.
