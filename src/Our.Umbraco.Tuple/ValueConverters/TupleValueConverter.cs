using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using Newtonsoft.Json;
using Our.Umbraco.Tuple.Models;
using Our.Umbraco.Tuple.PropertyEditors;
using Umbraco.Core;
using Umbraco.Core.Cache;
using Umbraco.Core.Models;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Core.PropertyEditors;

namespace Our.Umbraco.Tuple.ValueConverters
{
    public class TupleValueConverter : PropertyValueConverterBase, IPropertyValueConverterMeta
    {
        public override bool IsConverter(PublishedPropertyType propertyType)
        {
            return propertyType.PropertyEditorAlias.Equals(TuplePropertyEditor.PropertyEditorAlias);
        }

        public PropertyCacheLevel GetPropertyCacheLevel(PublishedPropertyType propertyType, PropertyCacheValue cacheValue)
        {
            return PropertyCacheLevel.Content;
        }

        public Type GetPropertyValueType(PublishedPropertyType propertyType)
        {
            var innerPropertyTypes = this.GetInnerPublishedPropertyTypes(propertyType);
            var clrTypes = innerPropertyTypes.Select(x => x.ClrType).ToArray();
            var tuple = Type.GetType($"System.Tuple`{clrTypes.Length}");
            return tuple.MakeGenericType(clrTypes);
        }

        public override object ConvertDataToSource(PublishedPropertyType propertyType, object source, bool preview)
        {
            var data = source?.ToString();
            if (string.IsNullOrWhiteSpace(data))
                return base.ConvertDataToSource(propertyType, source, preview);

            var innerPropertyTypes = this.GetInnerPublishedPropertyTypes(propertyType);
            if (innerPropertyTypes == null || innerPropertyTypes.Count == 0)
                return base.ConvertDataToSource(propertyType, source, preview);

            var model = JsonConvert.DeserializeObject<TupleValueItem[]>(data);
            if (model == null && model.Length > 0)
                return base.ConvertDataToSource(propertyType, source, preview);

            for (int i = 0; i < model.Length; i++)
            {
                var item = model[i];
                var innerPropertyType = innerPropertyTypes[i];
                var itemSource = innerPropertyType.ConvertDataToSource(item.Value, preview);

                var attempt = itemSource.TryConvertTo(innerPropertyType.ClrType);
                if (attempt.Success)
                    item.Value = attempt.Result;
            }

            return model;
        }

        public override object ConvertSourceToObject(PublishedPropertyType propertyType, object source, bool preview)
        {
            if (source is TupleValueItem[] model)
            {
                var innerPropertyTypes = this.GetInnerPublishedPropertyTypes(propertyType);
                if (innerPropertyTypes == null || innerPropertyTypes.Count == 0)
                    return base.ConvertSourceToObject(propertyType, source, preview);

                var objects = new object[model.Length];

                for (int i = 0; i < model.Length; i++)
                {
                    var innerPropertyType = innerPropertyTypes[i];
                    var itemObject = innerPropertyType.ConvertSourceToObject(model[i].Value, preview);

                    var attempt = itemObject.TryConvertTo(innerPropertyType.ClrType);
                    if (attempt.Success)
                        objects[i] = attempt.Result;
                }

                return Activator.CreateInstance(propertyType.ClrType, objects);
            }

            return base.ConvertSourceToObject(propertyType, source, preview);
        }

        public override object ConvertSourceToXPath(PublishedPropertyType propertyType, object source, bool preview)
        {
            if (source is TupleValueItem[] model)
            {
                var innerPropertyTypes = this.GetInnerPublishedPropertyTypes(propertyType);
                if (innerPropertyTypes == null || innerPropertyTypes.Count == 0)
                    return base.ConvertSourceToXPath(propertyType, source, preview);

                var elements = new XElement[model.Length];

                for (int i = 0; i < model.Length; i++)
                {
                    var itemXPath = innerPropertyTypes[i].ConvertSourceToXPath(model[i].Value, preview);

                    elements[i] = new XElement($"item{i}", itemXPath);
                }

                return new XElement("items", elements).CreateNavigator();
            }

            return base.ConvertSourceToXPath(propertyType, source, preview);
        }

        private List<PublishedPropertyType> GetInnerPublishedPropertyTypes(PublishedPropertyType propertyType)
        {
            var cacheKey = string.Format(
                "Our.Umbraco.Tuple.PropertyListValueConverter.GetInnerPublishedPropertyTypes_{0}_{1}",
                propertyType.DataTypeId,
                propertyType.ContentType.Id);

            return ApplicationContext.Current.ApplicationCache.RuntimeCache.GetCacheItem<List<PublishedPropertyType>>(
                cacheKey,
                () =>
                {
                    var list = new List<PublishedPropertyType>();
                    var dataTypeService = ApplicationContext.Current.Services.DataTypeService;
                    var prevalues = dataTypeService.GetPreValuesCollectionByDataTypeId(propertyType.DataTypeId);
                    var dict = prevalues.PreValuesAsDictionary;
                    if (dict.ContainsKey("dataTypes"))
                    {
                        var dtdPreValue = dict["dataTypes"];
                        var items = JsonConvert.DeserializeObject<IEnumerable<TupleValueItem>>(dtdPreValue.Value);
                        foreach (var item in items)
                        {
                            var dtd = dataTypeService.GetDataTypeDefinitionById(item.DataTypeGuid);
                            list.Add(new PublishedPropertyType(propertyType.ContentType, new PropertyType(dtd)));
                        }
                    }
                    return list;
                });
        }

        internal static void ClearDataTypeCache(int dataTypeId)
        {
            ApplicationContext.Current.ApplicationCache.RuntimeCache.ClearCacheByKeySearch(
                string.Concat("Our.Umbraco.Tuple.PropertyListValueConverter.GetInnerPublishedPropertyTypes_", dataTypeId));
        }
    }
}