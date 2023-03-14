using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    public class HomeController : Controller
    {
        List<string> cos = new List<string>();
        ButtonModel cc;
       
        public ActionResult Index(object o)
        {
           // ButtonModel b = (ButtonModel)o;
            cos.Add("ksdjksjd");
            cos.Add("kkkkkkkk");
            return View();
        }
        protected void openFileDialog(object sender, EventArgs e)
        {
            Console.WriteLine("OK");
        }
        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact(RouteValueDictionary b)
        {
            RouteValueDictionary c = b;
            
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}