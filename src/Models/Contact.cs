using System.ComponentModel.DataAnnotations;

namespace Miniblog.Core.Models
{
    public class Contact
    {
        [Required(AllowEmptyStrings = false)]
        public string Name { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required(AllowEmptyStrings = false)]
        public string Message { get; set; } = string.Empty;

        [Required(AllowEmptyStrings = false)]
        public string CaptchaToken { get; set; } = string.Empty;
    }
}
