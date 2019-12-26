﻿using SpiceSharp.Attributes;
using SpiceSharp.Behaviors;
using SpiceSharp.Simulations;
using SpiceSharp.Algebra;
using SpiceSharp.Components.CommonBehaviors;

namespace SpiceSharp.Components.SwitchBehaviors
{
    /// <summary>
    /// (DC) biasing behavior for switches.
    /// </summary>
    public class BiasingBehavior : Behavior, IBiasingBehavior
    {
        /// <summary>
        /// Gets the base parameters.
        /// </summary>
        protected BaseParameters BaseParameters { get; private set; }

        /// <summary>
        /// Gets the model parameters.
        /// </summary>
        protected ModelBaseParameters ModelParameters { get; private set; }

        /// <summary>
        /// Gets or sets the old state of the switch
        /// </summary>
        protected bool PreviousState { get; set; }

        /// <summary>
        /// Flag for using the old state or not
        /// </summary>
        protected bool UseOldState { get; set; }

        /// <summary>
        /// Gets the current state of the switch
        /// </summary>
        [ParameterName("state"), ParameterInfo("The current state of the switch.")]
        public bool CurrentState { get; protected set; }

        /// <summary>
        /// Gets the currently active conductance.
        /// </summary>
        public double Conductance { get; private set; }

        /// <summary>
        /// Gets the voltage over the switch.
        /// </summary>
        /// <returns></returns>
        [ParameterName("v"), ParameterInfo("Switch voltage")]
        public double GetVoltage() => BiasingState.ThrowIfNotBound(this).Solution[_posNode] - BiasingState.Solution[_negNode];

        /// <summary>
        /// Gets the current through the switch.
        /// </summary>
        /// <returns></returns>
        [ParameterName("i"), ParameterInfo("Switch current")]
        public double GetCurrent() => (BiasingState.ThrowIfNotBound(this).Solution[_posNode] - BiasingState.Solution[_negNode]) * Conductance;

        /// <summary>
        /// Gets the power dissipated by the switch.
        /// </summary>
        /// <returns></returns>
        [ParameterName("p"), ParameterInfo("Instantaneous power")]
        public double GetPower()
        {
            var v = (BiasingState.ThrowIfNotBound(this).Solution[_posNode] - BiasingState.Solution[_negNode]);
            return v * v * Conductance;
        }

        /// <summary>
        /// Gets the matrix elements.
        /// </summary>
        /// <value>
        /// The matrix elements.
        /// </value>
        protected ElementSet<double> Elements { get; private set; }

        /// <summary>
        /// Gets the method used for switching.
        /// </summary>
        protected Controller Method { get; }

        private readonly int _posNode, _negNode;
        private IIterationSimulationState _iteration;

        /// <summary>
        /// Gets the state of the biasing.
        /// </summary>
        /// <value>
        /// The state of the biasing.
        /// </value>
        protected IBiasingSimulationState BiasingState { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BiasingBehavior"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="context">The context.</param>
        public BiasingBehavior(string name, ComponentBindingContext context) : base(name)
        {
            context.ThrowIfNull(nameof(context));

            _iteration = context.GetState<IIterationSimulationState>();
            if (context is ControlledBindingContext ctx)
                Method = new CurrentControlled(ctx);
            else
                Method = new VoltageControlled(context);
            BiasingState = context.GetState<IBiasingSimulationState>();
            _posNode = BiasingState.Map[context.Nodes[0]];
            _negNode = BiasingState.Map[context.Nodes[1]];
            ModelParameters = context.ModelBehaviors.Parameters.GetValue<ModelBaseParameters>();
            BaseParameters = context.Behaviors.Parameters.GetValue<BaseParameters>();
            Elements = new ElementSet<double>(BiasingState.Solver,
                new MatrixLocation(_posNode, _posNode),
                new MatrixLocation(_posNode, _negNode),
                new MatrixLocation(_negNode, _posNode),
                new MatrixLocation(_negNode, _negNode));
        }

        /// <summary>
        /// Loads the Y-matrix and Rhs-vector.
        /// </summary>
        void IBiasingBehavior.Load()
        {
            bool currentState;
            var state = BiasingState.ThrowIfNotBound(this);

            // decide the state of the switch
            if (_iteration.Mode == IterationModes.Fix || _iteration.Mode == IterationModes.Junction)
            {
                if (BaseParameters.ZeroState)
                {
                    // Switch specified "on"
                    CurrentState = true;
                    currentState = true;
                }
                else
                {
                    // Switch specified "off"
                    CurrentState = false;
                    currentState = false;
                }
            }
            else
            {
                // Get the previous state
                var ctrl = Method.GetValue(state);
                if (UseOldState)
                {
                    // Calculate the current state
                    if (ctrl > ModelParameters.Threshold + ModelParameters.Hysteresis)
                        currentState = true;
                    else if (ctrl < ModelParameters.Threshold - ModelParameters.Hysteresis)
                        currentState = false;
                    else
                        currentState = PreviousState;
                    CurrentState = currentState;
                    UseOldState = false;
                }
                else
                {
                    PreviousState = CurrentState;

                    // Calculate the current state
                    if (ctrl > ModelParameters.Threshold + ModelParameters.Hysteresis)
                    {
                        CurrentState = true;
                        currentState = true;
                    }
                    else if (ctrl < ModelParameters.Threshold - ModelParameters.Hysteresis)
                    {
                        CurrentState = false;
                        currentState = false;
                    }
                    else
                        currentState = PreviousState;

                    // Ensure one more iteration
                    if (currentState != PreviousState)
                        _iteration.IsConvergent = false;
                }

                // Store the current state
                CurrentState = currentState;

                // If the state changed, ensure one more iteration
                if (currentState != PreviousState)
                    _iteration.IsConvergent = false;
            }

            // Get the current conduction
            var gNow = currentState ? ModelParameters.OnConductance : ModelParameters.OffConductance;
            Conductance = gNow;

            // Load the Y-matrix
            Elements.Add(gNow, -gNow, -gNow, gNow);
        }
    }
}
