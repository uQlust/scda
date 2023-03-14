using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    public class ButtonController : Controller
    {
        static List<ButtonModel> buttons = new List<ButtonModel>();
        // GET: Button
        public ActionResult Index()
        {
            buttons.Add(new ButtonModel());
            return View("Index",buttons);
        }

        public ActionResult ButtonClick(string name)
        {
            return RedirectToAction("Index", "List", new { Id = 5 });
            //return RedirectToAction("Contact","Home",new {Id = 5 });
            //return View(new HomeController(buttons[0]));
        }

    }
}
