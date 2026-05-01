using System.Numerics;

namespace MyMaxEntV01.Models
{
    /// <summary>
    /// Maximum Entropy Reinforcement Learning Agent
    /// Uses soft Q-learning with entropy regularization to encourage exploration
    /// </summary>
    public class MaxEntAgent
    {
        private readonly int stateSize;
        private readonly int actionSize;
        private readonly double learningRate;
        private readonly double discountFactor;
        private readonly double temperature; // Controls entropy regularization

        // Simple linear Q-function approximation: Q(s,a) = W * s + b
        private double[,] weights; // [actionSize, stateSize]
        private double[] biases; // [actionSize]

        // Policy probabilities (softmax over Q-values)
        private double[] actionProbabilities;

        private Random random = new Random();

        public MaxEntAgent(int stateSize, int actionSize, double learningRate = 0.01, 
                          double discountFactor = 0.99, double temperature = 1.0)
        {
            this.stateSize = stateSize;
            this.actionSize = actionSize;
            this.learningRate = learningRate;
            this.discountFactor = discountFactor;
            this.temperature = temperature;

            // Initialize weights and biases with small random values
            weights = new double[actionSize, stateSize];
            biases = new double[actionSize];
            actionProbabilities = new double[actionSize];

            InitializeParameters();
        }

        private void InitializeParameters()
        {
            for (int a = 0; a < actionSize; a++)
            {
                for (int s = 0; s < stateSize; s++)
                {
                    weights[a, s] = (random.NextDouble() - 0.5) * 0.1;
                }
                biases[a] = (random.NextDouble() - 0.5) * 0.1;
            }
        }

        /// <summary>
        /// Select action using softmax policy (MaxEnt exploration)
        /// </summary>
        public int SelectAction(double[] state)
        {
            // Compute Q-values for all actions
            double[] qValues = new double[actionSize];
            for (int a = 0; a < actionSize; a++)
            {
                qValues[a] = ComputeQValue(state, a);
            }

            // Compute softmax probabilities with temperature
            ComputeSoftmax(qValues);

            // Sample action from probability distribution
            return SampleAction();
        }

        /// <summary>
        /// Update Q-function using MaxEnt soft Q-learning
        /// </summary>
        public void Learn(double[] state, int action, double reward, double[] nextState, bool done)
        {
            // Compute current Q-value
            double qValue = ComputeQValue(state, action);

            // Compute soft value of next state (log-sum-exp)
            double nextValue = 0.0;
            if (!done)
            {
                nextValue = ComputeSoftValue(nextState);
            }

            // MaxEnt temporal difference target
            double target = reward + (done ? 0.0 : discountFactor * nextValue);
            double tdError = target - qValue;

            // Gradient descent update: W += α * tdError * state
            for (int s = 0; s < stateSize; s++)
            {
                weights[action, s] += learningRate * tdError * state[s];
            }
            biases[action] += learningRate * tdError;
        }

        /// <summary>
        /// Compute Q-value for a state-action pair: Q(s,a) = W_a · s + b_a
        /// </summary>
        private double ComputeQValue(double[] state, int action)
        {
            double qValue = biases[action];
            for (int s = 0; s < stateSize; s++)
            {
                qValue += weights[action, s] * state[s];
            }
            return qValue;
        }

        /// <summary>
        /// Compute soft value function: V(s) = τ * log Σ exp(Q(s,a) / τ)
        /// This is the log-sum-exp (smooth maximum) with temperature
        /// </summary>
        private double ComputeSoftValue(double[] state)
        {
            double[] qValues = new double[actionSize];
            double maxQ = double.NegativeInfinity;

            // Compute Q-values and find max (for numerical stability)
            for (int a = 0; a < actionSize; a++)
            {
                qValues[a] = ComputeQValue(state, a);
                if (qValues[a] > maxQ)
                    maxQ = qValues[a];
            }

            // Compute log-sum-exp with numerical stability trick
            double sumExp = 0.0;
            for (int a = 0; a < actionSize; a++)
            {
                sumExp += Math.Exp((qValues[a] - maxQ) / temperature);
            }

            return temperature * (Math.Log(sumExp) + maxQ / temperature);
        }

        /// <summary>
        /// Compute softmax probabilities over actions
        /// </summary>
        private void ComputeSoftmax(double[] qValues)
        {
            double maxQ = qValues.Max();
            double sumExp = 0.0;

            for (int a = 0; a < actionSize; a++)
            {
                actionProbabilities[a] = Math.Exp((qValues[a] - maxQ) / temperature);
                sumExp += actionProbabilities[a];
            }

            // Normalize to get probabilities
            for (int a = 0; a < actionSize; a++)
            {
                actionProbabilities[a] /= sumExp;
            }
        }

        /// <summary>
        /// Sample action from current probability distribution
        /// </summary>
        private int SampleAction()
        {
            double r = random.NextDouble();
            double cumulative = 0.0;

            for (int a = 0; a < actionSize; a++)
            {
                cumulative += actionProbabilities[a];
                if (r <= cumulative)
                    return a;
            }

            return actionSize - 1; // Fallback
        }

        /// <summary>
        /// Get current policy entropy: H = -Σ π(a) log π(a)
        /// Higher entropy means more exploration
        /// </summary>
        public double GetEntropy()
        {
            double entropy = 0.0;
            for (int a = 0; a < actionSize; a++)
            {
                if (actionProbabilities[a] > 0)
                {
                    entropy -= actionProbabilities[a] * Math.Log(actionProbabilities[a]);
                }
            }
            return entropy;
        }
    }
}
