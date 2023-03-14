using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Series;
using phiClustCore;

namespace Graph
{
    public partial class PlotForm : Form
    {
        static Dictionary<string, int> refGenes = new Dictionary<string, int>();
        OxyColor[] col = new OxyColor[10];
        OmicsDataSet omData;
        PlotModel mModel=new PlotModel();
        double[,] data;
        List<Tuple<int, double>>[] points=null;
        double[] val;
        double[] refData;
        HashSet<string>[] genes;
        public PlotForm(OmicsDataSet data)
        {
            omData = data;
            this.data = omData.data.GetArray();

            InitializeComponent();
            col[0] = OxyColors.Olive;
            col[1] = OxyColors.Red;
            col[2] = OxyColors.Yellow;
            col[3] = OxyColors.YellowGreen;
            col[4] = OxyColors.SkyBlue;
            col[5] = OxyColors.Azure;
            col[6] = OxyColors.Black;
            col[7] = OxyColors.BlueViolet;
            col[8] = OxyColors.Brown;
            col[9] = OxyColors.Coral;

            if (omData.geneLabels != null)
            {
                foreach (var item in omData.geneLabels)
                    comboBox1.Items.Add(item);
                comboBox1.SelectedIndex = 0;

            }
        }
        public PlotForm(OmicsDataSet data, double[] refData) : this(data)
        {
            this.refData = refData;            
        }
        public void TopOrder(double[] refData, List<string> geneOrder)
        {

        }
       
        public Tuple<int[], double[],HashSet<string>[]> Histog(double []refData,int n,List<string> geneOrder)
        {
            int []freq = new int[n+1];
            double[] v = new double[n+1];
            genes = new HashSet<string>[n + 1];
            double max = double.MinValue;
            for (int i = 0; i < refData.Length; i++)
                if (refData[i] > max)
                    max = refData[i];

            for (int i = 0; i < genes.Length; i++)
                genes[i] = new HashSet<string>();

            double step = max / n;
            int k;
            for (int i = 0; i < refData.Length; i++)
            {
                k = 0;
                for (double j = 0; j < max; j += step,k++)
                {
                    if (refData[i] >= j && refData[i] < j + step)
                    {
                        freq[k]++;
                        genes[k].Add(geneOrder[i]);
                        break;
                    }
                }
            }
            k = 0;
            for (double j = 0; j < max; j += step, k++)
                v[k] = j + step / 2;

            return new Tuple<int[], double[],HashSet<string>[]>(freq, v,genes);


            }

        List<Tuple<int, double>> GetPointsPatient(OmicsDataSet patient)
        {
            List<int>[] geneIndex = new List<int>[genes.Length];

            for (int i = 0; i < genes.Length; i++)
            {
                geneIndex[i] = new List<int>();
                for (int j = 0; j < patient.geneLabels.Count; j++)
                    if (genes[i].Contains(patient.geneLabels[j]))
                        geneIndex[i].Add(j);
            }

            List<Tuple<int, double>> res = new List<Tuple<int, double>>();
                for (int k = 0; k < geneIndex.Length; k++)
                {
                    double sum = 0;
                    for (int j = 0; j < geneIndex[k].Count; j++)
                        for(int i=0;i<patient.data.rows;i++)
                        sum += patient.data[i, j];
                    if (sum > 0)
                    {
                        sum /= data.GetLength(0);
                        res.Add(new Tuple<int, double>(k, sum));
                    }
                }

            return res;
        }

        List<Tuple<int,double>>[] GetPoints(OmicsDataSet patient, int num)
        {
            List<int>[] geneIndex = new List<int>[genes.Length];

            for(int i=0;i<genes.Length;i++)
            {
                geneIndex[i] = new List<int>();
                for (int j = 0; j < patient.geneLabels.Count; j++)
                    if (genes[i].Contains(patient.geneLabels[j]))
                        geneIndex[i].Add(j);
            }

            comboBox1.Items.Clear();
            for (int i = 0; i < num; i++)
                comboBox1.Items.Add(i);

            List<Tuple<int,double>>[] res = new List<Tuple<int,double>>[num];
            for (int i = 0; i < num; i++)
            {
                res[i] = new List<Tuple<int,double>>();
                Random r = new Random();
                int index = r.Next(patient.data.rows);
                for (int k = 0; k < geneIndex.Length; k++)
                {
                    double sum = 0;
                    for (int j = 0; j < geneIndex[k].Count; j++)                    
                        sum += patient.data[index, j];
                    if (sum > 0)
                        res[i].Add(new Tuple<int, double>(k,sum));
                }
            }

            return res;
        }

        public void PlotLines(List<Tuple<int,double>>points)
        {
            LineSeries l = new LineSeries();
            for (int i = 1; i < points.Count; i++)
                l.Points.Add(new DataPoint(val[points[i].Item1], points[i].Item2));

            
            mModel.Series.Add(l);

            
        }
        public void PlotRef3(List<OmicsDataSet> patients)
        {
            int[] freq;
            //var res = Descritize.NegativeBinomialFit(refData, false);
            var res = Histog(refData, 50,omData.geneLabels);
            freq = res.Item1;
            val = res.Item2;

            foreach(var item in patients)
            {
                var points=GetPointsPatient(item);

                LineSeries l = new LineSeries();
                for (int i = 1; i < points.Count; i++)
                    l.Points.Add(new DataPoint(val[points[i].Item1], points[i].Item2));


                mModel.Series.Add(l);
            }
            this.plotView1.Model = mModel;

        }
        public void PlotRankCells(List<OmicsDataSet> patients)
        {
            Dictionary<string, int>[] dic = new Dictionary<string, int>[patients.Count];
            int i = 0;

            dic[0] = new Dictionary<string, int>();
            patients[0].ApplyFilters(patients[0].filters);
            foreach (var it in patients[0].filters)
            {
                if (it is GlobalZScore)
                {
                    GlobalZScore x = (GlobalZScore)it;
                    int[] index = new int[x.columns.Length];
                    for (int n = 0; n < index.Length; n++)
                        index[n] = n;



                    Array.Sort(x.columns, index);
                    Array.Reverse(x.columns);
                    Array.Reverse(index);
                    //var res = Histog(x.columns, 10000, item.geneLabels);
                    for (int k = 0; k < index.Length; k++)
                        if (x.columns[k] > 0)
                            dic[i].Add(patients[0].geneLabels[index[k]], k);
                }

                Random r = new Random();
                for (int n = 1; n < patients.Count; n++)
                {
                    dic[n] = new Dictionary<string, int>();

                    for (int l = 0; l < 1; l++)
                    {

                        double[] col = new double[patients[n].data.columns];

                        int ind = r.Next(patients[n].data.rows);
                        for (int z = 0; z < col.Length; z++)
                            col[z] = patients[n].data[ind, z];

                        int[] index = new int[col.Length];
                        for (int s = 0; s < index.Length; s++)
                            index[s] = s;


                        Array.Sort(col, index);
                        Array.Reverse(col);
                        Array.Reverse(index);
                        //var res = Histog(x.columns, 10000, item.geneLabels);
                        for (int k = 0; k < index.Length; k++)
                            if (col[k] > 0)
                                dic[n].Add(patients[n].geneLabels[index[k]], k);
                    }
                }
            }
            var myModel = new PlotModel { Title = "Global Histogram" };


            for (int n = 1; n < dic.Length; n++)
            {
                var scater = new ScatterSeries { MarkerType = MarkerType.Circle };
                foreach (var item in dic[n].Keys)
                    if (dic[0].ContainsKey(item) && dic[n][item] < 18000)
                        //if(dic[0][item]>19000)
                        scater.Points.Add(new ScatterPoint(dic[0][item], dic[n][item], 2, n, item));

                myModel.Series.Add(scater);
            }


            this.plotView1.Model = myModel;

        }

        public void PlotRank(List<OmicsDataSet> patients)
        {
            Dictionary<string, int>[] dic = new Dictionary<string, int>[patients.Count];
            int i = 0;
            
            foreach (var item in patients)
            {
                dic[i] = new Dictionary<string, int>();
                item.ApplyFilters(item.filters);
                foreach (var it in item.filters)
                {
                    if (it is GlobalZScore)
                    {
                        GlobalZScore x = (GlobalZScore)it;
                        int[] index = new int[x.columns.Length];
                        for (int n = 0; n < index.Length; n++)
                            index[n] = n;


                        
                        Array.Sort(x.columns, index);
                        Array.Reverse(x.columns);
                        Array.Reverse(index);
                        //var res = Histog(x.columns, 10000, item.geneLabels);
                        for(int k=0;k<index.Length;k++)
                              if(x.columns[k]>0)
                                dic[i].Add(item.geneLabels[index[k]], k);
                    }
                }
                i++;
            }
            var myModel = new PlotModel { Title = "Global Histogram" };


            for(int n=1;n<dic.Length;n++)
            {
                var scater = new ScatterSeries { MarkerType = MarkerType.Circle };
                foreach (var item in dic[n].Keys)
                    if(dic[0].ContainsKey(item) && dic[n][item]<18000)
                        //if(dic[0][item]>19000)
                        scater.Points.Add(new ScatterPoint(dic[0][item], dic[n][item], 2, n,item));

                myModel.Series.Add(scater);
            }
            

            this.plotView1.Model = myModel;

        }
        public void PlotRef2(OmicsDataSet patient)
        {
            int[] freq;
            //var res = Descritize.NegativeBinomialFit(refData, false);
            var res = Histog(refData, 50,omData.geneLabels);
            freq = res.Item1;
            val = res.Item2;

            points = GetPoints(patient, 20);

            PlotLines(points[0]);
            this.plotView1.Model = mModel;



        }
        public HashSet<string> PlotRef(List<double[]> patients)
        {
            var myModel = new PlotModel { Title = "Global Histogram" };
            HashSet<string> tailGenes = new HashSet<string>();
            int j = 0;
            foreach (var item in patients)
            {
//                selectedGenes[j] = new HashSet<string>();
                int[] freq;
                var res = Histog(item, 50,null);
                freq = res.Item1;
                double[] val = res.Item2;
                LineSeries l = new LineSeries();
                for (int i = 3; i < freq.GetLength(0); i++)
                {
                    if (freq[i] > 0)
                        l.Points.Add(new DataPoint(val[i], freq[i]));
                    if (val[i] > 0.004)
                        foreach (var it in res.Item3[i])
                            tailGenes.Add(it);
                        
                    
                }
               
                myModel.Series.Add(l);
            }
            this.plotView1.Model = myModel;

            return tailGenes;
        }
        public void PlotRef(HashSet<string>genes)
        {
            int[] freq;
            var res = Histog(refData, 50,omData.geneLabels);
            freq = res.Item1;
            double[] val = res.Item2;
            var myModel = new PlotModel { Title = "Global Histogram" };
            LineSeries l = new LineSeries();
            for (int i = 1; i < freq.GetLength(0); i++)
            {
                if (freq[i] > 0)
                    l.Points.Add(new DataPoint(val[i], freq[i]));

                foreach(var item in res.Item3[i])
                {
                    if(genes.Contains(item))
                    {
                        LineAnnotation Line = new LineAnnotation()
                        {
                            StrokeThickness = 2,
                            LineStyle = LineStyle.Solid,
                            Color = col[0],
                            Type = LineAnnotationType.Vertical,
                            X = val[i],
                            // Y = 100
                        };
                        myModel.Annotations.Add(Line);

                    }
                }
            }


            myModel.Series.Add(l);

            this.plotView1.Model = myModel;

        }

        public void PlotRef(OmicsDataSet patient)
        {
            int[] freq;
            //var res = Descritize.NegativeBinomialFit(refData, false);
            var res = Histog(refData, 50,omData.geneLabels);
            freq = res.Item1;
            double []val = res.Item2;
            LineSeries l = new LineSeries();
            for (int i = 1; i < freq.GetLength(0); i++)
                if (freq[i] > 0)
                    l.Points.Add(new DataPoint(val[i], freq[i]));

            var myModel = new PlotModel { Title = "Global Histogram" };
            myModel.Series.Add(l);


            var points = GetPoints(patient, 5);
           

            LineSeries[] x = new LineSeries[points.Length];
            for (int i = 0; i < points.Length; i++)
            {


                for (int k = 0; k < points[i].Count; k++)
                {
                    LineAnnotation Line = new LineAnnotation()
                    {                        
                        StrokeThickness = 2,
                        LineStyle = LineStyle.Solid,
                        Color = col[i],
                        Type = LineAnnotationType.Vertical,
                        X = val[points[i][k].Item1]+ val[points[i][k].Item1]*i/10.0,
                       // Y = 100
                    };
                    myModel.Annotations.Add(Line);
                }
                
            }
            this.plotView1.Model = myModel;

        }
        public void PlotData()
        {

            int[] freq;
            double[] reconst;

            int n = comboBox1.SelectedIndex;

            double[] column = new double[data.GetLength(0)];

            
            for (int i = 0; i < data.GetLength(0); i++)
                    column[i] = data[i, n];                                 

            var res = Descritize.NegativeBinomialFit(column,false);

            Interval[] intervals = Descritize.GetIntervals(res.Item1, res.Item2, 10);
            Descritize.AssignCodeToIntervals(res.Item1, intervals);
            freq = res.Item3;
            LineSeries l = new LineSeries();
            for (int i = 0; i < freq.GetLength(0); i++)
                if(freq[i]>0)
                l.Points.Add(new DataPoint(i, freq[i]));

            reconst = res.Item4;
            LineSeries l2 = new LineSeries();
            for (int i = 0; i < reconst.GetLength(0); i++)
                if((int)reconst[i]>0)
                    l2.Points.Add(new DataPoint(i, reconst[i]));

            LineSeries l3 = new LineSeries();
            l3.Points.Add(new DataPoint(res.Item1 - res.Item2/2, 50));
            l3.Points.Add(new DataPoint(res.Item1 + res.Item2/2, 50));

            
            LineSeries l4 = new LineSeries();
            
            foreach (var item in intervals)
            {
                if (item.max != double.MaxValue)
                {
                    l4.Points.Add(new DataPoint(item.max,0));
                    l4.Points.Add(new DataPoint(item.max,500));
                }
            }



            var myModel = new PlotModel { Title = "Histogram" };
            myModel.Series.Add(l);
            myModel.Series.Add(l2);
            myModel.Series.Add(l3);
            myModel.Series.Add(l4);
            this.plotView1.Model = myModel;

        }
        public void ColumnHistogram(int barNums)
        {
            int n = comboBox1.SelectedIndex;
            double min = double.MaxValue;
            double max = double.MinValue;
            for (int i = 0; i < data.GetLength(0); i++)
            {
                if (data[i, n] > max)
                    max = data[i, n];
                if (data[i, n] < min)
                    min = data[i, n];
            }
            double step = (max - min) / barNums;

            double[,] count = new double[barNums,2];

            for (int i = 0; i < data.GetLength(0); i++)
            {
                for(int j=0;j<barNums;j++)
                {
                    double val = data[i, n];
                    count[j, 0] = min + j * step + step / 2;
                    if (val>=min+j*step && val<min+(j+1)*step)
                    {
                        
                        count[j, 1]++;
                    }
                }
            }
            LineSeries l = new LineSeries();
            for (int i = 0; i < count.GetLength(0); i++)
                l.Points.Add(new DataPoint(count[i, 0], count[i, 1]));

           
            var myModel = new PlotModel { Title = "Histogram" };
            myModel.Series.Add(l);
            this.plotView1.Model = myModel;

        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            //ColumnHistogram(30);
            //PlotData();
            if(points!=null)
                PlotLines(points[comboBox1.SelectedIndex]);
        }
    }
}
