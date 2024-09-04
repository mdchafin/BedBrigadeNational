﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BedBrigade.Common.Enums;

namespace BedBrigade.Common.Models
{
    [Table("UserPersist")]
    public class UserPersist : BaseEntity
    {
        [Key, Column(Order = 0)]
        public String UserName { get; set; }

        [Key, Column(Order = 1)]
        public PersistGrid Grid { get; set; }

        [MaxLength(4000)]
        public string? Data { get; set; }
    }
}
