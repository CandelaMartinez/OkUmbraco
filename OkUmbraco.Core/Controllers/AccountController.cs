using OkUmbraco.Core.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Web.Security;
using Umbraco.Web.Mvc;

namespace OkUmbraco.Core.Controllers
{
   
    public class AccountController: SurfaceController
    {
        public const string PARTIAL_VIEW_FOLDER = "~/Views/Partials/MyAccount/";
        public ActionResult RenderMyAccount()
        {
            var vm = new AccountViewModel();
            //I want to render some data and pre populate the form with this data

            //is loggin?
            if (!Umbraco.MemberIsLoggedOn())
            {
                ModelState.AddModelError("Error", "You are not logged in.");
                return CurrentUmbracoPage();
            }

            //get members details
            var member = Services.MemberService.GetByUsername(Membership.GetUser().UserName);

            if (member == null)
            {
                ModelState.AddModelError("Error", "could not find you");
                return CurrentUmbracoPage();
            }

            //populate the view model
            vm.Name = member.Name;
            vm.Email = member.Email;
            vm.Username = member.Username;

            //return the partial view

            return PartialView(PARTIAL_VIEW_FOLDER + "MyAccount.cshtml", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult HandleUpdateDetails(AccountViewModel vm)
        {
            //is model valid?
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("Error", "Problem!");
                return CurrentUmbracoPage();
            }

            //is there a member? is them logged in?
            if (!Umbraco.MemberIsLoggedOn() || Membership.GetUser() == null)
            {
                ModelState.AddModelError("Error", "You are not logged on");
                return CurrentUmbracoPage();
            }

            //get the member
            var member = Services.MemberService.GetByUsername(Membership.GetUser().UserName);

            if (member == null)
            {
                ModelState.AddModelError("Error", "You are not logged on.");
                return CurrentUmbracoPage();
            }
            //yes: update details
            member.Name = vm.Name;
            member.Email = vm.Email;

            Services.MemberService.Save(member);

            //thanks
            TempData["status"] = "Updated Details";
            return RedirectToCurrentUmbracoPage();

            
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult HandlePasswordChange(AccountViewModel vm)
        {
            //Model valid?
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("Error", "Problem");
                return CurrentUmbracoPage();
            }

            //member?
            if (!Umbraco.MemberIsLoggedOn() || Membership.GetUser() == null)
            {
                ModelState.AddModelError("Error", "Not logged in");
                return CurrentUmbracoPage();
            }
            //Update the password who is logged in
            var member = Services.MemberService.GetByUsername(Membership.GetUser().UserName);

            if (member == null)
            {
                ModelState.AddModelError("Error", "You are not logged in");
                return CurrentUmbracoPage();
            }
            try
            {
                Services.MemberService.SavePassword(member, vm.Password);

            }catch (MembershipPasswordException e)
            {
                ModelState.AddModelError("Error", "Problem with your password " + e.Message);
                return CurrentUmbracoPage();
            }

            //Thanks
            TempData["status"] = "Updated Password";
            return RedirectToCurrentUmbracoPage();
        }

    }
}
