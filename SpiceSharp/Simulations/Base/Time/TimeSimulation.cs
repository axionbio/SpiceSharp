﻿using System.Collections.Generic;
using SpiceSharp.Behaviors;
using SpiceSharp.Entities;

namespace SpiceSharp.Simulations
{
    /// <summary>
    /// A base class for time-domain analysis.
    /// </summary>
    /// <seealso cref="BiasingSimulation" />
    public abstract partial class TimeSimulation : BiasingSimulation,
        ITimeSimulation,
        IBehavioral<IAcceptBehavior>
    {
        /// <summary>
        /// Time-domain behaviors.
        /// </summary>
        private BehaviorList<ITimeBehavior> _transientBehaviors;
        private BehaviorList<IAcceptBehavior> _acceptBehaviors;
        private readonly List<ConvergenceAid> _initialConditions = new List<ConvergenceAid>();
        private bool _shouldReorder = true, _useIc;

        /// <summary>
        /// Gets the statistics.
        /// </summary>
        /// <value>
        /// The statistics.
        /// </value>
        public new TimeSimulationStatistics Statistics { get; }

        /// <summary>
        /// Gets the state of the time.
        /// </summary>
        /// <value>
        /// The state of the time.
        /// </value>
        protected TimeSimulationState TimeState { get; }

        /// <summary>
        /// Gets the state.
        /// </summary>
        /// <value>
        /// The state.
        /// </value>
        public new ITimeSimulationState State => TimeState;

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeSimulation"/> class.
        /// </summary>
        /// <param name="name">The identifier of the simulation.</param>
        protected TimeSimulation(string name) : base(name)
        {
            Configurations.Add(new TimeConfiguration());
            Statistics = new TimeSimulationStatistics();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeSimulation"/> class.
        /// </summary>
        /// <param name="name">The identifier of the simulation.</param>
        /// <param name="step">The step size.</param>
        /// <param name="final">The final time.</param>
        protected TimeSimulation(string name, double step, double final)
            : base(name)
        {
            Configurations.Add(new TimeConfiguration(step, final));
            Statistics = new TimeSimulationStatistics();
            TimeState = new TimeSimulationState();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeSimulation"/> class.
        /// </summary>
        /// <param name="name">The identifier of the simulation.</param>
        /// <param name="step">The step size.</param>
        /// <param name="final">The final time.</param>
        /// <param name="maxStep">The maximum step.</param>
        protected TimeSimulation(string name, double step, double final, double maxStep)
            : base(name)
        {
            Configurations.Add(new TimeConfiguration(step, final, maxStep));
            Statistics = new TimeSimulationStatistics();
            TimeState = new TimeSimulationState();
        }

        /// <summary>
        /// Gets the state.
        /// </summary>
        /// <param name="state">The state.</param>
        public void GetState(out ITimeSimulationState state) => state = TimeState;

        /// <summary>
        /// Set up the simulation.
        /// </summary>
        /// <param name="entities">The circuit that will be used.</param>
        protected override void Setup(IEntityCollection entities)
        {
            entities.ThrowIfNull(nameof(entities));

            // Get behaviors and configurations
            var config = Configurations.GetValue<TimeConfiguration>().ThrowIfNull("time configuration");
            _useIc = config.UseIc;
            TimeState.Method = config.Method.ThrowIfNull("method");

            // Setup
            base.Setup(entities);

            // Cache local variables
            _transientBehaviors = EntityBehaviors.GetBehaviorList<ITimeBehavior>();
            _acceptBehaviors = EntityBehaviors.GetBehaviorList<IAcceptBehavior>();

            TimeState.Setup(this);

            // Set up initial conditions
            foreach (var ic in config.InitialConditions)
                _initialConditions.Add(new ConvergenceAid(ic.Key, ic.Value));
        }

        /// <summary>
        /// Executes the simulation.
        /// </summary>
        protected override void Execute()
        {
            base.Execute();

            // Apply initial conditions if they are not set for the devices (UseIc).
            if (_initialConditions.Count > 0 && !BiasingState.UseIc)
            {
                // Initialize initial conditions
                foreach (var ic in _initialConditions)
                    ic.Initialize(this);
                AfterLoad += LoadInitialConditions;
            }

            // Calculate the operating point of the circuit
            BiasingState.UseIc = _useIc;
            BiasingState.UseDc = true;
            Op(DcMaxIterations);
            Statistics.TimePoints++;

            // Stop calculating the operating point
            BiasingState.UseIc = false;
            BiasingState.UseDc = false;
            InitializeStates();
            AfterLoad -= LoadInitialConditions;
        }

        /// <summary>
        /// Destroys the simulation.
        /// </summary>
        protected override void Unsetup()
        {
            // Remove references
            _transientBehaviors = null;
            _acceptBehaviors = null;

            // Destroy the integration method
            TimeState.Unsetup();

            // Destroy the initial conditions
            AfterLoad -= LoadInitialConditions;
            _initialConditions.Clear();

            base.Unsetup();
        }

        /// <summary>
        /// Iterates to a solution for time simulations.
        /// </summary>
        /// <param name="maxIterations">The maximum number of iterations.</param>
        /// <returns>
        ///   <c>true</c> if the iterations converged to a solution; otherwise, <c>false</c>.
        /// </returns>
        protected bool TimeIterate(int maxIterations)
        {
            var solver = BiasingState.Solver;
            // var pass = false;
            var iterno = 0;
            var initTransient = TimeState.Method.BaseTime.Equals(0.0);
            var state = BiasingState;

            // Ignore operating condition point, just use the solution as-is
            if (state.UseIc)
            {
                state.StoreSolution();

                // Voltages are set using IC statement on the nodes
                // Internal initial conditions are calculated by the components
                Load();
                return true;
            }

            // Perform iteration
            while (true)
            {
                // Reset convergence flag
                state.IsConvergent = true;

                try
                {
                    // Load the Y-matrix and Rhs-vector for DC and transients
                    Load();
                    iterno++;
                }
                catch (CircuitException)
                {
                    iterno++;
                    base.Statistics.Iterations = iterno;
                    throw;
                }

                // Preordering is already done in the operating point calculation
                if (state.Init == InitializationModes.Junction || initTransient)
                    _shouldReorder = true;

                // Reorder
                if (_shouldReorder)
                {
                    base.Statistics.ReorderTime.Start();
                    solver.OrderAndFactor();
                    base.Statistics.ReorderTime.Stop();
                    _shouldReorder = false;
                }
                else
                {
                    // Decompose
                    base.Statistics.DecompositionTime.Start();
                    var success = solver.Factor();
                    base.Statistics.DecompositionTime.Stop();

                    if (!success)
                    {
                        _shouldReorder = true;
                        continue;
                    }
                }

                // The current solution becomes the old solution
                state.StoreSolution();

                // Solve the equation
                base.Statistics.SolveTime.Start();
                solver.Solve(state.Solution);
                base.Statistics.SolveTime.Stop();

                // Reset ground nodes
                state.Solution[0] = 0.0;
                state.OldSolution[0] = 0.0;

                // Exceeded maximum number of iterations
                if (iterno > maxIterations)
                {
                    base.Statistics.Iterations += iterno;
                    return false;
                }

                if (state.IsConvergent && iterno != 1)
                    state.IsConvergent = IsConvergent();
                else
                    state.IsConvergent = false;

                if (initTransient)
                {
                    initTransient = false;
                    if (iterno <= 1)
                        _shouldReorder = true;
                    state.Init = InitializationModes.Float;
                }
                else
                {
                    switch (state.Init)
                    {
                        case InitializationModes.Float:
                            if (state.IsConvergent)
                            {
                                base.Statistics.Iterations += iterno;
                                return true;
                            }

                            break;

                        case InitializationModes.Junction:
                            state.Init = InitializationModes.Fix;
                            _shouldReorder = true;
                            break;

                        case InitializationModes.Fix:
                            if (state.IsConvergent)
                                state.Init = InitializationModes.Float;
                            break;

                        case InitializationModes.None:
                            state.Init = InitializationModes.Float;
                            break;

                        default:
                            base.Statistics.Iterations += iterno;
                            throw new CircuitException("Could not find flag");
                    }
                }
            }
        }

        /// <summary>
        /// Initializes all transient behaviors to assume that the current solution is the DC solution.
        /// </summary>
        protected virtual void InitializeStates()
        {
            foreach (var behavior in _transientBehaviors)
                behavior.InitializeStates();
            TimeState.Method.Initialize(this);
        }

        /// <summary>
        /// Applies initial conditions.
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Arguments</param>
        protected void LoadInitialConditions(object sender, LoadStateEventArgs e)
        {
            foreach (var ic in _initialConditions)
                ic.Aid();
        }

        /// <summary>
        /// Load all behaviors for time simulation.
        /// </summary>
        protected override void LoadBehaviors()
        {
            base.LoadBehaviors();

            // Not calculating DC behavior
            if (!BiasingState.UseDc)
            {
                foreach (var behavior in _transientBehaviors)
                    behavior.Load();
            }
        }

        /// <summary>
        /// Accepts the current simulation state as a valid timepoint.
        /// </summary>
        protected void Accept()
        {
            foreach (var behavior in _acceptBehaviors)
                behavior.Accept();
            TimeState.Method.Accept(this);
            Statistics.Accepted++;
        }

        /// <summary>
        /// Probe for a new time point.
        /// </summary>
        /// <param name="delta">The timestep.</param>
        protected void Probe(double delta)
        {
            TimeState.Method.Probe(this, delta);
            foreach (var behavior in _acceptBehaviors)
                behavior.Probe();
        }
    }
}
