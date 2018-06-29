using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Newtonsoft.Json;
using Our.Umbraco.Tuple.Models;
using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Core.Models.Editors;
using Umbraco.Core.PropertyEditors;
using Umbraco.Core.Services;
using Umbraco.Web.PropertyEditors;

namespace Our.Umbraco.Tuple.PropertyEditors
{
    internal class TuplePropertyValueEditor : PropertyValueEditorWrapper
    {
        public TuplePropertyValueEditor(PropertyValueEditor wrapped)
            : base(wrapped)
        {
            Validators.Add(new TupleValidator());
        }

        public override void ConfigureForDisplay(PreValueCollection preValues)
        {
            base.ConfigureForDisplay(preValues);

            if (preValues.PreValuesAsDictionary.ContainsKey("hideLabel"))
            {
                var boolAttempt = preValues.PreValuesAsDictionary["hideLabel"].Value.TryConvertTo<bool>();
                if (boolAttempt.Success)
                {
                    this.HideLabel = boolAttempt.Result;
                }
            }
        }

        public override object ConvertDbToEditor(Property property, PropertyType propertyType, IDataTypeService dataTypeService)
        {
            var propertyValue = property?.Value?.ToString();
            if (propertyValue == null || string.IsNullOrWhiteSpace(propertyValue))
                return base.ConvertDbToEditor(property, propertyType, dataTypeService);

            var items = JsonConvert.DeserializeObject<TupleValueItems>(propertyValue);
            if (items == null || items.Count == 0)
                return base.ConvertDbToEditor(property, propertyType, dataTypeService);

            foreach (var item in items)
            {
                // Get the associated datatype definition
                var dtd = dataTypeService.GetDataTypeDefinitionById(item.DataTypeGuid); // TODO: Caching? [LK:2018-06-25]
                if (dtd == null)
                    continue;

                // Lookup the property editor and convert the db to editor value
                var propEditor = PropertyEditorResolver.Current.GetByAlias(dtd.PropertyEditorAlias); // TODO: Caching? [LK:2018-06-25]
                if (propEditor == null)
                    continue;

                var propType = new PropertyType(dtd);
                var prop = new Property(propType, item.Value);

                item.Value = propEditor.ValueEditor.ConvertDbToEditor(prop, propType, dataTypeService);
            }

            // Return the strongly-typed object, Umbraco will handle the JSON serializing/parsing, then Angular can handle it directly
            return items;
        }

        public override string ConvertDbToString(Property property, PropertyType propertyType, IDataTypeService dataTypeService)
        {
            var propertyValue = property?.Value?.ToString();
            if (propertyValue == null || string.IsNullOrWhiteSpace(propertyValue))
                return base.ConvertDbToString(property, propertyType, dataTypeService);

            var items = JsonConvert.DeserializeObject<TupleValueItems>(propertyValue);
            if (items == null || items.Count == 0)
                return base.ConvertDbToString(property, propertyType, dataTypeService);

            foreach (var item in items)
            {
                var dtd = dataTypeService.GetDataTypeDefinitionById(item.DataTypeGuid); // TODO: Caching? [LK:2018-06-25]
                if (dtd == null)
                    continue;

                var propEditor = PropertyEditorResolver.Current.GetByAlias(dtd.PropertyEditorAlias); // TODO: Caching? [LK:2018-06-25]
                if (propEditor == null)
                    continue;

                var propType = new PropertyType(dtd);
                var prop = new Property(propType, item.Value);

                item.Value = propEditor.ValueEditor.ConvertDbToString(prop, propType, dataTypeService);
            }

            return JsonConvert.SerializeObject(items);
        }

        public override XNode ConvertDbToXml(Property property, PropertyType propertyType, IDataTypeService dataTypeService)
        {
            var propertyValue = property?.Value?.ToString();
            if (propertyValue == null || string.IsNullOrWhiteSpace(propertyValue))
                return base.ConvertDbToXml(property, propertyType, dataTypeService);

            var items = JsonConvert.DeserializeObject<TupleValueItems>(propertyValue);
            if (items == null || items.Count == 0)
                return base.ConvertDbToXml(property, propertyType, dataTypeService);

            foreach (var item in items)
            {
                var dtd = dataTypeService.GetDataTypeDefinitionById(item.DataTypeGuid); // TODO: Caching? [LK:2018-06-25]
                if (dtd == null)
                    continue;

                var propEditor = PropertyEditorResolver.Current.GetByAlias(dtd.PropertyEditorAlias); // TODO: Caching? [LK:2018-06-25]
                if (propEditor == null)
                    continue;

                var propType = new PropertyType(dtd);
                var prop = new Property(propType, item.Value);

                item.DataTypeUdi = dtd.GetUdi();
                item.Value = propEditor.ValueEditor.ConvertDbToXml(prop, propType, dataTypeService);
            }

            return new XElement("values", items.Select(x => new XElement("value", new XAttribute("udi", x.DataTypeUdi), x.Value)));
        }

        public override object ConvertEditorToDb(ContentPropertyData editorValue, object currentValue)
        {
            var value = editorValue?.Value?.ToString();
            if (value == null || string.IsNullOrWhiteSpace(value))
                return base.ConvertEditorToDb(editorValue, currentValue);

            var model = JsonConvert.DeserializeObject<TupleValueItems>(value);
            if (model == null || model.Count == 0)
                return base.ConvertEditorToDb(editorValue, currentValue);

            var dataTypeService = ApplicationContext.Current.Services.DataTypeService;

            for (var i = 0; i < model.Count; i++)
            {
                var obj = model[i];

                var dtd = dataTypeService.GetDataTypeDefinitionById(obj.DataTypeGuid); // TODO: Caching? [LK:2018-06-25]
                if (dtd == null)
                    continue;

                var preValues = dataTypeService.GetPreValuesCollectionByDataTypeId(dtd.Id); // TODO: Caching? [LK:2018-06-25]
                if (preValues == null)
                    continue;

                var propEditor = PropertyEditorResolver.Current.GetByAlias(dtd.PropertyEditorAlias);
                if (propEditor == null)
                    continue;

                var propData = new ContentPropertyData(obj.Value, preValues, new Dictionary<string, object>());

                model[i].Value = propEditor.ValueEditor.ConvertEditorToDb(propData, obj.Value);
            }

            return JsonConvert.SerializeObject(model);
        }
    }
}