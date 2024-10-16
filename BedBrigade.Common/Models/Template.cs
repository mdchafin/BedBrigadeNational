using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BedBrigade.Common.Enums;

namespace BedBrigade.Common.Models
{
    [Table("Templates")]
    public class Template : BaseEntity
    {
		[Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Int32 TemplateId { get; set; }

		[Required]
		public ContentType ContentType { get; set; } = ContentType.Body;

		[Required]
		[MaxLength(30)]
		public String Name { get; set; } = string.Empty;

		public String? ContentHtml { get; set; } = string.Empty;
    }
}
