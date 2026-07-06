# Emergence Engineering Lab

Emergence Engineering Lab is a research MVP for observing how multi-agent organizations form under different goals, cultural constraints, KPIs, trust dynamics, and agent diversity settings. It now supports both single simulation runs and repeated experiments built from the same condition set.

The UI is mainly written for Japanese research workflows, while model names, database names, class names, routes, and API contracts remain in English.

## Purpose

This app is meant to observe organization formation as a dynamic system, not only as isolated LLM turns.

- Create a simulation with purpose, boundary conditions, KPI, agent count, and step count
- Run step-by-step agent conversations and actions
- Persist messages, memory updates, trust changes, phase history, and final aggregate metrics
- Repeat the same condition set through experiments and compare outcomes
- Inspect logs, phase timelines, trust summaries, trust deltas, trust networks, and experiment-level aggregates

## Stack

- ASP.NET Core 8 Razor Pages
- Minimal Web API endpoints
- SQL Server
- Entity Framework Core
- `Microsoft.EntityFrameworkCore.SqlServer`
- Swappable LLM service through `ILlmService`

## Run

```powershell
dotnet restore
dotnet run
```

Open the URL printed by ASP.NET Core. The SQL Server database is created automatically from EF Core migrations at startup.

If you are using an existing SQL Server database that was created before the latest schema changes, apply the pending EF Core migrations first:

```powershell
dotnet ef database update
```

This is required for newly added columns such as the Knowledge / Shock / Rewiring fields, the Challenge Event fields, and the Serendipity fields on `Experiments`, `Scenarios`, and `SimulationProjects`. If this step is skipped, SQL Server will return errors such as `Invalid column name`.

## Database connection

The app reads the SQL Server connection string from `ORG_SIM_CONNECTION_STRING`.

```powershell
$env:ORG_SIM_CONNECTION_STRING="Server=localhost\\SQLEXPRESS;Database=OrgSimDb;Trusted_Connection=True;TrustServerCertificate=True;"
dotnet run
```

If `ORG_SIM_CONNECTION_STRING` is not set, the app falls back to:

1. `Server=(localdb)\\MSSQLLocalDB;Database=OrgSimDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True`
2. `Server=.\\SQLEXPRESS;Database=OrgSimDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True`

## OpenAI environment variables

API keys and database connection strings are not stored in `appsettings.json`.

```powershell
$env:ORGSIM_OPENAI_API_KEY="sk-..."
dotnet run
```

This app uses `ORGSIM_OPENAI_API_KEY`.

It does not read `OPENAI_API_KEY`.

If a simulation or experiment is configured with `LlmProvider = OpenAI` but `ORGSIM_OPENAI_API_KEY` is not set, the runner falls back to `MockLlmService` and records that fallback in `AgentAction.RawLlmResponse`.

The mock preserves the same JSON response shape as the OpenAI-backed implementation:

```json
{
  "message": "...",
  "memory": "...",
  "action": "...",
  "targetAgentName": "..."
}
```

The mock does not return purely random actions anymore. It biases output from:

- Boundary conditions
- KPI definition
- Agent personality
- Agent orientation
- Boundary condition parameters

## LLM provider selection

Both `SimulationProject` and `Experiment` store:

- `LlmProvider`
- `LlmModel`

This makes it possible to distinguish whether a run used `Mock` or `OpenAI`, and which model name was selected for that condition.

The app no longer switches to OpenAI automatically based only on environment variables.

### Provider meaning

- `Mock`: fast, free, and suitable for large-scale exploratory runs
- `OpenAI`: higher fidelity, but slower and cost-bearing

### Recommended operation

1. use `Mock` for large-scale checks
2. use `OpenAI` for smaller validation runs
3. re-run only important conditions with `OpenAI`
4. use `Mock` for broad parameter sweeps first
5. validate narrow, high-value sweep regions with `OpenAI`

## Main concepts

### Scenario

A `Scenario` is a reusable experiment-condition template.

It stores:

- `Purpose`
- `BoundaryConditions`
- `KpiDefinition`
- `AgentCount`
- `TotalSteps`
- `RunCount`
- `LlmProvider`
- `LlmModel`
- boundary condition parameters

It is meant for reusing the same condition design across multiple experiments.

When an experiment is created from a scenario, the condition values are copied into the experiment so later scenario edits do not rewrite past experiment conditions.

Scenario pages also support copying an existing scenario into a new draft:

- Scenario list and details pages expose a `コピー` action
- `/Scenarios/Create?sourceScenarioId={id}` opens Create with the source values prefilled
- the copied scenario gets a name suffix such as ` - コピー`
- duplicate copy names are auto-numbered as ` - コピー 2`, ` - コピー 3`, and so on
- the copy is saved as a brand-new `Scenario`; IDs, timestamps, experiments, simulations, and run history are not copied

Create pages for `Scenario`, `Experiment`, and `Simulation` also expose a research preset picker. It applies standardized condition sets in the browser and can be combined with scenario-copy mode.

`External Shock` and `Challenge Event` are optional settings. When they are disabled, the related fields are grayed out in the UI and normalized on save to `None`, `0`, or an empty string. Their description fields are optional, so research presets can leave them blank.

Available presets:

- 標準市場学習型
- 低信頼・学習型
- 選択的信頼・創発型
- 高信頼・仲良し停滞型
- 独裁・サイロ型
- OSS・レビュー型
- 危機対応・適応型

### SimulationProject

A `SimulationProject` is one concrete run of an organization simulation. Standalone simulations still work without any experiment linkage.

It also stores:

- `LlmProvider`
- `LlmModel`
- boundary condition parameters

### Experiment

An `Experiment` is a shared condition definition used to produce repeated runs:

- `Purpose`
- `BoundaryConditions`
- `KpiDefinition`
- `AgentCount`
- `TotalSteps`
- `RunCount`

The meaning is: "run this same setup multiple times and compare what emerges."

It also stores the intended LLM condition for generated runs:

- `LlmProvider`
- `LlmModel`
- boundary condition parameters

If it was created from a scenario, the experiment also keeps `ScenarioId` as a reference to the source template.

### ExperimentRun

An `ExperimentRun` is one finished execution of an `Experiment`. It stores:

- Final phase
- Average trust
- Completed steps
- Network density
- Effective network density
- Strong link count
- Weak link count
- Component count
- Isolated agent count
- Hub agent and hub score
- ShareInfo rate
- ProposeIdea rate
- Criticize/Support ratio
- Steps to Learning
- Steps to Emergent
- Phase change count
- Phase stability

Re-running the same experiment appends new runs using `max(RunNo) + 1`.

### ParameterSweep

A `ParameterSweep` is a one-parameter sensitivity-analysis batch built from a base `Scenario`.

The intended workflow is:

- keep the scenario fixed
- change only one boundary condition parameter
- run multiple experiments for each parameter value
- compare phase distribution and trust outcomes

Stored fields include:

- `ScenarioId`
- `TargetParameter`
- `StartValue`
- `EndValue`
- `StepValue`
- `RunCountPerValue`
- `AgentCount`
- `TotalSteps`
- `LlmProvider`
- `LlmModel`
- `Status`

`TargetParameter` is one of:

- `InformationSharingLevel`
- `CooperationLevel`
- `CompetitionLevel`
- `PsychologicalSafetyLevel`
- `LearningOrientationLevel`
- `CustomerOrientationLevel`
- `ShortTermResultPressureLevel`
- `EffectiveTrustThreshold`
- `KnowledgeStock`
- `KnowledgeDiversity`
- `ExternalShockLevel`
- `CrossDomainExposure`
- `RewiringSensitivity`
- `ExplorationTendency`
- `SerendipitySensitivity`
- `KnowledgeRecombinationRate`
- `SerendipityThreshold`
- `TrustGrowthRate`
- `TrustDecayRate`
- `TrustSaturationStrength`
- `TrustCapacity`
- `TrustCapacityPenalty`
- `DistrustPenalty`
- `ConstructiveCriticismBonus`
- `ChallengeLevel`
- `RequiredKnowledgeDiversity`
- `RequiredCrossDomainExposure`
- `RequiredRewiringScore`

### PhaseDiagram

A `PhaseDiagram` is a two-parameter exploration feature for mapping phase regions in a 2D condition space.

It stores:

- `BaseScenarioId`
- `XParameterName`, `XStartValue`, `XEndValue`, `XStepValue`
- `YParameterName`, `YStartValue`, `YEndValue`, `YStepValue`
- `RunsPerPoint`
- `AgentCount`
- `TotalSteps`
- `LlmProvider`
- `LlmModel`
- `Status`

Each grid point is persisted as a `PhaseDiagramPoint` and summarizes:

- `DominantPhase`
- `EmergentRate`
- `AverageTrust`
- `AverageEffectiveDensity`
- `AverageKnowledgeDiversity`
- `AverageKnowledgeRecombinationScore`
- `AverageKnowledgeReconfigurationScore`
- `AverageSerendipityRate`
- `AveragePipelineCompletionScore`
- `DominantBottleneck`

The Phase Diagram UI shows:

- Dominant phase heatmap with phase-colored cells
- Emergent rate heatmap with green intensity
- Pipeline completion heatmap with blue intensity
- Bottleneck map with bottleneck-specific colors
- phase boundary candidates detected from neighboring grid cells
- simple emergent-region and pre-emergent-region extraction
- point-by-point result table with links to the generated experiments
- Adaptive Sweep buttons for boundary, high-pipeline, and emergent regions

Recommended use:

- use Fast execution scale first
- keep the grid at 100 points or fewer
- explore a broad region with `Mock`
- then narrow around interesting coordinates and re-run with finer steps or `OpenAI`

Recommended presets:

- `TrustGrowthRate × KnowledgeDiversity`
- `EffectiveTrustThreshold × SerendipityThreshold`
- `CrossDomainExposure × RewiringSensitivity`
- `Colloid Stability Template`

This feature is implemented as a research MVP for organizational emergence, but the same 2D phase-map structure can later be reused for colloids, crystallization, supercooling, gelation, and other systems with phase transitions.

Adaptive Sweep takes an existing phase diagram as the parent and automatically creates a child diagram that re-explores a narrower region around a phase boundary, a high-pipeline region, or an emergent region. This supports the research workflow:

- coarse sweep
- boundary detection
- fine sweep around the interesting region
- more precise phase diagram

The adaptive child keeps the same base scenario, X/Y parameter names, LLM settings, agent count, and total steps, but narrows the start/end range and halves the step size, while keeping the grid at 100 points or fewer.

Each parameter value produces one `Experiment`, and the sweep stores a compact result summary in `ParameterSweepRun`.

Sweep status is recalculated from stored data when you open the sweep list or details page. If all parameter values have finished, all related experiments are completed, and the run counts are sufficient, the sweep becomes `Completed`. If an error occurs during execution, the sweep is marked `Failed`. A stale `Running` status can therefore be corrected by reopening the sweep pages, and once a sweep reaches `Completed` it stays `Completed` instead of being rewritten back to `Running`.

When you start a Parameter Sweep from the UI, the sweep status is updated to `Running` immediately before execution continues, so the details page can show that the job has already started. While it is running, the details page shows a short in-progress message and the execute button is replaced by a visible running state.

Use `TrustGrowthRate` when you want to inspect the trust-to-emergence curve directly. A typical sweep is:

- Parameter: `TrustGrowthRate`
- Start: `0.20`
- End: `0.80`
- Step: `0.10`
- Runs per value: `20`

The sweep UI accepts `0.001` increments for start, end, and step values so you can test fine-grained trust-dynamics settings such as `TrustDecayRate = 0.005`.

The parameter sweep details page now shows:

- average trust and effective density
- strong / weak link counts
- emergent / stable / learning / silo rates
- serendipity rate and serendipity-to-emergence link rate
- signed deltas between neighboring parameter values
- knowledge diversity and knowledge recombination curves
- emergent unmet reasons, such as trust shortage, density shortage, or missing knowledge recombination

This makes it easier to inspect trust-emergence curves, especially when sweeping `TrustGrowthRate`.

The parameter sweep details page now also shows:

- Phase score comparison for the final step
- `Emergent` unmet reason ranking
- differences in `EmergentScore`, `StableScore`, and `LearningScore`

This lets you see not only where trust and density rise, but also why a run still remains in `Learning` instead of moving to `Emergent`.

## Execution Scale Presets

The create screens for Scenario, Experiment, Simulation, and Parameter Sweep include an execution scale preset panel. It only adjusts `AgentCount`, `TotalSteps`, and run-count style fields such as `RunCount` or `RunCountPerValue`. It does not touch organizational parameters, trust dynamics, or other research condition settings.

Available presets:

- Fast: `10` agents / `50` steps / `5` runs
- Standard: `20` agents / `100` steps / `10` runs
- Research: `30` agents / `200` steps / `20` runs
- Publication: `50` agents / `500` steps / `50` runs

`Publication` is intentionally heavy and should be used with care. For Parameter Sweep, `Fast` or `Standard` is recommended.

Storage guidance:

- `Full` 保存: 単体 Simulation 向け
- `Summary` 保存: 通常 Experiment 向け
- `Minimal` 保存: Parameter Sweep 向け

SQL Server Express / LocalDB は DB 容量が限られるため、`SimulationSteps`、`TrustSnapshots`、`AgentActions` は大きくなりやすいことに注意してください。`PRIMARY` filegroup full が出た場合は、古い実験データの削除、DB サイズ拡張、または保存モードの見直しが必要です。

Research hypothesis:

> Emergence may not rise as a simple monotonic increase in average trust.  
> It may appear when trust-network density, serendipity, and knowledge recombination enter a particular region together.

Phase judgement is ordered so that `Collapse`, `Chaos`, `Silo`, and `Adaptation` are checked before `Emergent`, `Stable`, `Learning`, and `Forming`. `Learning` means sharing and learning are progressing, but the run has not yet reached a structural change. `Stable` means trust and density are high, but recombination and serendipity are still weak. `Emergent` requires both network conditions and knowledge-rewiring signals to be present.

The simulation details page also shows:

- `Phase Score` timeline
- `Emergent` criteria breakdown for the final step
- a Japanese phase-decision reason derived from the current state

Research hypothesis:

> When a run does not become `Emergent`, the reason can usually be decomposed into missing trust, missing effective density, missing knowledge recombination, missing serendipity, or weak constructive criticism rather than a single "trust shortage" alone.

### SimulationMetrics

`SimulationMetrics` stores the final aggregate metrics for one `SimulationProject`.

This is the main research summary for a single run. `ExperimentRun` copies the same metrics so experiment pages can compare runs without recalculating them every time.

## Why repeated runs matter

Even under the same condition set, agent interaction trajectories can diverge. Repeating the same experiment helps answer:

- Does the final phase converge consistently
- Does trust usually accumulate or decay
- Are stable and unstable outcomes mixed under the same conditions
- Do certain boundary conditions create fragile or resilient trajectories

## Main screens

- Simulation list: `/`
- New simulation: `/Simulations/Create`
- Simulation details: `/Simulations/Details/{id}`
- Scenario list: `/Scenarios`
- New scenario: `/Scenarios/Create`
- Scenario details: `/Scenarios/Details/{id}`
- Parameter sweep list: `/ParameterSweeps`
- New parameter sweep: `/ParameterSweeps/Create`
- Parameter sweep details: `/ParameterSweeps/Details/{id}`
- Phase diagram list: `/PhaseDiagrams`
- New phase diagram: `/PhaseDiagrams/Create`
- Phase diagram details: `/PhaseDiagrams/Details/{id}`
- Experiment list: `/Experiments`
- New experiment: `/Experiments/Create`
- Experiment details: `/Experiments/Details/{id}`
- Experiment run trigger: `/Experiments/Run/{id}`

The simulation details page now includes:

- organization metrics
- phase timeline
- action timeline
- trust network visualization
- effective trust threshold sweep
- step slider
- replay controls
- agent trust summary
- trust change log

## Stop and delete operations

### Experiment stop

`Experiment` now has a `Status` field:

- `Created`
- `Running`
- `StopRequested`
- `Stopped`
- `Completed`

The stop button on experiment details does not interrupt the currently running simulation in the middle of `RunAllAsync`.

Instead, stop is handled at the experiment run loop boundary:

- the current run is allowed to finish
- before the next run starts, the app reloads `Experiment.Status`
- if it is `StopRequested`, the loop ends and the experiment becomes `Stopped`

This is intentionally simple for the current MVP.

### Experiment delete

Deleting an experiment removes:

- the `Experiment`
- related `ExperimentRuns`
- related `SimulationProjects`
- and simulation child data such as `Agents`, `AgentActions`, `SimulationSteps`, `TrustSnapshots`, and `SimulationMetrics`

The delete confirmation page shows:

- experiment name
- run count
- related simulation count
- agent action count
- trust snapshot count

Experiment list pages also support bulk deletion. You can select multiple `Experiment` rows and remove them together with their related `ExperimentRuns`, `SimulationProjects`, `SimulationSteps`, `AgentActions`, `TrustSnapshots`, `SimulationMetrics`, and `ParameterSweepRuns`.

`Scenario` rows are never deleted by experiment cleanup. If an experiment is still running, it is excluded from bulk deletion and the UI shows that running experiments cannot be deleted.

### Run delete

Experiment details also supports deleting a single run.

Deleting one run removes:

- the selected `ExperimentRun`
- its linked `SimulationProject`
- and that simulation's dependent data

This is useful when an experiment mostly succeeded but one run should be excluded from comparison.

### Simulation delete

Standalone simulations and experiment-generated simulations can be deleted from the simulation list or simulation details page.

Deleting a simulation removes:

- the `SimulationProject`
- related `Agents`
- related `AgentActions`
- related `SimulationSteps`
- related `TrustSnapshots`
- related `SimulationMetrics`
- any linked `ExperimentRun` rows

### Recommended operation for accidental execution

If an experiment was started by mistake:

1. open the experiment details page
2. press the stop button
3. wait for the currently running run to finish
4. delete unnecessary runs or the whole experiment

This keeps the current MVP simple while still allowing cleanup of accidental data.

## API endpoints

- `GET /api/simulations`
- `GET /api/simulations/{id}`
- `POST /api/simulations`
- `POST /api/simulations/{id}/run-one`
- `POST /api/simulations/{id}/run-all`

## Data model

- `Scenarios`: reusable experiment-condition templates with LLM settings, numeric boundary parameters, knowledge / challenge / serendipity settings, and `EffectiveTrustThreshold`
- `ParameterSweeps`: one-parameter sensitivity analysis batches built from a base scenario
- `ParameterSweepRuns`: one result summary per tested parameter value
- `SimulationProjects`: simulation definition, status, progress, current phase, optional experiment linkage, `LlmProvider`, `LlmModel`, copied boundary parameters, knowledge / challenge / serendipity settings, and `EffectiveTrustThreshold`
- `Agents`: generated agents with role, memory, trust JSON, position, personality, and orientation
- `SimulationSteps`: step-level state snapshots with persisted phase history
- `AgentActions`: per-agent message, action, target, memory, raw LLM response, and trust delta log
- `TrustSnapshots`: step-level trust network state for every directed agent pair
- `SimulationMetrics`: final aggregate metrics for one simulation
- `Experiments`: shared condition definitions for repeated runs, optional `ScenarioId`, `LlmProvider`, `LlmModel`, copied boundary parameters, knowledge / challenge / serendipity settings, and `EffectiveTrustThreshold`
- `ExperimentRuns`: one completed run result per experiment execution

## Agent diversity

### Personality

Each agent gets one of the following personalities through auto-rotation:

- `Conservative`
- `Challenger`
- `Coordinator`
- `Critic`
- `Supporter`
- `Analyst`

Meaning:

- `Conservative`: prefers safe and proven actions
- `Challenger`: proposes new ideas and challenges assumptions
- `Coordinator`: aligns discussion and connects agents
- `Critic`: surfaces risks and weak assumptions constructively
- `Supporter`: supports others and maintains trust
- `Analyst`: organizes information and evaluates evidence

### Orientation

Each agent also gets one of the following orientations through auto-rotation:

- `CustomerFocused`
- `FieldFocused`
- `QualityFocused`
- `SpeedFocused`
- `CostFocused`
- `LearningFocused`

Meaning:

- `CustomerFocused`: prioritize customer success and market response
- `FieldFocused`: prioritize practical operation and field constraints
- `QualityFocused`: prioritize reliability and reproducibility
- `SpeedFocused`: prioritize fast execution and decision speed
- `CostFocused`: prioritize efficiency and resource constraints
- `LearningFocused`: prioritize feedback loops and organizational learning

### Why diversity is included

Without diversity, all agents tend to collapse into one generic behavior profile. Adding personality and orientation makes it easier to observe:

- Whether certain teams become more collaborative or more siloed
- Whether learning-focused agents stabilize trust
- Whether challenger or critic heavy teams move faster but destabilize

For now, diversity is assigned automatically by rotation. A future step is to let experiments control the composition ratio directly.

## Prompt design

Each agent prompt includes:

- Simulation purpose
- Boundary conditions
- Boundary condition parameters
- KPI
- Agent name, role, memory
- Agent personality
- Agent orientation
- Short personality guidance
- Short orientation guidance
- Other agent names
- Recent messages from the previous step
- Allowed actions
- A strict instruction to return JSON only

## Boundary condition parameters

In addition to free-text boundary conditions, the app now stores numeric boundary parameters in `Scenario`, `Experiment`, and `SimulationProject`.

- `InformationSharingLevel`
- `CooperationLevel`
- `CompetitionLevel`
- `PsychologicalSafetyLevel`
- `LearningOrientationLevel`
- `CustomerOrientationLevel`
- `ShortTermResultPressureLevel`

Meaning:

- `InformationSharingLevel`: how strongly information sharing is encouraged
- `CooperationLevel`: how strongly mutual support and coordination are encouraged
- `CompetitionLevel`: how strongly individual competition is encouraged
- `PsychologicalSafetyLevel`: how safe it is to criticize, ask for help, and expose failure
- `LearningOrientationLevel`: how strongly feedback loops and organizational learning are prioritized
- `CustomerOrientationLevel`: how strongly customer success and market response are prioritized
- `ShortTermResultPressureLevel`: how strongly short-term results and speed pressure dominate
- `EffectiveTrustThreshold`: the minimum positive trust level treated as an effective network connection

Why they are stored in three places:

- `Scenario`: reusable template definition
- `Experiment`: copied repeated-run condition set
- `SimulationProject`: copied one-run execution condition

Current defaults for existing data are:

- boundary parameters: `0.5`
- `EffectiveTrustThreshold`: `0.30`

The mock LLM uses these parameters as a simple heuristic weighting input. This is intentionally lightweight rather than calibrated.

Why the threshold exists:

- research often cares less about whether trust exists at all
- and more about whether trust is strong enough to function as an organizational connection
- in emergence engineering, this supports the hypothesis that sufficiently strong trust, not merely positive trust, drives coordination, silo collapse, and emergence

Psychological safety is not just a passive boundary parameter in this model. It now also shifts action selection and downstream knowledge metrics:

- high psychological safety increases `ShareInfo`, `AskHelp`, `ProposeIdea`, and constructive criticism
- low psychological safety increases `WorkAlone`, `Wait`, and destructive criticism
- the direct emergence bonus only appears when psychological safety combines with knowledge recombination or serendipity
- `PsychologicalSafetyActionEffect`, `ConstructiveCriticismRate`, `DestructiveCriticismRate`, `PsychologicalSafetyRecombinationBonus`, `PsychologicalSafetySerendipityBonus`, and `PsychologicalSafetyEmergenceBonus` are stored in step state JSON for analysis

## Parameter sweep output

`ParameterSweepRun` stores a per-parameter-value summary:

- `FinalPhaseSummaryJson`
- `AverageTrust`
- `AverageAbsTrust`
- source `ExperimentId`

This supports quick comparisons such as:

- which parameter range produces more `Emergent`
- whether trust magnitude rises or falls as one parameter changes
- whether the same condition produces stable or mixed phase distributions
- why a sweep still failed to reach `Emergent`, including `PsychologicalSafety` and `ConstructiveCriticism` insufficiency

The details page currently shows:

- phase distribution comparison table
- average trust comparison
- average absolute trust comparison
- a simple chart for `AverageAbsTrust`
- a simple chart for `Emergent` count

## TrustJson

`Agents.TrustJson` is a simple JSON map from target agent name to trust score.

Example:

```json
{
  "Agent 1": 0.0,
  "Agent 2": 0.3,
  "Agent 3": -0.1
}
```

Each value is clamped to `-1.0` to `1.0`.

`TrustJson` represents the current simulation-wide trust state at the agent level.

## Trust update rules

- `SupportOther`: actor to target `+0.10`, target to actor `+0.15`
- `AskHelp`: actor to target `+0.05`, target to actor `+0.05`
- `ShareInfo`: actor to all other agents `+0.03`, all other agents to actor `+0.03`
- `ProposeIdea`: actor to all other agents `+0.02`
- `Criticize`: actor to target `-0.05`, target to actor `-0.10`
- `Criticize` under psychologically safe or constructive-criticism conditions: actor to target `+0.02`, target to actor `+0.02`
- `WorkAlone`: no trust update
- `Wait`: no trust update

If `TargetAgentName` is empty or `-`, then `ShareInfo` and `ProposeIdea` still apply to all other agents, while `SupportOther`, `AskHelp`, and `Criticize` skip trust updates.

## AgentActions trust columns

`AgentActions` stores action-level trust movement through nullable columns:

- `TrustBefore`
- `TrustDelta`
- `TrustAfter`

Meaning:

- `TrustBefore`: representative trust value before the action
- `TrustDelta`: actual change after clamp, not the raw rule delta
- `TrustAfter`: representative trust value after the action

These columns are nullable so older rows remain valid.

`TrustJson` is the current state. `AgentActions.TrustBefore/TrustDelta/TrustAfter` is the step-by-step trust movement log.

If future work needs explicit source/target trust events, reasons, weights, or agent-pair level histories, this can be separated into a dedicated `TrustEvents` table.

## TrustSnapshots

`TrustSnapshots` stores the full directed trust network at the end of each step.

Each row represents:

- one simulation
- one step
- one source agent
- one target agent
- the trust value from source to target at the end of that step

For `N` agents, each step stores `N * (N - 1)` rows.

This is different from `AgentActions.TrustBefore/TrustDelta/TrustAfter`:

- `AgentActions.*Trust*` is a local action-level log
- `TrustSnapshots` is the full global network state at step end

Together they support both:

- micro analysis of which action changed trust
- macro analysis of what the whole network looked like after the step

`TrustSnapshots` also enables threshold-based network analysis such as:

- effective network density
- strong vs weak link counts
- connected component count
- hub change under a stronger trust criterion

The app uses a shared classification rule for both metrics and SVG rendering:

- `Strong Link`: `TrustValue >= EffectiveTrustThreshold`
- `Weak Link`: `0.01 < TrustValue < EffectiveTrustThreshold`
- `No Link`: `abs(TrustValue) <= 0.01`
- `Negative Link`: `TrustValue < -0.01`

## Trust Network Visualization

The simulation details page includes a lightweight SVG-based trust network view.

- agents are shown as circular nodes
- links at or above `EffectiveTrustThreshold` are shown as strong blue edges
- positive links below the threshold are shown as weak gray edges
- links with `abs(TrustValue) <= 0.01` are hidden
- negative links may be shown as thin red edges
- stronger effective trust produces thicker lines
- the current hub node is visually emphasized
- the network can be inspected step by step with a slider
- simple replay controls advance the step automatically every 500ms

The network view is now based on persisted `TrustSnapshots` when they exist.

Hub emphasis in the network view is also based on threshold-qualified strong links, so the highlighted node reflects the effective network rather than every weak positive tie.

### Effective trust threshold sweep

The simulation details page also shows a threshold sweep over these values:

- `0.10`
- `0.20`
- `0.30`
- `0.40`
- `0.50`
- `0.60`
- `0.70`
- `0.80`
- `0.90`

For the final step trust state, the app recalculates:

- `EffectiveNetworkDensity`
- `StrongLinkCount`
- `WeakLinkCount`
- `ComponentCount`
- `IsolatedCount`

This makes it easier to see how the same organization looks under stricter or looser definitions of effective trust.

If `TrustSnapshots` are missing for older simulations, the sweep falls back to the final `Agents.TrustJson` state using the same metric definitions.

### Step slider

The step slider changes the displayed step without reloading the page.

For the selected step, the UI updates:

- displayed step number
- phase
- average trust
- hub agent
- isolated agent count

### Replay

Replay controls currently support:

- move to first step
- play
- stop
- move to last step

This is intended as a foundation for later dynamic animation work.

### Current limitation

Older simulations may not have `TrustSnapshots` yet.

For those older rows, the UI may fall back to the final `Agents.TrustJson` state so the network section still renders.

That means:

- new simulations have exact per-step trust state in the database
- old simulations may still display a fallback approximation
- if historical backfill is needed, an offline recomputation step may be added later

## Phase detection

The runner updates `SimulationProject.Phase` after each step with a lightweight heuristic over recent actions, usually the last 10 steps.

- `Forming`: early steps with too little history
- `Learning`: collaboration actions dominate
- `Emergent`: proposal, sharing, support, and critique coexist productively
- `Silo`: solo work dominates while sharing stays low
- `Chaos`: critique is high and support is low
- `Collapse`: waiting dominates and idea/sharing signals are weak
- `Stable`: actions are distributed without one extreme taking over

## Phase time series

Each `SimulationStep` stores the phase at that step so the UI can show the path to the current state, not only the current phase.

## Phase mapping

- `Forming = 0`
- `Learning = 1`
- `Stable = 2`
- `Emergent = 3`
- `Silo = -1`
- `Chaos = -2`
- `Collapse = -3`

## Metrics

### Why metrics were added

`FinalPhase` and `AverageTrust` alone are not enough to judge how the organization evolved. The metrics layer adds:

- Trust network structure
- Isolation and hub concentration
- Action composition
- Time to reach learning or emergence
- Phase volatility vs stability

### SimulationMetrics fields

- `FinalPhase`
- `AverageTrust`
- `NetworkDensity`
- `EffectiveNetworkDensity`
- `StrongLinkCount`
- `WeakLinkCount`
- `ComponentCount`
- `IsolatedAgentCount`
- `HubAgentName`
- `HubScore`
- `ShareInfoRate`
- `ProposeIdeaRate`
- `CriticizeSupportRatio`
- `StepsToEmergent`
- `StepsToLearning`
- `PhaseChangeCount`
- `PhaseStability`

### Metric definitions

#### FinalPhase

`SimulationProject.Phase`

#### AverageTrust

Average of all final numeric values found in `Agents.TrustJson`. Empty or invalid payloads are treated as `0`.

#### NetworkDensity

Treat `TrustJson` as a directed trust network.

- Directed edges are trust links where `trust > 0.01`
- Maximum directed edges is `n * (n - 1)`
- `NetworkDensity = directed edges with trust > 0.01 / max directed edges`
- If `n <= 1`, the value is `0`

#### EffectiveNetworkDensity

Treat only sufficiently strong positive trust as a real network edge.

- Effective edges are links where `trust >= EffectiveTrustThreshold`
- Maximum directed edges is `n * (n - 1)`
- `EffectiveNetworkDensity = strong directed edges / max directed edges`
- If `n <= 1`, the value is `0`

#### StrongLinkCount

Count of directed links where `trust >= EffectiveTrustThreshold`.

#### WeakLinkCount

Count of directed links where `0.01 < trust < EffectiveTrustThreshold`.

#### ComponentCount

Build an undirected graph from strong links only.

- use only strong links
- if `A -> B` is strong or `B -> A` is strong, then `A-B` is treated as connected
- convert them to undirected connectivity
- `ComponentCount` is the number of connected components

Interpretation:

- `1`: effectively connected network
- `2` or `3`: silo formation
- `4+`: network fragmentation or division
- if there are no strong links, then `ComponentCount = AgentCount`

#### IsolatedAgentCount

An agent is isolated when:

- it has degree `0` in the undirected strong-link graph
- if there are no strong links at all, then `IsolatedAgentCount = AgentCount`

#### HubAgentName / HubScore

For each agent, sum strong-link inbound and outbound absolute trust.

- `HubScore = sum(abs(trust))` over strong directed edges touching that agent
- highest strong-link score becomes `HubAgentName`
- if no strong links exist, the values are `-` and `0`

## Threshold sweep analysis

Threshold sweep asks a different question from the fixed-threshold dashboard:

- trust can exist without being strong enough for effective collaboration
- a network that looks connected at `0.10` may fragment at `0.50`
- some scenarios keep strong links across a wide threshold range, while others collapse quickly

This supports observation of:

- silo formation critical points
- network fragmentation
- robustness of strong trust links
- differences between open-source-like, hierarchical, and market-learning scenarios

## Impact Path and Sensitivity Ranking

Parameter Sweep details now include an `Impact Path` view and a `Sensitivity Ranking` table.

`Impact Path` shows how the selected sweep parameter propagates through:

- `AverageTrust`
- `EffectiveDensity`
- `StrongLinks`
- `AverageKnowledgeDiversity`
- `AverageKnowledgeRecombinationScore`
- `AverageKnowledgeReconfigurationScore`
- `SerendipityRate`
- `SerendipityToEmergenceRate`
- `AverageEmergentScore`
- `EmergentRate`

The page also highlights bottlenecks such as:

- trust-to-network conversion
- network-to-recombination conversion
- recombination-to-serendipity conversion
- serendipity-to-emergence-score conversion
- emergence-score-to-phase conversion

`Sensitivity Ranking` sorts the metrics by absolute change between the minimum and maximum parameter values. It helps answer:

- which metric reacted most strongly
- whether the parameter acts as a direct emergence lever
- whether the effect stops before knowledge recombination or serendipity

This is useful when a sweep changes trust and density but still does not move `EmergentRate`.

Research hypothesis: the parameters that matter for emergence are not the ones that move a single metric, but the ones that chain together trust, network structure, knowledge recombination, and serendipity.

## Emergence pipeline analysis

Simulation details and Parameter Sweep details now include `創発パイプライン分析`.

The pipeline is treated as:

- `Serendipity`
- `KnowledgeRecombination`
- `KnowledgeReconfiguration`
- `Learning`
- `Adaptation`
- `Emergence`

For each step, the app reads or reconstructs:

- `SerendipityScore`
- `KnowledgeRecombinationScore`
- `KnowledgeReconfigurationScore`
- `LearningScore`
- `AdaptationScore`
- `EmergentScore`

It then derives:

- `PipelineBottleneck`: the weakest stage in the six-step chain
- `PipelineBottleneckScore`: the score of that weakest stage
- `PipelineCompletionScore`: the average score across the whole pipeline

Interpretation:

- `Serendipity` is not emergence itself; it is an upstream trigger for knowledge recombination
- `PipelineBottleneck` shows where emergence is stopping as a process
- `PipelineCompletionScore` shows how far the overall emergence process has progressed

This is different from `Emergent未達理由`:

- `Emergent未達理由` is condition-based insufficiency analysis
- `創発パイプライン` is process-based blockage analysis

Research hypothesis:

> Emergence is not a single metric.  
> It appears when serendipity, knowledge recombination, knowledge reconfiguration, learning, and adaptation propagate as a chain and eventually produce a phase change.

Parameter Sweep details now also include a research dashboard layer:

- `実験サマリー`: top-level outcome summary, most common final phase, dominant bottleneck, and recommended interpretation
- `創発パイプライン概要`: horizontal-bar view of average progress from `Serendipity` to `Emergence`
- `感度ランキング`: which metrics reacted most strongly to the swept parameter

Recommended reading order for analysis:

- first check `実験サマリー`
- then review `感度ランキング`
- then open the detailed graphs and tables only where needed

#### ShareInfoRate

`ShareInfoCount / TotalAgentActions`

#### ProposeIdeaRate

`ProposeIdeaCount / TotalAgentActions`

#### CriticizeSupportRatio

`CriticizeCount / SupportOtherCount`

If `SupportOtherCount == 0`:

- if `CriticizeCount == 0`, the value is `0`
- otherwise the value is `CriticizeCount`

#### StepsToEmergent

First `SimulationStep.StepNo` where `Phase == Emergent`, otherwise `null`

#### StepsToLearning

First `SimulationStep.StepNo` where `Phase == Learning`, otherwise `null`

#### PhaseChangeCount

Count of adjacent step pairs where `Phase` changed

#### PhaseStability

Value between `0.0` and `1.0`

- `TotalTransitions = max(0, steps.Count - 1)`
- if `TotalTransitions == 0`, then `PhaseStability = 1.0`
- otherwise `PhaseStability = 1.0 - (PhaseChangeCount / TotalTransitions)`

## Experiment aggregates

### FinalPhase distribution

This shows how many runs ended in each final phase. It is a quick way to see whether the same condition consistently converges or splits into multiple outcomes.

### Phase transition counts

This aggregates adjacent phase moves across all runs.

Examples:

- frequent `Forming -> Learning` means runs often establish collaboration early
- frequent `Learning -> Stable` means learning tends to consolidate
- frequent `Stable -> Chaos` suggests fragility

### Trust average trend

This groups all `AgentActions` by `StepNo` and averages non-null `TrustDelta`.

It helps show whether trust tends to rise, flatten, or decay as runs proceed.

### Step trust state trend

Experiment details also aggregates `TrustSnapshots` by `StepNo`.

This shows:

- `AverageTrust`
- `AverageAbsTrust`
- `Count`

It is different from trust delta:

- trust delta asks how much trust changed because of actions
- trust state asks what the network trust level looked like at the end of the step

This makes it easier to inspect:

- hub transitions
- silo formation
- trust collapse
- network changes immediately before phase transitions
- recovery after shock injection

## Action Timeline

`Action Timeline` aggregates `AgentActions.Action` by step and shows the per-step action distribution as rates.

Tracked actions are:

- `ShareInfo`
- `AskHelp`
- `ProposeIdea`
- `Criticize`
- `WorkAlone`
- `SupportOther`
- `Wait`
- `Other` for unknown actions

This is useful as a middle layer between boundary conditions and phase:

- boundary conditions bias action selection
- action distributions shape trust and phase transitions
- phase labels summarize the larger organizational state

The simulation details page shows:

- a step-by-step action-rate table
- phase labels for each step
- highlighting when the phase changed from the previous step
- a Chart.js line chart for the major action rates

This makes it easier to inspect:

- action changes immediately before phase transitions
- information-sharing decline during silo formation
- the balance of proposal, support, and critique in `Emergent`
- behavior differences between centralized and open-source-like scenarios

## Phase Transition Inspector

The `Phase Transition Inspector` treats a phase transition as:

- a step `N`
- where `SimulationSteps.Phase` at step `N` differs from step `N - 1`

Step `1` is excluded because it has no previous step to compare against.

For each detected transition, the app compares:

- action-rate deltas between step `N - 1` and step `N`
- trust-network metric deltas between step `N - 1` and step `N`

The action comparison is based on `Action Timeline`.

The network comparison is based on `TrustSnapshots` and derived network metrics such as:

- `AverageTrust`
- `EffectiveNetworkDensity`
- `StrongLinkCount`
- `WeakLinkCount`
- `ComponentCount`
- `IsolatedCount`

This is meant to expose the middle process in the emergence-engineering model:

- `Boundary Conditions`
- `Action Distribution`
- `Trust Network`
- `Phase`

In practice, this helps answer questions such as:

- what changed immediately before `Stable -> Silo`
- whether `Silo -> Stable` is preceded by more sharing and support
- whether a phase change reflects fragmentation, integration, or mostly behavior change

Experiment pages also aggregate transition patterns across runs, so repeated transitions can be compared by average action and network deltas.

## Emergence Explainer

`Emergence Explainer` extends the phase-transition view from "what changed" to "why it may have changed".

It uses a trigger window of the last `3` steps:

- from `Step N - 3`
- through `Step N`
- for a detected transition at `Step N`

Within that window, the app analyzes:

- `Trigger Agent`
- `Trigger Action`
- `Trigger Trust`
- `Trigger Edge`
- rule-based `ExplainerText`

### Difference from Phase Transition Inspector

- `Phase Transition Inspector`: shows the before/after delta at the transition boundary
- `Emergence Explainer`: inspects the preceding 3-step window and surfaces likely trigger factors

### Trigger Agent

For each agent in the trigger window, the app aggregates:

- action count
- `ShareInfo`
- `ProposeIdea`
- `SupportOther`
- `Criticize`
- `WorkAlone`
- trust delta sum
- trigger score

This is a lightweight way to identify which agents were most active around the transition.

### Trigger Action

The explainer tracks:

- the largest increasing action
- the largest decreasing action
- the most common action within the trigger window

### Trigger Trust

The explainer extracts the largest absolute trust changes from `AgentActions.TrustDelta` inside the trigger window.

### Trigger Edge

The explainer compares `TrustSnapshots` at `Step N - 1` and `Step N` and extracts:

- `New Strong Link`
- `Lost Strong Link`

using `EffectiveTrustThreshold` as the boundary for strong trust.

### ExplainerText

`ExplainerText` is generated by rule, not by LLM.

The intent is not to claim certainty, but to produce a compact hypothesis about why the transition happened:

- which agents were active
- which actions increased or decreased
- whether strong links appeared or weakened
- whether the network integrated or fragmented

This supports the emergence-engineering view of the intermediate process:

- `Boundary Conditions`
- `Action Distribution`
- `Trust Network`
- `Phase`

## Precursor Analysis

`Precursor Analysis` treats emergence as a phase-transition phenomenon with observable early signals.

For each step, the app calculates a rule-based precursor profile:

- `SiloRiskScore`
- `StableScore`
- `EmergentScore`
- `MainSignal`
- `Interpretation`

These values combine:

- action distribution
- local trust-network state
- effective network metrics
- recent phase stability
- newly formed strong links when they can be observed

### Phase Forecast

`Phase Forecast` is the step-level heuristic derived from the three precursor scores.

It does not claim certainty. It indicates which tendency is currently stronger:

- `Silo`
- `Stable`
- `Emergent`

This makes it easier to inspect:

- whether silo risk rises before an actual `Silo` transition
- whether sharing, support, and idea proposal accumulate before `Emergent`
- whether a stable-looking run is structurally stable or only temporarily quiet

The modeling assumption is that emergence is not only visible at the final phase label. It leaves intermediate signals in:

- `Boundary Conditions`
- `Action Distribution`
- `Trust Network`
- `Knowledge Stock / Knowledge Diversity`
- `Exploration / Serendipity / Knowledge Recombination`
- `Knowledge Rewiring`
- `Phase`

## Emergence Fingerprint

`Emergence Fingerprint` summarizes one run as a feature vector.

The goal is to compare organizations by emergence behavior rather than only by role chart or scenario label.

The fingerprint includes:

- final phase
- average trust
- network density
- effective density
- strong and weak links
- component count
- isolated count
- phase transition count
- phase stability
- action rates
- trigger-agent concentration
- strong-link gain/loss around transitions
- threshold fragility
- average knowledge stock
- average knowledge diversity
- average exploration score
- average serendipity score
- average knowledge recombination score
- serendipity occurrence count and rate
- serendipity-to-emergence link count and rate
- max and final knowledge rewiring score
- shock occurrence and shock type
- cross-domain exposure

## Knowledge Rewiring

This MVP extends the organizational model from:

- `Boundary Conditions`
- `Action Distribution`
- `Trust Network`
- `Phase`

to:

- `Boundary Conditions`
- `Action Distribution`
- `Trust Network`
- `Knowledge Stock / Knowledge Diversity`
- `Knowledge Rewiring`
- `Phase / Emergence`

The research hypothesis is that research and development organizations do not produce breakthroughs only from internal trust and coordination.

They also adapt and reorganize through:

- `Knowledge Stock`
- `Knowledge Diversity`
- `External Shock`
- `Cross-domain Injection`
- `Knowledge Rewiring`

### Knowledge Stock

`Knowledge Stock` is a lightweight proxy for how much knowledge has been accumulated and shared at the organizational level.

It increases mainly through:

- `ShareInfo`
- `SupportOther`
- `AskHelp`

and grows more slowly when `WorkAlone` dominates.

### Knowledge Diversity

`Knowledge Diversity` is a lightweight proxy for how varied the active knowledge in the organization is.

It increases mainly through:

- `ProposeIdea`
- constructive `Criticize`
- `CrossDomainExposure`
- `ExternalShock`

### External Shock

`External Shock` represents outside stimulus that perturbs the current organizational trajectory.

The MVP supports simple shock types such as:

- `CrossDomainExpert`
- `CustomerDemandShift`
- `NewTechnology`
- `CompetitorMove`
- `FailureIncident`
- `CultureShock`

The goal is to observe whether the organization absorbs the shock, fragments under it, or rewires around it.

### Cross-domain Injection

`Cross-domain Injection` is represented by `CrossDomainExposure`.

This models situations where unfamiliar expertise, outside methods, or non-native problem frames enter the organization and make recombination more likely.

### Serendipity layer

This MVP now separates `Serendipity` from `Emergence`.

- `Emergence`: a phase transition where the system-level organizational state changes
- `Serendipity`: a prior condition where exploration and cross-domain contact create an accidental but useful knowledge combination

The current causal view is:

- `Challenge`
- `Exploration`
- `Serendipity`
- `Knowledge Recombination`
- `Knowledge Reconfiguration`
- `Network Change`
- `Emergence`

Serendipity is therefore not treated as emergence itself. It is a mediating layer that may or may not propagate into network change and phase change.

### ExplorationScore

`ExplorationScore` is a lightweight proxy for how much exploratory behavior occurred in one step.

It combines:

- `AskHelp`
- `ShareInfo`
- `ProposeIdea`
- `SupportOther`
- `CrossDomainExposure`
- `ExplorationTendency`

Challenge pressure can raise this score further.

### SerendipityScore

`SerendipityScore` estimates how likely it is that exploration, diversity, and cross-domain contact produce an accidental useful combination.

It combines:

- `ExplorationScore`
- `KnowledgeDiversity`
- `CrossDomainExposure`
- positive `ChallengeGap`
- `SerendipitySensitivity`

If `SerendipityScore >= SerendipityThreshold`, the step is marked as `SerendipityOccurred`.

### KnowledgeRecombinationScore

`KnowledgeRecombinationScore` estimates how strongly the organization is recombining existing knowledge rather than only accumulating it.

It combines:

- `SerendipityScore`
- `ProposeIdea`
- constructive `Criticize`
- `SupportOther`
- `KnowledgeRecombinationRate`

### SerendipityToEmergenceLink

`SerendipityToEmergenceLink` is a rule-based flag used when serendipity occurred and was followed within a short window by `Adaptation` or `Emergent`.

This is meant to support the research hypothesis:

> Serendipity is not emergence itself.  
> It is a prior condition that can mediate emergence through knowledge recombination and knowledge reconfiguration.

### Knowledge Rewiring

`Knowledge Rewiring` is not treated as simple knowledge growth.

The key idea is:

- emergence is not only that the organization knows more
- emergence can also mean that the knowledge network has been reconfigured

The app calculates a `KnowledgeRewiringScore` at each step and stores it in `SimulationSteps.StateJson` together with:

- `knowledgeStock`
- `knowledgeDiversity`
- `externalShockLevel`
- `crossDomainExposure`
- `explorationScore`
- `serendipityScore`
- `serendipityOccurred`
- `knowledgeRecombinationScore`
- `serendipityDrivenReconfiguration`
- `serendipityToEmergenceLink`
- `shockOccurred`
- `shockType`

This makes it possible to inspect how external stimuli and internal idea dynamics affect phase formation.

### ThresholdFragilityScore

`ThresholdFragilityScore` is derived from the effective-threshold sweep:

- `1 - AverageEffectiveDensityAcrossThresholds`

Higher values mean the network looks connected only under low trust thresholds and fragments quickly when stricter collaboration strength is required.

This is useful for distinguishing:

- robust integrated networks
- fragile weak-tie networks
- runs that look connected in a permissive view but collapse in an effective-network view

Experiment pages also assign a lightweight run label such as:

- `Integrated Learning Type` 
- `Controlled Silo Type` 
- `Fragile Network Type` 
- `Catalyst Driven Type` 
- `Unclassified` 

Knowledge-oriented labels are also assigned, such as:

- `知識蓄積型`
- `異分野創発型`
- `外乱駆動創発型`
- `閉鎖学習型`
- `未分類`

### Effective network summary

Experiment run summaries also surface effective-network metrics copied from `SimulationMetrics`:

- `EffectiveNetworkDensity`
- `StrongLinkCount`
- `ComponentCount`

This makes it easier to compare questions such as:

- which runs reached `Emergent` with one connected component
- which runs ended in `Silo` with multiple components
- whether a condition improves trust strength, not only trust existence

### Threshold sweep summary

Experiment details also aggregate threshold sweep results across all runs in the experiment.

For each threshold, the page averages:

- `EffectiveNetworkDensity`
- `StrongLinkCount`
- `WeakLinkCount`
- `ComponentCount`
- `IsolatedCount`

This supports comparisons such as:

- whether one condition keeps the network connected under stricter trust thresholds
- whether another condition produces many weak links but few durable strong links
- where fragmentation starts on average across repeated runs

Older runs without `TrustSnapshots` can still participate through fallback calculation from final `Agents.TrustJson`.

### Action timeline summary

Experiment details also aggregate action rates by `StepNo` across all runs.

The current implementation:

- computes action rates for each run-step pair
- then averages those rates across runs with the same `StepNo`

The experiment page also shows phase-specific action distributions aggregated across runs.

This supports analysis such as:

- whether idea proposals spike before `Emergent`
- whether `WorkAlone` rises before `Silo`
- whether `ShareInfo` and `SupportOther` stabilize before `Stable`

## EF Core migrations

Migrations are included under `Migrations/`, including:

- phase columns for `SimulationProjects` and `SimulationSteps`
- action-level trust delta columns for `AgentActions`
- step-level trust state storage in `TrustSnapshots`
- experiment and experiment-run support
- `SimulationMetrics`
- agent diversity columns on `Agents`
- experiment metric columns on `ExperimentRuns`
- experiment status on `Experiments`
- explicit LLM provider/model columns on `Experiments` and `SimulationProjects`
- knowledge / shock / rewiring columns on `Experiments`, `Scenarios`, and `SimulationProjects` via `20260703081000_AddKnowledgeAndShockParameters`
- serendipity-layer columns on `Experiments`, `Scenarios`, and `SimulationProjects` via `20260703083000_AddSerendipityParameters`
- phase-diagram tables via `20260704090000_AddPhaseDiagrams`

For future schema changes:

```powershell
dotnet ef migrations add <MigrationName>
dotnet ef database update
```

If your local database already exists, `dotnet ef database update` is still required to apply pending columns before startup. This is especially relevant after the `20260703081000_AddKnowledgeAndShockParameters` migration, which adds:

- `CrossDomainExposure`
- `EnableExternalShock`
- `ExternalShockLevel`
- `KnowledgeDiversity`
- `KnowledgeStock`
- `RewiringSensitivity`
- `ShockDescription`
- `ShockStep`
- `ShockType`

## Future extensions

- Personality / Orientation composition as an experiment parameter
- Scenario comparison
- 2-factor sweep
- Parameter sweep
- Sensitivity analysis
- sensitivity ranking
- Local LLM support
- Azure OpenAI support
- Metrics comparison across experiments
- Phase transition probability matrix
- Trust network visualization
- dynamic network animation
- silo detection
- hub transition analysis
- network change analysis immediately before phase transitions
- network repair observation under shock injection
- Shock injection
- Shock Injection combined with sweep analysis
- threshold-specific phase transition probability
- threshold-specific hub transition analysis
- threshold-specific network visualization
- automatic phase-transition or fragmentation-point detection
- automatic comparison of the last N steps before a phase change
- average comparison over the last N steps before a phase transition
- phase-transition prediction models
- comparison with real organizational logs
- LLM-generated phase-transition interpretation
- linking phase transitions with KPI changes
- trigger-score weight tuning
- Emergence Report PDF export
- explicit knowledge-network graph visualization
- external expert agents
- customer-demand shocks
- technology-introduction shocks
- learning from failure incidents
- fingerprint clustering
- fingerprint comparison against real organization data
- comparison with real R&D organization logs
- before/after intervention fingerprint comparison
- AI-driven boundary-condition optimization
- predictive models from action distribution to phase transition
- mapping to Slack / Teams / GitHub / email activity
- KPI switch experiments
- DOE (design of experiments)
- AI-driven boundary-condition search
- Bayesian optimization
- optimization-based emergence condition search
- LLM model comparison
- provider-specific runtime and cost logging
- Background jobs for large batch runs
- Dedicated `TrustEvents` table for richer pairwise trust histories

## Challenge Event and Adaptation

`External Shock` and `Challenge Event` are different concepts.

- `External Shock`: outside stimulus that perturbs the current trajectory
- `Challenge Event`: a structural problem that the current organization cannot solve with its existing knowledge, trust pattern, and action style

Challenge configuration is stored on:

- `Scenario`
- `Experiment`
- `SimulationProject`

Fields:

- `EnableChallengeEvent`
- `ChallengeType`
- `ChallengeStep`
- `ChallengeLevel`
- `ChallengeDescription`
- `RequiredKnowledgeDiversity`
- `RequiredCrossDomainExposure`
- `RequiredRewiringScore`

If `EnableChallengeEvent` is off, these fields are not required and are normalized to default values on save.

The migration for these columns is:

- `20260703082000_AddChallengeEventParameters`

### Challenge types

- `ExistingMethodFailure`
- `NewMarketRequirement`
- `CrossFunctionalProblem`
- `QualityCrisis`
- `TechnologyShift`
- `CustomerComplexityIncrease`

### Adaptation phase

`Adaptation` is an intermediate phase used after a challenge appears.

It represents a state where:

- the challenge is still active
- the organization is increasing cross-boundary coordination
- knowledge rewiring / reconfiguration is rising
- the system has not yet stabilized into `Emergent`, `Stable`, `Silo`, or `Chaos`

### Challenge metrics

The step state now stores and displays:

- `ChallengeOccurred`
- `ChallengeActive`
- `ChallengeResolved`
- `ChallengeResolutionScore`
- `ChallengeGap`
- `KnowledgeReconfigurationScore`

Interpretation:

- `ChallengeResolutionScore`: how well the current organization matches the knowledge / exposure / rewiring requirements implied by the challenge
- `ChallengeGap`: required level minus achieved resolution score
- `KnowledgeReconfigurationScore`: a lightweight proxy for whether the organization is actually reorganizing knowledge, not merely accumulating it

Research hypothesis:

> Emergence is not caused only by knowledge accumulation.  
> It emerges when a challenge forces the organization to reconfigure its knowledge network.

## Serendipity analysis

The app now exposes serendipity-related parameters on:

- `Scenario`
- `Experiment`
- `SimulationProject`

Stored fields:

- `ExplorationTendency`
- `SerendipitySensitivity`
- `KnowledgeRecombinationRate`
- `SerendipityThreshold`
- `EnableSerendipity`

Migration:

- `20260703083000_AddSerendipityParameters`

Simulation detail pages show a serendipity timeline with:

- `ExplorationScore`
- `SerendipityScore`
- `SerendipityOccurred`
- `KnowledgeRecombinationScore`
- `KnowledgeReconfigurationScore`
- `ChallengeGap`

Experiment detail pages aggregate:

- average exploration score
- average serendipity score
- serendipity occurrence run count and rate
- average knowledge recombination score
- serendipity-to-emergence link count and rate

### Research hypothesis

The working hypothesis for this layer is:

> Serendipity is not the cause of emergence by itself.  
> It mediates emergence by increasing the chance of knowledge recombination, which can then propagate into knowledge reconfiguration and network-level phase change.

Future extensions:

- LLM-generated serendipity explanations
- explicit knowledge-graph visualization
- comparison against real research-and-development organization logs
- calibration of trigger weights for exploration and recombination

This implementation is still rule-based. It does not use an LLM to explain or solve the challenge.

## Trust dynamics

The trust model now treats trust as a finite resource instead of a value that can grow forever.

Stored configuration fields on `Scenario`, `Experiment`, and `SimulationProject`:

- `EnableTrustDynamics`
- `TrustGrowthRate`
- `TrustDecayRate`
- `TrustSaturationStrength`
- `TrustCapacity`
- `TrustCapacityPenalty`
- `DistrustPenalty`
- `ConstructiveCriticismBonus`

These trust-dynamics parameters are editable in `0.001` increments in the UI. In practice, `TrustDecayRate` is often tuned in the `0.002` to `0.010` range.

Migration:

- `20260703084000_AddTrustDynamicsParameters`

Meaning:

- `TrustSaturation`: positive trust increases become harder as current trust rises
- `TrustDecay`: trust slowly returns toward zero when relationships are not reinforced
- `TrustCapacity`: each agent can only maintain a limited number of deep trusted ties
- `ConstructiveCriticism`: criticism can become trust-building when psychological safety is high

Behavior:

- `AgentAction.TrustBefore / TrustDelta / TrustAfter` store the effective action-level trust change
- `TrustSnapshots` store the post-dynamics step-end trust network
- `AverageTrustDecayApplied`, `TrustCapacityPenaltyTotal`, and related fields are stored in step state JSON
- `StrongTrustConcentration` summarizes how concentrated each agent's strong ties are
- `TrustNetworkType` classifies runs as overtrusted complete, selective trust, or fragile trust networks

Research hypothesis:

> Trust is not an infinite resource.  
> Emergence is more likely in a selective trust network that can rewire, not in a fully connected network that saturates into uniform trust.

Future analysis directions:

- compare overtrusted complete networks with selective trust networks
- detect fragile trust networks and trust-capacity overload
- compare trust dynamics across scenarios and parameter sweeps

If you are using an existing database, apply the new migration so these columns appear in SQL Server:

- `dotnet ef database update`

## Interpretation support for sweeps and phase diagrams

Parameter Sweep and Phase Diagram detail pages now include a lightweight interpretation layer that helps researchers read the results faster:

- summary cards for the main maxima
- recommended regions and danger regions
- automatic transition candidate detection from adjacent values or cells
- bottleneck analysis for runs or cells that do not reach emergence
- CSV export of the visible aggregated data

The interpretation service is rule-based. It does not store extra data in the database.

Research hypothesis:

> Emergence is not explained by a single parameter.  
> It appears when trust network formation, knowledge reconfiguration, serendipity, and phase stability align across a usable region of the parameter space.

## Notes

- This pass was implemented without build verification, startup verification, `dotnet run`, test execution, or migration apply.
- If compile errors remain, they are most likely around Razor typing, manual migration drift, or EF relationship alignment and should be fixed from the reported build output.


