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

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Humanizer;

namespace DataCentric.Cli
{
    public class CppFileInfo
    {
        public string Content { get; set; }
        public string FileName { get; set; }
        public string FolderName { get; set; }
    }

    public static class DeclConverter
    {
        public static List<IDeclData> ReadDeclUnits(string path)
        {
            var result = new List<IDeclData>();

            var typeFiles = Directory.GetFiles(path, "*.cltype", SearchOption.AllDirectories);
            foreach (var file in typeFiles)
            {
                string text = File.ReadAllText(file);
                result.Add(DeclarationSerializer.Deserialize<TypeDeclData>(text));
            }

            var enumFiles = Directory.GetFiles(path, "*.clenum", SearchOption.AllDirectories);
            foreach (var file in enumFiles)
            {
                string text = File.ReadAllText(file);
                result.Add(DeclarationSerializer.Deserialize<EnumDeclData>(text));
            }

            return result;
        }

        public static List<CppFileInfo> ConvertSet(List<IDeclData> declarations)
        {
            List<TypeDeclData> typeDecls = declarations.OfType<TypeDeclData>().ToList();
            List<EnumDeclData> enumDecls = declarations.OfType<EnumDeclData>().ToList();

            var structureInfo = typeDecls.ToDictionary(t => t.Name, GetIncludePath);

            var typeHeaders = typeDecls.SelectMany(d => ConvertType(d, structureInfo));
            var enumHeaders = enumDecls.SelectMany(ConvertEnum);

            return typeHeaders.Concat(enumHeaders).ToList();
        }

        private static string GetIncludePath(TypeDeclData decl)
        {
            var settings = GeneratorSettingsProvider.Get(decl.Module.ModuleId);

            return $"{settings.Namespace}.{decl.Category}".Underscore().Replace('.', '/');
        }

        private static List<CppFileInfo> ConvertType(TypeDeclData decl, Dictionary<string, string> structureInfo)
        {
            string pathInProject = structureInfo[decl.Name];

            List<CppFileInfo> result = new List<CppFileInfo>();

            var dataHeader = new CppFileInfo
            {
                Content = CppDataBuilder.BuildDataHeader(decl, structureInfo),
                FileName = $"{decl.Name.Underscore()}_data.hpp",
                FolderName = pathInProject
            };
            result.Add(dataHeader);

            var dataSource = new CppFileInfo
            {
                Content = CppDataBuilder.BuildDataSource(decl, structureInfo),
                FileName = $"{decl.Name.Underscore()}_data.cpp",
                FolderName = pathInProject
            };
            result.Add(dataSource);

            if (decl.Keys.Any())
            {
                var keyHeader = new CppFileInfo
                {
                    Content = CppKeyBuilder.BuildKeyHeader(decl, structureInfo),
                    FileName = $"{decl.Name.Underscore()}_key.hpp",
                    FolderName = pathInProject
                };
                result.Add(keyHeader);

                var keySource = new CppFileInfo
                {
                    Content = CppKeyBuilder.BuildKeySource(decl, structureInfo),
                    FileName = $"{decl.Name.Underscore()}_key.cpp",
                    FolderName = pathInProject
                };
                result.Add(keySource);
            }

            return result;
        }

        private static List<CppFileInfo> ConvertEnum(EnumDeclData decl)
        {
            var result = new List<CppFileInfo>();
            IGeneratorSettings settings = GeneratorSettingsProvider.Get(decl.Module.ModuleId);
            string folderName = $"{settings.Namespace}.{decl.Category}".Underscore().Replace('.', '/');

            var enumHeader = new CppFileInfo
            {
                Content = CppEnumBuilder.BuildEnumHeader(decl),
                FileName = $"{decl.Name.Underscore()}.hpp",
                FolderName = folderName
            };
            result.Add(enumHeader);

            var enumSource = new CppFileInfo
            {
                Content = CppEnumBuilder.BuildEnumSource(decl),
                FileName = $"{decl.Name.Underscore()}.cpp",
                FolderName = folderName
            };
            result.Add(enumSource);

            return result;
        }
    }
}