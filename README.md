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

## References

- Hazan, E., Kakade, S. M., Singh, K., & Van Soest, A. (2019). *Provably Efficient Maximum Entropy Exploration*. arXiv:1812.02690v2.
- Original reference implementations: https://github.com/abbyvansoest/maxent_base and https://github.com/abbyvansoest/maxent_ant
