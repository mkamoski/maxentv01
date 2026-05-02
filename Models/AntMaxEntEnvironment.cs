namespace MyMaxEntV01.Models
{
    /// <summary>
    /// Ant MaxEnt environment implementation for reinforcement learning.
    /// Simulates a quadruped ant robot learning to walk using maximum entropy RL.
    /// The ant has 8 actuated joints (2 per leg) and must learn coordinated locomotion.
    /// </summary>
    public class AntMaxEntEnvironment
    {
        // Physics constants
        private const double Gravity = 9.8;
        private const double TimeStep = 0.02; // 20ms per step
        private const double TorqueLimit = 1.0;

        // State space dimensions
        private const int StateSize = 27; // Position, orientation, joint angles, velocities
        private const int ActionSize = 8; // 8 joint torques (2 per leg)

        // Thresholds
        private const double MaxHeight = 2.0;
        private const double MinHeight = 0.2;
        private const double MaxDistance = 50.0;

        // State variables - Core body
        public double PositionX { get; private set; }
        public double PositionY { get; private set; }
        public double PositionZ { get; private set; } // Height
        public double VelocityX { get; private set; }
        public double VelocityY { get; private set; }
        public double VelocityZ { get; private set; }

        // Orientation (simplified - using Euler angles)
        public double Roll { get; private set; }
        public double Pitch { get; private set; }
        public double Yaw { get; private set; }
        public double AngularVelocityX { get; private set; }
        public double AngularVelocityY { get; private set; }
        public double AngularVelocityZ { get; private set; }

        // Joint angles (8 joints: 2 per leg × 4 legs)
        private double[] jointAngles = new double[8];
        private double[] jointVelocities = new double[8];

        private Random random = new Random();
        private int stepCount = 0;

        public AntMaxEntEnvironment()
        {
            Reset();
        }

        /// <summary>
        /// Reset the environment to initial state
        /// </summary>
        /// <returns>Initial state vector [27 dimensions]</returns>
        public double[] Reset()
        {
            // Reset position to standing pose
            PositionX = 0.0;
            PositionY = 0.0;
            PositionZ = 0.75; // Standing height

            VelocityX = 0.0;
            VelocityY = 0.0;
            VelocityZ = 0.0;

            // Reset orientation (upright)
            Roll = (random.NextDouble() - 0.5) * 0.1;
            Pitch = (random.NextDouble() - 0.5) * 0.1;
            Yaw = 0.0;
            AngularVelocityX = 0.0;
            AngularVelocityY = 0.0;
            AngularVelocityZ = 0.0;

            // Initialize joints with slight random variations
            for (int i = 0; i < 8; i++)
            {
                jointAngles[i] = (random.NextDouble() - 0.5) * 0.2;
                jointVelocities[i] = 0.0;
            }

            stepCount = 0;

            return GetState();
        }

        /// <summary>
        /// Execute one time step in the environment
        /// </summary>
        /// <param name="actions">Array of 8 torque values (one per joint), normalized to [-1, 1]</param>
        /// <returns>Tuple of (next_state, reward, done)</returns>
        public (double[] nextState, double reward, bool done) Step(double[] actions)
        {
            if (actions.Length != ActionSize)
                throw new ArgumentException($"Expected {ActionSize} actions, got {actions.Length}");

            stepCount++;

            // Clamp actions to valid range and apply torque limits
            double[] torques = new double[ActionSize];
            for (int i = 0; i < ActionSize; i++)
            {
                torques[i] = Math.Clamp(actions[i], -1.0, 1.0) * TorqueLimit;
            }

            // Simulate joint dynamics (simplified)
            for (int i = 0; i < ActionSize; i++)
            {
                // Simple damped joint model: acceleration = torque - damping * velocity
                double damping = 0.5;
                double jointAccel = torques[i] - damping * jointVelocities[i];

                jointVelocities[i] += jointAccel * TimeStep;
                jointAngles[i] += jointVelocities[i] * TimeStep;

                // Limit joint angles to realistic ranges
                jointAngles[i] = Math.Clamp(jointAngles[i], -Math.PI / 2, Math.PI / 2);
            }

            // Compute forward kinematics (simplified - based on average joint configuration)
            double avgLegExtension = 0.0;
            for (int i = 0; i < ActionSize; i++)
            {
                avgLegExtension += Math.Cos(jointAngles[i]);
            }
            avgLegExtension /= ActionSize;

            // Update body position based on joint configuration
            double forwardComponent = CalculateForwardMovement(torques);
            VelocityX += forwardComponent * TimeStep;
            VelocityX *= 0.98; // Air resistance

            PositionX += VelocityX * TimeStep;
            PositionY += VelocityY * TimeStep;

            // Update height based on leg extension
            double targetHeight = 0.5 + avgLegExtension * 0.25;
            PositionZ += (targetHeight - PositionZ) * 0.1; // Smooth height adjustment

            // Update orientation based on asymmetric forces
            UpdateOrientation(torques);

            // Check termination conditions
            bool fallen = PositionZ < MinHeight || PositionZ > MaxHeight;
            bool tooFar = Math.Abs(PositionX) > MaxDistance || Math.Abs(PositionY) > MaxDistance;
            bool tooTilted = Math.Abs(Roll) > Math.PI / 3 || Math.Abs(Pitch) > Math.PI / 3;

            bool done = fallen || tooFar || tooTilted;

            // Compute reward: forward progress + stability bonus - control cost
            double forwardReward = VelocityX; // Reward forward movement
            double stabilityBonus = 1.0 - Math.Abs(Roll) - Math.Abs(Pitch); // Stay upright
            double controlCost = 0.0;
            for (int i = 0; i < ActionSize; i++)
            {
                controlCost += torques[i] * torques[i];
            }
            controlCost *= 0.01; // Small penalty for energy use

            double heightBonus = (PositionZ > 0.6 && PositionZ < 0.9) ? 0.5 : 0.0;

            double reward = forwardReward + stabilityBonus + heightBonus - controlCost;

            if (done)
                reward -= 10.0; // Large penalty for falling

            return (GetState(), reward, done);
        }

        /// <summary>
        /// Calculate forward movement based on coordinated leg actions
        /// </summary>
        private double CalculateForwardMovement(double[] torques)
        {
            // Simple model: synchronized leg movements produce forward motion
            // Left legs (0, 2, 4, 6) vs right legs (1, 3, 5, 7)
            double leftTorque = (torques[0] + torques[2] + torques[4] + torques[6]) / 4.0;
            double rightTorque = (torques[1] + torques[3] + torques[5] + torques[7]) / 4.0;

            // Forward motion when legs push backward in coordination
            double forwardForce = (leftTorque + rightTorque) * 0.5;

            return forwardForce;
        }

        /// <summary>
        /// Update body orientation based on asymmetric torque application
        /// </summary>
        private void UpdateOrientation(double[] torques)
        {
            // Calculate torque imbalance
            double leftTorque = (torques[0] + torques[2] + torques[4] + torques[6]) / 4.0;
            double rightTorque = (torques[1] + torques[3] + torques[5] + torques[7]) / 4.0;
            double frontTorque = (torques[0] + torques[1] + torques[2] + torques[3]) / 4.0;
            double backTorque = (torques[4] + torques[5] + torques[6] + torques[7]) / 4.0;

            // Update angular velocities based on torque imbalance
            double rollTorque = (rightTorque - leftTorque) * 0.1;
            double pitchTorque = (frontTorque - backTorque) * 0.1;

            AngularVelocityX += rollTorque * TimeStep;
            AngularVelocityY += pitchTorque * TimeStep;

            // Apply damping
            AngularVelocityX *= 0.9;
            AngularVelocityY *= 0.9;
            AngularVelocityZ *= 0.9;

            // Update angles
            Roll += AngularVelocityX * TimeStep;
            Pitch += AngularVelocityY * TimeStep;
            Yaw += AngularVelocityZ * TimeStep;
        }

        /// <summary>
        /// Get current state as a vector (27 dimensions)
        /// </summary>
        private double[] GetState()
        {
            var state = new double[27];
            int idx = 0;

            // Position (3)
            state[idx++] = PositionX;
            state[idx++] = PositionY;
            state[idx++] = PositionZ;

            // Velocity (3)
            state[idx++] = VelocityX;
            state[idx++] = VelocityY;
            state[idx++] = VelocityZ;

            // Orientation (3)
            state[idx++] = Roll;
            state[idx++] = Pitch;
            state[idx++] = Yaw;

            // Angular velocity (3)
            state[idx++] = AngularVelocityX;
            state[idx++] = AngularVelocityY;
            state[idx++] = AngularVelocityZ;

            // Joint angles (8)
            for (int i = 0; i < 8; i++)
            {
                state[idx++] = jointAngles[i];
            }

            // Joint velocities (7 - last one implicit)
            for (int i = 0; i < 7; i++)
            {
                state[idx++] = jointVelocities[i];
            }

            return state;
        }

        /// <summary>
        /// Get current step count
        /// </summary>
        public int GetStepCount() => stepCount;
    }
}
