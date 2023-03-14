using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using phiClustCore;

namespace WebApplication1.Models
{
    public class HeatMapController : Controller
    {
        HeatMapModel model = new HeatMapModel();
        // GET: HeatMap
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult SubmitModel(HeatMapModel modelF)
        {
            Settings set = new Settings();
            set.Load();
            set.mode = INPUTMODE.OMICS;
            Options opt = new Options();
            opt.dataDir.Clear();
            opt.profileFiles.Clear();
            opt.hash.relClusters = modelF.reqColumnClusters;
            opt.hash.reqClusters = modelF.reqRowClusters;
            opt.hash.useConsensusStates = true ;
            opt.hash.perData = 90;
            opt.hierarchical.distance = modelF.dist;
            opt.hierarchical.microCluster = modelF.cluster;
            opt.clusterAlgorithm.Clear();
            opt.clusterAlgorithm.Add(ClusterAlgorithm.OmicsHeatMap);
            JobManager manager = new JobManager();

            ListModel model = new ListModel();
            foreach(var item in model.fileNames)
            {
                OmicsDataSet oData = new OmicsDataSet();
                string fileName = Server.MapPath("~/UploadedFiles") + "\\" + item;
                oData.Load(fileName, new char[] { ' ', '\t' },"Column", 0, 0, 1, 1);
                model.omics.Add(oData);
            }
            OmicsDataSet final = OmicsDataSet.JoinOmicsData(model.omics);
            final.filters =new System.ComponentModel.BindingList<FilterOmics>(model.selectedFilters);
            manager.opt = opt;
            manager.RunJob("Test",final);
            manager.WaitAllNotFinished();
            return View();
        }
    }
}