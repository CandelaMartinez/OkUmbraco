using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using Umbraco.Web.Mvc;
using OkUmbraco.Core.ViewModel;
using OkUmbraco.Core.Interfaces;

namespace OkUmbraco.Core.Controllers
{
    //handle member registration
    public class RegisterController: SurfaceController
    {
        private const string PARTIAL_VIEW_FOLDER = "~/Views/Partials/";

        //inject emailservice throw the constructor
        private IEmailService _emailService;

        public RegisterController(IEmailService emailService)
        {
            _emailService = emailService;
        }

        #region Register Form
        //render the registration form
        public ActionResult RenderRegister()
        {
            var vm = new RegisterViewModel();
            return PartialView(PARTIAL_VIEW_FOLDER + "Register.cshtml", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]

        //handle the registration form post
        public ActionResult HandleRegister(RegisterViewModel vm)
        {
            //if form is not valid: return
            if (!ModelState.IsValid)
            {
                return CurrentUmbracoPage();
            }

            //check if there is already a member with that email address
            var existingMember = Services.MemberService.GetByEmail(vm.EmailAddress);

            if (existingMember != null)
            {
                ModelState.AddModelError("Account Error", "There is already a user with that email");
                return CurrentUmbracoPage();
            }

            //check if the username is already in use
            existingMember = Services.MemberService.GetByUsername(vm.UserName);

            if (existingMember != null)
            {
                ModelState.AddModelError("Account Error", "There is already a user with that username. Choose a different one");
                return CurrentUmbracoPage();
            }


            //create member in umbraco 
            var newMember = Services.MemberService
                                .CreateMember(vm.UserName, vm.EmailAddress, $"{vm.FirstName} {vm.LastName}", "Member");

            newMember.PasswordQuestion = "";
            newMember.RawPasswordAnswerValue = "";

            //I Need to save the member before you can set the password
            Services.MemberService.Save(newMember);
            Services.MemberService.SavePassword(newMember, vm.Password);

            //Assign role: Normal User, created in Umbraco
            Services.MemberService.AssignRole(newMember.Id, "Normal User");

            //create email verification token
            //Token creation
            var token = Guid.NewGuid().ToString();
            newMember.SetValue("emailVerifyToken", token);
            Services.MemberService.Save(newMember);


            //send email verification
            try
            {
                _emailService.SendVerifyEmailAddressNotification(newMember.Email, token);
            }catch(Exception e)
            {

            }

            //thank the user
            TempData["status"] = "OK";
            return RedirectToCurrentUmbracoPage();
        }
        #endregion

        #region Verification
        public ActionResult RenderEmailVerification(string token)
        {

            //Get token (querystring)
            var member = Services.MemberService.GetMembersByPropertyValue("emailVerifyToken", token).SingleOrDefault();

            if (member != null)
            {
                //If we find a member, verify
                var alreadyVerified = member.GetValue<bool>("emailVerified");
                if (alreadyVerified)
                {
                    ModelState.AddModelError("Verified", "You've already verified ");
                    return CurrentUmbracoPage();
                }
                //go and verify it if it is not verified
                member.SetValue("emailVerified", true);
                member.SetValue("emailVerifiedDate", DateTime.Now);
                Services.MemberService.Save(member);

                //Thank the user
                TempData["status"] = "OK";
                return PartialView(PARTIAL_VIEW_FOLDER + "EmailVerification.cshtml");
            }


            //Or a problem
            ModelState.AddModelError("Error", "some problem!");
            return PartialView(PARTIAL_VIEW_FOLDER + "EmailVerification.cshtml");
            
        }
        #endregion

    }

}



