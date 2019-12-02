﻿using SpiceSharp.Attributes;

namespace SpiceSharp.Simulations
{
    /// <summary>
    /// A configuration for a <see cref="Noise"/> analysis.
    /// </summary>
    public class NoiseConfiguration : ParameterSet
    {
        /// <summary>
        /// Gets or sets the noise output node name.
        /// </summary>
        [ParameterName("output"), ParameterInfo("Noise output summation node")]
        public string Output { get; set; }

        /// <summary>
        /// Gets or sets the noise output reference node name.
        /// </summary>
        [ParameterName("outputref"), ParameterInfo("Noise output reference node")]
        public string OutputRef { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="NoiseConfiguration"/> class.
        /// </summary>
        public NoiseConfiguration()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NoiseConfiguration"/> class.
        /// </summary>
        /// <param name="output">The output node name.</param>
        /// <param name="reference">The reference node name.</param>
        public NoiseConfiguration(string output, string reference)
        {
            Output = output;
            OutputRef = reference;
        }
    }
}
