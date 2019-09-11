﻿using System;

namespace SpiceSharp.Algebra
{
    /// <summary>
    /// Describes a vector element.
    /// </summary>
    /// <typeparam name="T">The base type.</typeparam>
    public interface IVectorElement<T> where T : IFormattable
    {
        /// <summary>
        /// Gets or sets the value of the vector element.
        /// </summary>
        /// <value>
        /// The value.
        /// </value>
        T Value { get; set; }

        /// <summary>
        /// Gets the index.
        /// </summary>
        /// <value>
        /// The index.
        /// </value>
        int Index { get; }

        /// <summary>
        /// Gets the vector element above this one.
        /// </summary>
        /// <value>
        /// The vector element.
        /// </value>
        IVectorElement<T> Above { get; }

        /// <summary>
        /// Gets the vector element below this one.
        /// </summary>
        /// <value>
        /// The vector element.
        /// </value>
        IVectorElement<T> Below { get; }
    }
}
