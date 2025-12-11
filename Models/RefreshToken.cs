
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VentifyAPI.Models
{
    [Table("refreshtokens")]
    public class RefreshToken
    {
        [Key]
	    [Column("Id")]
        public int Id { get; set; }

        [Required]
        [Column("Token")]
        public string Token { get; set; } = null!;

        [Required]
        [Column("UsuarioId")]
        public int UsuarioId { get; set; }

        [Column("ExpiresAt")]
        public DateTime? ExpiresAt { get; set; }

        [Column("Revoked")]
        public bool Revoked { get; set; }

        [ForeignKey("UsuarioId")]
        public Usuario? Usuario { get; set; }
    }
}
