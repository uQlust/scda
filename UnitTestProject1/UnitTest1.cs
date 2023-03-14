using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using Graph;
using phiClustCore.Distance;
using phiClustCore;
using System.Collections.Generic;
using Accord.Statistics.Distributions.Univariate;


namespace UnitTestProject1
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestQuantileRow()
        {
            float [,]dat = new float[,] { { 5, 3, 4 }, { 2, 1, 1 }, { 1, 0, 0 }, { 6, 0, 0 } };
            //double[,] dat = new double[,] { { 0.29, 0.47, 0.23 }, { 0.5, 0.25, 0.25 }, { 0.17, 0.33, 0.5 }, { 0.33, 0.39, 0.28 } };
            SpareArray data = new SpareArray(dat);            

        OmicsDataSet setO = new OmicsDataSet();
            setO.data = data;

            QuantileColumn q = new QuantileColumn();

            OmicsDataSet o = q.ApplyFilter(setO);

        }

        [TestMethod]
        public void TestQuantile()
        {
            //double[,] data = new double[,] { { 5, 8, 4 }, { 2, 1, 1 }, { 1, 2, 3 }, { 6, 7, 5 } };
            float[,] data = new float[,] { { 5, 2, 2,2 }, { 8, 1, 2,2 }, { 4, 1, 3,5 } };

            SpareArray xx = new SpareArray(data, false);
            OmicsDataSet setO = new OmicsDataSet();
            setO.data = xx;

            //QuantileColumn q = new QuantileColumn();
            QuantileRow q = new QuantileRow();

            OmicsDataSet o = q.ApplyFilter(setO);
            System.Diagnostics.Debug.WriteLine("");
            o.data.Print();

        }
        [TestMethod]
        public void TestFilters()
        {
            float[,] dat = { { 1, 3, 4, 5, 3 }, { 2, 1, 3, 4, 2 }, { 3, 2, 1, 3, 2 }, { 1, 1, 1, 1, 1 }, { 2, 2, 2, 2, 2 } };
            SpareArray data = new SpareArray( dat);
            OmicsDataSet setO = new OmicsDataSet();
            setO.data = data;

            ZScoreColumn zs = new ZScoreColumn();

            OmicsDataSet o = zs.ApplyFilter(setO);

            Assert.IsTrue(Math.Abs(o.data[0, 0] + 1.0690449676497) < 0.00001);
            Assert.IsTrue(Math.Abs(o.data[1, 0] - 0.2672612) < 0.00001);

            Descritize ds = new Descritize();
            Dictionary<string, string> map = new Dictionary<string, string>() { { "Number of states", "3" }, { "Coding algorithm", CodingAlg.Z_SCORE.ToString() } };

            ds.SetParameters(map);
            OmicsDataSet op = ds.ApplyFilter(o);
            Assert.AreEqual(op.data[0, 0], -1);
            Assert.AreEqual(op.data[1, 0], 0);
            Assert.AreEqual(op.data[2, 0], 1);

            RowNormalization nr = new RowNormalization();

            OmicsDataSet rn = nr.ApplyFilter(setO);



        }
        [TestMethod]
        public void TestFastDiscrete()
        {
            Dictionary<string, List<double>> data = new Dictionary<string, List<double>>()
        {
            { "test1",new List<double>(){ 0, 1 ,1 ,1 ,1, 0,0,0 } },
            { "test2",new List<double>(){ 1, 0 ,0 ,0 ,1, 0,0,1 } },
            { "test3",new List<double>(){ 0, 1 ,0 ,1 ,0, 0,1,0 } },
            { "test4",new List<double>(){ 0, 1 ,1 ,1 ,0, 0,1,0 } },
        };
            int[] resDist = new int[4] { 0, 5, 3, 3 };
            int[][] res2Dist = new int[][] {new int[] { 0, 5, 3, 3 }, new int[]{ 5, 0, 6, 8 },
                                            new int[]{ 3, 6, 0, 2 },new int[]{ 3, 8, 2, 0 } };

            Dictionary<string, List<double>> dataT;

            /*dataT = new Dictionary<string, List<double>>();
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
                dataT[item.Key] = dat;
            }*/




            DistanceMeasure m = new FastDiscreteDist(1, data);
            m.InitMeasure();
            int[] res = m.GetDistance("test1", new List<string>(data.Keys));

            for (int i = 0; i < res.Length; i++)
                Console.WriteLine($"{i}={res[i]}");

            int[][] res2 = m.GetDistance(new List<string>(data.Keys), new List<string>(data.Keys));

            for (int i = 0; i < res2.Length; i++)
                for (int j = 0; j < res2[i].Length; j++)
                    Console.WriteLine($"{i}{j}={res2[i][j]}");

            m.CalcDistMatrix(new List<string>(data.Keys));
        }
        [TestMethod]
        public void TestFast3States()
        {
            Dictionary<string, List<double>> data = new Dictionary<string, List<double>>()
        {
            { "test1",new List<double>(){ 0, 0 ,0, 0 ,0, 0,0,0 } },
            { "test2",new List<double>(){ 0, 1 ,0 ,0 ,0, 0,0,0 } },
            { "test3",new List<double>(){ 0, 1 ,0 ,1 ,0, 0,1,0 } },
            { "test4",new List<double>(){ 0, 2 ,1 ,2 ,0, 0,0,0 } },
        };
            int[] resDist = new int[4] { 0, 5, 3, 3 };
            int[][] res2Dist = new int[][] {new int[] { 0, 5, 3, 3 }, new int[]{ 5, 0, 6, 8 },
                                            new int[]{ 3, 6, 0, 2 },new int[]{ 3, 8, 2, 0 } };

            Dictionary<string, List<double>> dataT;

            /*dataT = new Dictionary<string, List<double>>();
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
                dataT[item.Key] = dat;
            }*/




            DistanceMeasure m = new Fast3States(1, data);
            m.InitMeasure();
            int[] res = m.GetDistance("test1", new List<string>(data.Keys));

            for (int i = 0; i < res.Length; i++)
                Console.WriteLine($"{i}={res[i]}");

            int[][] res2 = m.GetDistance(new List<string>(data.Keys), new List<string>(data.Keys));

            for (int i = 0; i < res2.Length; i++)
                for (int j = 0; j < res2[i].Length; j++)
                    Console.WriteLine($"{i}{j}={res2[i][j]}");

            m.CalcDistMatrix(new List<string>(data.Keys));
        }
        [TestMethod]
        public void TestBinomial()
        {
            BinomialDistribution b = new BinomialDistribution();
            double []x=new double[]{ 1,4,3,1};
            b.Fit(x);

            Console.WriteLine("avr=" + b.Mean);
            Console.WriteLine("std=" + b.Variance);


        }
    }
}
