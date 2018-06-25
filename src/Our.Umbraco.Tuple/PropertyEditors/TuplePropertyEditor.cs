using ClientDependency.Core;
using Umbraco.Core.PropertyEditors;
using Umbraco.Web.PropertyEditors;

namespace Our.Umbraco.Tuple.PropertyEditors
{
    [PropertyEditor(PropertyEditorAlias, PropertyEditorName, "JSON", PropertyEditorViewPath, Group = "Common", Icon = "icon-sience", IsParameterEditor = false)]
    [PropertyEditorAsset(ClientDependencyType.Css, "~/App_Plugins/Tuple/tuple.css")]
    [PropertyEditorAsset(ClientDependencyType.Javascript, "~/App_Plugins/Tuple/tuple.js")]
    public class TuplePropertyEditor : PropertyEditor
    {
        public const string PropertyEditorAlias = "Our.Umbraco.Tuple";
        public const string PropertyEditorName = "Tuple";
        public const string PropertyEditorViewPath = "~/App_Plugins/Tuple/tuple.html";

        protected override PreValueEditor CreatePreValueEditor()
        {
            return new TuplePreValueEditor();
        }

        protected override PropertyValueEditor CreateValueEditor()
        {
            return new TuplePropertyValueEditor(base.CreateValueEditor());
        }
    }
}