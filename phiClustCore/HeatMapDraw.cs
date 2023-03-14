using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using phiClustCore.Profiles;
using System.IO;

namespace phiClustCore
{
    public class HeatMapDraw
    {
        String Name;
        OmicsDataSet data;
        public List<int> indexLabels;
        public List<Color> barMap;
        public Dictionary<double, Color> profilesColorMap;
        public DrawHierarchical upper;
        public DrawHierarchical left;
        public Bitmap upperBitMap;
        public Bitmap leftBitMap;        
        Bitmap legendBitMap;
        public Bitmap heatMap;
        Bitmap bitAll;
        public HClusterNode upperNode, auxUpper;
        public HClusterNode leftNode, auxLeft;
        public ClusterOutput outp;
        Dictionary<string,List<string>> clustersUpper;
        Dictionary<string, List<string>> clustersLeft;
        Dictionary<string, string> labels;

        double[,] dataToDraw = null;
        public HeatMapDraw(Bitmap upper, Bitmap left,Bitmap heat,Bitmap legend,HClusterNode upperNode, HClusterNode leftNode, Dictionary<string, string> labels, ClusterOutput outp)
        {

            upperNode.ClearColors(Color.Black);
            leftNode.ClearColors(Color.Black);
            upperBitMap = upper;
            leftBitMap = left;
            legendBitMap = legend;
            heatMap = heat;
            clustersUpper = new Dictionary<string, List<string>>();
            clustersLeft = new Dictionary<string, List<string>>();
            foreach (var item in outp.ClustersUpper.list)
            {
                clustersUpper.Add(item[0], new List<string>());
                for (int i = 1; i < item.Count; i++)
                    clustersUpper[item[0]].Add(item[i]);
            }
            foreach (var item in outp.ClustersLeft.list)
            {
                clustersLeft.Add(item[0], new List<string>());
                for (int i = 1; i < item.Count; i++)
                    clustersLeft[item[0]].Add(item[i]);
            }
            
            this.outp = outp;
            this.bitAll = new Bitmap(left.Width+upper.Width,upper.Height+left.Height);
            this.upperNode = auxUpper = upperNode;
            this.leftNode = auxLeft = leftNode;
            this.labels = labels;
            data = outp.data;
            if (outp.data.intervals?.Count > 0)
            {
                List<int> counterM = new List<int>();
                List<int> counterP = new List<int>();
                int[] codes = new int[outp.data.codes.Count];
                profilesColorMap = new Dictionary<double, Color>();
                int i = 0;
                foreach(var item in outp.data.codes)
                {
                    codes[i] = item;
                    if (codes[i] < 0)
                        counterM.Add(codes[i]);
                    if (codes[i] == 0)
                        profilesColorMap.Add(0, Color.FromArgb(0, 0, 0));
                    if (codes[i] > 0)
                        counterP.Add(codes[i]);
                    i++;
                }
                if(counterM.Count==0)
                {
                    counterP.Add(0);
                    profilesColorMap.Clear();
                    int[] aux = counterP.ToArray();
                    Array.Sort(aux);
                    aux.Reverse();
                    counterP.Clear();
                    for (i = 0; i < aux.Length; i++)
                        if (i < aux.Length / 2)
                            counterM.Add(aux[i]);
                        else
                            counterP.Add(aux[i]);

                }
                
                if (counterM.Count > 0)
                {
                    int[] aux = counterM.ToArray();
                    Array.Sort(aux);
                    aux.Reverse();

                    double st = 255.0 / aux.Length;
                    for (i = 0; i < aux.Length; i++)
                        profilesColorMap.Add(aux[aux.Length-1-i], Color.FromArgb(0, 0, (int)(st * (i + 1))));
                }
                if (counterP.Count > 0)
                {
                    int[] aux = counterP.ToArray();
                    Array.Sort(aux);

                    double st = 255.0 / aux.Length;
                    for (i = 0; i < aux.Length; i++)
                        profilesColorMap.Add(aux[i], Color.FromArgb((int)(st * (i + 1)), (int)(st * (i + 1)), 0));
                }
            }
            else
                throw new Exception("Descrete states are not defined");
        }
        public void ChangeBitmpSizes(int widthLeft,int heightLeft,int widthUpper,int heightUpper)
        {

                leftBitMap = new Bitmap(widthLeft, heightLeft);
                upperBitMap = new Bitmap(widthUpper, heightUpper);
                heatMap = new Bitmap(widthUpper, heightLeft);
        }
        public HeatMapDraw(Bitmap bit, HClusterNode upperNode, HClusterNode leftNode, Dictionary<string, string> labels, ClusterOutput outp)
        {
            upperNode.ClearColors(Color.Black);
            leftNode.ClearColors(Color.Black);
            upperBitMap = new Bitmap(9*bit.Width / 10, bit.Height / 10);
            leftBitMap = new Bitmap(bit.Width / 10, 9*bit.Height / 10);
            legendBitMap = new Bitmap(bit.Width / 10, bit.Height / 10);
            heatMap = new Bitmap(9 * bit.Width / 10, 9 * bit.Height / 10);
            
            this.outp = outp;
            this.bitAll = bit;
            this.upperNode = auxUpper = upperNode;
            this.leftNode = auxLeft = leftNode;
            this.labels = labels;

        }
        public void PrepareDataForHeatMap()
        {
            this.Name = "HeatMap ";
            upper = new DrawHierarchical(upperNode, outp.measure, labels, upperBitMap, true);
            left = new DrawHierarchical(leftNode, outp.measure, labels, leftBitMap, false);

            List<HClusterNode> upperLeaves = auxUpper.GetLeaves();
            List<HClusterNode> leftLeaves = auxLeft.GetLeaves();

            upperLeaves = upperLeaves.OrderByDescending(o => o.gNode.x).Reverse().ToList();
            leftLeaves = leftLeaves.OrderByDescending(o => o.gNode.y).Reverse().ToList();

            BarColors();
        }
        void BarColors()
        {
            barMap = new List<Color>();
            barMap.Add(Color.Black);
            barMap.Add(Color.Red);
            barMap.Add(Color.Blue);
            barMap.Add(Color.Green);
            barMap.Add(Color.Orange);
            barMap.Add(Color.Plum);
            barMap.Add(Color.Navy);
            barMap.Add(Color.LightGreen);
            barMap.Add(Color.MediumTurquoise);
            barMap.Add(Color.Olive);
            barMap.Add(Color.Yellow);
            barMap.Add(Color.CornflowerBlue);
            barMap.Add(Color.Ivory);

        }

        public void DrawHeatMapNode(Bitmap bmp, List<string> profiles, List<string> leaves)
        {
            Graphics g = Graphics.FromImage(bmp);
            int width = bmp.Width;
            int height = bmp.Height;
            float xStep = (float)width;
            float yStep = (float)height;


            //if(upperLeaves.Count>1)
            xStep = (float)width / (leaves.Count);

            //if(profiles.Count>1)
            yStep = (float)height / (profiles.Count);

            int currentX = 0;
            int currentY = 0;
            SolidBrush b = new SolidBrush(Color.Black);
            for (int i = 0; i < profiles.Count; i++)
            {
                currentY = (int)(i * yStep);
                for (int j = 0; j < leaves.Count; j++)
                {
                    int ind = 1;// omicsProfiles[profiles[i]][leaves[j]];

                    Color c = profilesColorMap[ind];
                    b.Color = c;
                    currentX = (int)(j * xStep);
                    g.FillRectangle(b, currentX, currentY, xStep, yStep);

                }

            }


        }
        public void DrawHeatMap()
        {
            Graphics g = Graphics.FromImage(heatMap);
            List<HClusterNode> upperLeaves = auxUpper.GetLeaves();
            List<HClusterNode> leftLeaves = auxLeft.GetLeaves();

            upperLeaves = upperLeaves.OrderByDescending(o => o.gNode.x).Reverse().ToList();
            leftLeaves = leftLeaves.OrderByDescending(o => o.gNode.y).Reverse().ToList();
            SolidBrush b = new SolidBrush(Color.Black);
            double yPos1, yPos2;

            Dictionary<string, int> geneIndex = new Dictionary<string, int>();
            for (int i = 0; i < data.geneLabels.Count; i++)
                geneIndex.Add(data.geneLabels[i], i);

            Dictionary<string, int> sampleIndex = new Dictionary<string, int>();
            for (int i = 0; i < data.sampleLabels.Count; i++)
                sampleIndex.Add(data.sampleLabels[i], i);


            double yPos1Ref = 0;
            double yPos2Ref = 0;
            for (int i = 0; i < leftLeaves.Count; i++)
            {
                yPos1Ref = leftLeaves[i].gNode.areaLeft;
                yPos2Ref = leftLeaves[i].gNode.areaRight;
                //                double stepY=(yPos2Ref- yPos1Ref)/ leftLeaves[i].setStruct.Count;
                double stepY = (double)(leftLeaves[i].gNode.areaRight - leftLeaves[i].gNode.areaLeft) / leftLeaves[i].setStruct.Count;
                for (int k = 0; k < leftLeaves[i].setStruct.Count; k++)
                {
                    yPos1=yPos1Ref+k*stepY;
                    yPos2=yPos1Ref+(k+1)*stepY;

                    double xPos1, xPos2;
                    double xPos1Ref, xPos2Ref;
                    for (int j = 0; j < upperLeaves.Count; j++)
                    {
                        xPos1Ref = upperLeaves[j].gNode.areaLeft;
                        xPos2Ref = upperLeaves[j].gNode.areaRight;
                        if ((xPos2Ref - xPos1Ref) == 0)
                            continue;

                        //double stepX = (xPos2Ref - xPos1Ref) / upperLeaves[j].setStruct.Count;
                        double stepX = (double)(upperLeaves[j].gNode.areaRight - upperLeaves[j].gNode.areaLeft) / upperLeaves[j].setStruct.Count;
                        for (int n = 0; n < upperLeaves[j].setStruct.Count; n++)
                        {
                            xPos1 = xPos1Ref + stepX * n;
                            xPos2 = xPos2Ref + stepX * (n + 1);

                            double ind = data.data[sampleIndex[leftLeaves[i].setStruct[k]], geneIndex[upperLeaves[j].setStruct[n]]];
                            Color c = profilesColorMap[ind];
                            b.Color = c;
                            if (yPos2 - yPos1 > 0)                        
                            g.FillRectangle(b, (float)xPos1, (float)yPos1,(float)stepX, (float)stepY);
                            
                        }
                    }
                }
            }

        }
        public void DrawHeatMapN()
        {
            Graphics g = Graphics.FromImage(heatMap);
            List<HClusterNode> upperLeaves = auxUpper.GetLeaves();
            List<HClusterNode> leftLeaves = auxLeft.GetLeaves();

            upperLeaves = upperLeaves.OrderByDescending(o => o.gNode.x).Reverse().ToList();
            leftLeaves = leftLeaves.OrderByDescending(o => o.gNode.y).Reverse().ToList();
            SolidBrush b = new SolidBrush(Color.Black);
            double yPos1, yPos2;
            List<Tuple<Color,float,float>>[,] colors = new List<Tuple<Color, float, float>>[heatMap.Width,heatMap.Height];
            for (int i = 0; i < colors.GetLength(0); i++)
                for (int j = 0; j < colors.GetLength(1); j++)
                    colors[i, j] = new List<Tuple<Color, float, float>>();

            Dictionary<string, int> geneIndex = new Dictionary<string, int>();
            for (int i = 0; i < data.geneLabels.Count; i++)
                geneIndex.Add(data.geneLabels[i], i);

            Dictionary<string, int> sampleIndex = new Dictionary<string, int>();
            for (int i = 0; i < data.sampleLabels.Count; i++)
                sampleIndex.Add(data.sampleLabels[i], i);

            int ccc = 0;
            double yPos1Ref = 0;
            double yPos2Ref = 0;
            for (int i = 0; i < leftLeaves.Count; i++)
            {
                yPos1Ref = leftLeaves[i].gNode.areaLeft;
                yPos2Ref = leftLeaves[i].gNode.areaRight;
                //                double stepY=(yPos2Ref- yPos1Ref)/ leftLeaves[i].setStruct.Count;
                double stepY = (double)(leftLeaves[i].gNode.areaRight - leftLeaves[i].gNode.areaLeft) / leftLeaves[i].setStruct.Count;
                for (int k = 0; k < leftLeaves[i].setStruct.Count; k++)
                {
                    yPos1 = yPos1Ref + k * stepY;
                    yPos2 = yPos1Ref + (k + 1) * stepY;

                    double xPos1, xPos2;
                    double xPos1Ref, xPos2Ref;
                    for (int j = 0; j < upperLeaves.Count; j++)
                    {
                        xPos1Ref = upperLeaves[j].gNode.areaLeft;
                        xPos2Ref = upperLeaves[j].gNode.areaRight;
                        //if ((xPos2Ref - xPos1Ref) == 0)
                        //    continue;

                        //double stepX = (xPos2Ref - xPos1Ref) / upperLeaves[j].setStruct.Count;
                        double stepX = (double)(upperLeaves[j].gNode.areaRight - upperLeaves[j].gNode.areaLeft) / upperLeaves[j].setStruct.Count;
                        for (int n = 0; n < upperLeaves[j].setStruct.Count; n++)
                        {
                            xPos1 = xPos1Ref + stepX * n;
                            xPos2 = xPos2Ref + stepX * (n + 1);

                            double ind = data.data[sampleIndex[leftLeaves[i].setStruct[k]], geneIndex[upperLeaves[j].setStruct[n]]];
                            Color c = profilesColorMap[ind];
                            b.Color = c;
                            //if (yPos2 - yPos1 > 0)
                            {
                                colors[(int)xPos1, (int)yPos1].Add(new Tuple<Color, float, float>(c, (float)stepX, (float)stepY));
                                ccc++;
                            }
                        }
                    }
                }
            }
            int xxx = 0;
            for(int i=0;i<colors.GetLength(0);i++)
                for(int j=0;j<colors.GetLength(1);j++)
                {
                    Dictionary<Color, int> freq = new Dictionary<Color, int>();
                    for (int n = 0; n < colors[i, j].Count; n++)
                    {
                        if (!freq.ContainsKey(colors[i, j][n].Item1))
                            freq[colors[i, j][n].Item1] = 0;

                        freq[colors[i, j][n].Item1]++;
                    } 
                   List<KeyValuePair<Color, int>> mappings = freq.ToList();
                    if (mappings.Count > 0)
                    {
                        xxx = 0;
                        foreach (var item in mappings)
                            xxx += item.Value;
                        if (xxx > 20)
                            Console.Write("");
                        mappings.Sort((x, y) => x.Value.CompareTo(y.Value));
                        float h = 1;
                        if (colors[i, j][0].Item3 > 1)
                            h = colors[i, j][0].Item3;
                        b.Color = mappings[mappings.Count-1].Key;
                        g.FillRectangle(b, i, j, colors[i,j][0].Item2, h);
                    }
                }

        }

        private void PaintLegend()
        {
            Bitmap localBit = new Bitmap(100, 100);
            SolidBrush b = new SolidBrush(Color.Black);
            int xPos, yPos;
            xPos = 5; yPos = 5;
            System.Drawing.Font drawFont = new System.Drawing.Font("Arial", 10);
            System.Drawing.SolidBrush drawBrush = new System.Drawing.SolidBrush(System.Drawing.Color.Black);
            System.Drawing.StringFormat drawFormat = new System.Drawing.StringFormat();


            
            List<double> ordered = new List<double>(profilesColorMap.Keys);
            Graphics g = Graphics.FromImage(localBit);
            ordered.Sort();
            foreach (var item in ordered)
            {
                b.Color = profilesColorMap[item];
                g.FillRectangle(b, xPos, yPos, 15, 10);
                //e.Graphics.DrawString(item.ToString(), drawFont, drawBrush, xPos+25,yPos-3);
                g.DrawString(indexLabels[(int)item].ToString(), drawFont, drawBrush, xPos + 25, yPos - 3);
                yPos += 25;
                if (yPos >localBit.Height)
                {
                    yPos = 5;
                    xPos += 40;
                }
            }
            //test.Paint();             
            Graphics gr = Graphics.FromImage(legendBitMap);
            gr.DrawImage(localBit, 0, 0,legendBitMap.Width,legendBitMap.Height);
            
        }
        private void PaintUpperBitMap()
       {                       
            Graphics g = Graphics.FromImage(upperBitMap);
//            PrepareBarRegions(e.Graphics);
//            g.Clear(this.BackColor);
            
            upper.DrawOnBuffer(upperBitMap, false, 1, Color.Empty);
            g.DrawImage(upperBitMap, 0, 0);

        }

        private void PaintLeftBitMap()
        {
            Graphics g = Graphics.FromImage(leftBitMap);
            left.posStart = 5;
            left.DrawOnBuffer(leftBitMap, false, 1, Color.Empty);
            g.DrawImage(leftBitMap, 0, 0);
        }

        private void PaintHeatMap()
        {            
            DrawHeatMap();
        }
        public void SavePaint(string fileName)
        {
            PrepareDataForHeatMap();
            PaintLegend();
            PaintUpperBitMap();
            PaintLeftBitMap();
            PaintHeatMap();

            Graphics g = Graphics.FromImage(bitAll);


            g.DrawImage(legendBitMap, 0, 0);
            g.DrawImage(upperBitMap, bitAll.Width - upperBitMap.Width, 0);
            g.DrawImage(leftBitMap, 0, bitAll.Height - leftBitMap.Height);
            g.DrawImage(heatMap, bitAll.Width - upperBitMap.Width, bitAll.Height - leftBitMap.Height);

            bitAll.Save(fileName);
        }
    }
}
