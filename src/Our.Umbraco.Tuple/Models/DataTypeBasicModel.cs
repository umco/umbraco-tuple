using System;

namespace Our.Umbraco.Tuple.Models
{
    public class DataTypeBasicModel
    {
        public int id { get; set; }
        public Guid key { get; set; }
        public string name { get; set; }
        public string icon { get; set; }
        public string description { get; set; }
    }
}