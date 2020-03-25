﻿using System.Collections.Generic;

namespace SpiceSharp.Simulations
{
    /// <summary>
    /// A template for mapping a variable to indices.
    /// </summary>
    /// <seealso cref="IEnumerable{T}" />
    public interface IVariableMap : IEnumerable<KeyValuePair<IVariable, int>>
    {
        /// <summary>
        /// Gets the ground node variable.
        /// </summary>
        /// <value>
        /// The ground node variable.
        /// </value>
        IVariable Ground { get; }

        /// <summary>
        /// Gets the number of mapped variables.
        /// </summary>
        /// <value>
        /// The number of mapped variables.
        /// </value>
        int Count { get; }

        /// <summary>
        /// Gets the index associated with the specified variable.
        /// </summary>
        /// <value>
        /// The index.
        /// </value>
        /// <param name="variable">The variable.</param>
        /// <returns>
        /// The variable index.
        /// </returns>
        int this[IVariable variable] { get; }

        /// <summary>
        /// Gets the <see cref="Variable"/> at assiciated to the specified index.
        /// </summary>
        /// <value>
        /// The <see cref="Variable"/>.
        /// </value>
        /// <param name="index">The index.</param>
        /// <returns>
        /// The associated variable.
        /// </returns>
        IVariable this[int index] { get; }

        /// <summary>
        /// Gets all the variables in the map.
        /// </summary>
        /// <value>
        /// The variables.
        /// </value>
        IEnumerable<IVariable> Variables { get; }

        /// <summary>
        /// Determines whether a variable is mapped.
        /// </summary>
        /// <param name="variable">The variable.</param>
        /// <returns>
        ///   <c>true</c> if the variable is mapped; otherwise, <c>false</c>.
        /// </returns>
        bool Contains(IVariable variable);

        /// <summary>
        /// Tries to get the associated index of the specified variable.
        /// </summary>
        /// <param name="variable">The variable.</param>
        /// <param name="index">The associated index.</param>
        /// <returns>
        ///     <c>true</c> if the variable has been found; otherwise, <c>false</c>.
        /// </returns>
        bool TryGetIndex(IVariable variable, out int index);

        /// <summary>
        /// Clears the map.
        /// </summary>
        void Clear();
    }
}
