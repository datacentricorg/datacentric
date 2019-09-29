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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using CommandLine;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Xunit;

namespace DataCentric.Cli
{
    /// <summary>
    /// Entry point for the Runtime command line interface (CLI).
    /// </summary>
    public static class MainCli
    {
        /// <summary>
        /// Entry point method for CLI.
        /// </summary>
        public static int Main(string[] args)
        {
            // Interactive mode: commands separated by line
            if (args.Length == 0)
            {
                while (true)
                {
                    Console.Write("$ ");
                    args = Console.ReadLine()?.Split(' ');
                    ParserResult<object> parseInteractiveResult = Parser.Default.ParseArguments<RunVerbOptions,
                        ExtractVerbOptions,
                        TestVerbOptions,
                        GenerateVerbOptions,
                        HeadersVerbOptions,
                        CsvConvertVerbOptions,
                        ExitVerbOptions>(args);

                    if (parseInteractiveResult is Parsed<object> parsedInteractive)
                    {
                        switch (parsedInteractive.Value)
                        {
                            case RunVerbOptions runOptions:
                                DoRun(runOptions);
                                break;
                            case ExtractVerbOptions extractOptions:
                                DoExtract(extractOptions);
                                break;
                            case TestVerbOptions testOptions:
                                DoTest(testOptions);
                                break;
                            case GenerateVerbOptions generateOptions:
                                DoGenerate(generateOptions);
                                break;
                            case HeadersVerbOptions headersOptions:
                                DoHeadersGenerate(headersOptions);
                                break;
                            case CsvConvertVerbOptions convertOptions:
                                DoCsvConvert(convertOptions);
                                break;
                            case ExitVerbOptions _:
                                return 0;
                            default:
                                return -1;
                        }
                    }
                    // Exit and show help if command is not recognized
                    else if (parseInteractiveResult is NotParsed<object> notParsed)
                    {
                        return -1;
                    }
                }
            }

            // Single command mode
            ParserResult<object> parseResult = Parser.Default.ParseArguments<RunVerbOptions,
                ExtractVerbOptions,
                TestVerbOptions,
                GenerateVerbOptions,
                HeadersVerbOptions,
                CsvConvertVerbOptions,
                ExitVerbOptions>(args);

            if (parseResult is Parsed<object> parsed)
            {
                switch (parsed.Value)
                {
                    case RunVerbOptions runOptions:
                        DoRun(runOptions);
                        return 0;
                    case ExtractVerbOptions extractOptions:
                        DoExtract(extractOptions);
                        return 0;
                    case TestVerbOptions testOptions:
                        DoTest(testOptions);
                        return 0;
                    case GenerateVerbOptions generateOptions:
                        DoGenerate(generateOptions);
                        return 0;
                    case HeadersVerbOptions headersOptions:
                        DoHeadersGenerate(headersOptions);
                        break;
                    case CsvConvertVerbOptions convertOptions:
                        DoCsvConvert(convertOptions);
                        break;
                    case ExitVerbOptions _:
                        return 0;
                }
            }

            return -1;
        }

        /// <summary>
        /// Convert records stored in csv format to mongo storage.
        /// </summary>
        private static void DoCsvConvert(CsvConvertVerbOptions convertOptions)
        {
            DbNameKey dbName = new DbNameKey
            {
                InstanceType = InstanceType.USER, InstanceName = "user", EnvName = "Default"
            };
            LocalMongoDataStoreData dbServer = new LocalMongoDataStoreData
            {
                DataStoreId = "From Csv",
            };
            IContext context = new MongoCliContext(dbName, dbServer);

            // Process all directories inside given folder
            foreach (var dir in Directory.GetDirectories(convertOptions.CsvPath))
            {
                ProcessDirectory(context, dir, context.GetCommon());
            }
        }

        private static void ProcessDirectory(IContext context, string path, ObjectId parentDataset)
        {
            var dirName = Path.GetFileName(path);

            // Do not create dataset for Common
            var currentDataset = dirName != "Common"
                                     ? context.CreateDataSet(dirName, new[] {parentDataset}, context.DataSet)
                                     : parentDataset;

            foreach (var csvFile in Directory.GetFiles(path, "*.csv"))
            {
                var type = Path.GetFileNameWithoutExtension(csvFile);
                Type recordType = ActivatorUtils.ResolveType($"{type}Data", ActivatorSettings.Assemblies)
                                  ?? throw new ArgumentException($"Type '{type}' not found");

                MethodInfo convertToMongo = typeof(MainCli).GetMethod(nameof(ConvertCsvToMongo), BindingFlags.Static | BindingFlags.NonPublic)
                                                          ?.MakeGenericMethod(recordType);

                convertToMongo?.Invoke(null, new object[] { context, currentDataset, csvFile});
            }

            var directories = Directory.GetDirectories(path);
            foreach (string directory in directories)
            {
                ProcessDirectory(context, directory, currentDataset);
            }
        }

        private static void ConvertCsvToMongo<T>(IContext context, ObjectId dataset, string csvFile) where T : Record
        {
            string fileContent = File.ReadAllText(csvFile);
            var records = CsvRecordsSerializer<T>.Deserialize(fileContent);

            foreach (var record in records) context.Save(record, dataset);
        }

        /// <summary>
        /// Helper method to create and init instance of handler class.
        /// </summary>
        private static TRecord CreateHandler<TKey, TBaseRecord, TRecord>(IContext context, RunVerbOptions options)
            where TKey : TypedKey<TKey, TBaseRecord>, new()
            where TRecord : TBaseRecord
            where TBaseRecord : TypedRecord<TKey, TBaseRecord>
        {
            TKey key = Activator.CreateInstance<TKey>();
            key.PopulateFrom(options.Key);

            ObjectId dataSet = context.GetDataSet(options.Dataset, context.DataSet);
            TRecord record = (TRecord) context.LoadOrNull(key, dataSet);

            record.Init(context);

            return record;
        }

        /// <summary>
        /// Search Record in type hierarchy and returns its type arguments.
        /// </summary>
        private static (Type,Type) GetRecordTypeArguments(Type type)
        {
            while (type.BaseType != null)
            {
                type = type.BaseType;
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(TypedRecord<,>))
                {
                    return (type.GetGenericArgument(0), type.GetGenericArgument(1));
                }
            }
            throw new InvalidOperationException("Base TypedRecord<,> type was not found");
        }

        /// <summary>
        /// Corresponds to CLI "run" keyword. Executes handler specified by run options.
        /// </summary>
        public static void DoRun(RunVerbOptions options)
        {
            Type recordType = ActivatorUtils.ResolveType($"{options.Type}Data", ActivatorSettings.Assemblies)
                              ?? throw new ArgumentException($"Type '{options.Type}' not found");

            // Register type in BsonClassMap if not yet registered.
            // This part is critical for derived types, since collection creation registers only base type.
            if (!BsonClassMap.IsClassMapRegistered(recordType))
            {
                MethodInfo registerMap = typeof(BsonClassMap)
                                        .GetMethod(nameof(BsonClassMap.RegisterClassMap), new Type[] { })
                                        .MakeGenericMethod(recordType);
                registerMap.Invoke(null, new object[] { });
            }

            (Type keyType, Type baseRecordType) = GetRecordTypeArguments(recordType);

            MethodInfo createHandlerMethod = typeof(MainCli)
                .GetMethod(nameof(CreateHandler), BindingFlags.Static | BindingFlags.NonPublic)
                .MakeGenericMethod(keyType, baseRecordType, recordType);

            // TODO: changes to db naming should be synchronized with client, afterwards connection should be parsed from source
            string connectionLiteral = "ConnectionString=";

            // Extract ConnectionString from source string
            string[] sourceParams = options.Source.Split(',');
            string connectionString = sourceParams.First(t => t.StartsWith(connectionLiteral)).Substring(connectionLiteral.Length);

            // Convert connection string to db name and hosts
            MongoUrl url = MongoUrl.Create(connectionString);
            string dbNameString = $"{url.DatabaseName};{options.Environment}";
            List<string> hosts = url.Servers.Select(t => t.ToString()).ToList();

            DbNameKey dbName = Activator.CreateInstance<DbNameKey>();
            dbName.PopulateFrom(dbNameString);
            LocalMongoDataStoreData dbServer = new LocalMongoDataStoreData
            {
                DataStoreId = "LOCAL_TEST",
            };

            IContext context = new MongoCliContext(dbName, dbServer);

            object handler = createHandlerMethod.Invoke(null, new object[] { context, options });

            MethodInfo handlerMethod = handler.GetType().GetMethod(options.Handler)
                ?? throw new ArgumentException($"Method '{options.Handler}' not found");

            handlerMethod.Invoke(handler, ActivatorUtils.CreateParameterValues(handlerMethod, options.Arguments));
        }

        /// <summary>
        /// Corresponds to CLI "extract" keyword. Converts assembly types to declarations.
        /// ExtractVerbOptions.ProjectPath has been introduced to add project structure info to declarations.
        /// </summary>
        public static void DoExtract(ExtractVerbOptions options)
        {
            AssemblyCache assemblies = new AssemblyCache();

            // Create list of assemblies (enrolling masks when needed)
            foreach (string assemblyPath in options.Assemblies)
            {
                string assemblyName = Path.GetFileName(assemblyPath);
                if (!string.IsNullOrEmpty(assemblyName))
                {
                    string assemblyDirectory =
                        string.IsNullOrEmpty(assemblyDirectory = Path.GetDirectoryName(assemblyPath)) ?
                        Environment.CurrentDirectory :
                        Path.GetFullPath(assemblyDirectory);
                    assemblies.AddFiles(Directory.EnumerateFiles(assemblyDirectory, assemblyName));
                }
            }

            // When no assemblies provided, search inside working directory
            if (assemblies.IsEmpty)
            {
                assemblies.AddFiles(Directory.EnumerateFiles(Environment.CurrentDirectory, "*.dll"));
            }

            Directory.CreateDirectory(options.OutputFolder);

            foreach (Assembly assembly in assemblies)
            {
                Console.Write("A> ");
                Console.WriteLine(assembly.Location);

                bool hasDocumentation = CommentNavigator.TryCreate(assembly, out CommentNavigator docNavigator);
                if (hasDocumentation)
                {
                    Console.Write("D> ");
                    Console.WriteLine(docNavigator.XmlLocation);
                }

                bool isProjectLocated = ProjectNavigator.TryCreate(options.ProjectPath, assembly, out ProjectNavigator projNavigator);
                if (isProjectLocated)
                {
                    Console.Write("P> ");
                    Console.WriteLine(projNavigator.Location);
                }

                List<Type> types = TypesExtractor.GetTypes(assembly, options.Types);

                foreach (Type type in types)
                {
                    TypeDeclData decl = options.Legacy
                                            ? DeclarationConvertor.TypeToDecl(type, docNavigator, projNavigator).ToLegacy()
                                            : DeclarationConvertor.TypeToDecl(type, docNavigator, projNavigator);

                    string outputFolder = Path.Combine(options.OutputFolder, decl.Module.ModuleId.Replace('.','\\'));
                    Directory.CreateDirectory(outputFolder);

                    string extension = type.IsSubclassOf(typeof(Enum)) ? "clenum" : "cltype";
                    string outputFile = Path.Combine(outputFolder, $"{decl.Name}.{extension}");

                    Console.Write(type.FullName);
                    Console.Write(" => ");
                    Console.WriteLine(outputFile);

                    File.WriteAllText(outputFile, DeclarationSerializer.Serialize(decl, options.Legacy));
                }

                List<Type> enums = TypesExtractor.GetEnums(assembly, options.Types);
                foreach (Type type in enums)
                {
                    EnumDeclData decl = DeclarationConvertor.EnumToDecl(type, docNavigator, projNavigator);

                    string outputFolder = Path.Combine(options.OutputFolder, decl.Module.ModuleId.Replace('.','\\'));
                    Directory.CreateDirectory(outputFolder);

                    string extension = type.IsSubclassOf(typeof(Enum)) ? "clenum" : "cltype";
                    string outputFile = Path.Combine(outputFolder, $"{decl.Name}.{extension}");

                    Console.Write(type.FullName);
                    Console.Write(" => ");
                    Console.WriteLine(outputFile);

                    File.WriteAllText(outputFile, DeclarationSerializer.Serialize(decl, options.Legacy));
                }
            }
        }

        /// <summary>
        /// Corresponds to CLI "test" keyword. Executes specified test.
        /// </summary>
        private static void DoTest(TestVerbOptions options)
        {
            AssemblyCache assemblies = new AssemblyCache();

            Regex filter = TypesExtractor.CreateTypeNameFilter(new[] { options.TestPattern });

            assemblies.AddFiles(Directory.EnumerateFiles(Environment.CurrentDirectory, "*.dll"));

            foreach (Assembly assembly in assemblies)
            {
                // Get types with tests
                HashSet<Type> testClasses = assembly.GetTypes()
                                                    .SelectMany(t => t.GetMethods())
                                                    .Where(m => m.GetCustomAttributes().OfType<FactAttribute>().Any())
                                                    .ToList().Select(m => m.DeclaringType).ToHashSet();

                // Filter tests to run
                IEnumerable<Type> testToRun = testClasses.Where(t => filter == null || filter.IsMatch(t.FullName));
                foreach (Type test in testToRun)
                {
                    TestRunner.Run(assembly.GetName().Name, test.FullName);
                }
            }
        }

        /// <summary>
        /// Corresponds to CLI "generate" keyword. Converts given declarations to corresponding c++ files.
        /// </summary>
        public static void DoGenerate(GenerateVerbOptions generateOptions)
        {
            var declFiles = DeclConverter.ReadDeclUnits(generateOptions.InputFolder);

            GeneratorSettingsProvider.PopulateFromFile(generateOptions.SettingsPath);

            // Check Category field. In case if type name != file name it will be empty
            Console.ForegroundColor = ConsoleColor.Yellow;
            foreach (var decl in declFiles)
            {
                if (string.IsNullOrEmpty(decl.Category))
                    Console.WriteLine($"Warning! Unable to locate: {decl.Name}. Possible type<->file names mismatch.");
            }
            Console.ResetColor();

            var fileContentInfos = DeclConverter.ConvertSet(declFiles);

            foreach (var hppFile in fileContentInfos)
            {
                var fullPath = Path.Combine(generateOptions.OutputFolder, hppFile.FolderName, hppFile.FileName);
                var directory = Path.GetDirectoryName(fullPath);
                Directory.CreateDirectory(directory);

                if (File.Exists(fullPath))
                {
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine($"Warning! File already exists. Overwriting: {hppFile.FolderName}/{hppFile.FileName}");
                    Console.ResetColor();
                }
                else
                {
                    Console.WriteLine($"Generated: {hppFile.FolderName}/{hppFile.FileName}");
                }

                File.WriteAllText(fullPath, hppFile.Content);
            }
        }

        /// <summary>
        /// Corresponds to CLI "headers" keyword. Converts given c# assemblies to corresponding c++ files.
        /// Combination of extract and generate keywords.
        /// </summary>
        private static void DoHeadersGenerate(HeadersVerbOptions options)
        {
            AssemblyCache assemblies = new AssemblyCache();

            // Create list of assemblies (enrolling masks when needed)
            foreach (string assemblyPath in options.Assemblies)
            {
                string assemblyName = Path.GetFileName(assemblyPath);
                if (!string.IsNullOrEmpty(assemblyName))
                {
                    string assemblyDirectory = string.IsNullOrEmpty(assemblyDirectory = Path.GetDirectoryName(assemblyPath))
                                                   ? Environment.CurrentDirectory
                                                   : Path.GetFullPath(assemblyDirectory);
                    assemblies.AddFiles(Directory.EnumerateFiles(assemblyDirectory, assemblyName));
                }
            }

            List<IDeclData> declarations = new List<IDeclData>();
            foreach (Assembly assembly in assemblies)
            {
                CommentNavigator.TryCreate(assembly, out CommentNavigator docNavigator);
                ProjectNavigator.TryCreate(options.ProjectPath, assembly, out ProjectNavigator projNavigator);

                List<Type> types = TypesExtractor.GetTypes(assembly, options.Types);
                List<Type> enums = TypesExtractor.GetEnums(assembly, options.Types);
                declarations.AddRange(types.Concat(enums)
                                           .Select(type => DeclarationConvertor.ToDecl(type, docNavigator, projNavigator)));
            }

            GeneratorSettingsProvider.PopulateFromFile(options.SettingsPath);
            var fileContentInfos = DeclConverter.ConvertSet(declarations);
            foreach (var hppFile in fileContentInfos)
            {
                var fullPath = Path.Combine(options.OutputFolder, hppFile.FolderName, hppFile.FileName);
                var directory = Path.GetDirectoryName(fullPath);
                Directory.CreateDirectory(directory);

                if (File.Exists(fullPath))
                {
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine($"Warning! File already exists. Overwriting: {hppFile.FolderName}/{hppFile.FileName}");
                    Console.ResetColor();
                }
                else
                {
                    Console.WriteLine($"Generated: {hppFile.FolderName}/{hppFile.FileName}");
                }

                File.WriteAllText(fullPath, hppFile.Content);
            }
        }
    }
}
