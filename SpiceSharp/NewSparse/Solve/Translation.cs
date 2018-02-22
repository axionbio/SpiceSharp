﻿using System;

namespace SpiceSharp.NewSparse.Solve
{
    /// <summary>
    /// Can map external to internal indices and vice-versa
    /// </summary>
    public class Translation
    {
        /// <summary>
        /// Constants
        /// </summary>
        const float ExpansionFactor = 1.5f;

        /// <summary>
        /// Private variable
        /// </summary>
        int[] extToInt;
        int[] intToExt;
        int allocated;

        /// <summary>
        /// Gets the current length of the translation vector
        /// </summary>
        public int Length { get; private set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="size">Number</param>
        public Translation(int size)
        {
            extToInt = new int[size + 1];
            intToExt = new int[size + 1];
            for (int i = 1; i <= size; i++)
            {
                extToInt[i] = i;
                intToExt[i] = i;
            }
            allocated = size;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public Translation()
            : this(4)
        {
        }

        /// <summary>
        /// Indexer will map external indices to internal indices
        /// </summary>
        /// <param name="index">External index</param>
        /// <returns></returns>
        public int this[int index]
        {
            get
            {
                // Zero is mapped to zero
                if (index == 0)
                    return 0;
                if (index > allocated)
                    ExpandTranslation(index);
                return extToInt[index];
            }
        }

        /// <summary>
        /// Reverse lookup for the translation
        /// </summary>
        /// <param name="index">Internal index</param>
        /// <returns></returns>
        public int Reverse(int index)
        {
            if (index == 0)
                return 0;
            if (index > allocated)
                ExpandTranslation(index);
            return intToExt[index];
        }

        /// <summary>
        /// Swap two (internal) indices
        /// </summary>
        /// <param name="index1">Index 1</param>
        /// <param name="index2">Index 2</param>
        public void Swap(int index1, int index2)
        {
            if (index1 > Length || index2 > Length)
                ExpandTranslation(Math.Max(index1, index2));

            // Get the matching external indices
            var tmp = intToExt[index1];
            intToExt[index1] = intToExt[index2];
            intToExt[index2] = tmp;

            // Update the external indices
            extToInt[intToExt[index1]] = index1;
            extToInt[intToExt[index2]] = index2;
        }

        /// <summary>
        /// Scramble a vector
        /// </summary>
        /// <typeparam name="T">Base type</typeparam>
        /// <param name="source">Source</param>
        /// <param name="target">Target</param>
        public void Scramble<T>(Vector<T> source, Vector<T> target) where T : IFormattable
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (target == null)
                throw new ArgumentNullException(nameof(target));
            if (source.Length != target.Length)
                throw new ArgumentException("Length of inputs does not match");

            // Expand translation vectors if necessary
            if (allocated < source.Length || allocated < target.Length)
                ExpandTranslation(Math.Max(source.Length, target.Length));

            for (int i = 1; i < extToInt.Length; i++)
                target[extToInt[i]] = source[i];
        }

        /// <summary>
        /// Unscramble a vector
        /// The first index of the array is ignored
        /// </summary>
        /// <typeparam name="T">Base type</typeparam>
        /// <param name="source">Source</param>
        /// <param name="target">Target</param>
        public void Unscramble<T>(T[] source, Vector<T> target) where T : IFormattable
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (target == null)
                throw new ArgumentNullException(nameof(target));
            if (source.Length != target.Length + 1)
                throw new ArgumentException("Length of inputs does not match");

            // Expand translation vectors if necessary
            if (allocated < source.Length || allocated < target.Length)
                ExpandTranslation(Math.Max(source.Length - 1, target.Length));

            for (int i = 1; i < source.Length; i++)
                target[intToExt[i]] = source[i];
        }

        /// <summary>
        /// Expand translation
        /// </summary>
        /// <param name="newLength">New length</param>
        void ExpandTranslation(int newLength)
        {
            // No need to reallocate vector
            if (newLength <= allocated)
            {
                Length = newLength;
                return;
            }

            // Reallocate
            int oldAllocated = allocated;
            allocated = Math.Max(newLength, (int)(allocated * ExpansionFactor));

            Array.Resize(ref extToInt, allocated + 1);
            Array.Resize(ref intToExt, allocated + 1);
            for (int i = oldAllocated + 1; i <= allocated; i++)
            {
                extToInt[i] = i;
                intToExt[i] = i;
            }
            Length = newLength;
        }
    }
}
