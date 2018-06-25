using Umbraco.Core.IO;
using Umbraco.Core.PropertyEditors;

namespace Our.Umbraco.Tuple.PropertyEditors
{
    internal class TuplePreValueEditor : PreValueEditor
    {
        public TuplePreValueEditor()
            : base()
        {
            // In terms of inheritance, we'd like the "dataType" field to always be at the top.
            Fields.Insert(0, new PreValueField
            {
                Key = "dataTypes",
                Name = "Data Types",
                View = IOHelper.ResolveUrl("~/App_Plugins/Tuple/datatype-picker.html"),
                Description = "Select data types for this tuple."
            });

            // The rest of the fields can be added at the bottom.
            Fields.AddRange(new[]
            {
                new PreValueField
                {
                    Key = "hideLabel",
                    Name = "Hide Label",
                    View = "boolean",
                    Description = "Set whether to hide the editor label and have the list take up the full width of the editor window."
                }
            });
        }
    }
}