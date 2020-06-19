using System.ComponentModel.DataAnnotations;

namespace EbParser.Context
{
    internal class File
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(1024)]
        public string Url { get; set; }

        [Required]
        [StringLength(128)]
        public string FileName { get; set; }
    }
}