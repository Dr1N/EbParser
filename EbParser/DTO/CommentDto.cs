using System;

namespace EbParser.DTO
{
    class CommentDto
    {
        public int Id { get; set; }

        public int ParrentId { get; set; }

        public string Author { get; set; }

        public DateTime Publish { get; set; }

        public string Content { get; set; }
    }
}