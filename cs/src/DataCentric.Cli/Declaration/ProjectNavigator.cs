﻿/*
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

using System.IO;
using System.Linq;
using System.Reflection;

namespace DataCentric.Cli
{
    /// <summary>
    /// Helper class to determine project structure.
    /// This is needed since assembly and documentation does not contain file location inside project,
    /// while this info is needed for other languages implementations.
    /// </summary>
    public class ProjectNavigator
    {
        /// <summary>
        /// Creates navigator from the given project file.
        /// </summary>
        public ProjectNavigator(string projectPath)
        {
            Location = projectPath;
        }

        /// <summary>
        /// Helper method which tries to create project navigator instance for given assembly.
        /// For given assembly it searches inside provided path for corresponding csproj file.
        /// </summary>
        public static bool TryCreate(string searchPath, Assembly assembly, out ProjectNavigator navigator)
        {
            navigator = null;
            if (!Directory.Exists(searchPath))
                return false;

            string assemblyName = assembly.GetName().Name;
            string projectName = $"{assemblyName}.csproj";
            string projectCsName = $"{assemblyName}.Cs.csproj";

            string projectPath = Directory.GetFiles(searchPath, projectName, SearchOption.AllDirectories).SingleOrDefault();
            string projectCsPath = Directory.GetFiles(searchPath, projectCsName, SearchOption.AllDirectories).SingleOrDefault();

            if (File.Exists(projectCsPath))
            {
                navigator = new ProjectNavigator(projectCsPath);
                return true;
            }

            if (File.Exists(projectPath))
            {
                navigator = new ProjectNavigator(projectPath);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Full path to csproj file.
        /// </summary>
        public string Location { get; }


        /// <summary>
        /// For a given type returns its .cs file path relative to project path.
        /// </summary>
        public string GetTypeLocation(System.Type type)
        {
            string projectDir = Path.GetDirectoryName(Location);
            string typeLocation = Directory.GetFiles(projectDir, $"{type.Name}.cs", SearchOption.AllDirectories)
                                           .SingleOrDefault();
            if (typeLocation == null)
                return null;

            string fileDir = Path.GetDirectoryName(typeLocation);
            string relativePath = Path.GetRelativePath(projectDir, fileDir);
            return relativePath.Replace('\\', '.').Replace('/', '.');
        }
    }
}