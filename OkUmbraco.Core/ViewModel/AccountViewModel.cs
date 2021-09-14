using DataAnnotationsExtensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OkUmbraco.Core.ViewModel
{//view model for the my account page
    public class AccountViewModel
    {

        [DisplayName("Full Name")]
        [Required(ErrorMessage = "Enter your name")]
        public string Name { get; set; }


        [DisplayName("Email")]
        [Required(ErrorMessage = "Enter your email address")]
        [Email(ErrorMessage = "Enter a valid email address")]
        public string Email { get; set; }


        public string Username { get; set; }

        [UIHint("Password")]
        [DisplayName("Password")]
        [MinLength(10, ErrorMessage = "Please make your password at least 10 characters")]

        public string Password { get; set; }
        [UIHint("Confirm Password")]
        [DisplayName("Confirm Password")]
        [EqualTo("Password", ErrorMessage = "Please ensure your passwords match")]

        public string ConfirmPassword { get; set; }
    }
}
