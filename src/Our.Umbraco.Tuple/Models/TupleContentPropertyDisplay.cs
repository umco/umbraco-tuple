using System;
using System.Runtime.Serialization;
using Umbraco.Web.Models.ContentEditing;

namespace Our.Umbraco.Tuple.Models
{
    public class TupleContentPropertyDisplay : ContentPropertyDisplay
    {
        [DataMember(Name = "dataTypeGuid")]
        public Guid DataTypeGuid { get; set; }
    }
}