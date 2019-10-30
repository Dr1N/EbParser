using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EbParser.Context
{
    class Post
    {
        [Key]
        public int Id { get; set; }

        [StringLength(1024)]
        [Required]
        public string Url { get; set; }

        [StringLength(128)]
        [Required]
        public string Title { get; set; }

        [Required]
        public DateTimeOffset Publish { get; set; }

        [Required]
        public string Content { get; set; }

        [StringLength(32)]
        public string Category { get; set; }

        public ICollection<PostTag> PostTags { get; set; }

        [ForeignKey("PostId")]
        public ICollection<Comment> Comments { get; set; }

        [ForeignKey("PostId")]
        public ICollection<File> Files { get; set; }

        [Required]
        public DateTime Updated { get; set; }
    }
}