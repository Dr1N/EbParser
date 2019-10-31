using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EbParser.Context
{
    class Tag
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(64)]
        public string Name { get; set; }

        public ICollection<PostTag> PostTags { get; set; }
    }
}