using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EbParser.Context
{
    class Comment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(128)]
        public string Author { get; set; }

        [Required]
        public DateTime Publish { get; set; }

        [Required]
        [ForeignKey("Post")]
        public int PostId { get; set; }

        public Post Post { get; set; }

        [Required]
        public string Content { get; set; }

        public int? ParentId { get; set; }

        public Comment ParentComment { get; set; }

        [Required]
        public DateTime Updated { get; set; }
    }
}