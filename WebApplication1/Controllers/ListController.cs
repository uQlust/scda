using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebApplication1.Models;
using phiClustCore;
using System.Web.Routing;
using System.IO;

namespace WebApplication1.Controllers
{
    public class ListController : Controller
    {
        ListModel model = new ListModel();

        public ListController()
        {


           

        }

        // GET: list
        public ActionResult Index()
        {            
            return View("List",model);
        }
        
        [HttpPost]
        public ActionResult UploadFile(HttpPostedFileBase file)
        {
            if (Upload(file))
            {
                model.fileNames.Add(file.FileName);
                Session["files"] = model.fileNames;
            }        
            return View("List",model);
    }
        bool Upload(HttpPostedFileBase file)
        {
            try
            {

                if (file != null)
                {
                    string path = Path.Combine(Server.MapPath("~/UploadedFiles"), Path.GetFileName(file.FileName));
                    file.SaveAs(path);
                }
                else return false;
                ViewBag.FileStatus = "File uploaded successfully.";
            }
            catch (Exception)
            {

                ViewBag.FileStatus = "Error while file uploading.";
                return false;
            }

            return true;
        }
        [HttpPost]
        public ActionResult UploadSuperGenes(HttpPostedFileBase file)
        {
            if (Upload(file))
            {
                model.superGenesFile = file.FileName;
                Session["superGenes"] = model.superGenesFile;
            }
            return View("List", model);
        }



        [HttpPost]
        public ActionResult AddFile(HttpPostedFileBase file)
        {
           // OmicsDataSet data = new OmicsDataSet();
            //data.Load(m, new char[] { '\t', ' ' }, "Column", 1, 1, 2, 2);
            
            return View("List", model);
        }
        [HttpPost]
        public ActionResult Add(string m)
        {
            foreach (var item in ListModel.avFilters)
            {
                if (item.Name == m)
                {
                    model.selectedFilters.Add(item);
                }

            }
            Session["filters"] = model.selectedFilters;
            return View("List", model);
        }
        [HttpPost]
        public ActionResult ClearFiles()
        {
            model.omics.Clear();
            Session["files"] = null;
            return View("List", model);
        }
        [HttpPost]
        public ActionResult ClearSuperGenes()
        {
            model.superGenesFile = ""; ;
            Session["superGenes"] = null;
            return View("List", model);
        }

        public ActionResult RemoveSelection(string m)
        {
            List<FilterOmics> xx = new List<FilterOmics>(model.selectedFilters);
            foreach (var item in xx)
            {
                if (item.Name == m)
                {
                    model.selectedFilters.Remove(item);
                    break;
                }

            }
            Session["filters"] = model.selectedFilters;
            return View("List",model);
        }
        public ActionResult SetupSelection(string m)
        {
            Dictionary<string, Type> filterParams = null;
            foreach (var item in model.selectedFilters)
            {
                if (item.Name.Contains(m))
                {
                    Session["SelectedFilter"] = item;
                }

            }
            return View("List",model);
        }
            public string AddSelection(string m)
        {
            Dictionary<string, Type> filterParams = null;
            foreach (var item in ListModel.avFilters)
            {
                if (item.Name == m)
                {
                    FilterOmics om= (FilterOmics)item.Clone();
                    model.selectedFilters.Add(om);
                    filterParams = item.GetParameters();
                }

            }
            string res = "";
            if (filterParams != null)            
                foreach (var item in filterParams)
                {
                    res += "?" + item.Key + "=" + item.Value.ToString();
                }

            Session["filters"] = model.selectedFilters;
            Session["SelectedFilter"] = model.selectedFilters[model.selectedFilters.Count-1];
            return res;
            

        }
        [HttpGet]
        public ActionResult ChangeView(string filterName)
        {
            Dictionary<string, Type> filterParams = null;
            foreach(var item in ListModel.avFilters)
            {
                if (item.Name == filterName)
                {
                    filterParams=item.GetParameters();
                }

            }
            if (filterParams != null)
            {
                IDictionary<string, Type> dicR = new Dictionary<string, Type>();
                foreach(var item in filterParams)
                {
                    dicR.Add(item.Key, item.Value);
                }
                TempData["my"] = dicR;
                return RedirectToAction("Index", "Filter");
            }
            else
                return View("List", model);
        }
    }
}