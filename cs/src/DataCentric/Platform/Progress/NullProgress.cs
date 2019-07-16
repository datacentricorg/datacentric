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

namespace DataCentric
{
    /// <summary>Use this class to skip recording progress to permanent storage.
    /// The values of Ratio and Message work normally through the API.</summary>
    public class NullProgress : IProgress
    {
        /// <summary>Create from context.</summary>
        public NullProgress(IContext context)
        {
            Context = context;
        }

        /// <summary>Context for which this interface is defined.
        /// Use to access other interfaces of the same context.</summary>
        public IContext Context { get; }

        /// <summary>Get or set progress ratio from 0 to 1 (0 if not set).</summary>
        public double Ratio { get; set; }

        /// <summary>Get or set message displayed next to the progress ratio (null if not set).</summary>
        public string Message { get; set; }

        /// <summary>Flush progress state to permanent storage.</summary>
        public void Flush() { }
    }
}
