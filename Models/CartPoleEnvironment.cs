namespace MyMaxEntV01.Models
{
    /// <summary>
    /// CartPole environment implementation for reinforcement learning.
    /// The goal is to balance a pole on a moving cart by applying forces to the cart.
    /// </summary>
    public class CartPoleEnvironment
    {
        // Physics constants
        private const double Gravity = 9.8;
        private const double MassCart = 1.0;
        private const double MassPole = 0.1;
        private const double TotalMass = MassCart + MassPole;
        private const double Length = 0.5; // Half the pole's length
        private const double PoleMassLength = MassPole * Length;
        private const double ForceMag = 10.0;
        private const double Tau = 0.02; // Time step (20ms)

        // Thresholds
        private const double ThetaThresholdRadians = 12 * 2 * Math.PI / 360;
        private const double XThreshold = 2.4;

        // State variables
        public double CartPosition { get; private set; }
        public double CartVelocity { get; private set; }
        public double PoleAngle { get; private set; }
        public double PoleAngularVelocity { get; private set; }

        private Random random = new Random();

        public CartPoleEnvironment()
        {
            Reset();
        }

        /// <summary>
        /// Reset the environment to initial state
        /// </summary>
        /// <returns>Initial state vector [cart_pos, cart_vel, pole_angle, pole_vel]</returns>
        public double[] Reset()
        {
            // Initialize with small random values
            CartPosition = (random.NextDouble() - 0.5) * 0.1;
            CartVelocity = (random.NextDouble() - 0.5) * 0.1;
            PoleAngle = (random.NextDouble() - 0.5) * 0.1;
            PoleAngularVelocity = (random.NextDouble() - 0.5) * 0.1;

            return GetState();
        }

        /// <summary>
        /// Execute one time step in the environment
        /// </summary>
        /// <param name="action">0 = push left, 1 = push right</param>
        /// <returns>Tuple of (next_state, reward, done)</returns>
        public (double[] nextState, double reward, bool done) Step(int action)
        {
            // Apply force to the cart (left or right)
            double force = action == 1 ? ForceMag : -ForceMag;

            // Calculate physics using simplified equations
            double costheta = Math.Cos(PoleAngle);
            double sintheta = Math.Sin(PoleAngle);

            double temp = (force + PoleMassLength * PoleAngularVelocity * PoleAngularVelocity * sintheta) / TotalMass;
            double thetaacc = (Gravity * sintheta - costheta * temp) / 
                             (Length * (4.0 / 3.0 - MassPole * costheta * costheta / TotalMass));
            double xacc = temp - PoleMassLength * thetaacc * costheta / TotalMass;

            // Update state using Euler's method
            CartPosition += Tau * CartVelocity;
            CartVelocity += Tau * xacc;
            PoleAngle += Tau * PoleAngularVelocity;
            PoleAngularVelocity += Tau * thetaacc;

            // Check if episode is done (pole fell or cart went off screen)
            bool done = CartPosition < -XThreshold || 
                       CartPosition > XThreshold ||
                       PoleAngle < -ThetaThresholdRadians || 
                       PoleAngle > ThetaThresholdRadians;

            // Reward is 1.0 for each step the pole is balanced
            double reward = done ? 0.0 : 1.0;

            return (GetState(), reward, done);
        }

        /// <summary>
        /// Get current state as a vector
        /// </summary>
        private double[] GetState()
        {
            return new double[] 
            { 
                CartPosition, 
                CartVelocity, 
                PoleAngle, 
                PoleAngularVelocity 
            };
        }
    }
}
