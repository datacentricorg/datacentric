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
using System.Linq;
using System.IO;
using System.Text;

namespace DataCentric
{
    /// <summary>Portable replacement for .NET System.IO.File with additional functionality.
    /// Only those methods from System.IO.File that are actually used by the library are included.
    /// Provides static methods for the creation, copying, deletion, moving, and
    /// opening of files, and aids in the creation of System.IO.FileStream objects.</summary>
    public static class FileUtil
    {
        /// <summary>Convert path to use operating system specific separator.</summary>
        public static string ToSystemSeparator(string path)
        {
            return path.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
        }

        /// <summary>Convert dot delimited string to path by replacing dots with backshash path separator.
        /// Error message if the argument string already contains either of the two path separators.</summary>
        public static string DotDelimitedToPath(string dotDelimitedString)
        {
            // Check that the path does not contain spaces or dots
            if (dotDelimitedString.Contains("\\")) throw new Exception("Dot delimited path must not contain backslash path separator.");
            if (dotDelimitedString.Contains("/")) throw new Exception("Dot delimited path must not contain forward slash path separator.");

            // Replace dots with backslash
            return dotDelimitedString.Replace('.', '\\');
        }

        /// <summary>Try to find the specified filename in the specified path
        /// This function returns null if the subfolder is not found.</summary>
        public static string FindProjectRootFolder(string startFromFolder, string lookForFolder)
        {
            string result = null;
            while (!string.IsNullOrEmpty(startFromFolder))
            {
                // Break if a directory with the specified name exists
                result = Path.Combine(startFromFolder, lookForFolder);
                if (Directory.Exists(result)) break;

                DirectoryInfo parentFolder = Directory.GetParent(startFromFolder);
                if (!parentFolder.Exists) return null;
                startFromFolder = parentFolder.FullName;
            }

            return string.IsNullOrEmpty(startFromFolder) ? null : startFromFolder;
        }

        /// <summary>Appends the specified string to the file, creating the file if it does not
        /// already exist.</summary>
        public static void AppendAllText(string path, string contents, Encoding encoding)
        {
            string pathWithSystemSeparator = FileUtil.ToSystemSeparator(path);
            if (File.Exists(pathWithSystemSeparator)) File.AppendAllText(pathWithSystemSeparator, contents, encoding);
            else WriteAllText(path, contents, encoding);
        }

        /// <summary>Determines whether the specified file exists.</summary>
        public static bool Exists(string path)
        {
            return File.Exists(FileUtil.ToSystemSeparator(path));
        }

        /// <summary>Opens a binary file, reads the contents of the file into a byte array, and
        /// then closes the file.</summary>
        public static byte[] ReadAllBytes(string path)
        {
            return File.ReadAllBytes(FileUtil.ToSystemSeparator(path));
        }

        /// <summary>Opens a text file, reads all lines of the file, and then closes the file.</summary>
        public static string ReadAllText(string path)
        {
            return File.ReadAllText(FileUtil.ToSystemSeparator(path));
        }

        /// <summary>Creates a new file, writes the specified string to the file using the specified
        /// encoding, and then closes the file. If the target file already exists, it
        ///  is overwritten.</summary>
        public static void WriteAllText(string path, string contents, Encoding encoding)
        {
            // Create target folder if does not exist
            string folderPath = Path.GetDirectoryName(path);
            if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

            File.WriteAllText(FileUtil.ToSystemSeparator(path), contents, encoding);
        }

        /// <summary>Creates a new file, writes the specified byte array to the file, and then
        /// closes the file. If the target file already exists, it is overwritten.</summary>
        public static void WriteAllBytes(string path, byte[] bytes)
        {
            // Create target folder if does not exist
            string folderPath = Path.GetDirectoryName(path);
            if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

            File.WriteAllBytes(FileUtil.ToSystemSeparator(path), bytes);
        }

        /// <summary>Delete file if exists.</summary>
        public static void Delete(string path)
        {
            string pathWithSystemSeparator = FileUtil.ToSystemSeparator(path);
            if(File.Exists(pathWithSystemSeparator)) File.Delete(FileUtil.ToSystemSeparator(path));
        }

        /// <summary>Remove the specified folder from the specified file path.
        /// The path must be within the specified original folder.</summary>
        public static string RemoveFolder(string folderPath, string filePath)
        {
            if (folderPath.Contains("\\\\")) throw new Exception($"More than one path separator in a row in folder path {folderPath}");
            if (filePath.Contains("\\\\")) throw new Exception($"More than one path separator in a row in file path {filePath}");
            if (!filePath.StartsWith(folderPath)) throw new Exception("File path does not start from folder path.");
            int pos = folderPath.EndsWith("\\") ? folderPath.Length : folderPath.Length + 1;
            string result = filePath.Substring(pos);
            return result;
        }

        /// <summary>Substitute original folder by new folder in the specified file path.
        /// The path must be within the specified original folder.</summary>
        public static string SwitchFolder(string originalFolderPath, string newFolderPath, string filePath)
        {
            if (newFolderPath.Contains("\\\\")) throw new Exception($"More than one path separator in a row in folder path {newFolderPath}");
            string relativePath = RemoveFolder(originalFolderPath, filePath);
            string result = Path.Combine(newFolderPath, relativePath);
            return result;
        }
    }
}
