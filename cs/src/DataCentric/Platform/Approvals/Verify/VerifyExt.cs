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
    /// <summary>Extension methods for IVerify.</summary>
    public static class VerifyExt
    {
        /// <summary>Record 'Approval.Text: {message}'.</summary>
        public static void Text(this IVerify obj, string message, params object[] messageParams)
        {
            obj.Context.Log.Append(LogEntryType.Verify, "Text", message, messageParams);
        }

        /// <summary>Record 'Approval.Passed: {message}' when condition
        /// is true and 'Approval.Failed: {message}' when condition is false.</summary>
        public static void Assert(this IVerify obj, bool condition, string messageWhenFalse, params object[] messageParams)
        {
            string conditionString = condition ? "Passed" : "Failed";
            obj.Context.Log.Append(LogEntryType.Verify, conditionString, messageWhenFalse, messageParams);
        }

        /// <summary>Record 'Approval.Value: {message} = {value}'.</summary>
        public static void Value(this IVerify obj, object value, string message, params object[] messageParams)
        {
            if (obj.IsSet)
            {
                // Use AsString() instead of ToString() for custom formatting of certain types
                string approvalMessage = System.String.Concat(message, " = ", value.AsString());
                obj.Context.Log.Append(LogEntryType.Verify, "Value", approvalMessage, messageParams);
            }
        }

        /// <summary>Record 'Approval.File: {fileName} ({N} bytes) and save contents to a file.</summary>
        public static void File(this IVerify obj, string fileName, string fileContents)
        {
            if (obj.IsSet)
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
                obj.Context.Log.Append(LogEntryType.Verify, "File", approvalMessage);

                // Save contents to a file
                var fileWriter = obj.Context.Out.CreateTextWriter(fileNameWithPrefix, FileWriteMode.Replace);
                fileWriter.Write(fileContents);
                fileWriter.Flush();
            }
        }
    }
}

