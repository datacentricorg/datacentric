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

using System;
using System.Text;

namespace DataCentric.Cli
{
    public static class CommentHelper
    {
        public static string FormatComment(string comment)
        {
            if (!comment.HasNonWhiteSpaceValue())
                return string.Empty;

            comment = comment.Replace("\r\n","\n").Replace("\n", Environment.NewLine).TrimEnd(Environment.NewLine);

            comment = $"<summary> {comment} </summary>";

            var sb = new StringBuilder();
            var lines = comment.Split(new []{Environment.NewLine}, StringSplitOptions.None);
            foreach (var line in lines)
            {
                sb.AppendLine(string.IsNullOrWhiteSpace(line) ? "///" : $"/// {line}");
            }

            return sb.ToString().TrimEnd(Environment.NewLine);
        }
    }
}