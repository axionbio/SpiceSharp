﻿using SpiceSharp.Attributes;
using SpiceSharp.Simulations;

namespace SpiceSharp.Components.ResistorBehaviors
{
    /// <summary>
    /// Base set of parameters for a <see cref="Resistor"/>
    /// </summary>
    public class BaseParameters : ParameterSet
    {
        /// <summary>
        /// Gets the resistance parameter.
        /// </summary>
        [ParameterName("resistance"), ParameterInfo("Resistance", Units = "\u03a9", IsPrincipal = true)]
        public GivenParameter<double> Resistance { get; set; }

        /// <summary>
        /// Gets or sets the temperature in degrees Celsius.
        /// </summary>
        [ParameterName("temp"), DerivedProperty(), ParameterInfo("Instance operating temperature", Units = "\u00b0C", Interesting = false)]
        public double TemperatureCelsius
        {
            get => Temperature - Constants.CelsiusKelvin;
            set => Temperature = value + Constants.CelsiusKelvin;
        }

        /// <summary>
        /// Gets the temperature parameter in degrees Kelvin.
        /// </summary>
        public GivenParameter<double> Temperature { get; set; } = new GivenParameter<double>(Constants.ReferenceTemperature);

        /// <summary>
        /// Gets the width parameter of the resistor.
        /// </summary>
        [ParameterName("w"), ParameterInfo("Width", Units = "m", Interesting = false)]
        public GivenParameter<double> Width { get; set; }

        /// <summary>
        /// Gets the length parameter of the resistor.
        /// </summary>
        [ParameterName("l"), ParameterInfo("Length", Units = "m", Interesting = false)]
        public GivenParameter<double> Length { get; set; }

        /// <summary>
        /// Gets or sets the parallel multiplier.
        /// </summary>
        [ParameterName("m"), ParameterInfo("Parallel multiplier")]
        public double ParallelMultiplier { get; set; } = 1.0;

        /// <summary>
        /// Gets or sets the series multiplier.
        /// </summary>
        /// <value>
        /// The series multiplier.
        /// </value>
        [ParameterName("n"), ParameterInfo("Series multiplier")]
        public double SeriesMultiplier { get; set; } = 1.0;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseParameters"/> class.
        /// </summary>
        public BaseParameters()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseParameters"/> class.
        /// </summary>
        /// <param name="res">Resistor</param>
        public BaseParameters(double res)
        {
            Resistance = res;
        }
    }
}
