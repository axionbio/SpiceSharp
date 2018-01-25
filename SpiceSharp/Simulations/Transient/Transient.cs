﻿using System;
using System.Collections.Generic;
using SpiceSharp.Behaviors;
using SpiceSharp.Circuits;
using SpiceSharp.Diagnostics;

namespace SpiceSharp.Simulations
{
    /// <summary>
    /// A time-domain analysis (Transient simulation)
    /// </summary>
    public class Transient : TimeSimulation
    {
        /// <summary>
        /// Event handler when cutting a timestep
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="data">Timestep cut data</param>
        public delegate void TimestepCutEventHandler(object sender, TimestepCutEventArgs data);

        /// <summary>
        /// Event that is called when the timestep has been cut due to convergence problems
        /// </summary>
        public event TimestepCutEventHandler TimestepCut;

        /// <summary>
        /// Private variables
        /// </summary>
        List<AcceptBehavior> acceptbehaviors;
        List<TruncateBehavior> truncatebehaviors;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="name">Name</param>
        public Transient(Identifier name) : base(name)
        {
            Parameters.Add(new TimeConfiguration());
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="name">Name</param>
        /// <param name="step">Step</param>
        /// <param name="final">Final time</param>
        public Transient(Identifier name, double step, double final) : base(name)
        {
            Parameters.Add(new TimeConfiguration(step, final));
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="name">Name</param>
        /// <param name="step">Step</param>
        /// <param name="final">Final time</param>
        /// <param name="maxstep">Maximum timestep</param>
        public Transient(Identifier name, double step, double final, double maxstep) : base(name)
        {
            Parameters.Add(new TimeConfiguration(step, final, maxstep));
        }

        /// <summary>
        /// Setup the simulation
        /// </summary>
        protected override void Setup()
        {
            base.Setup();

            // Get behaviors and configurations
            acceptbehaviors = SetupBehaviors<AcceptBehavior>();
            truncatebehaviors = SetupBehaviors<TruncateBehavior>();
        }

        /// <summary>
        /// Unsetup the behavior
        /// </summary>
        protected override void Unsetup()
        {
            // Remove references
            foreach (var behavior in truncatebehaviors)
                behavior.Unsetup();
            foreach (var behavior in acceptbehaviors)
                behavior.Unsetup();
            truncatebehaviors.Clear();
            truncatebehaviors = null;
            acceptbehaviors.Clear();
            acceptbehaviors = null;

            base.Unsetup();
        }

        /// <summary>
        /// Execute the transient simulation
        /// </summary>
        protected override void Execute()
        {
            // First do temperature-dependent calculations and IC
            base.Execute();
            var exportargs = new ExportDataEventArgs(State, Method);

            var ckt = Circuit;
            var state = State;
            var baseconfig = BaseConfiguration;
            var timeconfig = TimeConfiguration;

            double delta = Math.Min(timeconfig.FinalTime / 50.0, timeconfig.Step) / 10.0;

            // Initialize before starting the simulation
            state.UseIC = timeconfig.UseIC;
            state.UseDC = true;
            state.UseSmallSignal = false;
            state.Domain = State.DomainTypes.Time;
            state.Gmin = baseconfig.Gmin;

            // Setup breakpoints
            Method.Initialize(tranbehaviors);
            state.Initialize(ckt);

            // Calculate the operating point
            Op(baseconfig.DcMaxIterations);
            Statistics.TimePoints++;
            for (int i = 0; i < Method.DeltaOld.Length; i++)
            {
                Method.DeltaOld[i] = timeconfig.MaxStep;
            }
            Method.Delta = delta;
            Method.SaveDelta = timeconfig.FinalTime / 50.0;

            // Stop calculating a DC solution
            state.UseIC = false;
            state.UseDC = false;
            foreach (var behavior in tranbehaviors)
                behavior.GetDCstate(this);
            States.ClearDC();

            // Start our statistics
            Statistics.TransientTime.Start();
            int startIters = Statistics.NumIter;
            var startselapsed = Statistics.SolveTime.Elapsed;

            try
            {
                while (true)
                {
                    // nextTime:

                    // Accept the current timepoint (CKTaccept())
                    foreach (var behavior in acceptbehaviors)
                        behavior.Accept(this);
                    Method.SaveSolution(state.Solution);
                    // end of CKTaccept()

                    // Check if current breakpoint is outdated; if so, clear
                    Method.UpdateBreakpoints();
                    Statistics.Accepted++;

                    // Export the current timepoint
                    if (Method.Time >= timeconfig.InitTime)
                    {
                        Export(exportargs);
                    }

                    // Detect the end of the simulation
                    if (Method.Time >= timeconfig.FinalTime)
                    {
                        // Keep our statistics
                        Statistics.TransientTime.Stop();
                        Statistics.TranIter += Statistics.NumIter - startIters;
                        Statistics.TransientSolveTime += Statistics.SolveTime.Elapsed - startselapsed;

                        // Finished!
                        return;
                    }

                    // Pause test - pausing not supported

                    // resume:
                    Method.Delta = Math.Min(Method.Delta, timeconfig.MaxStep);
                    Method.Resume();
                    States.ShiftStates();

                    // Calculate a new solution
                    while (true)
                    {
                        Method.TryDelta();

                        // Compute coefficients and predict a solution and reset states to our previous solution
                        Method.ComputeCoefficients(this);
                        Method.Predict(this);

                        // Try to solve the new point
                        if (Method.SavedTime == 0.0)
                            state.Init = State.InitFlags.InitTransient;
                        bool converged = TranIterate(timeconfig.TranMaxIterations);
                        Statistics.TimePoints++;

                        // Spice copies the states the first time, we're not
                        // I believe this is because Spice treats the first timepoint after the OP as special (MODEINITTRAN)
                        // We don't treat it special (we just assume it started from a circuit in rest)

                        if (!converged)
                        {
                            // Failed to converge, let's try again with a smaller timestep
                            Method.Rollback();
                            Statistics.Rejected++;
                            Method.Delta /= 8.0;
                            Method.CutOrder();

                            var data = new TimestepCutEventArgs(ckt, Method.Delta / 8.0, TimestepCutEventArgs.TimestepCutReason.Convergence);
                            TimestepCut?.Invoke(this, data);
                        }
                        else
                        {
                            // Do not check the first time point
                            if (Method.SavedTime == 0.0 || Method.LteControl(this))
                            {
                                // goto nextTime;
                                break;
                            }
                            else
                            {
                                Statistics.Rejected++;
                                var data = new TimestepCutEventArgs(ckt, Method.Delta, TimestepCutEventArgs.TimestepCutReason.Truncation);
                                TimestepCut?.Invoke(this, data);
                            }
                        }

                        if (Method.Delta <= timeconfig.DeltaMin)
                        {
                            if (Method.OldDelta > timeconfig.DeltaMin)
                                Method.Delta = timeconfig.DeltaMin;
                            else
                                throw new CircuitException($"Timestep too small at t={Method.SavedTime.ToString("g")}: {Method.Delta.ToString("g")}");
                        }
                    }
                }
            }
            catch (CircuitException ex)
            {
                // Keep our statistics
                Statistics.TransientTime.Stop();
                Statistics.TranIter += Statistics.NumIter - startIters;
                Statistics.TransientSolveTime += Statistics.SolveTime.Elapsed - startselapsed;
                throw new CircuitException($"{Name}: transient terminated", ex);
            }
        }
    }
}
