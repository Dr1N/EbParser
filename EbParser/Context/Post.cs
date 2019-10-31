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

        [Required]
        [StringLength(1024)]
        public string Url { get; set; }

        [Required]
        [StringLength(128)]
        public string Title { get; set; }

        [Required]
        [StringLength(256)]
        public string Poster { get; set; }

        [Required]
        public DateTimeOffset Publish { get; set; }

        [Required]
        public string Content { get; set; }

        [StringLength(32)]
        public string Category { get; set; }

        public ICollection<PostTag> PostTags { get; set; }

        [ForeignKey("PostId")]
        public ICollection<Comment> Comments { get; set; }

        [Required]
        public DateTime Updated { get; set; }
    }
}