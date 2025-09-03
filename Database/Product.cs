using System.ComponentModel.DataAnnotations;

namespace IdentityEfApi.Database
{
    public class Product
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(120)]
        public string Name { get; set; } = string.Empty;

        [Range(0, 999999)]
        public decimal Price { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }
    }
}
