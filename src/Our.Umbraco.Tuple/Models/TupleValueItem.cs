using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Umbraco.Core;

namespace Our.Umbraco.Tuple.Models
{
    public class TupleValueItems : List<TupleValueItem>
    { }

    public class TupleValueItem
    {
        [JsonProperty("key")]
        public Guid Key { get; set; }

        [JsonProperty("dtd")]
        public Guid DataTypeGuid { get; set; }

        [JsonIgnore]
        internal Udi DataTypeUdi { get; set; }

        [JsonProperty("value")]
        public object Value { get; set; }
    }
}