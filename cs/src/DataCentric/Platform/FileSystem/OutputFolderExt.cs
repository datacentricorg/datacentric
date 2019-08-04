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
    /// <summary>Extension methods for IOutputFolder.</summary>
    public static class OutputFolderExt
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
            TextWriter textWriter = obj.CreateTextWriter(filePath, writeMode);
            textWriter.Write(text);
            textWriter.Flush();
        }
    }
}
