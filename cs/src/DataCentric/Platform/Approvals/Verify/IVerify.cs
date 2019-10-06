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
using System.Diagnostics;
using System.Text;

namespace DataCentric
{
    /// <summary>Approval testing interface.</summary>
    public interface IVerify : IDisposable
    {
        /// <summary>Context for which this interface is defined.
        /// Use to access other interfaces of the same context.</summary>
        IContext Context { get; }

        /// <summary>Test class name.</summary>
        string ClassName { get; }

        /// <summary>Test method name.</summary>
        string MethodName { get; }

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
        void Init(IContext context);

        /// <summary>Flush data to permanent storage.</summary>
        void Flush();
    }

    /// <summary>Extension methods for IVerify.</summary>
    public static class IVerifyExt
    {
        /// <summary>Record 'Verify.File: {fileName} ({N} bytes) and save contents to a file.</summary>
        public static void File(this IVerify obj, string fileName, string fileContents)
        {
            string[] fileNameTokens = fileName.Split('.');
            if (fileNameTokens.Length == 1) throw new Exception(
                $"Filename {fileName} passed to Verify.File method must have an extension.");
            if (fileNameTokens.Length > 2) throw new Exception(
                $"Filename {fileName} passed to Verify.File method must not have dot delimiters other than in front of the file extension.");

            string fileNameWithPrefix = String.Join(".", obj.ClassName, obj.MethodName, fileNameTokens[0], "approved", fileNameTokens[1]);

            // Record approval message with file byte size
            int byteSize = Encoding.UTF8.GetByteCount(fileContents);
            string approvalMessage = System.String.Concat(fileNameWithPrefix, " (", byteSize, " bytes)");
            obj.Context.Log.Entry(LogVerbosity.Verify, "File", approvalMessage);

            // Save contents to a file
            var fileWriter = obj.Context.OutputFolder.GetTextWriter(fileNameWithPrefix, FileWriteMode.Replace);
            fileWriter.Write(fileContents);
            fileWriter.Flush();
        }
    }
}
