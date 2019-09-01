using System.ComponentModel.DataAnnotations;

namespace Miniblog.Core.Models
{
    public class Contact
    {
        [Required]
        public string Name { get; set; }

        [Required]
        public string Email { get; set; }

        [Required]
        public string Message { get; set; }
    }
}
