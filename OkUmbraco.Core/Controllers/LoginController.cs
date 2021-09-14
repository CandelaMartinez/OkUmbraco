using OkUmbraco.Core.Interfaces;
using OkUmbraco.Core.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using Umbraco.Web.Mvc;
using Umbraco.Core.Logging;

namespace OkUmbraco.Core.Controllers
{
    public class LoginController : SurfaceController
    {
        public const string PARTIAL_VIEW_FOLDER = "~/Views/Partials/Login/";

        //email service dependency injection
        private IEmailService _emailService;

        public LoginController (IEmailService emailService){
            _emailService = emailService;

    }


        #region Login
        //action result for render action
        public ActionResult RenderLogin()
        {
            var vm = new LoginViewModel();
            vm.RedirectUrl = HttpContext.Request.Url.AbsolutePath;
            return PartialView(PARTIAL_VIEW_FOLDER + "Login.cshtml", vm);
        }

        //handle the post: cuando envio el formulario login lleno para que se valide
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult HandleLogin(LoginViewModel vm)
        {
            //check if the model is ok
            if (!ModelState.IsValid)
            {
                return CurrentUmbracoPage();
            }

            //check if the member exists
            var member = Services.MemberService.GetByUsername(vm.Username);
            if (member == null)
            {
                ModelState.AddModelError("Login", "Cannot find that username");
                return CurrentUmbracoPage();
            }

            //check if the member is locked out: asp.net membership provider
            //webConfig/membership provider: maxInvalidPasswordAttempts="5"
            if (member.IsLockedOut)
            {
                ModelState.AddModelError("Login", "The account is locked, reset");
                return CurrentUmbracoPage();
            }

            //check if have validated email address: emailVerified added in the details of the member: true when the member verified the mail
            var emailVerified = member.GetValue<bool>("emailVerified");
            
            //the member havent click the link with the token and verified the email
            if (!emailVerified)
            {
                ModelState.AddModelError("Login", "Please verify your email");
                return CurrentUmbracoPage();
            }


            //check credentials
            //log them in
            if (!Members.Login(vm.Username, vm.Password))
            {
                ModelState.AddModelError("Login", "The username/password is not correct");
                return CurrentUmbracoPage();
            }

            if (!string.IsNullOrEmpty(vm.RedirectUrl))
            {
                return Redirect(vm.RedirectUrl);
            }
            return RedirectToCurrentUmbracoPage();
        }
        #endregion


        #region Forgotten Password
        public ActionResult RenderForgottenPassword()
        {
            var vm = new ForgottenPasswordViewModel();

            return PartialView(PARTIAL_VIEW_FOLDER + "ForgottenPassword.cshtml", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult HandleForgottenPassword(ForgottenPasswordViewModel vm)
        {
            //model ok?
            if (!ModelState.IsValid)
            {
                return CurrentUmbracoPage();
            }

            //is there a member with this email? surface controller helps me
            //return IMember: allow me to do the updates in the member
            var member = Services.MemberService.GetByEmail(vm.EmailAddress);
            if (member == null)
            {
                ModelState.AddModelError("Error", "Sorry we can't find that email ");
                return CurrentUmbracoPage();
            }


            //create the reset token
            var resetToken = Guid.NewGuid().ToString();

            //set the reset expiry date: now + 12 hours
            var expiryDate = DateTime.Now.AddHours(12);

            //save to member
            member.SetValue("resetExpiryDate", expiryDate);
            member.SetValue("resetLinkToken", resetToken);
            Services.MemberService.Save(member);

            //fire off the email reset password link, call the method from the core
            _emailService.SendResetPasswordNotification(vm.EmailAddress, resetToken);

            Logger.Info<LoginController>($"Sent a password reset to {vm.EmailAddress}");

           //set the tempdate status
            TempData["status"] = "OK";

            return RedirectToCurrentUmbracoPage();

           
        
        }

        #endregion


        #region Reset Password

        public ActionResult RenderResetPassword()
        {
            //return a partial view from the partial view folder/ login folder: called ResetPassword
            var vm = new ResetPasswordViewModel();
            return PartialView(PARTIAL_VIEW_FOLDER + "ResetPassword.cshtml", vm);
        }

        //acept the reset pass view model
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult HandleResetPassword(ResetPasswordViewModel vm)
        {
            //check if the viewmodel according con the data anotation is ok
            if (!ModelState.IsValid)
            {
                return CurrentUmbracoPage();
            }

            //get the reset token
            var token = Request.QueryString["token"];

            if (string.IsNullOrEmpty(token))
            {
                Logger.Warn<LoginController>("No token found");
                ModelState.AddModelError("Error", "Invalid Token");
                return CurrentUmbracoPage();
            }


            //get the member for the token. use member service that return an IMember and can update
            var member = Services.MemberService.GetMembersByPropertyValue("resetLinkToken", token).SingleOrDefault();
           
            if (member == null)
            {
                ModelState.AddModelError("Error", "Link no longer valid");
                return CurrentUmbracoPage();
            }

            //check the time window hasnt expired
            var membersTokenExpiryDate = member.GetValue<DateTime>("resetExpiryDate");
            var currentTime = DateTime.Now;
            
            if (currentTime.CompareTo(membersTokenExpiryDate) >= 0)
            {
                ModelState.AddModelError("Error", "the link has expired, use the Forgotten Password page to generate a new one");
                return CurrentUmbracoPage();
            }


            //if ok, update pass for the member
            //change web.config: allowManuallyChangingPassword=true
            Services.MemberService.SavePassword(member, vm.Password);

            member.SetValue("resetLinkToken", string.Empty);
            member.SetValue("resetExpiryDate", null);
            member.IsLockedOut = false;

            Services.MemberService.Save(member);

            //Send out the email notification that the password changed
            _emailService.SendPasswordChangedNotification(member.Email);


            //thanks
            TempData["status"] = "OK";
            Logger.Info<LoginController>($"User {member.Username} has changed their password.");

            return RedirectToCurrentUmbracoPage();

          

        }

        #endregion
    }

}
