﻿using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.IO;
using SpiceSharp.Algebra;

namespace SpiceSharpTest.Sparse
{
    public class SolveFramework
    {
        /// <summary>
        /// Read a .MTX file
        /// </summary>
        /// <param name="filename">Filename</param>
        /// <returns></returns>
        protected LUSolver<double> ReadMtxFile(string filename)
        {
            LUSolver<double> result;

            using (var sr = new StreamReader(filename))
            {
                // The first line is a comment
                sr.ReadLine();

                // The second line tells us the dimensions
                var line = sr.ReadLine() ?? throw new Exception("Invalid Mtx file");
                var match = Regex.Match(line, @"^(?<rows>\d+)\s+(?<columns>\d+)\s+(\d+)");
                var size = int.Parse(match.Groups["rows"].Value);
                if (int.Parse(match.Groups["columns"].Value) != size)
                    throw new Exception("Matrix is not square");

                result = new RealSolver(size);

                // All subsequent lines are of the format [row] [column] [value]
                while (!sr.EndOfStream)
                {
                    // Read the next line
                    line = sr.ReadLine();
                    if (line == null)
                        break;

                    match = Regex.Match(line, @"^(?<row>\d+)\s+(?<column>\d+)\s+(?<value>.*)\s*$");
                    if (!match.Success)
                        throw new Exception("Could not recognize file");
                    var row = int.Parse(match.Groups["row"].Value);
                    var column = int.Parse(match.Groups["column"].Value);
                    var value = double.Parse(match.Groups["value"].Value, System.Globalization.CultureInfo.InvariantCulture);

                    // Set the value in the matrix
                    result.GetMatrixElement(row, column).Value = value;
                }
            }

            return result;
        }

        /// <summary>
        /// Reads a matrix file generated by Spice 3f5.
        /// </summary>
        /// <param name="matFilename">The matrix filename.</param>
        /// <param name="vecFilename">The vector filename.</param>
        /// <returns></returns>
        protected LUSolver<double> ReadSpice3f5File(string matFilename, string vecFilename)
        {
            var solver = new RealSolver();

            // Read the spice file
            string line;
            using (var reader = new StreamReader(matFilename))
            {
                // The file is organized using (row) (column) (value) (imag value)
                while (!reader.EndOfStream && (line = reader.ReadLine()) != null)
                {
                    if (line == "first")
                        continue;
                    var match = Regex.Match(line, @"^(?<size>\d+)\s+(complex|real)$");
                    
                    // Try to read an element
                    match = Regex.Match(line, @"^(?<row>\d+)\s+(?<col>\d+)\s+(?<value>[^\s]+)(\s+[^\s]+)?$");
                    if (match.Success)
                    {
                        int row = int.Parse(match.Groups["row"].Value);
                        int col = int.Parse(match.Groups["col"].Value);
                        var value = double.Parse(match.Groups["value"].Value, CultureInfo.InvariantCulture);
                        solver.GetMatrixElement(row, col).Value = value;
                    }
                }
            }

            // Read the vector file
            using (var reader = new StreamReader(vecFilename))
            {
                var index = 1;
                while (!reader.EndOfStream && (line = reader.ReadLine()) != null)
                {
                    var value = double.Parse(line, CultureInfo.InvariantCulture);
                    solver.GetVectorElement(index).Value = value;
                    index++;
                }
            }

            return solver;
        }
    }
}
