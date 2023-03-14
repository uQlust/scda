using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using phiClustCore.Interface;
using System.Data;
using phiClustCore.Distance;

namespace phiClustCore
{
    public class FastRPart : IProgressBar
    {
        double startProgress = 0;
        double endProgress = 1;
        int maxV = 100;
        protected int currentV = 0;

        Dictionary<string,List<double>> data;

        DistanceMeasure distance = null;

        public double StartProgress { set { startProgress = value; } get { return startProgress; } }
        public double EndProgress { set { endProgress = value; } get { return endProgress; } }

        public FastRPart(Dictionary<string,List<double>> data)
        {
            this.data = data;/* new Dictionary<string, List<double>>();
            foreach (var item in data)
            {
                List<double> dat = new List<double>();
                foreach (var it in item.Value)
                {
                    switch (it)
                    {
                        case 0:
                            dat.Add(1); dat.Add(0); dat.Add(0);
                            break;
                        case 1:
                            dat.Add(0); dat.Add(1); dat.Add(0);
                            break;
                        case -1:
                        case 2:
                            dat.Add(0); dat.Add(0); dat.Add(1);
                            break;


                    }
                }
                this.data[item.Key] = dat;
            }*/

            //distance = new FastDiscreteDist(1, this.data);
            distance = new Fast3States(1, this.data);
            //distance = new Euclidian(data,true);
        }

        public double ProgressUpdate()
        {
            double res = 0;
            return StartProgress + (EndProgress - StartProgress) * (res * 0.45 + 0.55 * (double)currentV / maxV);
        }
        public Exception GetException()
        {
            return null;
        }
        public List<KeyValuePair<string, DataTable>> GetResults()
        {
            return null;
        }
        public List<List<string>> FastCombineKeysNew(int clusterNum)
        {
            Dictionary<string, List<int>> res = new Dictionary<string, List<int>>();
            distance.InitMeasure();
            //List<string> structures = new List<string>(((FastDiscreteDist) distance).dataBaseKeys);
            List<string> structures = new List<string>(((Fast3States)distance).dataBaseKeys);
            //List<string> structures = new List<string>(data.Keys);
            jury1D jury = new jury1D();
            jury.PrepareJury(data);
            var jOut=jury.JuryOptWeights(structures).juryLike;

            HashSet<string> indexUsed = new HashSet<string>();            

           

            int[] distL = distance.GetDistance(jOut[0].Key, structures);
            int [] ccc =(int []) distL.Clone();
            Array.Sort(distL);
            int prev = 0;
            int pos = distL.Length / 2;
            int remStart = 0;
            int remEnd = distL.Length;
            int distThreshold = distL[pos];
            if(distThreshold==0)
            {
                while (pos < distL.Length && distL[pos] == 0)
                    pos++;

                if (pos == distL.Length)
                    throw new Exception("all distances are 0");
                distThreshold = distL[pos];
            }

            int[] indexes = new int[structures.Count];
            
            while (remEnd-remStart>1)
            {
                res.Clear();                
                int n = 0;
                prev = 0;
                for (int i = 0; i < structures.Count; i++)
                    indexUsed.Add(structures[i]);

                string refStruct = jOut[0].Key;
                while (indexUsed.Count > 0)
                {
                    int[] dist=null;
                    List<int> cluster = new List<int>();
                        for (int i = 0; i < indexes.Length; i++)
                            indexes[i] = i;

                        dist = distance.GetDistance(refStruct, structures);

                        Array.Sort(dist, indexes);
                        
                        cluster.Add(indexes[0]);
                        indexUsed.Remove(structures[indexes[0]]);
                    int k = 1;
                        while (dist[k] <= distThreshold)
                        {
                            if (indexUsed.Contains(structures[indexes[k]]))
                            {
                                cluster.Add(indexes[k]);
                                indexUsed.Remove(structures[indexes[k]]);
                            }
                            k++;
                        }
                        res.Add(refStruct, cluster);
                        refStruct = "";
                        for (int i = n + 1; i < jOut.Count; i++)
                            if (indexUsed.Contains(jOut[i].Key))
                            {
                                {
                                    prev = n;
                                    refStruct = jOut[i].Key;
                                    
                                    n = i;
                                    break;
                                }
                            }
                        
                }
                if (res.Count > clusterNum)
                    remStart = pos;
                else
                    remEnd = pos;

                pos = (remStart + remEnd) / 2;
                    distThreshold = distL[pos];
            }
            List<List<string>> clusters = new List<List<string>>();

            foreach(var item in res)
            {
                List<string> cl = new List<string>();
                cl.Add(item.Key);
                for (int i = 0; i < item.Value.Count;i++)
                    cl.Add(structures[item.Value[i]]);
                clusters.Add(cl);
            }

            return clusters;
        }
    }
}
