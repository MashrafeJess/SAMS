using System.ComponentModel.DataAnnotations;

namespace Business.DTO
{
    public class LoginDto
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// False: cookie dies with the browser session.
        /// True: cookie persists on disk until it expires.
        /// </summary>
        public bool RememberMe { get; set; }
    }
}
