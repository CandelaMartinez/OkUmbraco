using Umbraco.Core.Logging;
using OkUmbraco.Core.Interfaces;
using OkUmbraco.Core.ViewModel;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Umbraco.Core.Events;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Core.Services;
using Umbraco.Web;
using System.Web;

namespace OkUmbraco.Core.Services
{

    //the home to all outbound emails from my site
    public class EmailService : IEmailService
    {
        //inject Umbraco using the dependency injection framework
        private UmbracoHelper _umbraco;

        //inject content service so I can use it in sent email 
        private IContentService _contentService;

        //inject the logger: using Umbraco.Core.Logging;
        private ILogger _logger;


        //constructor where I inject the helper
        public EmailService(UmbracoHelper umbraco, IContentService contentService, ILogger logger)
        {
            _umbraco = umbraco;
            _contentService = contentService;
            _logger = logger;
        }

        //sending of a email to admin when a new contact form comes in
        public void SendContactNotificationToAdmin(ContactFormViewModel vm)
        {
            //get email template from Umbraco for the notification
            var emailTemplate = GetEmailTemplate("New Contact Form Notification");

            if(emailTemplate == null)
            {
                throw new Exception("Template not found");
            }

            //get the template data: email template
            var subject = emailTemplate.Value<string>("emailTemplateSubjectLine");
            var htmlContent = emailTemplate.Value<string>("emailTemplateHtmlContent");
            var textContent = emailTemplate.Value<string>("emailTemplateTextContent");

            //mail merge, string replace of the #fields from the template with the data
            //##name##
            MailMerge("name", vm.Name, ref htmlContent, ref textContent);

            //##email##
            MailMerge("email", vm.EmailAddress, ref htmlContent, ref textContent);

            //##comment##
            MailMerge("comment", vm.Comment, ref htmlContent, ref textContent);
            

            //send email out to whoever
            //obtener los datos del mail del site settings
            var siteSettings = _umbraco.ContentAtRoot().DescendantsOrSelfOfType("siteSettings").FirstOrDefault();

            if (siteSettings == null)
            {
                throw new Exception("no hay site settings");
            }

            //get  to addresses
          
            var toAddresses = siteSettings.Value<string>("emailSettingsAdminAccounts");

          

            if (string.IsNullOrEmpty(toAddresses))
            {
                throw new Exception("necesito una to address");
            }

            SendMail(toAddresses,subject,htmlContent, textContent);

       
           
        }

        //generic send method that logs the mail in umbraco and sends 
        private void SendMail(string toAddresses, string subject, string htmlContent, string textContent)
        {
            //obtener los datos del mail del site settings
            var siteSettings = _umbraco.ContentAtRoot().DescendantsOrSelfOfType("siteSettings").FirstOrDefault();

            if (siteSettings == null)
            {
                throw new Exception("no hay site settings");
            }

            var fromAddress = siteSettings.Value<string>("emailSettingsFromAddress");

            if (string.IsNullOrEmpty(fromAddress))
            {
                throw new Exception("necesito una from address");
            }

            //debug Mode
            var debugMode = siteSettings.Value<bool>("testMode");
            var testEmailAccounts = siteSettings.Value<string>("emailTestAccounts");

            var recipients = toAddresses;

            if (debugMode)
            {
                //change the to address to be the one on testEmailAccounts
                recipients = testEmailAccounts;

                //alter the subject line to show that are in test mode
                subject += "(TEST MODE) - " + toAddresses;
            }


            //log the email in umbraco
            //create a Email document type under Emails(name, parent, document type)

            var emails = _umbraco.ContentAtRoot().DescendantsOrSelfOfType("emails").FirstOrDefault();
            var newEmail = _contentService.Create(toAddresses, emails.Id, "Email");
            //mapping values of email document type
            newEmail.SetValue("emailSubject", subject);
            newEmail.SetValue("emailToAddress", recipients);
            newEmail.SetValue("emailHtmlContent", htmlContent);
            newEmail.SetValue("emailTextContent", textContent);
            newEmail.SetValue("emailSent", false);

            _contentService.SaveAndPublish(newEmail);

            //-----------------------------------------creation of the mail message
            //send email via smtp
            //the email will choose if the content is text or html
            var mimeType = new System.Net.Mime.ContentType("text/html");
            var alternateView = AlternateView.CreateAlternateViewFromString(htmlContent, mimeType);


            var smtpMessage = new MailMessage();
            smtpMessage.AlternateViews.Add(alternateView);

            //to, collection or one mail
            //recipient is a coma delimited string, split
            foreach (var recipient in recipients.Split(','))
            {
                if (!string.IsNullOrEmpty(recipient))
                {
                    smtpMessage.To.Add(recipient);
                }
            }
            //from
            smtpMessage.From = new MailAddress(fromAddress);

            //subject
            smtpMessage.Subject = subject;

            //body
            smtpMessage.Body = textContent;


            //-----------------------------------------sending
            using (var smtp = new SmtpClient())
            {
                try
                {
                    smtp.Send(smtpMessage);
                    //if email was sent I have to change the reference from not sent to sent
                    newEmail.SetValue("emailSent", true);
                    newEmail.SetValue("emailSentDate", DateTime.Now);
                    _contentService.SaveAndPublish(newEmail);


                }
                catch (Exception e)
                {
                    //log the error
                    _logger.Error<EmailService>("Problem sending email", e);
                    throw e;
                }
            }


        }
   // }


        //send email verification link to the new member
        public void SendVerifyEmailAddressNotification(string membersEmail, string verificationtoken)
        {
            //get the template: create a new template in umbraco: Verify Email
            var emailTemplate = GetEmailTemplate("Verify Email");
            if (emailTemplate == null)
            {
                throw new Exception("Template not found");
            }

            //get the fields from template
            //Get the template data
            var subject = emailTemplate.Value<string>("emailTemplateSubjectLine");
            var htmlContent = emailTemplate.Value<string>("emailTemplateHtmlContent");
            var textContent = emailTemplate.Value<string>("emailTemplateTextContent");

            //mail merge ##verify-url##
            //Build the url to be the absolute url to the verify page
            var url = HttpContext.Current.Request.Url.AbsoluteUri.Replace(HttpContext.Current.Request.Url.AbsolutePath, string.Empty);
            url += $"/verify?token={verificationtoken}";

            MailMerge("verify-url", url, ref htmlContent, ref textContent);

            SendMail(membersEmail, subject, htmlContent, textContent);

        }

        private void MailMerge(string token, string value, ref string htmlContent, ref string textContent)
        {
            htmlContent = htmlContent.Replace($"##{token}##", value);
            textContent = textContent.Replace($"##{token}##", value);
        }

        //return the email template as IPublishedContent where the title matches the template name
        private IPublishedContent GetEmailTemplate(string templateName)
        {
            //use Umbraco Helper that I injected in the constructor
            var template = _umbraco.ContentAtRoot()
                            .DescendantsOrSelfOfType("emailTemplate")
                            .Where(w => w.Name == templateName)
                            .FirstOrDefault();

            return template;
        }
        //send the reset password link to the user
        public void SendResetPasswordNotification(string membersEmail, string resetToken)
        {
            //get the template
            //get the template: create a new template in umbraco: Reset Password email templates
            var emailTemplate = GetEmailTemplate("Reset Password");
            if (emailTemplate == null)
            {
                throw new Exception("Template not found");
            }

            //get the data
            //get the fields from template
            //Get the template data
            var subject = emailTemplate.Value<string>("emailTemplateSubjectLine");
            var htmlContent = emailTemplate.Value<string>("emailTemplateHtmlContent");
            var textContent = emailTemplate.Value<string>("emailTemplateTextContent");

            //merge mail
            //mail merge ##verify-url##
            //Build the url to be the absolute url to the verify page
            var url = HttpContext.Current.Request.Url.AbsoluteUri.Replace(HttpContext.Current.Request.Url.AbsolutePath, string.Empty);
            url += $"/reset-password?token={resetToken}";

            //in mail template the hiperlink was called: ##reset-url##
            MailMerge("reset-url", url, ref htmlContent, ref textContent);

            //send
            SendMail(membersEmail, subject, htmlContent, textContent);



        }

        //send a note to the user telling him that the pass was changed
        public void SendPasswordChangedNotification(string membersEmail)
        {
            //get  template
            var emailTemplate = GetEmailTemplate("Password Changed");
            if (emailTemplate == null)
            {
                throw new Exception("Template not found");
            }

            //get data from the template
            var subject = emailTemplate.Value<string>("emailTemplateSubjectLine");
            var htmlContent = emailTemplate.Value<string>("emailTemplateHtmlContent");
            var textContent = emailTemplate.Value<string>("emailTemplateTextContent");

            //send
            SendMail(membersEmail, subject, htmlContent, textContent);
        }
    }
}
