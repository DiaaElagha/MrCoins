using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Coins.Core.Helpers.Models
{
    public class Pagination
    {
        [JsonIgnore]
        public int TotalItems { get; set; }
        [JsonIgnore]
        public int CurrentPage { get; set; }
        [JsonIgnore]
        public int PageSize { get; set; }
        public int TotalPages { get; set; } // this is the only property that is assigned
    }
}
