using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Mvc;
using phiClustCore;

namespace WebApplication1.Models
{
    public class ListModel
    {
        
       public static List<FilterOmics> avFilters = new List<FilterOmics>();
       public List<FilterOmics> selectedFilters = new List<FilterOmics>();
       public List<OmicsDataSet> omics = new List<OmicsDataSet>();
        public ListModel()
        {
            if (avFilters.Count == 0)
            {
                Type[] xx = Assembly.GetAssembly(typeof(FilterOmics)).GetTypes();
                IEnumerable<Type> subclasses = xx.Where(t => t.IsSubclassOf(typeof(FilterOmics)));
                foreach (var item in subclasses)
                    avFilters.Add((FilterOmics)Activator.CreateInstance(item));
            }
            if (HttpContext.Current.Session != null && HttpContext.Current.Session["files"] != null)
            {
                fileNames = HttpContext.Current.Session["files"] as List<string>;
            }
            else
                fileNames = new List<string>();

            if (HttpContext.Current.Session != null && HttpContext.Current.Session["filters"] != null)
            {
                selectedFilters = HttpContext.Current.Session["filters"] as List<FilterOmics>;
            }
            else
                selectedFilters = new List<FilterOmics>();

            if (HttpContext.Current.Session != null && HttpContext.Current.Session["superGenes"] != null)
            {
                superGenesFile = HttpContext.Current.Session["superGenes"] as string;
            }
            else
                superGenesFile = "";


        }
        public HttpPostedFileBase val { get; set; }
        public List<string> selected { get; set; }
        public List<string> fileNames { get; set; }
        public List<SelectListItem> filtersForFiles { get; set; }
        public string superGenesFile { get; set; }
        public List<SelectListItem> filtersJoined { get; set; }        
    }
}