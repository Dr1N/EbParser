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

        [StringLength(128)]
        [Required]
        public string Date { get; set; }

        [Required]
        public string Content { get; set; }

        public ICollection<PostTag> PostTags { get; set; }

        [ForeignKey("PostId")]
        public ICollection<Comment> Comments { get; set; }

        [ForeignKey("PostId")]
        public ICollection<File> Files { get; set; }

        public DateTime Updated { get; set; }
    }
}