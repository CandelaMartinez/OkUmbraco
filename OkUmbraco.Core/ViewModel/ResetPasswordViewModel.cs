using DataAnnotationsExtensions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OkUmbraco.Core.ViewModel
{
    public class ResetPasswordViewModel
    {
        [UIHint ("Password")]
        [Required(ErrorMessage ="enter your pass")]
        [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d)(?=.*[$@$!%*#?&_])[A-Za-z\d$@$!%*#?&_]{10,}$", ErrorMessage ="enter at least 10 characters, one of the a special and a number")]
        public string Password { get; set; }

        [UIHint("Password")]
        [Required(ErrorMessage = "enter your pass")]
        [EqualTo("Password", ErrorMessage = "Please ensure you passwords match")]
        public string ConfirmPassword { get; set; }

    }
}
