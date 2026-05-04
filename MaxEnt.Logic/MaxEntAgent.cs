namespace MaxEnt.Logic
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
            double[] qValues = new double[actionSize];
            for (int a = 0; a < actionSize; a++)
            {
                qValues[a] = ComputeQValue(state, a);
            }

            ComputeSoftmax(qValues);

            return SampleAction();
        }

        /// <summary>
        /// Update Q-function using MaxEnt soft Q-learning
        /// </summary>
        public void Learn(double[] state, int action, double reward, double[] nextState, bool done)
        {
            double qValue = ComputeQValue(state, action);

            double nextValue = 0.0;
            if (!done)
            {
                nextValue = ComputeSoftValue(nextState);
            }

            double target = reward + (done ? 0.0 : discountFactor * nextValue);
            double tdError = target - qValue;

            for (int s = 0; s < stateSize; s++)
            {
                weights[action, s] += learningRate * tdError * state[s];
            }
            biases[action] += learningRate * tdError;
        }

        private double ComputeQValue(double[] state, int action)
        {
            double qValue = biases[action];
            for (int s = 0; s < stateSize; s++)
            {
                qValue += weights[action, s] * state[s];
            }
            return qValue;
        }

        private double ComputeSoftValue(double[] state)
        {
            double[] qValues = new double[actionSize];
            double maxQ = double.NegativeInfinity;

            for (int a = 0; a < actionSize; a++)
            {
                qValues[a] = ComputeQValue(state, a);
                if (qValues[a] > maxQ)
                    maxQ = qValues[a];
            }

            double sumExp = 0.0;
            for (int a = 0; a < actionSize; a++)
            {
                sumExp += Math.Exp((qValues[a] - maxQ) / temperature);
            }

            return temperature * (Math.Log(sumExp) + maxQ / temperature);
        }

        private void ComputeSoftmax(double[] qValues)
        {
            double maxQ = qValues.Max();
            double sumExp = 0.0;

            for (int a = 0; a < actionSize; a++)
            {
                actionProbabilities[a] = Math.Exp((qValues[a] - maxQ) / temperature);
                sumExp += actionProbabilities[a];
            }

            for (int a = 0; a < actionSize; a++)
            {
                actionProbabilities[a] /= sumExp;
            }
        }

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

            return actionSize - 1;
        }

        /// <summary>
        /// Get current policy entropy: H = -sum pi(a) log pi(a)
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
