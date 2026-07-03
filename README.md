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

Each parameter value produces one `Experiment`, and the sweep stores a compact result summary in `ParameterSweepRun`.

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

- `Scenarios`: reusable experiment-condition templates with LLM settings, numeric boundary parameters, and `EffectiveTrustThreshold`
- `ParameterSweeps`: one-parameter sensitivity analysis batches built from a base scenario
- `ParameterSweepRuns`: one result summary per tested parameter value
- `SimulationProjects`: simulation definition, status, progress, current phase, optional experiment linkage, `LlmProvider`, `LlmModel`, copied boundary parameters, and `EffectiveTrustThreshold`
- `Agents`: generated agents with role, memory, trust JSON, position, personality, and orientation
- `SimulationSteps`: step-level state snapshots with persisted phase history
- `AgentActions`: per-agent message, action, target, memory, raw LLM response, and trust delta log
- `TrustSnapshots`: step-level trust network state for every directed agent pair
- `SimulationMetrics`: final aggregate metrics for one simulation
- `Experiments`: shared condition definitions for repeated runs, optional `ScenarioId`, `LlmProvider`, `LlmModel`, copied boundary parameters, and `EffectiveTrustThreshold`
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

For future schema changes:

```powershell
dotnet ef migrations add <MigrationName>
dotnet ef database update
```

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

## Notes

- This pass was implemented without build verification, startup verification, `dotnet run`, test execution, or migration apply.
- If compile errors remain, they are most likely around Razor typing, manual migration drift, or EF relationship alignment and should be fixed from the reported build output.

