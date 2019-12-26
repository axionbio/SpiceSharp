﻿using SpiceSharp.Attributes;
using SpiceSharp.Behaviors;
using SpiceSharp.Components.InductorBehaviors;
using SpiceSharp.Simulations;

namespace SpiceSharp.Components
{
    /// <summary>
    /// An inductor
    /// </summary>
    [Pin(0, "L+"), Pin(1, "L-"), VoltageDriver(0, 1)]
    public class Inductor : Component
    {
        /// <summary>
        /// Constants
        /// </summary>
        [ParameterName("pincount"), ParameterInfo("Number of pins")]
		public const int InductorPinCount = 2;

        /// <summary>
        /// Initializes a new instance of the <see cref="Inductor"/> class.
        /// </summary>
        /// <param name="name">The name of the inductor</param>
        public Inductor(string name)
            : base(name, InductorPinCount)
        {
            Parameters.Add(new BaseParameters());
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Inductor"/> class.
        /// </summary>
        /// <param name="name">The name of the inductor</param>
        /// <param name="pos">The positive node</param>
        /// <param name="neg">The negative node</param>
        /// <param name="inductance">The inductance</param>
        public Inductor(string name, string pos, string neg, double inductance) 
            : base(name, InductorPinCount)
        {
            Parameters.Add(new BaseParameters(inductance));
            Connect(pos, neg);
        }

        /// <summary>
        /// Creates the behaviors for the specified simulation and registers them with the simulation.
        /// </summary>
        /// <param name="simulation">The simulation.</param>
        public override void CreateBehaviors(ISimulation simulation)
        {
            var behaviors = new BehaviorContainer(Name,
                LinkParameters ? Parameters : (IParameterSetDictionary)Parameters.Clone());
            behaviors.Parameters.CalculateDefaults();
            var context = new ComponentBindingContext(simulation, behaviors, MapNodes(simulation.Variables), Model);
            behaviors
                .AddIfNo<ITimeBehavior>(simulation, () => new TimeBehavior(Name, context))
                .AddIfNo<IFrequencyBehavior>(simulation, () => new FrequencyBehavior(Name, context))
                .AddIfNo<IBiasingBehavior>(simulation, () => new BiasingBehavior(Name, context))
                .AddIfNo<ITemperatureBehavior>(simulation, () => new TemperatureBehavior(Name, context));
            simulation.EntityBehaviors.Add(behaviors);
        }
    }
}
