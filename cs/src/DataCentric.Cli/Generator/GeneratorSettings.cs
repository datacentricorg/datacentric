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

namespace DataCentric.Cli
{
    public interface IGeneratorSettings
    {
        string Namespace { get; }
        string DeclSpec { get; }
        string DeclareInclude { get; }
        string Copyright { get; }
    }

    public abstract class GeneratorSettingsProvider
    {
        public static IGeneratorSettings Get(string module)
        {
            if (module == "DataCentric")
                return new DataCentricSettings();
            throw new ArgumentOutOfRangeException($"No settings for module: {module}");
        }
    }

    public class DataCentricSettings : IGeneratorSettings
    {
        public string Namespace => "dc";
        public string DeclSpec => "DC_CLASS";
        public string DeclareInclude => "dc/declare";
        public string Copyright => @"/*
Copyright (C) 2013-present The DataCentric Authors.

Licensed under the Apache License, Version 2.0 (the ""License"");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

   http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an ""AS IS"" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/";
    }
}