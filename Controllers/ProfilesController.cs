using LimeChat.Data;
using LimeChat.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Security.Cryptography;
//using Microsoft.Build.Framework.Profiler;
using System.Linq.Expressions;
//using Humanizer;
using System.Text.RegularExpressions;

namespace LimeChat.Controllers
{
    public class ProfilesController : Controller
    {
        private readonly ApplicationDbContext db;

        private readonly UserManager<ApplicationUser> _userManager;

        private readonly RoleManager<IdentityRole> _roleManager;

        public ProfilesController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager
            )
        {
            db = context;

            _userManager = userManager;

            _roleManager = roleManager;
        }


        [Authorize(Roles = "User,Admin")]
        public IActionResult Index()
        {

            string uid = _userManager.GetUserId(User);
            var prof = db.Profiles.Where(p => p.UserId == uid);
            ViewBag.profil = prof;
            ViewBag.myProfile = true;
            if (prof.Count() == 0)
            {
                return RedirectToAction("New");
            }
            else
            {
                int pid = prof.FirstOrDefault().ProfileId;   // Current user -> profile id
                return RedirectToAction("Show", new { id = pid });
            }
        }
        public IActionResult SearchBar()
        {

            ViewBag.cautare = false;
            //var profiles = db.Profiles.Include("User").OrderBy(a => a.ProfileName);
            var search = "";
            // MOTOR DE CAUTARE
            if (Convert.ToString(HttpContext.Request.Query["search"]) != null)
            {
                ViewBag.cautare = true;
                var profiles = db.Profiles.Include("User").OrderBy(a => a.ProfileName);
                // eliminam spatiile libere
                search = Convert.ToString(HttpContext.Request.Query["search"]).Trim();

                // Cautare in articol (Title si Content)
                List<int> profileIds = db.Profiles.Where
                (
                at => at.ProfileName.Contains(search)
                || at.ProfileUsername.Contains(search)
                ).Select(a => a.ProfileId).ToList();

                // Lista articolelor care contin cuvantul cautat
                // fie in articol -> Title si Content
                // fie in comentarii -> Content
                profiles = db.Profiles.Where(prof => profileIds.Contains(prof.ProfileId))
                .Include("User")
                .OrderBy(a => a.ProfileName);
                ViewBag.profile = profiles;
            }
            ViewBag.SearchString = search;

            //ViewBag.profile = profiles;
            // AFISARE PAGINATA


            if (search != "")
            {
                ViewBag.PaginationBaseUrl = "/Profiles/Show/?search=" + search + "&page";
            }
            else
            {
                ViewBag.PaginationBaseUrl = "/Profiles/Show/?page";
            }

            return View();

        }



        [Authorize(Roles = "User,Admin")]
        public IActionResult New()
        {
            Profile profile = new Profile();

            return View(profile);
        }
        // Se adauga articolul in baza de date
        // Doar utilizatorii cu rolul de Editor sau Admin pot adauga articole in platforma

        [Authorize(Roles = "User,Admin")]
        [HttpPost]
        public IActionResult New(Profile profile)
        {
            profile.UserId = _userManager.GetUserId(User);

            if (ModelState.IsValid)
            {
                db.Profiles.Add(profile);
                db.SaveChanges();
                TempData["message"] = "Profilul a fost adaugat";
                return RedirectToAction("Index");
            }
            else
            {
                return View(profile);
            }
        }


        [Authorize(Roles = "User,Admin")]
        public IActionResult Show(int id)
        {
            

                Profile profile = db.Profiles
                                                 .Where(p => p.ProfileId == id)
                                                 .First();
                if(profile.ProfilePublic==true){ ViewBag.Public=true;}
                else { ViewBag.Public=false;}
            var user1 = db.Profiles
                    .Where(p => p.UserId == _userManager.GetUserId(User)).First();
                ApplicationUser u1 = db.ApplicationUsers.Where(p => p.Profile == user1).First();
                ApplicationUser u2 = db.ApplicationUsers.Where(p => p.Profile == profile).First();
            ViewBag.prieteni = true;
            try
                {
                    Friend verificare = db.Friends.Where(p => (p.User1 == u1 && p.User2 == u2) || (p.User1 == u2 && p.User2 == u1)).First();
                    if(verificare.Accepted==false)
                    {
                        ViewBag.prieteni = false;
                    }
                }

                catch (InvalidOperationException)
                {
                    ViewBag.prieteni = false;
                    if (profile.UserId != _userManager.GetUserId(User))
                    {

                        ViewBag.buton = true;

                    }
                }
                SetAccessRights();

                var posts = db.Posts.Include("User").Where(p => p.UserId == profile.UserId);
                ViewBag.Posts = posts;

                if (TempData.ContainsKey("message"))
                {
                    ViewBag.Message = TempData["message"];
                }

                return View(profile);
            }
        
        [Authorize(Roles = "User,Admin")]
        public IActionResult Show2(string id)
        {


            Profile profile = db.Profiles
                                         .Where(p => p.UserId == id)
                                         .First();
            if (profile.ProfilePublic == true) { ViewBag.Public = true; }
            else { ViewBag.Public = false; }
            var user1 = db.Profiles
                .Where(p => p.UserId == _userManager.GetUserId(User)).First();
            ApplicationUser u1 = db.ApplicationUsers.Where(p => p.Profile == user1).First();
            ApplicationUser u2 = db.ApplicationUsers.Where(p => p.Profile == profile).First();
            ViewBag.prieteni = true;
            try
            {
                Friend verificare = db.Friends.Where(p => (p.User1 == u1 && p.User2 == u2) || (p.User1 == u2 && p.User2 == u1)).First();
                if (verificare.Accepted == false)
                {
                    ViewBag.prieteni = false;
                }
            }

            catch (InvalidOperationException)
            {
                ViewBag.prieteni = false;
                if (profile.UserId != _userManager.GetUserId(User))
                {

                    ViewBag.buton = true;

                }
            }
            SetAccessRights();

            var posts = db.Posts.Include("User").Where(p => p.UserId == id);
            ViewBag.Posts = posts;

            if (TempData.ContainsKey("message"))
            {
                ViewBag.Message = TempData["message"];
            }

            return View(profile);
        }
        [HttpPost]
        [Authorize(Roles = "Admin,User")]
        public ActionResult Delete(int id)
        {
            Profile profile = db.Profiles
                                         
                                         .Where(p => p.ProfileId == id)
                                         .First();
            var prietenii1 = db.Friends.Where(p => p.User1_Id == profile.UserId).ToList();
            var prietenii2 = db.Friends.Where(p => p.User2_Id == profile.UserId).ToList();
            foreach(var pr in prietenii1)
            { db.Friends.Remove(pr);
                db.SaveChanges();
            }
            foreach (var pr in prietenii2)
            { db.Friends.Remove(pr);
                db.SaveChanges();
            }
            
            if (profile.UserId == _userManager.GetUserId(User) || User.IsInRole("Admin"))
            {
                db.Profiles.Remove(profile);
                db.SaveChanges();
                TempData["message"] = "Profilul a fost sters";
                return RedirectToAction("New");
            }

            else
            {
                TempData["message"] = "Nu aveti dreptul sa stergeti un profil care nu va apartine";
                return RedirectToAction("Index");
            }
        }
         
        [Authorize(Roles = "Admin,User")]
        public IActionResult Edit(int id)
        {

            Profile profile = db.Profiles
                                        .Where(p => p.ProfileId == id)
                                        .First();


            if (profile.UserId == _userManager.GetUserId(User) || User.IsInRole("Admin"))
            {
                return View(profile);
            }

            else
            {
                TempData["message"] = "Nu aveti dreptul sa faceti modificari asupra unui profil care nu va apartine";
                return RedirectToAction("Index");
            }

        }
        // Se adauga articolul modificat in baza de date
        [HttpPost]
        [Authorize(Roles = "Admin,User")]
        public IActionResult Edit(int id, Profile requestProfile)
        {
            Profile profile = db.Profiles.Find(id);


            if (ModelState.IsValid)
            {
                if (profile.UserId == _userManager.GetUserId(User) || User.IsInRole("Admin"))
                {
                    profile.ProfileName = requestProfile.ProfileName;
                    profile.ProfileBio = requestProfile.ProfileBio;
                    profile.ProfileUsername = requestProfile.ProfileUsername;
                    profile.ProfilePublic = requestProfile.ProfilePublic;
                    //post.PostDate = requestPost.PostDate;
                    //trb si group id ulterior
                    TempData["message"] = "Profilul a fost modificat";
                    db.SaveChanges();
                    return RedirectToAction("Show", new { id = profile.ProfileId });
                }
                else
                {
                    TempData["message"] = "Nu aveti dreptul sa faceti modificari asupra unui profil care nu va apartine";
                    return RedirectToAction("Index");
                }
            }
            else
            {
                return View(requestProfile);
            }
        }

        public IActionResult AddFriend(string id)
        {
            if (id != _userManager.GetUserId(User))
            {
                var user2 = db.Profiles
                .Where(p => p.UserId == id).First();
                var user1 = db.Profiles
                    .Where(p => p.UserId == _userManager.GetUserId(User)).First();
                ApplicationUser u1 = db.ApplicationUsers.Where(p => p.Profile == user1).First();
                ApplicationUser u2 = db.ApplicationUsers.Where(p => p.Profile == user2).First();

                
                    Friend prietenie = new Friend();
                    prietenie.User1 = u1;
                    prietenie.User2 = u2;
                    prietenie.User1_Id = _userManager.GetUserId(User);
                    prietenie.User2_Id = id;
                    prietenie.RequestTime = DateTime.Now;
                    prietenie.Accepted = false;
                    db.Friends.Add(prietenie);
                    db.SaveChanges();
                    TempData["message"] = "Acum sunteti prieteni!";
                    }
             
            return RedirectToAction("Index");
        }

        public IActionResult All(string id)
        {
            var prietenii1 = db.Friends.Where(p => p.User1_Id == id)
                //.Where(p=>p.Accepted==true)
                .ToList();
            var prietenii2 = db.Friends.Where(p => p.User2_Id == id)
                //.Where(p => p.Accepted == true)
                .ToList();
            List<string> prieteni = new List<string>();
            foreach (var pr in prietenii1)
            {
                if (pr.Accepted == true)
                { prieteni.Add(pr.User2_Id); }
            }
            foreach (var pr in prietenii2)
            {
                if (pr.Accepted == true)
                { prieteni.Add(pr.User1_Id); }
            }
            var profil = db.Profiles.Where(p => prieteni.Contains(p.UserId)).ToList();
            
            ViewBag.Prietenie=profil;
            return View();
            
        }
        [HttpPost]
        public IActionResult DeleteFriendship(string id)
        {
            try {var prietenie = db.Friends.Where(p => p.User1_Id == id)
                .Where(p => p.User2_Id == _userManager.GetUserId(User)).First();
                db.Friends.Remove(prietenie);
                db.SaveChanges();
            }
            catch (InvalidOperationException)
            {
                var prietenie2 = db.Friends.Where(p => p.User1_Id == _userManager.GetUserId(User))
              .Where(p => p.User2_Id == id).First();
                db.Friends.Remove(prietenie2);
                db.SaveChanges(); 
            }



            return Redirect("/Profiles/All/"+ _userManager.GetUserId(User));
        }
        public IActionResult Request()
        {/*
            Friend prietenie=db.Friends.Where(p=>p.FriendId==id).First();
            ViewBag.Prietenie = prietenie;
            return View();*/
            
            var prietenii2 = db.Friends.Where(p => p.User2_Id == _userManager.GetUserId(User))
                .ToList();
            List<string> prieteni = new List<string>();
            
            foreach (var pr in prietenii2)
            {
                if (pr.Accepted == false)
                { prieteni.Add(pr.User1_Id); }
            }
            var profil = db.Profiles.Where(p => prieteni.Contains(p.UserId)).ToList();

            ViewBag.Prietenie = profil;
            return View();
        }
        [HttpPost]
        public IActionResult AcceptFr(string id)
        {
            var prietenie=db.Friends.Where(p=>p.User1_Id==id)
                .Where(p=>p.User2_Id==_userManager.GetUserId(User)).First();
            prietenie.Accepted=true;
            db.SaveChanges();
            return Redirect("/Profiles/All/"+_userManager.GetUserId(User));
        }
        [HttpPost]
        public IActionResult DeleteFr(string id)
        {
            var prietenie = db.Friends.Where(p => p.User1_Id == id)
                .Where(p => p.User2_Id == _userManager.GetUserId(User)).First();
            prietenie.Accepted = false;
            db.Friends.Remove(prietenie);
            db.SaveChanges();
            return RedirectToAction("Request");
        }

        private void SetAccessRights()
        {
            ViewBag.AfisareButoane = false;

            ViewBag.EsteAdmin = User.IsInRole("Admin");

            ViewBag.UserCurent = _userManager.GetUserId(User);

            if (ViewBag.UserCurent == _userManager.GetUserId(User))
            {
                ViewBag.AfisareButoane = true;
            }
        }

    }
}
