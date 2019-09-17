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
using System.Linq;
using Humanizer;

namespace DataCentric.Cli
{
    public static class CppDataBuilder
    {
        public static string BuildDataFile(TypeDeclData decl, Dictionary<string, string> declSet)
        {
            var writer = new CppCodeWriter();

            var module = decl.Module.ModuleId;
            var settings = GeneratorSettingsProvider.Get(module);

            writer.AppendLines(settings.Copyright);
            writer.AppendNewLineWithoutIndent();

            writer.AppendLine("#pragma once");
            writer.AppendNewLineWithoutIndent();

            // includes
            writer.AppendLine($"#include <{settings.DeclareInclude}.hpp>");
            if (decl.Keys.Any())
            {
                writer.AppendLine($"#include <dc/types/record/record.hpp>");
                writer.AppendLine($"#include <{declSet[decl.Name]}/{decl.Name.Underscore()}_key.hpp>");
            }
            else if (decl.Inherit != null)
            {
                var baseName = decl.Inherit.Name;
                if (declSet.ContainsKey(baseName))
                {
                    writer.AppendLine($"#include <{declSet[baseName]}/{baseName.Underscore()}_data.hpp>");
                }
            }
            // pure data
            else
            {
                writer.AppendLine($"#include <dc/types/record/data.hpp>");
            }
            writer.AppendNewLineWithoutIndent();

            writer.AppendLine($"namespace {settings.Namespace}");
            writer.AppendLine("{");
            writer.PushIndent();
            BuildClass(decl, writer);
            writer.PopIndent();
            writer.AppendLine("}");

            return writer.ToString();
        }

        private static void BuildClass(TypeDeclData decl, CppCodeWriter writer)
        {
            var settings = GeneratorSettingsProvider.Get(decl.Module.ModuleId);
            var type = decl.Name.Underscore();
            bool isRecordBase = decl.Keys.Any();
            bool isDerived = decl.Inherit != null;

            // Self-forward
            writer.AppendLine($"class {type}_data_impl; using {type}_data = dot::ptr<{type}_data_impl>;");
            if (isRecordBase)
                writer.AppendLine($"class {type}_key_impl; using {type}_key = dot::ptr<{type}_key_impl>;");

            // Get unique keys and data from elements
            var dataForwards = decl.Elements.Where(e => e.Data != null).Select(e => $"{e.Data.Name.Underscore()}_data").ToList();
            var keysForwards = decl.Elements.Where(e => e.Key != null).Select(e => $"{e.Key.Name.Underscore()}_key").ToList();
            var forwards = keysForwards.Union(dataForwards);
            // Appends forwards
            foreach (var f in forwards)
                writer.AppendLine($"class {f}_impl; using {f} = dot::ptr<{f}_impl>;");
            writer.AppendNewLineWithoutIndent();

            writer.AppendLine($"inline {type}_data make_{type}_data();");
            writer.AppendNewLineWithoutIndent();

            var declComment = decl.Comment;
            var comment = CommentHelper.FormatComment(declComment);
            writer.AppendLines(comment);

            var baseTypeImpl = isRecordBase ? $"record_impl<{type}_key_impl, {type}_data_impl>" :
                               isDerived    ? $"{decl.Inherit.Name.Underscore()}_data_impl" :
                                              "data_impl";

            writer.AppendLine($"class {settings.DeclSpec} {type}_data_impl : public {baseTypeImpl}");
            writer.AppendLine("{");

            writer.PushIndent();
            writer.AppendLine($"typedef {type}_data_impl self;");
            writer.AppendLine($"friend {type}_data make_{type}_data();");
            writer.PopIndent();
            writer.AppendNewLineWithoutIndent();

            writer.AppendLine("public: // FIELDS");
            writer.AppendNewLineWithoutIndent();

            var elements = decl.Elements;
            if (elements.Any())
            {
                writer.PushIndent();
                CppElementBuilder.WriteElements(decl.Elements, writer);
                writer.PopIndent();
            }

            var baseType = isRecordBase ? $"record<{type}_key_impl, {type}_data_impl>" :
                           isDerived    ? $"{decl.Inherit.Name.Underscore()}_data" :
                                          "data";

            writer.PushIndent();
            writer.AppendLine($"DOT_TYPE_BEGIN(\"{decl.Module.ModuleID}\", \"{decl.Name}\")");
            writer.PushIndent();
            foreach (var element in elements)
            {
                if (element.BsonIgnore != YesNo.Y)
                    writer.AppendLine($"DOT_TYPE_FIELD(\"{element.Name}\", {element.Name.Underscore()})");
            }
            writer.AppendLine($"DOT_TYPE_CTOR(make_{type}_data)");

            writer.AppendLine($"DOT_TYPE_BASE({baseType})");
            writer.PopIndent();
            writer.AppendLine("DOT_TYPE_END()");
            writer.PopIndent();

            writer.AppendLine("};");
            writer.AppendNewLineWithoutIndent();

            writer.AppendLine("/// Create an empty instance.");
            writer.AppendLine($"inline {type}_data make_{type}_data() {{ return new {type}_data_impl; }}");
        }
    }
}