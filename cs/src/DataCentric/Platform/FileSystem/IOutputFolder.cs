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
using System.IO;
using System.Xml;

namespace DataCentric
{
    /// <summary>Virtual file system interface can be used to work with
    /// a physical file system, Mongo GridFS, AWS S3, etc.</summary>
    public interface IOutputFolder
    {
        /// <summary>Context for which this interface is defined.
        /// Use to access other interfaces of the same context.</summary>
        IContext Context { get; }

        /// <summary>Determines whether the specified file exists.
        /// This method accepts dot delimited folder path.</summary>
        bool Exists(string filePath);

        /// <summary>Creates or opens the specified file for writing
        /// using UTF-8 encoding. Append vs. overwrite behavior is determined by writeMode.
        /// This method accepts dot delimited folder path.</summary>
        ITextWriter CreateTextWriter(string filePath, FileWriteMode writeMode);

        /// <summary>Deletes the specified file.
        /// This method accepts dot delimited folder path.</summary>
        void Delete(string filePath);
    }

    /// <summary>Extension methods for IOutputFolder.</summary>
    public static class IOutputFolderEx
    {
        /// <summary>Appends text to the specified file, creating it if does not exist.</summary>
        public static void AppendText(this IOutputFolder obj, string filePath, string fileContents)
        {
            obj.SaveText(filePath, fileContents, FileWriteMode.Append);
        }

        /// <summary>Writes text to the specified file, overwriting it if exists and crating otherwise.</summary>
        public static void WriteText(this IOutputFolder obj, string filePath, string fileContents)
        {
            obj.SaveText(filePath, fileContents, FileWriteMode.Replace);
        }

        /// <summary>Appends or overwrites the specified file with text followed by EOL.</summary>
        private static void SaveText(this IOutputFolder obj, string filePath, string text, FileWriteMode writeMode)
        {
            ITextWriter textWriter = obj.CreateTextWriter(filePath, writeMode);
            textWriter.Write(text);
            textWriter.Flush();
        }
    }
}
