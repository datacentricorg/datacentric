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
using System.Collections.Generic;
using MongoDB.Bson;

namespace DataCentric
{
    /// <summary>
    /// Context defines dataset and provides access to data,
    /// logging, and other supporting functionality.
    /// </summary>
    public interface IContext : IDisposable
    {
        /// <summary>Get the default data source of the context.</summary>
        IDataSource DataSource { get; }

        /// <summary>Returns ObjectId of the context dataset.</summary>
        ObjectId DataSet { get; }

        /// <summary>Output folder root of the context's virtualized filesystem.</summary>
        IOutputFolder Out { get; }

        /// <summary>Logging interface.</summary>
        ILog Log { get; }

        /// <summary>Progress interface.</summary>
        IProgress Progress { get; }

        /// <summary>Flush context data to permanent storage.</summary>
        void Flush();
    }
}
