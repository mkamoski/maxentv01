# CartPole with Maximum Entropy Reinforcement Learning

## Overview
This implementation combines the classic CartPole control problem with Maximum Entropy (MaxEnt) reinforcement learning.

## What is CartPole?
CartPole is a standard reinforcement learning benchmark where an agent must balance a pole on a moving cart by applying forces left or right. The episode ends if:
- The pole angle exceeds ±12 degrees
- The cart moves beyond ±2.4 units from center
- The agent successfully balances for the maximum number of steps

## What is Maximum Entropy RL?
Maximum Entropy Reinforcement Learning encourages exploration by maximizing both expected reward AND policy entropy. This leads to:
- **More robust policies**: The agent learns multiple ways to solve the task
- **Better exploration**: Higher entropy prevents premature convergence
- **Improved generalization**: The policy is less brittle to disturbances

### Key Concepts

**Soft Q-Learning Update:**
```
Q(s,a) ← Q(s,a) + α[r + γV(s') - Q(s,a)]
```

**Soft Value Function (log-sum-exp):**
```
V(s) = τ log Σ exp(Q(s,a) / τ)
```

**Softmax Policy:**
```
π(a|s) = exp(Q(s,a) / τ) / Σ exp(Q(s,a') / τ)
```

Where:
- `τ` (tau) is the temperature parameter controlling entropy
- Higher temperature = more exploration
- Lower temperature = more exploitation

## Implementation Details

### State Space (4 dimensions)
1. Cart Position: [-2.4, 2.4]
2. Cart Velocity: continuous
3. Pole Angle: [−12°, 12°] in radians
4. Pole Angular Velocity: continuous

### Action Space (2 discrete actions)
- Action 0: Push cart left
- Action 1: Push cart right

### Reward Structure
- +1.0 for each time step the pole remains balanced
- 0.0 when the episode terminates

### Q-Function Approximation
Uses a simple linear model:
```
Q(s,a) = W_a · s + b_a
```
Where `W_a` is a weight vector and `b_a` is a bias for each action.

## Usage

1. Navigate to the **CartPole MaxEnt** page from the menu
2. Adjust parameters:
   - **Episodes**: Number of training episodes (1-100)
   - **Max Steps**: Maximum steps per episode (50-500)
   - **Learning Rate**: How quickly the agent learns (0.0001-0.1)
3. Click **Reset** to initialize the environment
4. Click **Run** to start training
5. Monitor the output log and state display
6. After training completes:
   - A chart is automatically generated and saved to the `output/` folder
   - Filename format: `YYYY-MM-dd-HH-mm-ss-fffff-cartpole.png`
   - The output log shows the full path to the saved chart
7. Click **View Graph** to open the chart in Windows 11 default image viewer
8. Click **Cleanup Old Charts** to delete charts older than 28 days

## Chart Features

The generated chart includes three subplots:

### 1. Episode Rewards
- Shows total reward achieved in each episode
- Includes a 5-episode moving average (dark green line)
- Higher rewards indicate better performance

### 2. Episode Duration (Steps)
- Shows how many steps the agent survived before failure
- Includes a 5-episode moving average (dark blue line)
- Longer episodes mean the pole was balanced longer

### 3. Policy Entropy
- Shows the exploration level of the agent's policy
- Includes a 5-episode moving average (dark orange line)
- High entropy = more exploration, Low entropy = more exploitation

### File Management
- Charts are saved with microsecond-precision timestamps to avoid collisions
- The `output/` folder is automatically created if it doesn't exist
- Old charts (>28 days) can be cleaned up with one click
- The output folder is in `.gitignore` to avoid committing large chart files

## Expected Behavior

Initially, the agent will struggle to balance the pole, with episodes ending quickly. As training progresses:
- Episode length increases (more steps before failure)
- Total reward per episode grows
- Policy entropy gradually decreases as the agent finds good solutions

A well-trained agent should maintain balance for close to the maximum number of steps.

## Physics Parameters

The simulation uses realistic physics:
- Gravity: 9.8 m/s²
- Cart mass: 1.0 kg
- Pole mass: 0.1 kg
- Pole length: 1.0 m (0.5 m half-length)
- Force magnitude: 10.0 N
- Time step: 0.02 s (20 ms)

## References

- **Soft Actor-Critic**: Haarnoja et al. (2018) - Modern MaxEnt RL algorithm
- **Maximum Entropy RL**: Ziebart et al. (2008) - Original MaxEnt framework
- **CartPole**: Classic OpenAI Gym environment

## Future Enhancements

Possible improvements:
- Visualize the cart and pole in real-time
- Plot training curves (reward vs episode)
- Add temperature annealing schedule
- Implement replay buffer for more stable learning
- Support for neural network Q-function approximation
