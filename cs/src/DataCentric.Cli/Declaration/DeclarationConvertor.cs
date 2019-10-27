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
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using MongoDB.Bson.Serialization.Attributes;
using NodaTime;

namespace DataCentric.Cli
{
    /// <summary>
    /// Converts types to declarations.
    /// </summary>
    public static class DeclarationConvertor
    {
        /// <summary>
        /// Contains all allowed primitive types which could be converted in declarations.
        /// </summary>
        private static readonly Type[] AllowedPrimitiveTypes = {
            typeof(string),

            typeof(bool),
            typeof(DateTime),
            typeof(double),
            typeof(int),
            typeof(long),
            typeof(LocalDate),
            typeof(LocalTime),
            typeof(LocalMinute),
            typeof(LocalDateTime),
            typeof(Instant),
            typeof(TemporalId),

            // Nullables
            typeof(bool?),
            typeof(DateTime?),
            typeof(double?),
            typeof(int?),
            typeof(long?),
            typeof(LocalDate?),
            typeof(LocalTime?),
            typeof(LocalMinute?),
            typeof(LocalDateTime?),
            typeof(Instant?),
            typeof(TemporalId?),
        };

        /// <summary>
        /// Flags to extract public instance members declared at current level.
        /// </summary>
        private const BindingFlags PublicInstanceDeclaredFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly;

        /// <summary>
        /// Factory method which creates declaration corresponding to given type.
        /// </summary>
        public static IDecl ToDecl(Type type, CommentNavigator navigator, ProjectNavigator projNavigator)
        {
            if (type.IsSubclassOf(typeof(Enum)))
                return EnumToDecl(type, navigator, projNavigator);

            if (type.IsSubclassOf(typeof(Data)))
                return TypeToDecl(type, navigator, projNavigator);

            throw new ArgumentException($"{type.FullName} is not subclass of Enum or Data", nameof(type));
        }

        /// <summary>
        /// Converts enum to EnumDecl
        /// </summary>
        public static EnumDecl EnumToDecl(Type type, CommentNavigator navigator, ProjectNavigator projNavigator)
        {
            if (!type.IsSubclassOf(typeof(Enum)))
                throw new ArgumentException($"Cannot create enum declaration from type: {type.FullName}.");

            EnumDecl decl = new EnumDecl();

            decl.Name = type.Name;
            decl.Comment = type.GetCommentFromAttribute() ?? navigator?.GetXmlComment(type);
            decl.Category = projNavigator?.GetTypeLocation(type);
            decl.Module = new ModuleKey { ModuleName = type.Namespace };
            decl.Label = type.GetLabelFromAttribute() ?? type.Name;

            List<FieldInfo> items = type.GetFields(BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Static).ToList();

            decl.Items = items.Select(i => ToEnumItem(i, navigator)).ToList();
            return decl;
        }

        /// <summary>
        /// Converts type inherited from Data to TypeDecl
        /// </summary>
        public static TypeDecl TypeToDecl(Type type, CommentNavigator navigator, ProjectNavigator projNavigator)
        {
            if (!type.IsSubclassOf(typeof(Data)))
                throw new ArgumentException($"Cannot create type declaration from type: {type.FullName}.");

            TypeDecl decl = new TypeDecl();
            decl.Module = new ModuleKey { ModuleName = type.Namespace };
            decl.Category = projNavigator?.GetTypeLocation(type);
            decl.Name = type.Name;
            decl.Label = type.GetLabelFromAttribute() ?? type.Name;
            decl.Comment = type.GetCommentFromAttribute() ?? navigator?.GetXmlComment(type);
            decl.Kind = type.GetKind();
            decl.Inherit = IsRoot(type.BaseType)
                               ? null
                               : CreateTypeDeclKey(type.BaseType.Namespace, type.BaseType.Name);
            decl.Index = type.GetIndexesFromAttributes();

            // Skip special (property getters, setters, etc) and inherited methods
            List<MethodInfo> handlers = type.GetMethods(PublicInstanceDeclaredFlags)
                                           .Where(IsProperHandler)
                                           .ToList();

            var declares = new List<HandlerDeclareDecl>();
            var implements = new List<HandlerImplementDecl>();
            foreach (MethodInfo method in handlers)
            {
                // Abstract methods have only declaration
                if (method.IsAbstract)
                {
                    declares.Add(ToDeclare(method, navigator));
                }
                // Overriden methods are marked with ovveride
                else if(method.GetBaseDefinition() != method)
                {
                    implements.Add(ToImplement(method));
                }
                // Case for methods without modifiers
                else
                {
                    declares.Add(ToDeclare(method, navigator));
                    implements.Add(ToImplement(method));
                }
            }

            // Add method information to declaration
            if (declares.Any()) decl.Declare = new HandlerDeclareBlockDecl {Handlers = declares};
            if (implements.Any()) decl.Implement = new HandlerImplementBlockDecl {Handlers = implements};

            List<PropertyInfo> dataProperties = type.GetProperties(PublicInstanceDeclaredFlags)
                                                    .Where(p => IsAllowedType(p.PropertyType))
                                                    .Where(IsPublicGetSet).ToList();

            decl.Elements = dataProperties.Select(p => ToElement(p, navigator)).ToList();
            decl.Keys = type.GetKeyProperties()
                            .Where(p => IsAllowedType(p.PropertyType))
                            .Where(IsPublicGetSet)
                            .Select(t => t.Name).ToList();

            return decl;
        }

        /// <summary>
        /// Checks if given type is any of Data, Record&lt;,&gt;, RootRecord&lt;,&gt;
        /// </summary>
        private static bool IsRoot(Type type)
        {
            if (type == typeof(Data) || type == typeof(Record))
                return true;

            if (type.IsGenericType)
            {
                Type genericType = type.GetGenericTypeDefinition();
                return genericType == typeof(RootRecord<,>) ||
                       genericType == typeof(TypedRecord<,>) ||
                       genericType == typeof(TypedKey<,>);
            }

            return false;
        }

        /// <summary>
        /// Checks if method fits handler restrictions. Take parameters that are either
        /// atomic types or classes derived from Data; and its return type is void.
        /// </summary>
        private static bool IsProperHandler(MethodInfo method)
        {
            // Only void methods are allowed
            if (method.ReturnType != typeof(void))
                return false;

            // Filter out internal and private methods
            if (method.IsSpecialName)
                return false;

            // Check if method is declared in Data, Record or other root classes
            if (TypesExtractor.BasicTypes.Contains(method.GetBaseDefinition().ReflectedType))
                return false;

            var hasHandlerAttribute = method.GetCustomAttribute<HandlerAttribute>() != null;

            // Check if all method parameters are allowed:
            // either atomic types or classes derived from Data
            bool hasAllowedParameters = method.GetParameters().All(p => IsAllowedType(p.ParameterType));

            return hasAllowedParameters && hasHandlerAttribute;
        }

        /// <summary>
        /// Checks if property has both public getter and setter.
        /// </summary>
        private static bool IsPublicGetSet(PropertyInfo property)
        {
            return property.GetGetMethod() != null && property.GetSetMethod() != null;
        }

        /// <summary>
        /// Extracts argument type from List&lt;&gt;, [].
        /// </summary>
        private static Type GetListArgument(Type type)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
                return type.GetGenericArgument(0);
            if (type.IsArray)
                return type.GetElementType();

            return type;
        }

        /// <summary>
        /// Checks if type could be used in declaration.
        /// Namely, it checks if it is one of the following: is primitive, is enum or derived from Data
        /// </summary>
        private static bool IsAllowedType(Type type)
        {
            if (type.IsGenericMethodParameter)
                return false;

            if (IsRoot(type))
                return false;

            type = GetListArgument(type);

            return AllowedPrimitiveTypes.Contains(type) ||
                   type.IsSubclassOf(typeof(Data)) ||
                   type.IsEnum;
        }

        /// <summary>
        /// Returns properties for corresponding key class if exist.
        /// </summary>
        private static List<PropertyInfo> GetKeyProperties(this Type type)
        {
            var baseType = type.BaseType;
            if (baseType.IsGenericType && (baseType.GetGenericTypeDefinition() == typeof(TypedRecord<,>) ||
                                           baseType.GetGenericTypeDefinition() == typeof(RootRecord<,>)))
            {
                var keyType = baseType.GenericTypeArguments[0];
                return keyType.GetProperties(PublicInstanceDeclaredFlags).Where(IsPublicGetSet).ToList();
            }

            return new List<PropertyInfo>();
        }

        /// <summary>
        /// Determines kind of declaration.
        /// </summary>
        private static TypeKind? GetKind(this Type type)
        {
            // Kind
            return type.IsAbstract                    ? TypeKind.Abstract :
                   type.IsSealed                      ? TypeKind.Final :
                   !type.IsSubclassOf(typeof(Record)) ? TypeKind.Element :
                                                        (TypeKind?) null;
        }

        /// <summary>
        /// Checks if given member is hidden.
        /// </summary>
        private static YesNo IsHidden(this MemberInfo member)
        {
            BrowsableAttribute attribute = member.GetCustomAttribute<BrowsableAttribute>();
            return attribute?.Browsable ?? true ? YesNo.N : YesNo.Y;
        }

        /// <summary>
        /// Tries to get label from DisplayName or Display attribute.
        /// </summary>
        private static string GetLabelFromAttribute(this MemberInfo member)
        {
            return member.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ??
                   member.GetCustomAttribute<DisplayAttribute>()?.GetName();
        }

        /// <summary>
        /// Extract type index info and add to declaration.
        /// </summary>
        private static List<TypeIndex> GetIndexesFromAttributes(this Type type)
        {
            var attributes = type.GetCustomAttributes<IndexElementsAttribute>().ToList();

            // Type does not have index info - skip
            if (!attributes.Any())
                return null;

            var result = new List<TypeIndex>();

            // Process each attribute
            foreach (var attribute in attributes)
            {
                TypeIndex typeIndex = new TypeIndex
                {
                    Element = new List<TypeElementIndex>(), Name = attribute.Name
                };

                // Decompose string index definition "A, -B" to ordered list of tuples (ElementName,SortOrder): [("A",1), ("B",-1)]
                MethodInfo parseDefinitionMethod = typeof(IndexElementsAttribute)
                                                  .GetMethod(nameof(IndexElementsAttribute.ParseDefinition),
                                                             BindingFlags.Static | BindingFlags.Public)
                                                 ?.MakeGenericMethod(type);
                var definition = (List<(string, int)>) parseDefinitionMethod?.Invoke(null, new object[] {attribute.Definition});

                // Convert decomposed definition to declarations format
                foreach ((string, int) tuple in definition)
                {
                    TypeElementIndex elementIndex = new TypeElementIndex
                    {
                        Name = tuple.Item1,
                        Direction = tuple.Item2 == -1
                                        ? TypeElementIndexDirection.Descending
                                        : TypeElementIndexDirection.Ascending
                    };

                    typeIndex.Element.Add(elementIndex);
                }

                result.Add(typeIndex);
            }

            return result;
        }

        /// <summary>
        /// Tries to get comment from DisplayName or Display attributes.
        /// </summary>
        private static string GetCommentFromAttribute(this MemberInfo member)
        {
            return member.GetCustomAttribute<DescriptionAttribute>()?.Description ??
                   member.GetCustomAttribute<DisplayAttribute>()?.GetDescription();
        }

        /// <summary>
        /// Generates handler declare section which corresponds to given method.
        /// </summary>
        private static HandlerDeclareDecl ToDeclare(MethodInfo method, CommentNavigator navigator)
        {
            return new HandlerDeclareDecl
            {
                Name = method.Name,
                Type = HandlerType.Job,
                Label = method.GetLabelFromAttribute(),
                Comment = method.GetCommentFromAttribute() ?? navigator?.GetXmlComment(method),
                Hidden = method.IsHidden(),
                Static = method.IsStatic ? YesNo.Y : (YesNo?) null,
                Params = method.GetParameters().Select(ToHandlerParam).ToList(),
                Return = method.ReturnType != typeof(void) ? ToReturnType(method.ReturnType) : null
            };
        }

        /// <summary>
        /// Generates handler implement section which corresponds to given method.
        /// </summary>
        private static HandlerImplementDecl ToImplement(MethodInfo method)
        {
            return new HandlerImplementDecl
            {
                Name = method.Name,
                Language = new LanguageKey {LanguageName = "cs"}
            };
        }

        /// <summary>
        /// Converts method parameter into corresponding handler parameter declaration section.
        /// </summary>
        private static HandlerParamDecl ToHandlerParam(ParameterInfo parameter)
        {
            var handlerParam = ToTypeMember<HandlerParamDecl>(parameter.ParameterType);

            handlerParam.Name = parameter.Name;
            handlerParam.Optional = parameter.IsOptional ? YesNo.Y : YesNo.N;
            handlerParam.Vector = parameter.ParameterType.IsVector();

            return handlerParam;
        }

        /// <summary>
        /// Converts method return type into corresponding handler return type declaration section.
        /// </summary>
        private static HandlerVariableDecl ToReturnType(Type type)
        {
            var returnType = ToTypeMember<HandlerVariableDecl>(type);

            returnType.Vector = type.IsVector();

            return returnType;
        }

        /// <summary>
        /// Converts given property into corresponding declaration element.
        /// </summary>
        private static TypeElementDecl ToElement(PropertyInfo property, CommentNavigator navigator)
        {
            var element = ToTypeMember<TypeElementDecl>(property.PropertyType);

            element.Vector = property.PropertyType.IsVector();
            element.Name = property.Name;
            element.Label = property.GetLabelFromAttribute();
            element.Comment = property.GetCommentFromAttribute() ?? navigator?.GetXmlComment(property);
            element.Viewer = property.GetCustomAttribute<DisplayAttribute>()?.GetGroupName();
            element.Optional = property.GetCustomAttribute<RequiredAttribute>() == null ? YesNo.Y : (YesNo?) null;
            element.BsonIgnore = property.GetCustomAttribute<BsonIgnoreAttribute>() != null ? YesNo.Y : (YesNo?) null;
            element.Hidden = property.IsHidden();

            return element;
        }

        /// <summary>
        /// Converts to enum item declaration.
        /// </summary>
        private static EnumItemDecl ToEnumItem(FieldInfo field, CommentNavigator navigator)
        {
            var item = new EnumItemDecl();

            item.Name = field.Name;
            item.Comment = navigator?.GetXmlComment(field);
            item.Label = field.GetLabelFromAttribute();

            return item;
        }

        /// <summary>
        /// Creates type member declaration for the given type.
        /// </summary>
        private static T ToTypeMember<T>(Type type) where T : TypeMemberDecl, new()
        {
            var typeDecl = new T();

            type = GetListArgument(type);
            if (type.IsEnum)
            {
                typeDecl.Enum = CreateTypeDeclKey(type.Namespace, type.Name);
            }
            else if (type.IsValueType || type == typeof(string))
            {
                typeDecl.Value = new ValueDecl();

                TypeCode typeCode = Type.GetTypeCode(type);
                typeDecl.Value.Type =
                    typeCode == TypeCode.String ? AtomicType.String :
                    // Basic value types
                    typeCode == TypeCode.Boolean  ? AtomicType.Bool :
                    typeCode == TypeCode.DateTime ? AtomicType.DateTime :
                    typeCode == TypeCode.Double   ? AtomicType.Double :
                    typeCode == TypeCode.Int32    ? AtomicType.Int :
                    typeCode == TypeCode.Int64    ? AtomicType.Long :
                    // Basic nullable value types
                    type == typeof(bool?)     ? AtomicType.NullableBool :
                    type == typeof(DateTime?) ? AtomicType.NullableDateTime :
                    type == typeof(double?)   ? AtomicType.NullableDouble :
                    type == typeof(int?)      ? AtomicType.NullableInt :
                    type == typeof(long?)     ? AtomicType.NullableLong :
                    // Noda types
                    type == typeof(LocalDateTime) ? AtomicType.DateTime :
                    type == typeof(Instant) ? AtomicType.DateTime :
                    type == typeof(LocalDate)     ? AtomicType.Date :
                    type == typeof(LocalTime)     ? AtomicType.Time :
                    type == typeof(LocalMinute)   ? AtomicType.Minute :
                    // Nullable Noda types
                    type == typeof(LocalDateTime?) ? AtomicType.NullableDateTime :
                    type == typeof(Instant?) ? AtomicType.NullableDateTime :
                    type == typeof(LocalDate?)     ? AtomicType.NullableDate :
                    type == typeof(LocalTime?)     ? AtomicType.NullableTime :
                    type == typeof(LocalMinute?)   ? AtomicType.NullableMinute :
                    // TemporalId
                    type == typeof(TemporalId)  ? AtomicType.TemporalId :
                    type == typeof(TemporalId?) ? AtomicType.NullableTemporalId :
                                                throw new ArgumentException($"Unknown value type: {type.FullName}");
            }
            else if (type.IsSubclassOf(typeof(Key)))
            {
                // Extract TRecord type from key base class TypedKey[TKey, TRecord]
                Type recordParameter = type.BaseType.GenericTypeArguments[1];
                if (!recordParameter.IsSubclassOf(typeof(Record)))
                    throw new ArgumentException($"Wrong generic argument of {type.Name} key.");
                typeDecl.Key = CreateTypeDeclKey(type.Namespace, recordParameter.Name);
            }
            else if (type.IsSubclassOf(typeof(Data)))
            {
                typeDecl.Data = CreateTypeDeclKey(type.Namespace, type.Name);
            }
            else
                throw new ArgumentException($"Unknown type: {type.FullName}");

            return typeDecl;
        }

        /// <summary>
        /// Simply creates type reference from type namespace and type name.
        /// </summary>
        private static TypeDeclKey CreateTypeDeclKey(string ns, string name)
        {
            return new TypeDeclKey { Name = name, Module = new ModuleKey { ModuleName = ns } };
        }

        /// <summary>
        /// Check if given type is List&lt;&gt; or array instance.
        /// </summary>
        private static YesNo? IsVector(this Type type)
        {
            bool isList = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>);
            bool isArray = type.IsArray;

            return isList || isArray ? YesNo.Y : (YesNo?) null;
        }
    }
}