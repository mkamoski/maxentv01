# maxentv01

## Overview

This project implements the **Provably Efficient Maximum Entropy Exploration** algorithm described in the paper:

> **Provably Efficient Maximum Entropy Exploration**  
> Elad Hazan, Sham M. Kakade, Karan Singh, Abby Van Soest  
> Google AI Princeton / Princeton University / University of Washington  
> arXiv:1812.02690v2 [cs.LG] — 26 Jan 2019

The paper addresses the problem of efficient exploration in unknown Markov Decision Processes (MDPs) **in the absence of a reward signal**. The core idea is to find a policy that maximizes the entropy of the induced state-visitation distribution — encouraging the agent to visit all reachable states as uniformly as possible.

---

## Theoretical Background (from the paper)

### Problem Setting

Given an infinite-horizon discounted MDP `M = (S, A, r, P, γ, d₀)`, a policy `π` induces a (discounted) state-visitation distribution:

```
dπ(s) = (1 - γ) Σ_t γ^t · P(sₜ = s | π)
```

The **Maximum Entropy exploration objective** is:

```
π* ∈ argmax H(dπ) = -E_{s ~ dπ} [log dπ(s)]
```

More generally, the paper optimizes any **β-smooth, B-bounded concave reward functional** `R(dπ)` over state-visitation distributions.

### Key Insight: Convexity in Distribution Space

Although `H(dπ)` is **not concave in the policy** `π` (Lemma 3.1 in the paper), it **is concave** when viewed as a function of the induced distribution `d`. The set of achievable distributions `K` forms a convex set, allowing the problem to be reformulated as convex optimization:

```
max R(d),   d ∈ K
```

### Algorithm 1 — Frank-Wolfe / Conditional Gradient Method

The paper's core algorithm maintains a **mixture of stationary policies** and iteratively refines it using the Frank-Wolfe (conditional gradient) method:

```
Input: step size η, iterations T, planning oracle error ε₁, density oracle error ε₀, functional R

Set C₀ = {π₀},  α₀ = 1
for t = 0 ... T-1:
    1. Estimate state distribution: d̂ = DensityEst(πmix,t, ε₀)
    2. Compute gradient reward: rₜ(s) = ∇R(d̂)|_{s}
    3. Plan optimal policy: πₜ₊₁ = ApproxPlan(rₜ, ε₁)
    4. Update mixture: αₜ₊₁ = ((1-η)αₜ, η),  Cₜ₊₁ = (C, πₜ₊₁)
return πmix,T
```

### Main Theorem (Theorem 4.1)

For any `ε > 0`, setting `η = 0.1β⁻¹ε` and running Algorithm 1 for:

```
T ≥ 10β/ε · log(10B/ε)
```

iterations guarantees:

```
R(dπmix,T) ≥ max_π R(dπ) - ε
```

This bound is **independent of the state space size** `|S|`.

### Smoothed Entropy Objective

Since the entropy `H` is not smooth, the paper uses a smoothed proxy:

```
Hσ(dπ) = -E_{s ~ dπ} [log(dπ(s) + σ)]
```

With `σ = 0.1ε² / (2|S|)` and `T ≥ (40|S| / 0.1ε²) · log(|S| / 0.1ε)`, Algorithm 1 guarantees:

```
H(dπmix,T) ≥ max_π H(dπ) - ε
```

### Oracle Requirements

The algorithm assumes access to two oracles:

| Oracle | Description | Guarantee |
|--------|-------------|-----------|
| `ApproxPlan(r, ε₁)` | Returns a near-optimal policy for reward function `r` | `Vπ ≥ max_π Vπ - ε₁` |
| `DensityEst(π, ε₀)` | Estimates the state-visitation distribution of `π` | `‖dπ - d̂π‖∞ ≤ ε₀` |

### Tabular MDP Complexity

| Setting | Sample Complexity | Episode Length |
|---------|-------------------|----------------|
| Known MDP | `poly(β, |S|, |A|, 1/(1-γ), 1/ε, log B)` | — |
| Unknown MDP | `Õ(B³\|S\|²\|A\|β³ / (ε³(1-γ)²) + B/ε³)` episodes | `Õ(log(\|S\|/ε) / log(1/γ))` |

---

## Implementation

This project is a **.NET 10 MAUI** application that implements the MaxEnt exploration algorithm across multiple environments, based directly on the paper.

### Environments

#### 1. CartPole (`Models/CartPoleEnvironment.cs`)
A classic control environment used as a proof-of-concept, matching the paper's Section 5 experimental setup.

- **State space** (4-dimensional): `[cart_position, cart_velocity, pole_angle, pole_angular_velocity]`
- **Action space**: 2 discrete actions — push left (`0`) or push right (`1`)
- **Physics**: Euler integration with standard CartPole dynamics (gravity = 9.8, pole half-length = 0.5m, force = 10N, τ = 20ms)
- **Termination**: pole angle exceeds ±12°, or cart position exceeds ±2.4 units
- **Reward**: +1 per step the pole is balanced (used for the planning oracle; the MaxEnt objective operates on state-visitation entropy)

#### 2. Ant (`Models/AntMaxEntEnvironment.cs`)
A 29-dimensional quadruped locomotion environment, mirroring the Ant experiment in Section 5 of the paper.

- **State space** (27-dimensional): body position/velocity, orientation, joint angles and velocities
- **Action space**: 8 continuous joint torques (2 per leg × 4 legs), clamped to `[-1, 1]`
- **Density estimation**: performed on a reduced 7-dimensional state representation (xy position + 5-dim random projection), discretized into bins — consistent with the paper's Section 5.1 approach

### MaxEnt Agent (`Models/MaxEntAgent.cs`)

Implements the **soft Q-learning** planning oracle (`ApproxPlan`) from Algorithm 1 of the paper.

#### Soft Q-Learning Update
```
Q(s,a) ← Q(s,a) + α · [r + γ · V(s') - Q(s,a)]
```
Where `α` is the learning rate, `r` is the reward, `γ` is the discount factor.

#### Soft Value Function (log-sum-exp)
Implements the paper's entropic value function (numerically stabilized):
```
V(s) = τ · log Σ_a exp(Q(s,a) / τ)
```

#### Softmax Policy
The action distribution derived from Q-values with temperature `τ`:
```
π(a|s) = exp(Q(s,a) / τ) / Σ_{a'} exp(Q(s,a') / τ)
```
Higher `τ` → higher entropy (more exploration). Lower `τ` → more exploitation.

#### Policy Entropy
The current policy entropy is tracked per the paper's objective (Section 3.1):
```
H(π) = -Σ_a π(a|s) · log π(a|s)
```

#### Q-Function Approximation
Linear function approximation (consistent with the paper's use of REINFORCE with a neural/linear policy class for MountainCar and Pendulum in Section 5.2):
```
Q(s,a) = W_a · s + b_a
```

### Charting (`Services/CartPoleChartService.cs`)
Generates training charts using ScottPlot, consistent with the paper's Figure 2:
- Entropy of the policy vs. training epochs (Figures 2a–2c in the paper)
- Log-probability of state-space occupancy (Figures 2d–2f in the paper)

---

## Key Differences from the Paper

| Paper | This Implementation |
|-------|---------------------|
| General β-smooth concave functional `R(dπ)` | Entropy `H` via soft Q-learning temperature `τ` |
| Frank-Wolfe mixture of policies (Algorithm 1) | Incremental soft Q-learning (single policy, online updates) |
| Count-based tabular density oracle (Algorithm 3) | Linear Q-function approximation with softmax |
| REINFORCE / SAC as planning oracle | Soft Q-learning with linear approximation |
| Proven `O(1/ε · log 1/ε)` oracle calls | Empirical convergence |

---

## Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| `Microsoft.Maui.Controls` | 10.0.20 | UI framework |
| `Microsoft.AspNetCore.Components.WebView.Maui` | 10.0.20 | Blazor hybrid UI |
| `ScottPlot` / `ScottPlot.Blazor` | 5.1.58 | Training charts |
| `SixLabors.ImageSharp` | 3.1.12 | Chart image export |
| `BepuPhysics` | 2.5.0-beta.29 | Physics simulation (Ant environment) |
| `System.Numerics.Vectors` | 4.6.1 | Vector math for physics |

---

# Summary: Efficient Algorithm for Exploration in Unknown Markov Decision Processes

## Overview of Max-Entropy Exploration in Reinforcement Learning
* This work develops an efficient, theoretically grounded method for exploration in unknown or large MDPs.
* It maximizes the entropy of state visitation distributions using a convex optimization approach and approximate planning oracles.

## Max-Entropy Objective and Its Significance
* Max-entropy aims to induce a uniform or diverse state visitation distribution, serving as an intrinsic exploration objective.
* It focuses on optimizing functions of state-visitation frequencies, such as entropy.
* Entropy maximization is convex when considering the distribution space, despite being non-concave in policy space.
* Other functionals like KL divergence and cross-entropy are also considered.
* The goal is to find policies that induce distributions with high or specific desired properties.

## Challenges in Policy Optimization
* The entropy of the induced distribution is not a concave function of the policy, complicating direct optimization.
* Lemma 3.1 shows non-convexity of entropy in policy space.
* The state distribution depends non-linearly on the policy, making the problem non-convex.
* The distribution space, however, forms a convex set, enabling convex reformulation.
* Stationary policies are sufficient for optimality in the distribution space (Lemma 3.3).

## Convex Reformulation in Distribution Space
* The set of all possible state distributions induced by policies is convex, allowing the problem to be cast as a convex optimization.
* The optimization over distributions is feasible and convex.
* Any policy's induced distribution can be represented within this convex set.
* This approach simplifies the maximization of functionals like entropy.

## Algorithmic Framework for Max-Entropy Exploration
* The proposed method uses a conditional gradient (Frank-Wolfe) algorithm with two key oracles.
* The oracles include an approximate planning oracle and a state distribution estimate oracle.
* The algorithm iteratively updates a mixture of policies to maximize the reward functional.
* It guarantees convergence with a number of oracle calls independent of the state space size.
* The reward functional is assumed to be β-smooth and bounded.
* The method is applicable in both known and unknown MDP settings, with sample-based algorithms for the latter.

## Main Theoretical Results and Guarantees
* The core theorem states that the algorithm converges to an ε-optimal policy in terms of the reward functional.
* Convergence occurs after a number of iterations logarithmic in 1/ε.
* For any smooth reward measure, the number of calls to oracles is O(1/ε log(1/ε)).
* In the case of entropy, a smoothed variant Hσ is used to ensure smoothness.
* The convergence guarantees hold for both known and unknown MDPs, with sample complexity bounds provided.
* For the entropy functional, the number of iterations depends on the size of the state space |S| and the smoothing parameter.

## Construction of Oracles in Tabular and Unknown MDPs
* Known MDPs utilize exact solutions via value iteration or linear programming.
* Unknown MDPs utilize sample-based algorithms inspired by E3, with sample complexity polynomial in key parameters.
* Oracles provide approximate policies and state distribution estimates with guarantees on sub-optimality and accuracy.
* Sample complexity in unknown MDPs scales with factors like (1−γ)^{-1}, |S|, |A|, and 1/ε.

## Experimental Validation and Practical Insights
* Preliminary experiments demonstrate the method's ability to increase entropy and explore effectively in MountainCar, Pendulum, and Ant environments.
* Discretization of state spaces was used for the environments.
* The approach successfully maximized entropy and coverage over reachable states.
* Results show the policy's evolution over iterations, with increased diversity and state visitation.
* Implementation is available open-source, highlighting practical applicability.

## Reinforcement Learning Algorithms and Agents Overview
* The planning oracle for MountainCar and Pendulum uses policy gradient methods with neural networks (REINFORCE agent with a single hidden layer of 128 units).
* MountainCar agents trained on 400 episodes per epoch; Pendulum agents trained on 200 episodes.
* The baseline agent chooses actions randomly at each step.
* The policy output from the previous iteration initializes the next policy.
* The Ant environment employs off-policy methods using a Soft Actor-Critic (SAC) agent as the planning oracle.
* The SAC neural network uses 2 hidden layers, each with 300 units, using ReLU activation.
* Ant training occurs over 30 episodes, each with a 5000-step roll-out.
* The mixed policy executes over 10 trials of 10,000 steps to estimate the policy distribution.
* The reward function for the next iteration is computed based on these trials.
* The baseline agent for Ant acts randomly for the same number of trials and steps.

## Related Work and Context
* The work relates to intrinsic motivation, curiosity-driven exploration, reward shaping, imitation learning, and confidence-based exploration in RL.
* Addresses limitations of policy gradient and sparse reward methods.
* Builds on convex optimization and distributional approaches.
* Extends prior empirical and theoretical exploration strategies with provable guarantees.
* Connects to classical PAC learning and count-based exploration methods, with a focus on intrinsic objectives.

## Funding, Acknowledgements, and References
* Sham Kakade received funding from the Washington Research Foundation, DARPA (FA8650-18-2-7836), and ONR (N00014-18-1-2247).
* Thanks extended to Shie Mannor for discussions.
* Key references include REINFORCE [SMSM00] and Soft Actor-Critic [HZAL18].
* References cover foundational and recent works in reinforcement learning, policy optimization, inverse reinforcement learning, regret bounds, and deep RL techniques.

# Experiment Runner

## Why it's designed this way

Blazor WebAssembly is **single-threaded**. There is no background worker, no server, and no state recovery after a tab closes or refreshes. The design accepts this honestly:

- Only **one experiment runs at a time** — enforced globally, not per-page.
- A run **lives and dies with the browser tab**. Navigating away or refreshing stops it.
- Every run is **immediately persisted** as an incomplete log the moment it starts. A log is only promoted to complete when the training loop exits cleanly.

This model is simple, honest, and safe. There are no hidden partial states.

---

## 1. Execution Model

| Aspect | Detail |
|---|---|
| Platform | Blazor WebAssembly (.NET 10) |
| Threading | Single-threaded; one experiment at a time, globally enforced |
| Lifecycle | Full stop on tab close/refresh — no recovery needed or attempted |
| Cancellation | `CancellationTokenSource` per run; cancelled by user, timeout, or navigation |
| Hard timeout | `CancellationTokenSource(TimeSpan.FromHours(N))`, configurable per experiment |

---

## 2. Single-Experiment Constraint

**Why:** WASM runs on the browser's main thread. Running two experiments concurrently would block the UI and produce corrupted logs.

**How:** A singleton `IExperimentRunnerState` service is registered in `Program.cs` and shared across all pages.

```csharp
// Services/IExperimentRunnerState.cs
public interface IExperimentRunnerState
{
    bool IsRunning { get; }
    string? ActiveSource { get; }   // "CartPole" or "AntMaxEnt"
    void Start(string source);
    void Stop();
}
```

`Start(source)` is called the moment **Run** is clicked. `Stop()` is called in the `finally` block of every run method — so it fires whether the run finishes cleanly, times out, or is cancelled.

**UI enforcement:** On page init, each experiment page checks `RunnerState.IsRunning`. If another experiment is already active, the Run button is disabled and a warning banner appears:

> *Cannot start — CartPole experiment is currently running. Stop it before starting this one.*

---

## 3. Navigation Guard

**Why:** If a user clicks away mid-run, the experiment must be stopped and the log must stay marked as incomplete — silently abandoning a run would leave a corrupted record.

**How:** Each experiment page implements `IDisposable` and registers a location-changing handler on init:

```csharp
NavigationManager.RegisterLocationChangingHandler(OnLocationChangingAsync);
```

When navigation is attempted while a run is active, a browser confirm dialog fires:

> *Navigating away will stop this experiment and mark the log as incomplete. Continue?*

- **Confirmed:** `cts.Cancel()` + `RunnerState.Stop()` → navigation proceeds.
- **Cancelled:** navigation is blocked; the run continues.

The handler is unregistered in `Dispose()`.

---

## 4. Run Lifecycle & Log Status

**Why:** A log created only at the end of a run would be lost if the user navigates away or the tab closes mid-run. Creating it at the start and marking it complete at the end guarantees a record always exists.

**Flow:**

```
Run clicked
  → RunnerState.Start("CartPole")
  → ExperimentLog created with Status = Incomplete  ← persisted immediately
  → Training loop runs ...
      (user may cancel, navigate away, or timeout)
  → finally: RunnerState.Stop()
  → if completed cleanly: LogRepo.MarkCompletedAsync(logId)
```

**`ExperimentRunStatus` enum:**

```csharp
public enum ExperimentRunStatus { Incomplete, Completed }
```

`ExperimentLog.Status` defaults to `Incomplete` on `Create(...)`. It is set to `Completed` only when the training loop exits without cancellation.

**Repository surface:**

```csharp
Task MarkCompletedAsync(Guid id);
```

---

## 5. Incomplete Log Badge

On the **Logs** and **Experiment Results** pages, any log whose `Status == Incomplete` shows a small red badge:

> <span style="background:#dc3545;color:#fff;font-size:10px;padding:1px 5px;border-radius:3px;">Incomplete</span>

This lets users immediately see which runs were interrupted or abandoned.

---

## 6. Data Management (SQLite in-memory)

| Aspect | Detail |
|---|---|
| Storage engine | SQLite via `Microsoft.Data.Sqlite`, in-memory connection held open for the tab lifetime |
| EF Core context | Singleton `AppDbContext`; schema created with `EnsureCreatedAsync()` on startup |
| Logs | Rolling table; max 10 entries pruned on `AddAsync`; content capped at 2 000 lines per entry (trimmed to last 1 000 when exceeded) |
| Graphs | SVG content stored as text per run (`ExperimentGraph` entity) |
| Persistence scope | Tab lifetime only — data is lost on refresh by design |

**Why in-memory and not OPFS?** The experiment lifecycle ends when the tab closes. Persisting to OPFS would add complexity with no user benefit — the user sees results immediately after a run, and completed logs/graphs are available for download before they leave the page.

### Log content trimming

Each log entry stores its output as a plain-text blob. To prevent unbounded memory growth during long training runs:

| Threshold | Action |
|---|---|
| Content rows ≤ 2 000 | Stored as-is |
| Content rows > 2 000 | Oldest lines discarded; last **1 000 lines** kept, then new content appended |

Trimming is applied on every write — both when a log entry is first created (`ExperimentLog.Create`) and on every incremental update (`ExperimentLogRepository.UpdateContentAsync`). The character-length cap (`MaxContentLength = 55 000`) is enforced after the row trim as a secondary safety net.

---

## 7. Anti-Throttling

The **Screen Wake Lock API** (`navigator.wakeLock`) is requested via JS interop when a run starts and released when it stops. This prevents the browser from throttling the WASM runtime when the tab is in the background.

---

## 8. UI Design

- **Pulse line:** Only the most recent log line is shown live during a run to keep DOM overhead minimal.
- **Full log:** Retrieved on-demand when the user opens the Logs or Results page.
- **Graphs:** Generated in SVG after a run completes, stored in SQLite, and rendered on the Results page.

---

## 9. Technical Stack

| Layer | Technology |
|---|---|
| Runtime | .NET 10 / Blazor WebAssembly |
| Language | C# 13, file-scoped namespaces, records, primary constructors |
| Persistence | EF Core 10 + `Microsoft.Data.Sqlite` (in-memory) |
| UI | Razor components, Bootstrap |
| Principles | SOLID · DRY · YAGNI |

---

# References

- Hazan, E., Kakade, S. M., Singh, K., & Van Soest, A. (2019). *Provably Efficient Maximum Entropy Exploration*. arXiv:1812.02690v2.
- Original reference implementations: https://github.com/abbyvansoest/maxent_base and https://github.com/abbyvansoest/maxent_ant
