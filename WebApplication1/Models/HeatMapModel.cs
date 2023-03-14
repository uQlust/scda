using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using phiClustCore;

namespace WebApplication1.Models
{
    public class HeatMapModel
    {
        [Display(Name = "Required number of column clusters")]
        public int reqColumnClusters { get; set; }
        [Display(Name = "Required number of row clusters")]
        public int reqRowClusters { get; set; }
        [Display(Name = "Distance measure")]
        public DistanceMeasures dist { get; set; }
        [Display(Name = "Cluster algorithm")]
        public ClusterAlgorithm cluster { get; set; }
    }
}