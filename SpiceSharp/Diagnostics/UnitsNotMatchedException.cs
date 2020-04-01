﻿using SpiceSharp.Simulations;
using SpiceSharp.Simulations.Variables;

namespace SpiceSharp.Diagnostics
{
    /// <summary>
    /// Exception thrown when two units are not matched.
    /// </summary>
    /// <seealso cref="SpiceSharpException" />
    public class UnitsNotMatchedException : SpiceSharpException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UnitsNotMatchedException"/> class.
        /// </summary>
        public UnitsNotMatchedException(IUnit first, IUnit second)
            : base(Properties.Resources.Units_UnitsNotMatched.FormatString(first, second))
        {
        }
    }
}