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
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using MongoDB.Bson;
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
            typeof(LocalDateTime),
            typeof(LocalDate),
            typeof(LocalTime),
            typeof(LocalMinute),
            typeof(ObjectId),
            // Nullables
            typeof(bool?),
            typeof(DateTime?),
            typeof(double?),
            typeof(int?),
            typeof(long?),
            typeof(LocalDateTime?),
            typeof(LocalDate?),
            typeof(LocalTime?),
            typeof(LocalMinute?),
            typeof(ObjectId?),
        };

        /// <summary>
        /// Flags to extract public instance members declared at current level.
        /// </summary>
        private const BindingFlags PublicInstanceDeclaredFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly;

        /// <summary>
        /// Factory method which creates declaration corresponding to given type.
        /// </summary>
        public static IDeclData ToDecl(Type type, CommentNavigator navigator, ProjectNavigator projNavigator)
        {
            if (type.IsSubclassOf(typeof(Enum)))
                return EnumToDecl(type, navigator, projNavigator);

            if (type.IsSubclassOf(typeof(Data)))
                return TypeToDecl(type, navigator, projNavigator);

            throw new ArgumentException($"{type.FullName} is not subclass of Enum or ClData", nameof(type));
        }

        /// <summary>
        /// Converts enum to EnumDeclData
        /// </summary>
        public static EnumDeclData EnumToDecl(Type type, CommentNavigator navigator, ProjectNavigator projNavigator)
        {
            if (!type.IsSubclassOf(typeof(Enum)))
                throw new ArgumentException($"Cannot create enum declaration from type: {type.FullName}.");

            EnumDeclData decl = new EnumDeclData();

            decl.Name = type.Name;
            decl.Comment = type.GetCommentFromAttribute() ?? navigator?.GetXmlComment(type);
            decl.Category = projNavigator.GetTypeLocation(type);
            decl.Module = new ModuleKey { ModuleId = type.Namespace };
            decl.Label = type.GetLabelFromAttribute() ?? type.Name;

            List<FieldInfo> items = type.GetFields(BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Static).ToList();

            decl.Items = items.Select(i => ToEnumItem(i, navigator)).ToList();
            return decl;
        }

        /// <summary>
        /// Converts type inherited from Data to TypeDeclData
        /// </summary>
        public static TypeDeclData TypeToDecl(Type type, CommentNavigator navigator, ProjectNavigator projNavigator)
        {
            if (!type.IsSubclassOf(typeof(Data)))
                throw new ArgumentException($"Cannot create type declaration from type: {type.FullName}.");

            TypeDeclData decl = new TypeDeclData();
            decl.Module = new ModuleKey { ModuleId = type.Namespace };
            decl.Category = projNavigator?.GetTypeLocation(type);
            decl.Name = type.Name.TrimEnd("Data");
            decl.Label = type.GetLabelFromAttribute() ?? type.Name.TrimEnd("Data");
            decl.Comment = type.GetCommentFromAttribute() ?? navigator?.GetXmlComment(type);
            decl.Kind = type.GetKind();
            decl.Inherit = IsRoot(type.BaseType)
                               ? null
                               : CreateTypeDeclKey(type.BaseType.Namespace, type.BaseType.Name.TrimEnd("Data"));

            // Skip special (property getters, setters, etc) and inherited methods
            List<MethodInfo> methods = type.GetMethods(PublicInstanceDeclaredFlags)
                                           .Where(IsAllowed)
                                           .ToList();

            var declares = new List<HandlerDeclareDeclData>();
            var implements = new List<HandlerImplementDeclData>();
            foreach (MethodInfo method in methods)
            {
                // Abstract methods have only declaration
                if (method.IsAbstract)
                {
                    declares.Add(ToDeclare(method, navigator));
                }
                // Overriden methods have only implementation tag and are marked with ovveride
                else if(method.GetBaseDefinition() != method)
                {
                    HandlerImplementDeclData implement = ToImplement(method);
                    implement.Override = YesNo.Y;
                    implements.Add(implement);
                }
                // Case for methods without modifiers
                else
                {
                    declares.Add(ToDeclare(method, navigator));
                    implements.Add(ToImplement(method));
                }
            }

            // Add method information to declaration
            if (declares.Any()) decl.Declare = new HandlerDeclareBlockDeclData {Handlers = declares};
            if (implements.Any()) decl.Implement = new HandlerImplementBlockDeclData {Handlers = implements};

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
            if (type == typeof(DataType) || type == typeof(RecordBase))
                return true;

            if (type.IsGenericType)
            {
                Type genericType = type.GetGenericTypeDefinition();
                return genericType == typeof(Record<,>) ||
                       genericType == typeof(Key<,>) ||
                       genericType == typeof(RootRecord<,>) ||
                       genericType == typeof(RootKey<,>);
            }

            return false;
        }

        /// <summary>
        /// Determines if given method could be converted to handler.
        /// </summary>
        private static bool IsAllowed(MethodInfo method)
        {
            // Check if return type is allowed
            bool isAllowedReturnType = (IsAllowedType(method.ReturnType) || method.ReturnType == typeof(void));
            // Check if all method parameters are allowed
            bool isAllowedParameterTypes = method.GetParameters().All(p => IsAllowedType(p.ParameterType));
            // Check if method is declared in Data, Record or other root classes
            bool isRootMethod = TypesExtractor.BasicTypes.Contains(method.GetBaseDefinition().ReflectedType);

            return !method.IsSpecialName &&
                   !isRootMethod &&
                   isAllowedReturnType &&
                   isAllowedParameterTypes;
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
                   type.IsSubclassOf(typeof(DataType)) ||
                   type.IsEnum;
        }

        /// <summary>
        /// Returns properties for corresponding key class if exist.
        /// </summary>
        private static List<PropertyInfo> GetKeyProperties(this Type type)
        {
            var baseType = type.BaseType;
            if (baseType.IsGenericType && (baseType.GetGenericTypeDefinition() == typeof(Record<,>) ||
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
            return type.IsAbstract                        ? TypeKind.Abstract :
                   type.IsSealed                          ? TypeKind.Final :
                   !type.IsSubclassOf(typeof(RecordBase)) ? TypeKind.Element :
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
        private static HandlerDeclareDeclData ToDeclare(MethodInfo method, CommentNavigator navigator)
        {
            return new HandlerDeclareDeclData
            {
                Name = method.Name,
                Type = HandlerType.Job,
                Label = method.GetLabelFromAttribute(),
                Comment = method.GetCommentFromAttribute() ?? navigator?.GetXmlComment(method),
                Hidden = method.IsHidden(),
                Params = method.GetParameters().Select(ToHandlerParam).ToList(),
                Return = method.ReturnType != typeof(void) ? ToReturnType(method.ReturnType) : null
            };
        }

        /// <summary>
        /// Generates handler implement section which corresponds to given method.
        /// </summary>
        private static HandlerImplementDeclData ToImplement(MethodInfo method)
        {
            return new HandlerImplementDeclData
            {
                Name = method.Name,
                Language = new LanguageKey {LanguageId = "cs"}
            };
        }

        /// <summary>
        /// Converts method parameter into corresponding handler parameter declaration section.
        /// </summary>
        private static HandlerParamDeclData ToHandlerParam(ParameterInfo parameter)
        {
            var handlerParam = ToTypeMember<HandlerParamDeclData>(parameter.ParameterType);

            handlerParam.Name = parameter.Name;
            handlerParam.Optional = parameter.IsOptional ? YesNo.Y : YesNo.N;
            handlerParam.Vector = parameter.ParameterType.IsVector();

            return handlerParam;
        }

        /// <summary>
        /// Converts method return type into corresponding handler return type declaration section.
        /// </summary>
        private static HandlerVariableDeclData ToReturnType(Type type)
        {
            var returnType = ToTypeMember<HandlerVariableDeclData>(type);

            returnType.Vector = type.IsVector();

            return returnType;
        }

        /// <summary>
        /// Converts given property into corresponding declaration element.
        /// </summary>
        private static TypeElementDeclData ToElement(PropertyInfo property, CommentNavigator navigator)
        {
            var element = ToTypeMember<TypeElementDeclData>(property.PropertyType);

            element.Vector = property.PropertyType.IsVector();
            element.Name = property.Name;
            element.Label = property.GetLabelFromAttribute();
            element.Comment = property.GetCommentFromAttribute() ?? navigator?.GetXmlComment(property);
            element.Viewer = property.GetCustomAttribute<DisplayAttribute>()?.GetGroupName();
            element.Optional = property.GetCustomAttribute<RequiredAttribute>() == null ? YesNo.Y : (YesNo?) null;
            element.BsonIgnore = property.GetCustomAttribute<BsonIgnoreAttribute>() != null ? YesNo.Y : (YesNo?) null;
            element.Hidden = property.IsHidden();
            element.Indices = Attribute.IsDefined(property, typeof(IndexedAttribute))
                                ? property.GetCustomAttributes<IndexedAttribute>().Select(ToIndex).ToList()
                                : null;

            return element;
        }

        /// <summary>
        /// Handles index attribute conversion.
        /// </summary>
        private static TypeElementIndexData ToIndex(IndexedAttribute attribute){
            TypeElementIndexData indexData = new TypeElementIndexData
            {
                Name = attribute.Index != string.Empty ? attribute.Index : null,
                Order = attribute.Order != IntUtils.Empty ? attribute.Order : (int?) null
            };
            return indexData;
        }

        /// <summary>
        /// Converts to enum item declaration.
        /// </summary>
        private static EnumItemDeclData ToEnumItem(FieldInfo field, CommentNavigator navigator)
        {
            var item = new EnumItemDeclData();

            item.Name = field.Name;
            item.Comment = navigator?.GetXmlComment(field);
            item.Label = field.GetLabelFromAttribute();

            return item;
        }

        /// <summary>
        /// Creates type member declaration for the given type.
        /// </summary>
        private static T ToTypeMember<T>(Type type) where T : TypeMemberDeclData, new()
        {
            var typeDecl = new T();

            type = GetListArgument(type);
            if (type.IsEnum)
            {
                typeDecl.Enum = CreateTypeDeclKey(type.Namespace, type.Name);
            }
            else if (type.IsValueType || type == typeof(string))
            {
                typeDecl.Value = new ValueDeclData();

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
                    type == typeof(LocalDate)     ? AtomicType.Date :
                    type == typeof(LocalTime)     ? AtomicType.Time :
                    type == typeof(LocalMinute)   ? AtomicType.Minute :
                    // Nullable Noda types
                    type == typeof(LocalDateTime?) ? AtomicType.NullableDateTime :
                    type == typeof(LocalDate?)     ? AtomicType.NullableDate :
                    type == typeof(LocalTime?)     ? AtomicType.NullableTime :
                    type == typeof(LocalMinute?)   ? AtomicType.NullableMinute :
                    // Mongo's ObjectId
                    type == typeof(ObjectId)  ? AtomicType.ObjectId :
                    type == typeof(ObjectId?) ? AtomicType.NullableObjectId :
                                                throw new ArgumentException($"Unknown value type: {type.FullName}");
            }
            else if (type.IsSubclassOf(typeof(KeyBase)) && type.Name.EndsWith("Key"))
            {
                typeDecl.Key = CreateTypeDeclKey(type.Namespace, type.Name.TrimEnd("Key"));
            }
            else if (type.IsSubclassOf(typeof(DataType)) && type.Name.EndsWith("Data"))
            {
                typeDecl.Data = CreateTypeDeclKey(type.Namespace, type.Name.TrimEnd("Data"));
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
            return new TypeDeclKey { Name = name, Module = new ModuleKey { ModuleId = ns } };
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