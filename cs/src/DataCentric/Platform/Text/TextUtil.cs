/*
Copyright (C) 2013-present The DataCentric Authors.

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

   http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/

using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

namespace DataCentric
{
    /// <summary>Text utilities.</summary>
    public static class TextUtil
    {
        const string alphabet_ = "abcdefghijklmnopqrstuvwxyz";

        /// <summary>
        /// Generate a non-repeating sequence of random lowercase character strings.
        /// A total of count strings with the length of stringLength will be generated.
        ///
        /// This method generates pure character strings, without including numbers.
        ///
        /// Change the value of random seed passed to this method to avoid generating
        /// the same sequence when this function is called multiple times.
        ///
        /// This method does not have cryptographic strength and should be used
        /// for test data generation only.
        /// </summary>
        public static List<string> GenerateRandomStrings(int stringLength, int sampleCount, int randomSeed)
        {
            var result = new HashSet<string>();

            // Use seed when creating the random generator
            var rand = new Random(randomSeed);

            // Error message if the number of requested samples
            // is more than half of the total available number
            // of samples. For stringLength >= 5 this number
            // if high enough for most realistic stringCount
            // choices.
            if (stringLength < 5 && sampleCount > Math.Pow(alphabet_.Length, stringLength) / 2)
                throw new Exception(
                    $"Requested number of random string samples {sampleCount} " +
                    $"is too high for string length {stringLength}.");

            int sampleIndex = 0;
            while (sampleIndex < sampleCount)
            {
                // Create sample string from random characters
                var builder = new StringBuilder();
                for (var j = 0; j < stringLength; j++)
                {
                    var c = alphabet_[rand.Next(0, alphabet_.Length)];
                    builder.Append(c);
                }
                string sample = builder.ToString();

                // Add if not yet present
                if (!result.Contains(sample))
                {
                    result.Add(sample);
                    sampleIndex++;
                }
            }

            return result.ToList();
        }
    }
}
