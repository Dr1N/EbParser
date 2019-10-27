using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EbParser.Context
{
    class Tag
    {
        [Key]
        public int Id { get; set; }

        [StringLength(64)]
        [Required]
        public string Name { get; set; }

        public ICollection<PostTag> PostTags { get; set; }
    }
}