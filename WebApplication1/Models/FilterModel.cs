using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using phiClustCore;
namespace WebApplication1.Models
{
    public class FilterModel
    {
        public  FilterOmics filter;
        public  Dictionary<string, Type> types;
        public  Dictionary<string, string> values = new Dictionary<string, string>();
        public  static List<string> coding;

        static FilterModel()
        {
            coding = new List<string>();
            foreach(var item in Enum.GetValues(typeof(CodingAlg)))
            {
                coding.Add(item.ToString());
            }
        }   
      
    }
}