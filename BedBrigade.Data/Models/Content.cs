using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static BedBrigade.Common.Common;

namespace BedBrigade.Data.Models
{
	[Table("Content")]
    public class Content : BaseEntity
    {
		[Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Int32 ContentId { get; set; }

		[ForeignKey("LocationId")]
		public Int32 LocationId { get; set; }

		[Required]
		public ContentType ContentType { get; set; } = ContentType.Body;

		[Required]
		[MaxLength(30)]
		public String Title { get; set; } = string.Empty;

		[Required]
		[MaxLength(30)]
		public String Name { get; set; } = string.Empty;

		public String? ContentHtml { get; set; } = string.Empty;




	}
}
