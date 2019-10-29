using System;

namespace EbParser.DTO
{
    class CommentDto
    {
        public string Id { get; set; }

        public string ParrentId { get; set; }

        public string Author { get; set; }

        public string Publish { get; set; }

        public string Content { get; set; }
    }
}