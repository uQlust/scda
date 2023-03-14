using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Graph;
using phiClustCore;

namespace DataAnalysis
{
    public partial class FormPlot : AddOmicsFile
    {
        OmicsDataSet refData=null;
        public FormPlot()
        {
            InitializeComponent();
        }       

        void RemoveGenes(List <OmicsDataSet> patients,double [] p)
        {
            HashSet<string>[] selectedGenes = new HashSet<string>[patients.Count];
            List<string> removeGenes = new List<string>();
            PlotForm pf = new PlotForm(patients[0]);
            var res = pf.Histog(p, 50,refData.geneLabels);

            for (int k = 0; k < res.Item1.Length; k++)
                if (res.Item1[k] < 20)
                    foreach (var it in res.Item3[k])
                        removeGenes.Add(it);

            for(int i=0;i< patients.Count;i++)
            {
                patients[i]=patients[i].RemoveGenes(removeGenes);
            }

        }
        public override void button9_Click(object sender, EventArgs e)
        {
            OmicsDataSet joined = null;
            List<OmicsDataSet> omicsFiltered = new List<OmicsDataSet>();
            if (omicsFiles.Items.Count > 0)
            {
                List<OmicsDataSet> xx = new List<OmicsDataSet>();
                for (int i = 0; i < omicsFiles.Items.Count; i++)
                {
                    xx.Add((OmicsDataSet)omicsFiles.Items[i]);
                    xx[xx.Count - 1].filters = jFilters;
                }
                PlotForm pF = new PlotForm(xx[0]);
                //pF.PlotRank(xx);
                pF.PlotRankCells(xx);
                pF.Show();


            }

            /*        public override void button9_Click(object sender, EventArgs e)
                    {
                        OmicsDataSet joined = null;
                        List<OmicsDataSet> omicsFiltered = new List<OmicsDataSet>();
                        if (omicsFiles.Items.Count > 0)
                        {
                            foreach (var item in omicsFiles.Items)
                            {
                                OmicsDataSet aux = (OmicsDataSet)item;
                                if (superGenes?.Count > 0)
                                    aux = aux.CreateSuperGenesData(superGenes);
                                aux.ApplyFilters(aux.filters);

                                omicsFiltered.Add(aux);
                            }
                            refData = omicsFiltered[0];
                            List<OmicsDataSet> xx = new List<OmicsDataSet>();
                            for (int i = 1; i < omicsFiltered.Count; i++)
                                xx.Add(omicsFiltered[i]);
                            joined = OmicsDataSet.JoinOmicsData(xx);
                            joined.filters = jFilters;
                            refData=refData.ApplyFilters(jFilters);
                            double[] dataList=null;
                            foreach (var item in jFilters)
                            {
                                if(item is GlobalZScore)
                                {
                                    GlobalZScore x = (GlobalZScore)item;
                                    dataList = x.columns;
                                }
                            }
                            PlotForm pF = new PlotForm(refData, dataList);
                            pF.PlotRef(xx[0]);
                            pF.Show();



                            List<double[]> datL = new List<double[]>();
                            foreach(var item in xx)
                            {
                                item.ApplyFilters(jFilters);
                                foreach (var it in jFilters)
                                {
                                    if (it is GlobalZScore)
                                    {
                                        GlobalZScore x = (GlobalZScore)it;
                                        datL.Add(x.columns);
                                    }
                                }
                            }
                            RemoveGenes(xx,datL[0]);
                            datL.Clear();
                            foreach (var item in xx)
                            {
                                item.ApplyFilters(jFilters);
                                foreach (var it in jFilters)
                                {
                                    if (it is GlobalZScore)
                                    {
                                        GlobalZScore x = (GlobalZScore)it;
                                        datL.Add(x.columns);
                                    }
                                }
                            }

                            PlotForm pF2 = new PlotForm(refData, dataList);
                            //pF2.PlotRef2(xx[0]);
                            //pF2.Show();
                            //pF2.PlotRef3(xx);
                            HashSet<string> gg=pF2.PlotRef(datL);
                            pF2.Show();

                            PlotForm pF3 = new PlotForm(refData, dataList);
                            pF3.PlotRef(gg);
                            pF3.Show();


                        }*/
        }
    }
}
