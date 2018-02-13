﻿using System;
using System.Text;
using SpiceSharp.NewSparse.Solve;

namespace SpiceSharp.NewSparse
{
    /// <summary>
    /// Template for a solver
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class Solver<T> : IFormattable where T : IFormattable, IEquatable<T>
    {
        /// <summary>
        /// Number of fill-ins in the matrix generated by the solver
        /// </summary>
        public int Fillins { get; private set; }

        /// <summary>
        /// Gets or sets a flag that reordering is required
        /// </summary>
        public bool NeedsReordering { get; set; }

        /// <summary>
        /// Relative threshold for pivots
        /// </summary>
        public double RelativePivotThreshold { get; set; } = 1e-3;

        /// <summary>
        /// Absolute threshold for pivots
        /// </summary>
        public double AbsolutePivotThreshold { get; set; } = 0.0;

        /// <summary>
        /// Gets the row translation
        /// </summary>
        public Translation Row { get; private set; }

        /// <summary>
        /// Gets the column translation
        /// </summary>
        public Translation Column { get; private set; }

        /// <summary>
        /// Gets or sets a flag that the translation has been set up
        /// </summary>
        protected bool TranslationSetup { get; set; }

        /// <summary>
        /// Gets the matrix to work on
        /// </summary>
        public SparseMatrix<T> Matrix { get; }

        /// <summary>
        /// Gets the right-hand side
        /// </summary>
        public DenseVector<T> Rhs
        {
            get
            {
                if (rhs.Length != Matrix.Size + 1)
                    rhs = new DenseVector<T>(Matrix.Size + 1);
                return rhs;
            }
        }
        DenseVector<T> rhs;

        /// <summary>
        /// Gets the pivot strategy used
        /// </summary>
        public PivotStrategy<T> Strategy { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        protected Solver(PivotStrategy<T> strategy)
        {
            Matrix = new SparseMatrix<T>();
            rhs = new DenseVector<T>(1);
            NeedsReordering = true;
            TranslationSetup = false;
            Strategy = strategy;
        }

        /// <summary>
        /// Solve
        /// </summary>
        /// <param name="solution">Solution vector</param>
        public abstract void Solve(DenseVector<T> solution);

        /// <summary>
        /// Solve the transposed problem
        /// </summary>
        /// <param name="solution">Solution vector</param>
        public abstract void SolveTransposed(DenseVector<T> solution);

        /// <summary>
        /// Factor the matrix
        /// </summary>
        public abstract void Factor();

        /// <summary>
        /// Order and factor the matrix
        /// </summary>
        public abstract void OrderAndFactor();

        /// <summary>
        /// Move a chosen pivot to (step, step)
        /// </summary>
        /// <param name="pivot">Pivot</param>
        /// <param name="step">Step</param>
        protected void MovePivot(Element<T> pivot, int step)
        {
            if (pivot == null)
                throw new ArgumentNullException(nameof(pivot));
            if (!TranslationSetup)
            {
                Row = new Translation(Matrix.Size + 1);
                Column = new Translation(Matrix.Size + 1);
                TranslationSetup = true;
            }

            Strategy.MovePivot(Matrix, Rhs, pivot, step);

            // Move the pivot in the matrix
            int row = pivot.Row;
            int column = pivot.Column;
            if (row != step)
            {
                Matrix.SwapRows(row, step);

                // Swap Right-hand side vector elements
                var tmp = Rhs[step];
                Rhs[step] = Rhs[row];
                Rhs[row] = tmp;

                // Swap translation indices
                Row.Swap(row, step);
            }
            if (column != step)
            {
                Matrix.SwapColumns(column, step);

                // Swap translation indices
                Column.Swap(column, step);
            }

            Strategy.Update(Matrix, pivot, step);
        }

        /// <summary>
        /// Create a fillin
        /// </summary>
        /// <param name="row">Row</param>
        /// <param name="column">Column</param>
        /// <returns></returns>
        protected virtual Element<T> CreateFillin(int row, int column)
        {
            var result = Matrix.GetElement(row, column);
            Fillins++;
            return result;
        }

        /// <summary>
        /// Convert to a string
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return ToString(null, System.Globalization.CultureInfo.CurrentCulture);
        }

        /// <summary>
        /// Convert to a string
        /// </summary>
        /// <param name="format"></param>
        /// <param name="formatProvider"></param>
        /// <returns></returns>
        public string ToString(string format, IFormatProvider formatProvider)
        {
            string[][] displayData = new string[Matrix.Size][];
            int[] columnWidths = new int[Matrix.Size + 1];
            for (int r = 1; r <= Matrix.Size; r++)
            {
                int extRow = Row.Reverse(r) - 1;
                var element = Matrix.GetFirstInRow(r);
                displayData[extRow] = new string[Matrix.Size + 1];
                
                for (int c = 1; c <= Matrix.Size; c++)
                {
                    int extColumn = Column.Reverse(c) - 1;

                    // go to the next element if necessary
                    if (element != null && element.Column < c)
                        element = element.Right;

                    // Show the element
                    if (element == null || element.Column != c)
                        displayData[extRow][extColumn] = "...";
                    else
                        displayData[extRow][extColumn] = element.Value.ToString(format, formatProvider);
                    columnWidths[extColumn] = Math.Max(columnWidths[extColumn], displayData[extRow][extColumn].Length);
                }

                // Rhs vector
                displayData[extRow][Matrix.Size] = Rhs[r].ToString(format, formatProvider);
            }

            // Build the string
            StringBuilder sb = new StringBuilder();
            for (int r = 0; r < Matrix.Size; r++)
            {
                for (int c = 0; c <= Matrix.Size; c++)
                {
                    if (c == Matrix.Size)
                        sb.Append(" : ");

                    var display = displayData[r][c];
                    sb.Append(new string(' ', columnWidths[c] - display.Length + 2));
                    sb.Append(display);
                }
                sb.Append(Environment.NewLine);
            }
            return sb.ToString();
        }
    }
}
