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
    public static class CppKeyBuilder
    {
        public static string BuildKeyFile(TypeDeclData decl, Dictionary<string, string> declSet)
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
            writer.AppendLine($"#include <dc/types/record/key.hpp>");
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

            // forwards
            writer.AppendLine($"class {type}_key_impl; using {type}_key = dot::ptr<{type}_key_impl>;");

            // Get unique keys and data from elements
            var keyElements = decl.Elements.Where(e => decl.Keys.Contains(e.Name)).ToList();
            var dataForwards = keyElements.Where(e => e.Data != null).Select(e => $"{e.Data.Name.Underscore()}_data").ToList();
            var keysForwards = keyElements.Where(e => e.Key != null).Select(e => $"{e.Key.Name.Underscore()}_key").ToList();
            var forwards = keysForwards.Union(dataForwards);
            // Appends forwards
            foreach (var f in forwards)
                writer.AppendLine($"class {f}_impl; using {f} = dot::ptr<{f}_impl>;");
            writer.AppendNewLineWithoutIndent();

            var declComment = decl.Comment;
            var comment = CommentHelper.FormatComment(declComment);
            writer.AppendLines(comment);

            writer.AppendLine($"class {settings.DeclSpec} {type}_key_impl : public key_impl<{type}_key_impl,{type}_data_impl>");
            writer.AppendLine("{");

            writer.PushIndent();
            writer.AppendLine($"typedef {type}_key_impl self;");
            writer.AppendLine($"friend {type}_key new_{type}_key();");
            writer.PopIndent();
            writer.AppendNewLineWithoutIndent();

            writer.AppendLine("public: // FIELDS");
            writer.AppendNewLineWithoutIndent();

            if (keyElements.Any())
            {
                writer.PushIndent();
                CppElementBuilder.WriteElements(keyElements, writer);
                writer.PopIndent();
            }

            #region REFLECTION
            writer.PushIndent();
            writer.AppendLine($"DOT_TYPE_BEGIN(\"{decl.Module.ModuleID}\", \"{decl.Name}\")");
            writer.PushIndent();
            foreach (var element in keyElements)
            {
                writer.AppendLine($"// DOT_TYPE_FIELD(\"{element.Name}\", {element.Name.Underscore()})");
            }
            writer.AppendLine($"DOT_TYPE_CTOR(make_{type}_key)");
            writer.AppendLine($"DOT_TYPE_BASE(key<{type}_key, {type}_data>)");
            writer.PopIndent();
            writer.AppendLine("DOT_TYPE_END()");
            writer.PopIndent();
            writer.AppendNewLineWithoutIndent();
            #endregion

            writer.AppendLine("protected: // CONSTRUCTORS");
            writer.AppendNewLineWithoutIndent();
            writer.PushIndent();
            writer.AppendLine($"inline {type}_key_impl() = default;");
            writer.PopIndent();

            writer.AppendLine("};");
            writer.AppendNewLineWithoutIndent();

            writer.AppendLine("/// Create an empty instance.");
            writer.AppendLine($"inline {type}_key make_{type}_key() {{ return new {type}_key_impl(); }}");
        }
    }
}