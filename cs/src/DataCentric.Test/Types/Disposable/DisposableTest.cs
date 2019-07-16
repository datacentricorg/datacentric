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
using Xunit;
using DataCentric;
using NodaTime;

namespace DataCentric.Test
{
    /// <summary>Sample base class implementing IDisposable.</summary>
    public class DisposableTestBaseSample : Disposable
    {
        /// <summary>Context.</summary>
        public IUnitTestContext Context { get; }

        /// <summary>Create from context.</summary>
        public DisposableTestBaseSample(IUnitTestContext context)
        {
            Context = context;
        }

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
        public override void Dispose()
        {
            Context.Verify.Text("Disposing resources of BaseClassSample");

            base.Dispose();
        }
    }

    /// <summary>Sample derived class implementing IDisposable.</summary>
    public class DisposableTestDerivedSample : DisposableTestBaseSample
    {
        /// <summary>Create from context.</summary>
        public DisposableTestDerivedSample(IUnitTestContext context) : base(context)
        {
        }

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
        public override void Dispose()
        {
            Context.Verify.Text("Disposing resources of DerivedClassSample");

            base.Dispose();
        }
    }

    /// <summary>Unit test for Disposable.</summary>
    public class DisposableTest
    {
        /// <summary>Test disposing resources via the ``using'' clause.</summary>
        [Fact]
        public void Smoke()
        {
            using (IUnitTestContext context = new UnitTestContext(this))
            {
                // Base class by its own reference
                using (DisposableTestBaseSample obj = new DisposableTestBaseSample(context)) { }

                // Derived class by its own reference
                using (DisposableTestDerivedSample obj = new DisposableTestDerivedSample(context)) { }

                // Derived class by base reference
                using (DisposableTestBaseSample obj = new DisposableTestDerivedSample(context)) { }
            }
        }
    }
}
