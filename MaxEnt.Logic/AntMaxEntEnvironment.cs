namespace MaxEnt.Logic
{
    /// <summary>
    /// Ant MaxEnt environment implementation for reinforcement learning.
    /// Simulates a quadruped ant robot learning to walk using maximum entropy RL.
    /// The ant has 8 actuated joints (2 per leg) and must learn coordinated locomotion.
    /// </summary>
    public class AntMaxEntEnvironment
    {
        private const double Gravity = 9.8;
        private const double TimeStep = 0.02;
        private const double TorqueLimit = 1.0;

        private const int StateSize = 27;
        private const int ActionSize = 8;

        private const double MaxHeight = 2.0;
        private const double MinHeight = 0.2;
        private const double MaxDistance = 50.0;

        public double PositionX { get; private set; }
        public double PositionY { get; private set; }
        public double PositionZ { get; private set; }
        public double VelocityX { get; private set; }
        public double VelocityY { get; private set; }
        public double VelocityZ { get; private set; }

        public double Roll { get; private set; }
        public double Pitch { get; private set; }
        public double Yaw { get; private set; }
        public double AngularVelocityX { get; private set; }
        public double AngularVelocityY { get; private set; }
        public double AngularVelocityZ { get; private set; }

        private double[] jointAngles = new double[8];
        private double[] jointVelocities = new double[8];

        private Random random = new Random();
        private int stepCount = 0;

        public AntMaxEntEnvironment()
        {
            Reset();
        }

        /// <summary>
        /// Reset the environment to initial state. Returns initial state vector (27 dimensions).
        /// </summary>
        public double[] Reset()
        {
            PositionX = 0.0;
            PositionY = 0.0;
            PositionZ = 0.75;

            VelocityX = 0.0;
            VelocityY = 0.0;
            VelocityZ = 0.0;

            Roll = (random.NextDouble() - 0.5) * 0.1;
            Pitch = (random.NextDouble() - 0.5) * 0.1;
            Yaw = 0.0;
            AngularVelocityX = 0.0;
            AngularVelocityY = 0.0;
            AngularVelocityZ = 0.0;

            for (int i = 0; i < 8; i++)
            {
                jointAngles[i] = (random.NextDouble() - 0.5) * 0.2;
                jointVelocities[i] = 0.0;
            }

            stepCount = 0;

            return GetState();
        }

        /// <summary>
        /// Execute one time step. actions: 8 torque values normalized to [-1, 1].
        /// Returns (next_state, reward, done).
        /// </summary>
        public (double[] nextState, double reward, bool done) Step(double[] actions)
        {
            if (actions.Length != ActionSize)
                throw new ArgumentException($"Expected {ActionSize} actions, got {actions.Length}");

            stepCount++;

            double[] torques = new double[ActionSize];
            for (int i = 0; i < ActionSize; i++)
            {
                torques[i] = Math.Clamp(actions[i], -1.0, 1.0) * TorqueLimit;
            }

            for (int i = 0; i < ActionSize; i++)
            {
                double damping = 0.5;
                double jointAccel = torques[i] - damping * jointVelocities[i];
                jointVelocities[i] += jointAccel * TimeStep;
                jointAngles[i] += jointVelocities[i] * TimeStep;
                jointAngles[i] = Math.Clamp(jointAngles[i], -Math.PI / 2, Math.PI / 2);
            }

            double avgLegExtension = 0.0;
            for (int i = 0; i < ActionSize; i++)
                avgLegExtension += Math.Cos(jointAngles[i]);
            avgLegExtension /= ActionSize;

            double forwardComponent = CalculateForwardMovement(torques);
            VelocityX += forwardComponent * TimeStep;
            VelocityX *= 0.98;

            PositionX += VelocityX * TimeStep;
            PositionY += VelocityY * TimeStep;

            double targetHeight = 0.5 + avgLegExtension * 0.25;
            PositionZ += (targetHeight - PositionZ) * 0.1;

            UpdateOrientation(torques);

            bool fallen = PositionZ < MinHeight || PositionZ > MaxHeight;
            bool tooFar = Math.Abs(PositionX) > MaxDistance || Math.Abs(PositionY) > MaxDistance;
            bool tooTilted = Math.Abs(Roll) > Math.PI / 3 || Math.Abs(Pitch) > Math.PI / 3;
            bool done = fallen || tooFar || tooTilted;

            double forwardReward = VelocityX;
            double stabilityBonus = 1.0 - Math.Abs(Roll) - Math.Abs(Pitch);
            double controlCost = 0.0;
            for (int i = 0; i < ActionSize; i++)
                controlCost += torques[i] * torques[i];
            controlCost *= 0.01;

            double heightBonus = (PositionZ > 0.6 && PositionZ < 0.9) ? 0.5 : 0.0;
            double reward = forwardReward + stabilityBonus + heightBonus - controlCost;

            if (done)
                reward -= 10.0;

            return (GetState(), reward, done);
        }

        private double CalculateForwardMovement(double[] torques)
        {
            double leftTorque = (torques[0] + torques[2] + torques[4] + torques[6]) / 4.0;
            double rightTorque = (torques[1] + torques[3] + torques[5] + torques[7]) / 4.0;
            return (leftTorque + rightTorque) * 0.5;
        }

        private void UpdateOrientation(double[] torques)
        {
            double leftTorque = (torques[0] + torques[2] + torques[4] + torques[6]) / 4.0;
            double rightTorque = (torques[1] + torques[3] + torques[5] + torques[7]) / 4.0;
            double frontTorque = (torques[0] + torques[1] + torques[2] + torques[3]) / 4.0;
            double backTorque = (torques[4] + torques[5] + torques[6] + torques[7]) / 4.0;

            double rollTorque = (rightTorque - leftTorque) * 0.1;
            double pitchTorque = (frontTorque - backTorque) * 0.1;

            AngularVelocityX += rollTorque * TimeStep;
            AngularVelocityY += pitchTorque * TimeStep;

            AngularVelocityX *= 0.9;
            AngularVelocityY *= 0.9;
            AngularVelocityZ *= 0.9;

            Roll += AngularVelocityX * TimeStep;
            Pitch += AngularVelocityY * TimeStep;
            Yaw += AngularVelocityZ * TimeStep;
        }

        private double[] GetState()
        {
            var state = new double[27];
            int idx = 0;

            state[idx++] = PositionX;
            state[idx++] = PositionY;
            state[idx++] = PositionZ;

            state[idx++] = VelocityX;
            state[idx++] = VelocityY;
            state[idx++] = VelocityZ;

            state[idx++] = Roll;
            state[idx++] = Pitch;
            state[idx++] = Yaw;

            state[idx++] = AngularVelocityX;
            state[idx++] = AngularVelocityY;
            state[idx++] = AngularVelocityZ;

            for (int i = 0; i < 8; i++)
                state[idx++] = jointAngles[i];

            for (int i = 0; i < 7; i++)
                state[idx++] = jointVelocities[i];

            return state;
        }

        /// <summary>
        /// Get current step count
        /// </summary>
        public int GetStepCount() => stepCount;
    }
}
