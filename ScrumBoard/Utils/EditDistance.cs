using System;
using System.Collections.Generic;
using System.Linq;

namespace ScrumBoard.Utils
{
    public enum EditSegmentType
    {
        Unchanged,
        Added,
        Removed,
    }

    public readonly record struct EditSegment(EditSegmentType Type, string Content);

    public static class EditDistance
    {
        /// <summary>
        /// Computes a table where each element i,j is the length of the longest substring of source up to i and destination up to j
        /// </summary>
        /// <param name="source">Initial string</param>
        /// <param name="destination">New string</param>
        /// <returns>Table of size (source.Length, destination.Length)</returns>
        private static int[,] ComputeLongestSubsequenceTable(string source, string destination)
        {
            var lengths = new int[source.Length, destination.Length];
            for (var sourceIndex = 0; sourceIndex < source.Length; sourceIndex++)
            {
                for (var destIndex = 0; destIndex < destination.Length; destIndex++)
                {
                    int similarity;
                    
                    if (source[sourceIndex] == destination[destIndex])
                    {
                        similarity = 1;
                        if (sourceIndex > 0 && destIndex > 0)
                        {
                            similarity += lengths[sourceIndex - 1, destIndex - 1];
                        }
                    }
                    else
                    {
                        similarity = 0;
                        if (sourceIndex > 0)
                        {
                            similarity = lengths[sourceIndex - 1, destIndex];
                        }
                        if (destIndex > 0)
                        {
                            similarity = Math.Max(similarity, lengths[sourceIndex, destIndex - 1]);
                        }
                    }

                    lengths[sourceIndex, destIndex] = similarity;
                }
            }

            return lengths;
        }

        /// <summary>
        /// Finds the smallest sequence of changes to convert the source string into the destination string 
        /// </summary>
        /// <param name="source">Initial string</param>
        /// <param name="destination">New string</param>
        /// <returns>Smallest enumerable of changes</returns>
        private static IEnumerable<(EditSegmentType, char)> UnchunkedLongestCommonSubsequence(string source, string destination)
        {
            var lengths = ComputeLongestSubsequenceTable(source, destination);

            int LongestCommon(int sourceLength, int destLength)
                => sourceLength == 0 || destLength == 0 ? 0 : lengths[sourceLength - 1, destLength - 1]; 
            
            var charResult = new List<(EditSegmentType, char)>();
            
            var sourceLength = source.Length;
            var destLength = destination.Length;

            while (sourceLength > 0 && destLength > 0)
            {
                var sourceChar = source[sourceLength - 1];
                var destChar = destination[destLength - 1];

                if (sourceChar == destChar)
                {
                    charResult.Add((EditSegmentType.Unchanged, sourceChar));
                    sourceLength--;
                    destLength--;
                }
                else if (LongestCommon(sourceLength - 1, destLength) > LongestCommon(sourceLength, destLength - 1))
                {
                    charResult.Add((EditSegmentType.Removed, sourceChar));
                    sourceLength--;
                }
                else
                {
                    charResult.Add((EditSegmentType.Added, destChar));
                    destLength--;
                }
            }
            // Handle left over pieces
            while (sourceLength > 0)
            {
                charResult.Add((EditSegmentType.Removed, source[--sourceLength]));
            }
            while (destLength > 0)
            {
                charResult.Add((EditSegmentType.Added, destination[--destLength]));
            }
            // Since we traverse from the end back to the start we need to reverse the results
            charResult.Reverse();
            return charResult;
        }

        /// <summary>
        /// Chunks together changes of the same type into strings
        /// </summary>
        /// <param name="unchunkedChanges">Non-chunked changes</param>
        /// <returns>List of chunked changes</returns>
        private static List<EditSegment> ChunkSequences(IEnumerable<(EditSegmentType, char)> unchunkedChanges)
        {
            var result = new List<EditSegment>();
            EditSegmentType? currentType = null;
            var currentSegment = new List<char>();
            foreach (var (type, currentChar) in unchunkedChanges)
            {
                if (currentType != null && currentType != type)
                {
                    result.Add(new(currentType.Value, new string(currentSegment.ToArray())));
                    currentSegment.Clear();
                }
            
                currentType = type;
                currentSegment.Add(currentChar);
            }
            
            if (currentType.HasValue)
            {
                result.Add(new(currentType.Value, new string(currentSegment.ToArray())));
            }

            return result;
        }

        /// <summary>
        /// Computes list of differences between a source and destination string
        /// Complexity O(n^2), should be good up to 10,000 chars
        /// </summary>
        /// <param name="source">Initial string</param>
        /// <param name="destination">New string</param>
        /// <returns>
        /// Combined list of changes, removed segments only appear in source, added segments only appear in destination, unchanged appear in both
        /// </returns>
        public static List<EditSegment> LongestCommonSubsequence(string source, string destination)
        {
            return ChunkSequences(UnchunkedLongestCommonSubsequence(source, destination));
        }
    }
    
    
}
