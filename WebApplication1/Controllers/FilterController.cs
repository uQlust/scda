using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebApplication1.Models;
using phiClustCore;
using System.Web.Routing;

namespace WebApplication1.Controllers
{
    public class FilterController : Controller
    {
        FilterModel model = new FilterModel();
        public FilterController()
        {
                if (System.Web.HttpContext.Current.Session != null && System.Web.HttpContext.Current.Session["SelectedFilter"] != null)
                {
                    model.filter = System.Web.HttpContext.Current.Session["SelectedFilter"] as FilterOmics;
                    model.values = model.filter.GetParametersValue();
                    model.types = model.filter.GetParameters();
                }

        }
        // GET: Filter
        [HttpGet]
        public ActionResult Index(RouteValueDictionary rdic)
        {            
            return View("Filter", model);
        }
        public ActionResult SaveParameters(string s)
        {
            Dictionary<string, string> dic = new Dictionary<string, string>();

            string[] x = s.Split(';');
            for(int i=0;i<x.Length-1;i++)
            {
                string[] aux = x[i].Split('=');                
                dic.Add(aux[0], aux[1]);
            }

            model.filter.SetParameters(dic);
            return View("Filter",model);
        }
    }
}