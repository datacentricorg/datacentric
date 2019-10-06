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
using System.Runtime.CompilerServices;
using MongoDB.Bson;

namespace DataCentric
{
    /// <summary>
    /// Context for use in handlers.
    /// </summary>
    public class Context : IContext
    {
        private IDataSource dataSource_;
        private ObjectId? dataSet_;
        private IOutputFolder out_;
        private ILog log_;
        private IProgress progress_;

        /// <summary>Output folder root of the context's virtualized filesystem.</summary>
        public IOutputFolder Out
        {
            get
            {
                if (out_ == null) throw new Exception(
                    $"Context type {GetType().Name} does not provide output folder, " +
                    $"or one has not been set.");
                return out_;
            }
            set { out_ = value; }
        }

        /// <summary>Logging interface.</summary>
        public ILog Log
        {
            get
            {
                if (log_ == null) throw new Exception(
                    $"Context type {GetType().Name} does not provide logging, " +
                    $"or Log property has not been set.");
                return log_;
            }
            set { log_ = value; }
        }

        /// <summary>Progress interface.</summary>
        public IProgress Progress
        {
            get
            {
                if (progress_ == null)
                    throw new Exception(
                        $"Context type {GetType().Name} does not provide progress reporting, " +
                        $"or Progress property has not been set.");
                return progress_;
            }
            set { progress_ = value; }
        }

        /// <summary>Get the default data source of the context.</summary>
        public IDataSource DataSource
        {
            get
            {
                if (dataSource_ == null)
                    throw new Exception(
                        $"Context type {GetType().Name} does not provide a data source, " +
                        $"or DataSource property has not been set.");
                return dataSource_;
            }
            set { dataSource_ = value; }
        }

        /// <summary>Returns default dataset of the context.</summary>
        public ObjectId DataSet
        {
            get
            {
                if (dataSet_ == null) throw new Exception(
                    $"Context type {GetType().Name} does not provide a dataset, " +
                    $"or DataSet property has not been set.");
                return dataSet_.Value;
            }
            set { dataSet_ = value; }
        }

        //--- METHODS

        /// <summary>
        /// Initialize the current context after its properties are set,
        /// and set default values for the properties that are not set.
        /// 
        /// Includes calling Init(this) for each property of the context.
        ///
        /// This method may be called multiple times for the same instance.
        ///
        /// IMPORTANT - Every override of this method must call base.Init()
        /// first, and only then execute the rest of the override method's code.
        /// </summary>
        public virtual void Init()
        {
            // Uncomment except in root class of the hierarchy
            // base.Init();

            // Call Init(this) for each initialized property of the context
            if (out_ != null) out_.Init(this);
            if (log_ != null) log_.Init(this);
            if (progress_!= null) progress_.Init(this);
            if (dataSource_ != null) dataSource_.Init(this);
        }

        /// <summary>
        /// Releases resources and calls base.Dispose().
        ///
        /// This method will not be called by the garbage collector.
        /// It will only be executed if:
        ///
        /// * This class implements IDisposable; and
        /// * The class instance is created through the using clause
        ///
        /// IMPORTANT - Every override of this method must call base.Dispose()
        /// after executing its own code.
        /// </summary>
        public virtual void Dispose()
        {
            // Call Dispose() for each initialized property of the context
            // in reverse order of initialization
            if (dataSource_ != null) dataSource_.Dispose();
            if (progress_ != null) progress_.Dispose();
            if (log_ != null) log_.Dispose();

            // Uncomment except in root class of the hierarchy
            // base.Dispose();
        }

        /// <summary>Flush data to permanent storage.</summary>
        public virtual void Flush()
        {
            // Call Flush() for each initialized property of the context
            if (dataSource_ != null) dataSource_.Flush();
            if (progress_ != null) progress_.Flush();
            if (log_ != null) log_.Flush();
        }
    }
}
