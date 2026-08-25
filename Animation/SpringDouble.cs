using System;

namespace Project_Nook.Animation
{
    /// <summary>
    /// A damped spring on a single scalar, parameterized the way Apple exposes motion to designers:
    /// a damping ratio (overshoot) and a response (how quickly the value reaches the target, in seconds)
    /// instead of raw mass/stiffness/damping.
    ///
    /// The spring is integrated analytically, so it is stable at any frame delta, and it always advances
    /// from its current value and velocity. That is what makes it interruptible: re-targeting mid-flight
    /// keeps both position and velocity continuous, so a reversed gesture never hits a velocity discontinuity.
    /// </summary>
    public sealed class SpringDouble
    {
        /// <summary>Damping ratio. 1.0 is critically damped (no overshoot); below 1.0 overshoots and settles.</summary>
        public double DampingRatio { get; private set; } = 1.0;

        /// <summary>Approximate time in seconds for the value to reach the target. Not a duration: the spring has none.</summary>
        public double Response { get; private set; } = 0.35;

        public double Value { get; private set; }
        public double Velocity { get; private set; }
        public double Target { get; private set; }

        /// <summary>Called with the new value after every integration step.</summary>
        public Action<double> Apply { get; }

        private const double RestDisplacement = 0.02;
        private const double RestVelocity = 0.2;

        public SpringDouble(Action<double> apply)
        {
            Apply = apply ?? throw new ArgumentNullException(nameof(apply));
        }

        public bool IsAtRest =>
            Math.Abs(Value - Target) < RestDisplacement && Math.Abs(Velocity) < RestVelocity;

        /// <summary>Jump to a value with no motion. Use for initial state only.</summary>
        public void SnapTo(double value)
        {
            Value = value;
            Target = value;
            Velocity = 0;
            Apply(Value);
        }

        /// <summary>
        /// Point the spring at a new target. Current value and velocity are preserved, so this is safe to
        /// call every frame (a slider drag) or mid-flight (a reversed gesture).
        /// </summary>
        public void Retarget(double target, double response, double dampingRatio)
        {
            Target = target;
            Response = Math.Max(0.02, response);
            DampingRatio = Math.Min(1.0, Math.Max(0.1, dampingRatio));
        }

        /// <summary>Advance by dt seconds. Returns false once the spring has settled.</summary>
        public bool Tick(double dt)
        {
            if (IsAtRest)
            {
                if (Value != Target)
                {
                    Value = Target;
                    Velocity = 0;
                    Apply(Value);
                }
                return false;
            }

            // Clamp so a stalled frame cannot teleport the value.
            if (dt > 0.064) dt = 0.064;
            if (dt <= 0) return true;

            double omega = 2.0 * Math.PI / Response;
            double zeta = DampingRatio;

            double x = Value - Target;   // displacement from rest
            double v = Velocity;
            double decay = Math.Exp(-zeta * omega * dt);

            double newX, newV;

            if (zeta < 0.9999)
            {
                double omegaD = omega * Math.Sqrt(1.0 - zeta * zeta);
                double c = (v + zeta * omega * x) / omegaD;
                double cos = Math.Cos(omegaD * dt);
                double sin = Math.Sin(omegaD * dt);

                newX = decay * (x * cos + c * sin);
                newV = decay * ((c * omegaD - zeta * omega * x) * cos
                              - (x * omegaD + zeta * omega * c) * sin);
            }
            else
            {
                // Critically damped closed form.
                double b = v + omega * x;
                newX = (x + b * dt) * decay;
                newV = (v - omega * b * dt) * decay;
            }

            Value = Target + newX;
            Velocity = newV;
            Apply(Value);
            return true;
        }
    }
}
