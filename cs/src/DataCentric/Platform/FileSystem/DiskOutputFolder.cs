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
using System.CodeDom.Compiler;
using System.IO;
using System.Reflection;

namespace DataCentric
{
    /// <summary>
    /// Output folder based on disk filesystem.
    /// </summary>
    public class DiskOutputFolder : IOutputFolder
    {
        private readonly string outputFolderPath_;

        /// <summary>Creates in the specified output folder.</summary>
        public DiskOutputFolder(IContext context, string outputFolderPath)
        {
            Context = context;
            outputFolderPath_ = outputFolderPath;
        }

        /// <summary>Context for which this interface is defined.
        /// Use to access other interfaces of the same context.</summary>
        public IContext Context { get; private set; }

        //--- METHODS

        /// <summary>
        /// Set Context property and perform validation of the record's data,
        /// then initialize any fields or properties that depend on that data.
        ///
        /// This method may be called multiple times for the same instance,
        /// possibly with a different context parameter for each subsequent call.
        ///
        /// IMPORTANT - Every override of this method must call base.Init()
        /// first, and only then execute the rest of the override method's code.
        /// </summary>
        public virtual void Init(IContext context)
        {
            // Uncomment except in root class of the hierarchy
            // base.Init(context);

            // Check that argument is not null and assign to the Context property
            if (context == null) throw new Exception($"Null context is passed to the Init(...) method for {GetType().Name}.");
            Context = context;
        }

        /// <summary>Determines whether the specified file exists.
        /// This method accepts dot delimited folder path.</summary>
        public bool Exists(string filePath)
        {
            string fullFilePath = Path.Combine(outputFolderPath_, filePath);
            bool result = File.Exists(fullFilePath);
            return result;
        }

        /// <summary>Creates or opens the specified file for writing
        /// using UTF-8 encoding. Append vs. overwrite behavior is determined by writeMode.
        /// This method accepts dot delimited folder path.</summary>
        public TextWriter CreateTextWriter(string filePath, FileWriteMode writeMode)
        {
            // Create full path by combining with output folder path
            string fullFilePath = Path.Combine(outputFolderPath_, filePath);
            string fullFolderPath = Path.GetDirectoryName(fullFilePath);

            // Check if directory exists, if not create
            if (!Directory.Exists(fullFolderPath)) Directory.CreateDirectory(fullFolderPath);

            switch (writeMode)
            {
                case FileWriteMode.Append:
                    return new StreamWriter(fullFilePath);
                case FileWriteMode.Replace:
                    if (File.Exists(fullFilePath)) File.Delete(fullFilePath);
                    return new StreamWriter(fullFilePath);
                case FileWriteMode.CreateNew:
                    if (File.Exists(fullFilePath)) Context.Log.Error("File {0} already exists.", fullFilePath);
                    return new StreamWriter(fullFilePath);
                default:
                    throw Context.Log.Exception("FileMode={0} is not supported.", writeMode.ToString());
            }
        }

        /// <summary>Deletes the specified file.
        /// This method accepts dot delimited folder path.</summary>
        public void Delete(string filePath)
        {
            string fullFilePath = Path.Combine(outputFolderPath_, filePath);
            if (File.Exists(fullFilePath)) File.Delete(fullFilePath);
        }
    }
}
