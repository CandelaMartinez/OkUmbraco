using DataAnnotationsExtensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OkUmbraco.Core.ViewModel
{
    //view model for the registration page
    public class RegisterViewModel
    {
        [DisplayName("First Name")]
        [Required(ErrorMessage = "please enter your name")]
        public string FirstName { get; set; }

        [DisplayName("Last Name")]
        [Required(ErrorMessage = "please enter your last name")]
        public string LastName { get; set; }

        [DisplayName("User Name")]
        [Required(ErrorMessage = "please enter your username")]
        [MinLength(6)]
        public string UserName { get; set; }

        [DisplayName("Email")]
        [Required(ErrorMessage = "please enter your email")]
        public string EmailAddress { get; set; }

        [UIHint("Password")]
        [DisplayName("Password")]
        [Required(ErrorMessage = "please enter your password")]
        //check webConfig to match the minimal lengh requerid in membership provider
        [MinLength(10, ErrorMessage = "al least 10 characters")]
        public string Password { get; set; }

        [UIHint("Confirm Password")]
        [DisplayName("Confirm Password")]
        [Required(ErrorMessage = "please enter your password confirmation")]
        //install package data anottation extensions
        [EqualTo("Password", ErrorMessage ="ensure your passwords match")]
        public string ConfirmPassword { get; set; }
    }
}
