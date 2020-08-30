using System.ComponentModel.DataAnnotations;
using Lib.AzureSearchStorage;
using Microsoft.Azure.Search;

namespace Lib.Models
{
    public class PersonIndex : IAzureSearchIndex
    {
        [Key]
        [IsFilterable]
        public string Id { get; set; }

        [IsFilterable]
        public string Name { get; set; }

        [IsFilterable, IsSortable]
        public int Age { get; set; }

        [IsFilterable]
        public string Email { get; set; }
    }
}