using BedBrigade.Common.Logic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BedBrigade.Common.Models
{
	[Table("Donations")]
	public class Donation : BaseEntity, ILocationId
    {
		[Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Int32 DonationId { get; set; }

		[ForeignKey("LocationId"), Required]
		public Int32 LocationId { get; set; }

        [Required(ErrorMessage = "Email Address is required")]
        [MaxLength(255)]
        public String? Email { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public Decimal Amount { get; set; }

		[MaxLength(255)]
		public String? TransactionId { get; set; } = string.Empty;

		[MaxLength(20)]
		public String? FirstName { get; set; } = string.Empty;

        [MaxLength(25)]
		public String? LastName { get; set; } = string.Empty;

		[Required]
		public DateTime? DonationDate { get; set; }

        [Required]
		public Boolean TaxFormSent { get; set; }

		[NotMapped]
		public string FullName 
		{ get
			{
				return $"{FirstName} {LastName}";
			}
		}

        [NotMapped]
        public string NameAndDate
        {
			get
            {
                return $"{FullName} - {DonationDate?.ToString("yyyy-MM-dd")}";
            }
        }
	}
}
