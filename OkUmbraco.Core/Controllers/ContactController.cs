using OkUmbraco.Core.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using Umbraco.Web;
using Umbraco.Web.Mvc;
using Umbraco.Core.Logging;
using System.Net.Mail;
using OkUmbraco.Core.Interfaces;

namespace OkUmbraco.Core.Controllers
{
    //operaciones del contact form
    public class ContactController: SurfaceController
    {
        //to use the email service
        private IEmailService _emailService;

        //constructor where I inject the email service
        public ContactController(IEmailService emailService)
        {
            _emailService = emailService;
        }

        //render el contact form
        public ActionResult RenderContactForm()
        {
            var vm = new ContactFormViewModel();
            return PartialView("~/Views/Partials/Contact Form.cshtml", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult HandleContactForm(ContactFormViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("Error", "Por favor verifique los datos que le pide el formulario");
                return CurrentUmbracoPage();
            }

            try
            {
                //entrar a mi contact form
                var contactForms = Umbraco.ContentAtRoot().DescendantsOrSelfOfType("contactForms").FirstOrDefault();

                if (contactForms != null)
                {
                    //crear un nuevo contact form en umbraco y agrego los valores
                    var newContact = Services.ContentService.Create("Contact", contactForms.Id, "contactForm");
                    newContact.SetValue("contactName", vm.Name);
                    newContact.SetValue("contactEmail", vm.EmailAddress);
                    newContact.SetValue("contactSubject", vm.Subject);
                    newContact.SetValue("contactComments", vm.Comment);
                    //save and publish el nuevo contacto
                    Services.ContentService.SaveAndPublish(newContact);
                }



                //enviar un mail to the site admin 
                //SendContactFormReceivedEmail(vm);
                //clase 77: email service
                _emailService.SendContactNotificationToAdmin(vm);

                //return confirmation message to the user
                TempData["status"] = "OK";

                return RedirectToCurrentUmbracoPage();
            }
            catch(Exception e)
            {
                Logger.Error<ContactController>("hubo un error en el contact form submission", e.Message);
                ModelState.AddModelError("Error", "Perdon hubo un error, podrias volver intentarlo mas tarde?");
            }

            //si no salieron bien las cosas
            return CurrentUmbracoPage();

           
        }
        //metodo que hace el trabajo de enviar el mail al admin avisando que una form ha sido submited
        private void SendContactFormReceivedEmail(ContactFormViewModel vm)
        {
            //obtener los datos del mail del site settings
            var siteSettings = Umbraco.ContentAtRoot().DescendantsOrSelfOfType("siteSettings").FirstOrDefault();

            if(siteSettings == null)
            {
                throw new Exception("no hay site settings");
            }

            //leer el from y to addresses
            var fromAddress = siteSettings.Value<string>("emailSettingsFromAddress");
            var toAddresses = siteSettings.Value<string>("emailSettingsAdminAccounts");

            if (string.IsNullOrEmpty(fromAddress))
            {
                throw new Exception("necesito una from address");
            }

            if (string.IsNullOrEmpty(toAddresses))
            {
                throw new Exception("necesito una to address");
            }

            //construir el mail
            var emailSubject = "ha habido un nuevo contact form";
            var emailBody = $"un nuevo contact form ha sido recibido de {vm.Name}. El comentario es: {vm.Comment}";
            var smtpMessage = new MailMessage();
            smtpMessage.Subject = emailSubject;
            smtpMessage.Body = emailBody;
            smtpMessage.From = new MailAddress (fromAddress);

            var toList = toAddresses.Split(',');
            foreach(var item in toList)
            {
                if(!string.IsNullOrEmpty(item))
                smtpMessage.To.Add(item);
            }
            

            //enviar el mail 
            using (var smtp = new SmtpClient())
            {
                smtp.Send(smtpMessage);
            }


        }
    }
}
