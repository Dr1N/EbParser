using System;
using System.Collections.Generic;

namespace EbParser.DTO
{
    class PostDto
    {
        public string Title { get; set; }

        public string Category { get; set; }

        public string Content { get; set; }

        public string Poster { get; set; }

        public DateTimeOffset Publish { get; set; }

        public string Author { get; set; }

        public IList<string> Tags { get; set; } = new List<string>();

        public IList<CommentDto> Comments { get; set; } = new List<CommentDto>();

        public IList<string> Files { get; set; } = new List<string>();
    }
}