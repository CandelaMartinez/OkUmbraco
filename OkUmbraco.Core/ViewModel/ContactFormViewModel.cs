using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OkUmbraco.Core.ViewModel
{
    public class ContactFormViewModel
    {
        [Required]
        [MaxLength(80, ErrorMessage ="intenta limitarte a 80 caracteres")]
        public string Name { get; set; }

        [Required]
        [EmailAddress(ErrorMessage ="agrega una direccion valida")]
        public string EmailAddress { get; set; }

        [Required]
        [MaxLength(500, ErrorMessage ="limita tu comentario a 500 caracteres")]
        public string Comment { get; set; }

        [MaxLength(255, ErrorMessage = "intenta limitarte a 80 caracteres")]
        public string Subject { get; set; }
        
    }
}
