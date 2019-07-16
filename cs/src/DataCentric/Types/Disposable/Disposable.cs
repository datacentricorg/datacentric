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
using NodaTime;

namespace DataCentric
{
    /// <summary>
    /// Implements the standard dispose pattern.
    /// Inheriting from this class is optional.
    /// </summary>
    public abstract class Disposable : IDisposable
    {
        /// <summary>
        /// Releases resources and calls base.Dispose().
        ///
        /// This method will NOT be called by the garbage
        /// collector, therefore instantiating it inside
        /// the ``using'' clause is essential to ensure
        /// that Dispose() method gets invoked.
        ///
        /// ATTENTION:
        ///
        /// Each class must call base.Dispose() at the end
        /// of its own Dispose() method.
        /// </summary>
        public virtual void Dispose()
        {
            // The line below is an example of calling Dispose() method for
            // the base class at the end of the Dispose() method. It should
            // be uncommented for all classes except the class at the root
            // of the inheritance hierarchy, such as this class.
            //
            // base.Dispose();
        }
    }
}
