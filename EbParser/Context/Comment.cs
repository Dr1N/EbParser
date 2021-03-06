﻿using System;
using System.ComponentModel.DataAnnotations;

namespace EbParser.Context
{
    internal class Comment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(128)]
        public string Author { get; set; }

        [Required]
        public DateTime Publish { get; set; }

        [Required]
        public int PostId { get; set; }

        public Post Post { get; set; }

        [Required]
        public string Content { get; set; }

        public int? ParentId { get; set; }

        public Comment Parent { get; set; }

        [Required]
        public DateTime Updated { get; set; }
    }
}