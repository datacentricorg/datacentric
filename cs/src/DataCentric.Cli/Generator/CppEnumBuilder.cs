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

using System.Linq;
using Humanizer;

namespace DataCentric.Cli
{
    public static class CppEnumBuilder
    {
        public static string BuildEnumFile(EnumDeclData decl)
        {
            var writer = new CppCodeWriter();

            var module = decl.Module.ModuleId;
            var settings = GeneratorSettingsProvider.Get(module);

            writer.AppendLines(settings.Copyright);
            writer.AppendNewLineWithoutIndent();

            writer.AppendLine("#pragma once");
            writer.AppendNewLineWithoutIndent();

            // includes
            writer.AppendLine("#include <dot/system/Enum.hpp>");
            // pure data

            writer.AppendNewLineWithoutIndent();

            writer.AppendLine($"namespace {settings.Namespace}");
            writer.AppendLine("{");
            writer.PushIndent();
            BuildEnum(decl, writer);
            writer.PopIndent();
            writer.AppendLine("}");

            return writer.ToString();
        }

        private static void BuildEnum(EnumDeclData decl, CppCodeWriter writer)
        {
            var settings = GeneratorSettingsProvider.Get(decl.Module.ModuleId);
            var type = decl.Name.Underscore();

            writer.AppendNewLineWithoutIndent();

            writer.AppendLines(CommentHelper.FormatComment(decl.Comment));


            writer.AppendLine($"class {settings.DeclSpec} {type} : public enum_base");
            writer.AppendLine("{");

            writer.PushIndent();
            writer.AppendLine($"typedef {type} self;");
            writer.PopIndent();
            writer.AppendNewLineWithoutIndent();

            var elements = decl.Items;
            if (elements.Any())
            {
                writer.AppendLine("public:");
                writer.AppendNewLineWithoutIndent();
                writer.PushIndent();

                writer.AppendLine("enum enum_type {");
                writer.PushIndent();
                foreach (EnumItemDeclData item in elements)
                {
                    writer.AppendLines(CommentHelper.FormatComment(item.Comment));
                    writer.AppendLine($"{item.Name.Underscore()},");
                    // Do not add new line after last item
                    if (elements.IndexOf(item) != elements.Count - 1)
                        writer.AppendNewLineWithoutIndent();
                }
                writer.PopIndent();
                writer.AppendLine("};");
                writer.AppendNewLineWithoutIndent();

                writer.AppendLine($"DOT_ENUM_BEGIN(\"{settings.Namespace}\", \"{type}\")");
                writer.PushIndent();
                foreach (EnumItemDeclData item in elements)
                {
                    writer.AppendLine($"DOT_ENUM_VALUE({item.Name.Underscore()})");
                }
                writer.PopIndent();
                writer.AppendLine("DOT_ENUM_END()");

                writer.PopIndent();
            }

            writer.AppendLine("};");
        }
    }
}