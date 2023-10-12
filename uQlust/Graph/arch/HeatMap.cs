using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using phiClustCore;
using phiClustCore.Profiles;
using phiClustCore.Interface;
using System.Drawing.Imaging;
using phiClustCore.Distance;

namespace Graph
{
    public partial class HeatMap : Form, IVisual
    {

        Random rand = new Random();
        bool avrRowAnnotations = false;
        bool cutHorizontalLine = false;
        bool cutVerticalLine = false;
        bool horizontalLines = false;
        bool cutBranch = false;
        bool swap = false;
        bool showLabels = false;
        bool showSelectedLabels = false;
        double horizontalCuttDistance = double.MaxValue;
        double verticalCuttDistance = double.MaxValue;
        int x_pos = 0;
        int y_pos = 0;
        int mouseX = 0;
        int mouseY = 0;
        Font drawFont = new Font("Arial", 10);
        string labelNum = "";
        SolidBrush brush = new SolidBrush(Color.Red);
        HeatMapDraw draw = null;
        Dictionary<Region, KeyValuePair<bool, string>> regionBarColor = new Dictionary<Region, KeyValuePair<bool, string>>();
        Dictionary<string, Color> labelToColor = new Dictionary<string, Color>();
        DistanceMeasure distM = null;

        bool fullImage = false;
        List<HClusterNode> upperLeaves;
        List<HClusterNode> leftLeaves;
        HClusterNode leftRoot=null;
        HClusterNode nodeForKruskal = null;
        HClusterNode remleftRoot=null;
        HClusterNode remAuxLeft = null;
        HashSet<string> geneSelection;
        Dictionary<string, Tuple<Dictionary<string, Tuple<Color, string>>, List<ColorRect>>> leftDescription;
        HClusterNode colorNodeUpper = null;
        HClusterNode colorNodeLeft = null;

        Dictionary<string, Tuple<string,int, int>> selectedGeneNames = new Dictionary<string, Tuple<string,int, int>>();
        //Bitmap heatmap=null;
        public void ToFront()
        {
            this.BringToFront();
        }

        public HeatMap(HClusterNode upperNode, HClusterNode leftNode, Dictionary<string, string> labels, ClusterOutput outp)
        {
            leftRoot=leftNode;
            upperLeaves = upperNode.GetLeaves();
            leftLeaves = leftRoot.GetLeaves();
            upperNode.ClearColors(Color.Black);
            leftRoot.ClearColors(Color.Black);
           
            InitializeComponent();
            this.Text = outp.name;
            //draw=new HeatMapDraw(new Bitmap(tableLayoutPanel1.Width, tableLayoutPanel1.Height), upperNode, leftNode, labels, outp);
            // draw = new HeatMapDraw(new Bitmap(pictureBox2.Width,pictureBox2.Height),new Bitmap(pictureBox3.Width,pictureBox3.Height),
            //     new Bitmap(pictureBox1.Width,pictureBox1.Height),new Bitmap(pictureBox4.Width,pictureBox4.Height),upperNode, leftNode, labels, outp);
            draw = new HeatMapDraw(new Bitmap(pictureBoxUpper.Width, pictureBoxUpper.Height), new Bitmap(pictureBoxLeft.Width, pictureBoxLeft.Height),
                new Bitmap(pictureBoxHeatMap.Width, pictureBoxHeatMap.Height), new Bitmap(pictureBox4.Width, pictureBox4.Height), upperNode, leftRoot, labels, outp);

            label1.Text = "Gene clusters: " + upperNode.GetLeaves().Count;
            label2.Text = "Sample clusters: " + leftRoot.GetLeaves().Count;
            distM = outp.distM;
            int max = 0;
            foreach (var item in leftRoot.GetLeaves())
                if (item.setStruct.Count > max)
                    max = item.setStruct.Count;

            label3.Text = "0";
            vScrollBar1.Minimum = 0;
            vScrollBar1.Maximum = max;
        }
        public void PrepareDataForHeatMap()
        {
            draw.PrepareDataForHeatMap();

            this.Name = "HeatMap " + draw.outp.dirName;

            comboBox1.Items.Add(".....................");
            for (int i = 1; i < draw.outp.aux2.Count; i++)
                comboBox1.Items.Add(draw.outp.aux2[i]);

            if (comboBox1.Items.Count > 0)
                comboBox1.SelectedIndex = 0;
            else
                comboBox1.Visible = false;


            pictureBoxUpper.Refresh();
        }
        void PrepareBarRegions(Graphics g)
        {
            int x = 50, y = 0;
            bool regionTest = false;

            if (draw.upper.labColor == null || draw.upper.labColor.Keys.Count == 0)
                return;

            if (regionBarColor.Count > 0)
                regionTest = true;

            List<string> labKeys = new List<string>(draw.upper.labColor.Keys);
            Font drawFont = new System.Drawing.Font("Arial", 8);
            foreach (var item in labKeys)
            {
                if (!regionTest)
                {
                    Region reg = new Region(new Rectangle(x, y, 15, 10));
                    regionBarColor.Add(reg, new KeyValuePair<bool, string>(false, item));
                    SizeF textSize = g.MeasureString(item, drawFont);
                    x += 25 + (int)textSize.Width;
                    if (x > this.Width)
                    {
                        y += 150;
                        x = 25;
                    }
                }
            }
            foreach (var regItem in regionBarColor)
            {
                if (regItem.Value.Key)
                    draw.upper.labColor[regItem.Value.Value] = Color.Empty;
            }

        }
        Bitmap PaintUpper(Bitmap bit)
        {
                Graphics g = Graphics.FromImage(bit);
                Brush drawBrush = new System.Drawing.SolidBrush(Color.Black);
                g.Clear(Color.White);
            //selectedGeneNames = null;
            if (selectedGeneNames != null && showSelectedLabels)
            {

                FinSelectedGenesPositions();
                drawFont = new System.Drawing.Font("Arial", 22 , FontStyle.Bold);
                Pen p = new Pen(Color.Red,3);
                foreach (var item in selectedGeneNames)
                {
                    if (item.Value.Item2 >= 0)
                    {
                        SizeF ss = g.MeasureString(item.Value.Item1, drawFont);
                        g.TranslateTransform(item.Value.Item2, bit.Height);
                        g.RotateTransform(-45);

                        g.DrawString(item.Value.Item1, drawFont, drawBrush, 10, -15);
                        g.ResetTransform();
                        g.DrawEllipse(p, item.Value.Item2, bit.Height-5, 3, 3);

                    }
                }

                return bit;
            }
            if (showLabels == true)
            {

                upperLeaves = draw.auxUpper.GetLeaves();
                drawFont = new System.Drawing.Font("Arial", draw.upper.labelSize);
                foreach (var item in upperLeaves)
                {
                    SizeF ss = g.MeasureString(item.refStructure, drawFont);
                    g.TranslateTransform(item.gNode.x, pictureBoxUpper.Height);
                    g.RotateTransform(-45);

                    g.DrawString(item.refStructure, drawFont, drawBrush, 5, -5);
                    g.ResetTransform();
                }
            }
            
            if(!showLabels && !showSelectedLabels)
            {
                pictureBoxUpper.SizeMode = PictureBoxSizeMode.AutoSize;
                PrepareBarRegions(g);                
                //e.Graphics.Clear(pictureBox2.BackColor);

                draw.upper.DrawOnBuffer(bit, false, 3, Color.Empty);

                //                e.Graphics.DrawImage(draw.upperBitMap, 0, 0);
                if (cutHorizontalLine)
                {
                    Pen p = new Pen(Color.Red);
                    g.DrawLine(p, 0, mouseY, this.Width, mouseY);
                }

                //pictureBox1.Image = draw.upperBitMap;
                if (draw.upper.labColor != null && draw.upper.labColor.Count > 0)
                {
                    Font drawFont = new System.Drawing.Font("Arial", 8);
                    foreach (var item in regionBarColor)
                    {
                        RectangleF rec = item.Key.GetBounds(g);
                        drawBrush = new System.Drawing.SolidBrush(draw.upper.labColor[item.Value.Value]);
                        g.FillRectangle(drawBrush, rec.X, rec.Y, rec.Width, rec.Height);

                        SizeF textSize = g.MeasureString(item.Value.Value, drawFont);
                        drawBrush = new System.Drawing.SolidBrush(Color.Black);

                        g.DrawString(item.Value.Value, drawFont, drawBrush, rec.X + 20, rec.Y);
                        if (item.Value.Key)
                        {
                            Pen p = new Pen(Color.Black);
                            g.DrawLine(p, rec.X, rec.Y, rec.X + rec.Width, rec.Y + rec.Height);
                            g.DrawLine(p, rec.X + rec.Width, rec.Y, rec.X, rec.Y + rec.Height);
                        }
                    }

                }


            }


            return bit;
        }
        private void pictureBoxUpper_Paint(object sender, PaintEventArgs e)
        {
            Bitmap bit = PaintUpper(draw.upperBitMap);
            e.Graphics.DrawImage(bit, 0, 0);
        }
        Bitmap PaintLeft(Bitmap bit)
        {
            draw.left.posStart = 5;
            Graphics g = Graphics.FromImage(bit);
            g.Clear(Color.White);
            if(!showSelectedLabels)
                draw.left.DrawOnBuffer(bit, false, 3, Color.Empty);
            else
            {
                if (leftDescription != null && leftDescription.Count > 0)
                {
                    drawFont = new System.Drawing.Font("Arial", 20, FontStyle.Bold);
                    Pen p = new Pen(Color.Red, 3);
                    List<string> keys = new List<string>(leftDescription.Keys);
                    Brush drawBrush = new SolidBrush(Color.Black);
                    var format = new StringFormat() { Alignment = StringAlignment.Far,LineAlignment=StringAlignment.Center };
                    
                    foreach (var item in leftLeaves)
                    {
                        if (item.setStruct.Count < 200)
                            continue;
                        if (leftDescription[keys[0]].Item1.ContainsKey(item.refStructure))
                        {
                            SizeF ss = g.MeasureString(leftDescription[keys[0]].Item1[item.refStructure].Item2, drawFont);
                            var rect = new RectangleF(0,item.gNode.y-10, bit.Width,22);
                            g.DrawString(leftDescription[keys[0]].Item1[item.refStructure].Item2, drawFont, drawBrush, rect,format);
                            p.Color = leftDescription[keys[0]].Item1[item.refStructure].Item1;
                            //g.DrawEllipse(p, item.gNode.x+2, item.gNode.y, 3, 3);

                        }
                    }
                }
            }
            
            
            if (cutVerticalLine)
            {
                Pen p = new Pen(Color.Red);
                g.DrawLine(p, mouseX, 0, mouseX, this.Height);
            }
            return bit;
        }
        private void pictureBox3_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.Clear(this.BackColor);
            Bitmap bit = PaintLeft(draw.leftBitMap);
            e.Graphics.DrawImage(bit, 0, 0);
            //draw.leftBitMap.Save("test.jpg");
        }

        Bitmap PaintHeatMap(Bitmap bit)
        {
            Graphics g = Graphics.FromImage(bit);
            g.Clear(Color.White);
            draw.DrawHeatMapN(bit);


            if (interactiveLabelColumn.Checked || interactiveLabelRow.Checked)
            {
                string label = "";
                if (interactiveLabelColumn.Checked)
                {
                    if (mouseY > 10)
                        label = GetLabel(mouseX, mouseY, false, upperLeaves);

                }
                else
                if (interactiveLabelRow.Checked)
                {
                    if (mouseX > 10)
                        label = GetLabel(mouseX, mouseY, true, leftLeaves);
                }
               
                
                Size sizeOfText = TextRenderer.MeasureText(label, drawFont);
                Rectangle rect = new Rectangle(new Point(mouseX, mouseY), sizeOfText);
                g.FillRectangle(Brushes.White, rect);
                g.DrawString(label, drawFont, brush, mouseX, mouseY);
            }
            if (horizontalLines)
            {
                SolidBrush b = new SolidBrush(Color.White);
                Pen p = new Pen(b, 2);
                int max = int.MinValue;
                int min = int.MaxValue;
                foreach(var item in upperLeaves)
                {
                    if (item.gNode.x < min)
                        min = item.gNode.areaLeft;
                    if (item.gNode.x > max)
                        max = item.gNode.areaRight;
                }


                foreach (var item in leftLeaves)
                {
                    g.DrawLine(p,min,item.gNode.areaLeft, max,item.gNode.areaLeft);
                }
            }
                if (geneSelection != null && geneSelection.Count > 0)
            {
                Color newColor = Color.FromArgb(50, 255, 0, 0);
                Brush br = new SolidBrush(newColor);
                foreach (var item in upperLeaves)
                {
                    if (geneSelection.Contains(item.refStructure))
                    {
                        Rectangle r = new Rectangle(item.gNode.areaLeft, 0, item.gNode.areaRight - item.gNode.areaLeft, pictureBoxHeatMap.Height);
                        g.FillRectangle(br, r);
                    }
                }
            }
            return bit;
        }
        private void pictureBoxHeatMap_Paint(object sender, PaintEventArgs e)
        {
            Bitmap bit = PaintHeatMap(draw.heatMap);
            e.Graphics.DrawImage(bit, x_pos, y_pos);
            
        }
        void LeftClusterRearange()
        {
            MST ms = new MST();
            GraphMST gr = ms.CreateGraph(distM, leftLeaves);

            if (nodeForKruskal != null)
            {
                List<List<string>> kruskalOut = ms.Kruskal(gr, nodeForKruskal);

                remleftRoot = leftRoot;
                leftRoot = HashClusterDendrog.MakeDummyDendrog(kruskalOut).hNode;
                draw.leftNode = leftRoot;
                draw.left.rootNode = leftRoot;
                remAuxLeft = draw.auxLeft;
                draw.auxLeft = leftRoot;
                leftLeaves = leftRoot.GetLeaves();
                draw.left.PrepareGraphNodes(draw.leftBitMap, verticalCuttDistance);
                pictureBoxLeft.Refresh();
                pictureBoxHeatMap.Refresh();
            }

        }
        void LeftBackToHistogram()
        {
            if (remleftRoot != null)
            {
                leftRoot = remleftRoot;
                draw.leftNode = remleftRoot;
                draw.left.rootNode = remleftRoot;
                draw.auxLeft = remAuxLeft;
                leftLeaves = leftRoot.GetLeaves();
                draw.left.PrepareGraphNodes(draw.leftBitMap, verticalCuttDistance);

                pictureBoxLeft.Refresh();
                pictureBoxHeatMap.Refresh();
            }

        }
        void PaintLeftDescription(int widthB)
        {
            if (leftDescription != null && leftDescription.Count>0)
            {
                int width = widthB / leftDescription.Count;
                Tuple<Dictionary<string, Tuple<Color, string>>, List<ColorRect>> element;
                List<string> keys = new List<string>(leftDescription.Keys);
                for (int i = 0; i < keys.Count; i++)
                {
                    int pos = i * width;
                    Dictionary<string, Tuple<Color, string>> x = leftDescription[keys[i]].Item1;
                    if (avrRowAnnotations)
                        element = CalcLeftBarsClusterAvr(x, pos, width);
                    else
                        element = CalcLeftBars(x, pos, width);

                    leftDescription[keys[i]] = element;

                }
            }

        }

        private void HeatMap_ResizeEnd(object sender, EventArgs e)
        {

            if (!fullImage)
            {
                draw.upperBitMap = new Bitmap(pictureBoxUpper.Width, pictureBoxUpper.Height);
                draw.leftBitMap = new Bitmap(pictureBoxLeft.Width, pictureBoxLeft.Height);
                draw.heatMap = new Bitmap(pictureBoxUpper.Width, pictureBoxLeft.Height);
            }
            draw.left.PrepareGraphNodes(draw.leftBitMap, verticalCuttDistance);
            draw.upper.PrepareGraphNodes(draw.upperBitMap, horizontalCuttDistance);
            pictureBoxHeatMap.Refresh();
            pictureBoxUpper.Refresh();
            pictureBoxLeft.Refresh();
            PaintLeftDescription(pictureBox6.Width);
            pictureBox6.Refresh();
            pictureBox7.Refresh();

            //this.Invalidate();
        }
        Bitmap PaintLegend(Bitmap bit)
        {
            Graphics g = Graphics.FromImage(bit);
            g.Clear(Color.White);
            SolidBrush b = new SolidBrush(Color.Black);
            int xPos, yPos;
            xPos = 5; yPos = 5;
            System.Drawing.Font drawFont = new System.Drawing.Font("Arial", 10);
            System.Drawing.SolidBrush drawBrush = new System.Drawing.SolidBrush(System.Drawing.Color.Black);
            System.Drawing.StringFormat drawFormat = new System.Drawing.StringFormat();

            List<double> ordered = new List<double>(draw.profilesColorMap.Keys);
            ordered.Sort();
            foreach (var item in ordered)
            {
                b.Color = draw.profilesColorMap[item];
                g.FillRectangle(b, xPos, yPos, 15, 10);
                //e.Graphics.DrawString(item.ToString(), drawFont, drawBrush, xPos+25,yPos-3);
                g.DrawString(item.ToString(), drawFont, drawBrush, xPos + 25, yPos - 3);
                yPos += 25;
                if (yPos > pictureBox4.Height)
                {
                    yPos = 5;
                    xPos += 40;
                }
            }
            return bit;
        }
        private void pictureBox4_Paint(object sender, PaintEventArgs e)
        {
            Bitmap b = new Bitmap(pictureBox4.Width, pictureBox4.Height);
            PaintLegend(b);
            e.Graphics.DrawImage(b,0,0);
            //test.Paint();             
        }

        private void tableLayoutPanel1_Paint_1(object sender, PaintEventArgs e)
        {

        }

        private void pictureBoxUpper_MouseMove(object sender, MouseEventArgs e)
        {
            bool test = false;
            mouseY = e.Y;

            if (cutHorizontalLine)
                pictureBoxUpper.Refresh();
            else
            {

                if (colorNodeUpper != null)
                {
                    draw.upper.ChangeColors(colorNodeUpper, Color.Black);
                    test = true;
                }

                colorNodeUpper = draw.upper.FindClosestNode(e.X, e.Y);



                if (colorNodeUpper != null)
                {
                    draw.upper.ChangeColors(colorNodeUpper, Color.Red);
                    test = true;
                }
                if (test)
                {
                    Graphics g = Graphics.FromImage(draw.upperBitMap);
                    g.Clear(pictureBoxUpper.BackColor);

                    if (colorNodeUpper != null)
                    {
                        float v = ((float)colorNodeUpper.setStruct.Count) / draw.upperNode.setStruct.Count * 360;
                        g.FillPie(new SolidBrush(Color.Black), new Rectangle(e.X - 20, e.Y, 15, 15), v, 360 - v);
                        g.FillPie(new SolidBrush(Color.Red), new Rectangle(e.X - 20, e.Y, 15, 15), 0, v);
                    }
                    pictureBoxUpper.Refresh();

                }
            }
        }
        private void pictureBoxUpper_MouseClick(object sender, MouseEventArgs e)
        {
            switch (e.Button)
            {

                case MouseButtons.Left:

                    if (regionBarColor.Count > 0)
                    {
                        List<Region> l = new List<Region>(regionBarColor.Keys);
                        bool testFlag = false;
                        foreach (var item in l)
                            if (item.IsVisible(e.X, e.Y))
                            {
                                regionBarColor[item] = new KeyValuePair<bool, string>(!regionBarColor[item].Key, regionBarColor[item].Value);
                                draw.upper.labColor = new Dictionary<string, Color>();
                                foreach (var itemColor in labelToColor)
                                    draw.upper.labColor.Add(itemColor.Key, itemColor.Value);
                                testFlag = true;
                            }
                        if (testFlag)
                        {

                            pictureBoxUpper.Refresh();
                            return;
                        }
                    }
                    HClusterNode nodeC = colorNodeUpper;//upper.CheckClick(upper.rootNode,e.X,e.Y);
                    if (nodeC != null && nodeC.joined == null && !cutBranch)
                    {
                        TextBoxView rr = new TextBoxView(nodeC.setStruct);

                        rr.Show();

                        DrawPanel pn = new DrawPanel("Leave profile: " + nodeC.refStructure, null);
                        int tmp = pn.Height;
                        pn.Height = pn.Width;
                        pn.Width = tmp;
                        pn.CreateBMP();
                        List<HClusterNode> leftLeaves = draw.auxLeft.GetLeaves();
                        leftLeaves = leftLeaves.OrderByDescending(o => o.gNode.y).Reverse().ToList();
                        List<string> refList = new List<string>();
                        foreach (var item in leftLeaves)
                            refList.Add(item.refStructure);
                        pn.drawPic = delegate { draw.DrawHeatMapNode(pn.bmp, refList, nodeC.setStruct); pn.Text = nodeC.consistency.ToString(); };
                        pn.Show();
                    }
                    else
                    {
                        if (cutBranch)
                        {
                            cutBranch = false;
                            if (colorNodeUpper != null)
                            {
                                colorNodeUpper.MakeInvisible();
                                draw.upper.PrepareGraphNodes(draw.upperBitMap, horizontalCuttDistance);
                                pictureBoxUpper.Refresh();

                            }
                        }
                        else
                            if (colorNodeUpper != null && colorNodeUpper.joined != null)
                                draw.auxUpper = colorNodeUpper;
                    }
                    if (cutHorizontalLine)
                    {
                        draw.upper.MakeAllVisible();
                        cutHorizontalLine = !cutHorizontalLine;
                        double distScale = draw.upper.maxRealDist - draw.upper.minRealDist;
                        distScale /= 100;
                        horizontalCuttDistance = draw.upper.minRealDist + distScale * ((double)(draw.upper.maxGraphicsY - e.Y)) / draw.upper.maxGraphicsY;
                        draw.upper.MakeGraphInvisible(draw.upper.rootNode, horizontalCuttDistance);
                    }
                    label1.Text = "Gene clusters: " + draw.upper.rootNode.GetLeaves().Count;
                    break;
                case MouseButtons.Right:
                    if (!swap)
                    {
                        upperLeaves = draw.upperNode.GetLeaves();
                        draw.auxUpper = draw.upperNode;
                    } 
                    else
                    {
                        if (colorNodeUpper != null && colorNodeUpper.joined != null)
                        {
                            var aux = colorNodeUpper.joined[0];
                            colorNodeUpper.joined[0] = colorNodeUpper.joined[colorNodeUpper.joined.Count - 1];
                            colorNodeUpper.joined[colorNodeUpper.joined.Count - 1] = aux;
                        }
                    }
                    break;
            }
            if (draw.auxUpper != null)
            {
                if (colorNodeUpper != null)
                    draw.upper.ChangeColors(colorNodeUpper, Color.Black);
                colorNodeUpper = null;
                draw.upper.rootNode = draw.auxUpper;
                draw.upper.PrepareGraphNodes(draw.upperBitMap, horizontalCuttDistance);
                Graphics g = Graphics.FromImage(draw.upperBitMap);
                g.Clear(pictureBoxUpper.BackColor);
                pictureBoxUpper.Refresh();
                pictureBoxHeatMap.Refresh();
            }
        }

        private void pictureBox3_MouseMove(object sender, MouseEventArgs e)
        {
            bool test = false;
            mouseX = e.X;
            if (cutVerticalLine)
                pictureBoxLeft.Refresh();
            else
            {
                if (colorNodeLeft != null)
                {
                    test = true;
                    draw.left.ChangeColors(colorNodeLeft, Color.Black);
                }

                colorNodeLeft = draw.left.FindClosestNode(e.X, e.Y);


                if (colorNodeLeft != null)
                {
                    draw.left.ChangeColors(colorNodeLeft, Color.Red);
                    test = true;
                }
                if (test)
                {
                    Graphics g = Graphics.FromImage(draw.leftBitMap);
                    g.Clear(pictureBoxLeft.BackColor);

                    if (colorNodeLeft != null)
                    {
                        float v = ((float)colorNodeLeft.setStruct.Count) / draw.leftNode.setStruct.Count * 360;
                        g.FillPie(new SolidBrush(Color.Black), new Rectangle(e.X - 20, e.Y, 15, 15), v, 360 - v);
                        g.FillPie(new SolidBrush(Color.Red), new Rectangle(e.X - 20, e.Y, 15, 15), 0, v);
                    }
                    //g.DrawArc(new Pen(Color.DarkGreen),new Rectangle(e.X,e.Y,10,10), 0.0, v);
                    pictureBoxLeft.Refresh();
                }
            }

        }

        private void pictureBox3_MouseClick(object sender, MouseEventArgs e)
        {
            switch (e.Button)
            {

                case MouseButtons.Left:
                    HClusterNode nodeC = colorNodeLeft;//  left.CheckClick(left.rootNode,e.X,e.Y);

                    if (nodeC != null && nodeC.joined == null && !cutBranch)
                    {                        
                        nodeForKruskal = nodeC;
                        return;
                    }
                    else                    
                     if (cutBranch)
                    {
                        cutBranch = false;
                        if (colorNodeLeft != null)
                        {
                            colorNodeLeft.MakeInvisible();
                            draw.left.PrepareGraphNodes(draw.leftBitMap,verticalCuttDistance);
                            pictureBoxLeft.Refresh();

                        }
                    }
                    else
                        if (colorNodeLeft != null && colorNodeLeft.joined != null)
                            draw.auxLeft = colorNodeLeft;

                    if (cutVerticalLine)
                    {
                        cutVerticalLine = !cutVerticalLine;
                        double distScale = draw.left.maxRealDist - draw.left.minRealDist;
                        distScale /= 100;
                        verticalCuttDistance = draw.left.minRealDist + distScale * ((double)(draw.left.maxGraphicsX - e.X)) / draw.left.maxGraphicsX;
                    }
                    label2.Text = "Sample clusters: " + draw.left.rootNode.GetLeaves().Count;                    
                    break;
                case MouseButtons.Right:
                    if(!swap)
                        draw.auxLeft = draw.leftNode;
                    else
                    {
                        if (colorNodeLeft == null)
                            colorNodeLeft=draw.leftNode;
                        if(colorNodeLeft!=null && colorNodeLeft.joined!=null)
                        {
                            var aux = colorNodeLeft.joined[0];
                            colorNodeLeft.joined[0] = colorNodeLeft.joined[colorNodeLeft.joined.Count - 1];
                            colorNodeLeft.joined[colorNodeLeft.joined.Count - 1] = aux;
                        }
                        
                    }
                    break;
            }
            if (draw.auxLeft != null)
            {
                if (colorNodeLeft != null)
                    draw.left.ChangeColors(colorNodeLeft, Color.Black);
                colorNodeLeft = null;
                draw.left.rootNode = draw.auxLeft;
                leftLeaves = draw.left.rootNode.GetLeaves();
                draw.left.PrepareGraphNodes(draw.leftBitMap, verticalCuttDistance);
                Graphics g = Graphics.FromImage(draw.leftBitMap);
                g.Clear(pictureBoxLeft.BackColor);
                pictureBoxLeft.Refresh();
                pictureBoxHeatMap.Refresh();
                PaintLeftDescription(pictureBox6.Width);
                pictureBox6.Refresh();
                pictureBox7.Refresh();

            }
        }

        private void labels_Click(object sender, EventArgs e)
        {


        }

        private void toolStripLabel1_Click(object sender, EventArgs e)
        {
            showLabels = !showLabels;
            draw.upper.showLabels = showLabels;
            numericUpDown1.Visible = showLabels;
            numericUpDown1.Value = draw.upper.labelSize;
            showSelectedLabels = false;
            //draw.left.showLabels = showLabels;
            pictureBoxUpper.Refresh();
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            draw.left.labelSize = (int)numericUpDown1.Value;
            draw.upper.labelSize = (int)numericUpDown1.Value;

            Graphics g = Graphics.FromImage(draw.upperBitMap);
            g.Clear(pictureBoxUpper.BackColor);
            g = Graphics.FromImage(draw.leftBitMap);
            g.Clear(pictureBoxLeft.BackColor);

            pictureBoxLeft.Refresh();
            pictureBoxUpper.Refresh();

        }

        private void pictureBoxUpper_MouseLeave(object sender, EventArgs e)
        {
            if (colorNodeUpper != null)
            {
                draw.upper.ChangeColors(colorNodeUpper, Color.Black);
                colorNodeUpper = null;
                Graphics g = Graphics.FromImage(draw.upperBitMap);
                g.Clear(pictureBoxUpper.BackColor);
                pictureBoxUpper.Refresh();
            }
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            DialogResult res = saveFileDialog1.ShowDialog();
            if (res == DialogResult.OK)
            {
                string fileName = saveFileDialog1.FileName;
                List<List<string>> clusters = new List<List<string>>();
                List<HClusterNode> nodes = draw.leftNode.GetLeaves();
                List<double> cons = new List<double>();
                foreach (var item in nodes)
                {
                    clusters.Add(item.setStruct);
                    cons.Add(item.consistency);
                }

                StreamWriter wS = new StreamWriter(fileName + "_gene_microclusters.dat");
                ClusterOutput.Save(clusters, cons, wS, true);
                wS.Close();

                clusters.Clear();
                cons = new List<double>();
                nodes = draw.upperNode.GetLeaves();
                foreach (var item in nodes)
                {
                    clusters.Add(item.setStruct);
                    cons.Add(item.consistency);
                }

                wS = new StreamWriter(fileName + "_sample_microclusters.dat");
                ClusterOutput.Save(clusters, cons, wS, true);
                wS.Close();


            }
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }

        private void pictureBox3_MouseLeave(object sender, EventArgs e)
        {
            if (colorNodeLeft != null)
            {
                draw.left.ChangeColors(colorNodeLeft, Color.Black);
                colorNodeLeft = null;
                Graphics g = Graphics.FromImage(draw.leftBitMap);
                g.Clear(pictureBoxLeft.BackColor);

                //g.DrawArc(new Pen(Color.DarkGreen),new Rectangle(e.X,e.Y,10,10), 0.0, v);
                pictureBoxLeft.Refresh();
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox1.SelectedItem == null)
                return;
            if (comboBox1.SelectedItem.ToString().Contains("..."))
            {
                draw.upper.rootNode.ClearColors(Color.Black);
            }
            else
            {
                List<HClusterNode> leaveas = draw.upperNode.GetLeaves();
                int index = 1;
                labelToColor.Clear();
                regionBarColor.Clear();
                foreach (var item in leaveas)
                {
                    if (item.refStructure.Contains("G2M"))
                        Console.WriteLine();
                    if (item.refStructure.Contains(comboBox1.SelectedItem.ToString()))
                    {
                        draw.upper.ChangeColors(item, Color.Red);
                        break;
                    }
                }
            }
            pictureBoxUpper.Refresh();
        }

        private void panel2_Scroll(object sender, ScrollEventArgs e)
        {
            x_pos = -e.NewValue;
            pictureBoxHeatMap.Invalidate();
        }

        private void panel3_Scroll(object sender, ScrollEventArgs e)
        {

            if (e.ScrollOrientation == ScrollOrientation.VerticalScroll)
            {
                y_pos = -e.NewValue;
                pictureBoxHeatMap.Invalidate();
            }
            pictureBoxLeft.Invalidate();

        }

        private void toolStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            cutVerticalLine = !cutVerticalLine;
        }

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            cutVerticalLine = !cutVerticalLine;
        }

        private void vScrollBar1_ValueChanged(object sender, EventArgs e)
        {
            draw.left.clusterSize = vScrollBar1.Value;
            label3.Text = vScrollBar1.Value.ToString();
            pictureBoxLeft.Invalidate();
        }

        private void interactiveLabelColumnToolStripMenuItem_Click(object sender, EventArgs e)
        {
            interactiveLabelColumn.Checked = !interactiveLabelColumn.Checked;
            if (interactiveLabelColumn.Checked)
                interactiveLabelRow.Checked = false;

        }

        private void interactiveLabelRowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            interactiveLabelRow.Checked = !interactiveLabelRow.Checked;
            if (interactiveLabelRow.Checked)
                interactiveLabelColumn.Checked = false;
        }
        string GetLabel(int x, int y, bool row, List<HClusterNode> nodes)
        {
            int pos = y;
            if (!row)
                pos = x;

            foreach (var item in nodes)
            {
                if (pos >= item.gNode.areaLeft && pos < item.gNode.areaRight)
                {
                    return item.refStructure;
                }
            }
            return "";
        }

        private void pictureBoxHeatMap_MouseMove(object sender, MouseEventArgs e)
        {
            if (interactiveLabelColumn.Checked || interactiveLabelRow.Checked)
            {
                mouseX = e.X + 5;
                mouseY = e.Y;
                pictureBoxHeatMap.Invalidate();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            splitContainer1.SplitterDistance += 20;
            draw.leftBitMap = new Bitmap(pictureBoxLeft.Width, pictureBoxLeft.Height);
            draw.heatMap = new Bitmap(pictureBoxUpper.Width, pictureBoxLeft.Height);
            draw.left.PrepareGraphNodes(draw.leftBitMap);

            pictureBoxLeft.Invalidate();
            pictureBoxUpper.Invalidate();

        }


        Dictionary<string, Tuple<Color, string>> ReadClusterFile(string fileName)
        {
            StreamReader r = new StreamReader(fileName);
            Dictionary<string, Tuple<Color, string>> clDic = new Dictionary<string, Tuple<Color, string>>();
            Dictionary<string, string> labels = new Dictionary<string, string>();
            HashSet<string> distinc = new HashSet<string>();
            Dictionary<string, Color> dicColor = new Dictionary<string, Color>();
            string line = r.ReadLine();
            bool colorDef = false;
            bool dataDef = false;
            while (line != null)
            {
                string[] tmp = line.Split(new char[] { ' ', '\t' });
                if(colorDef)
                {
                    if(tmp.Length==2)
                        dicColor.Add(tmp[0], ColorTranslator.FromHtml(tmp[1]));
                }
                if(dataDef)
                {
                    if (tmp.Length == 2)
                        labels.Add(tmp[0], tmp[1]);
                }
                if (line.Contains("[COLOR]"))
                {
                    colorDef = true;
                    dataDef = false;
                }
                if(line.Contains("[DATA]"))
                {
                    colorDef = false;
                    dataDef = true;
                }    

                line = r.ReadLine();
            }
            r.Close();

            foreach (var item in labels)
                clDic.Add(item.Key, new Tuple<Color, string>(dicColor[item.Value], item.Value));

            return clDic;
        }
        Tuple<Dictionary<string, Tuple<Color, string>>, List<ColorRect>> CalcLeftBarsClusterAvr(Dictionary<string, Tuple<Color, string>> colorMap, int pos = 0, int width = 20)
        {
            List<ColorRect> r = new List<ColorRect>();
            foreach (var item in leftLeaves)
            {
                float startPos = item.gNode.areaLeft;
                float step = ((float)(item.gNode.areaRight - item.gNode.areaLeft)) / item.setStruct.Count;
                List<Color> temp = new List<Color>();
                for (int i = 0; i < item.setStruct.Count; i++)
                    if (colorMap.ContainsKey(item.setStruct[i]))
                        temp.Add(colorMap[item.setStruct[i]].Item1);

                Dictionary<Color, int> freq = new Dictionary<Color, int>();
                foreach (var it in temp)
                {
                    if (!freq.ContainsKey(it))
                        freq.Add(it, 0);

                    freq[it]++;
                }
                List<KeyValuePair<Color, int>> mappings = freq.ToList();
                if (mappings.Count > 0)
                {
                    mappings.Sort((x, y) => x.Value.CompareTo(y.Value));
                    ColorRect z = new ColorRect();
                    z.sY = item.gNode.areaLeft;
                    z.sX = 0 + pos;
                    z.width = width;
                    z.height = item.gNode.areaRight - item.gNode.areaLeft;
                    z.c = mappings[mappings.Count - 1].Key;
                    r.Add(z);
                }
            }
            return new Tuple<Dictionary<string, Tuple<Color, string>>, List<ColorRect>>(colorMap, r);
        }

        Tuple<Dictionary<string, Tuple<Color, string>>, List<ColorRect>> CalcLeftBars(Dictionary<string, Tuple<Color, string>> colorMap, int pos = 0, int width = 20)
        {
            List<ColorRect> r = new List<ColorRect>();
            foreach (var item in leftLeaves)
            {
                float startPos = item.gNode.areaLeft;
                float step = ((float)(item.gNode.areaRight - item.gNode.areaLeft)) / item.setStruct.Count;
                List<ColorRect> temp = new List<ColorRect>();
                for (int i = 0; i < item.setStruct.Count; i++)
                    if (colorMap.ContainsKey(item.setStruct[i]))
                    {
                        ColorRect x = new ColorRect();
                        x.sX = 0 + pos;
                        x.sY = startPos + step * i;
                        x.width = width;
                        x.height = step;

                        x.c = colorMap[item.setStruct[i]].Item1;
                        x.text = colorMap[item.setStruct[i]].Item2;
                        //r.Add(x);
                        temp.Add(x);
                    }
                //  if (step < 1)
                {
                    HashSet<int> ss = new HashSet<int>();
                    foreach (var it in temp)
                    {
                        ss.Add((int)it.sY);
                    }

                    Dictionary<int, Dictionary<Color, int>> freq = new Dictionary<int, Dictionary<Color, int>>();
                    foreach (var it in temp)
                    {
                        if (!freq.ContainsKey((int)Math.Floor(it.sY)))
                            freq.Add((int)Math.Floor(it.sY), new Dictionary<Color, int>());

                        if (!freq[(int)Math.Floor(it.sY)].ContainsKey(it.c))
                            freq[(int)Math.Floor(it.sY)].Add(it.c, 1);
                        else
                            freq[(int)Math.Floor(it.sY)][it.c]++;
                    }
                    foreach (var it in freq.Keys)
                    {
                        
                        List<KeyValuePair<Color, int>> mappings = freq[it].ToList();
                        KeyValuePair<Color, int> gkeyPair = mappings.OrderByDescending(MaxGuid => MaxGuid.Value).First();
                        double colorBrihtness = 0;
                        foreach(var am in mappings)                        
                            colorBrihtness += am.Value;
                        colorBrihtness =gkeyPair.Value/colorBrihtness;
                        if (mappings.Count > 0)
                        {
                            mappings.Sort((x, y) => x.Value.CompareTo(y.Value));
                            for (int i = 0; i < temp.Count; i++)
                                if ((int)Math.Floor(temp[i].sY) == it)
                                {
                                    Color col= mappings[mappings.Count - 1].Key;
                                    int re = col.R - System.Convert.ToInt32(colorBrihtness / (double)100 * col.R);
                                    int gr = col.G - System.Convert.ToInt32(colorBrihtness / (double)100 * col.G);
                                    int bl = col.B - System.Convert.ToInt32(colorBrihtness / (double)100 * col.B);
                                    //temp[i].c = mappings[mappings.Count - 1].Key;
                                    temp[i].c = Color.FromArgb(255,re, gr, bl);
                                    temp[i].height = 1;
                                    if (step > 1)
                                        temp[i].height = step;

                                    temp[i].sY = (int)Math.Floor(temp[i].sY);
                                    r.Add(temp[i]);
                                    break;
                                }
                        }
                    }

                }
                //else
                //    r.Add(temp[0]);


            }
            return new Tuple<Dictionary<string, Tuple<Color, string>>, List<ColorRect>>(colorMap, r);
        }
        private void rowAnnotationClick(object sender, EventArgs e)
        {
            DialogResult res = openFileDialog1.ShowDialog();
            if (res == DialogResult.OK)
            {
                checkBox1.Visible = true;
                if (leftDescription == null)
                    leftDescription = new Dictionary<string, Tuple<Dictionary<string, Tuple<Color, string>>, List<ColorRect>>>();
                var colorMap = ReadClusterFile(openFileDialog1.FileName);
                int pos = 1;
                if (leftDescription.Count > 0)
                    pos = 35;

                foreach (var item in leftDescription)
                {
                    pos += GetDistinctColors(item.Value).Count * 20;
                }
                string NameT = "Name" + rand.Next();
                leftDescription.Add(NameT, new Tuple<Dictionary<string, Tuple<Color, string>>, List<ColorRect>>(colorMap, null));
                LocalButton b = new LocalButton();

                b.Location = new Point(1, pos);
                b.Text = "X";
                b.Size = new Size(10, 10);
                PaintLeftDescription(pictureBox6.Width);
                pictureBox6.Refresh();
                pictureBox7.Refresh();
                b.tupleName = NameT;
                b.Click += (s, ev) =>
                {
                    leftDescription.Remove(((LocalButton)s).tupleName);
                    pictureBox7.Controls.Remove((LocalButton)s);
                    pictureBox6.Invalidate();
                    pictureBox7.Invalidate();
                };
                pictureBox7.Controls.Add(b);
            }
        }
        Bitmap PaintLeftAnnotation(Bitmap bit)
        {
            if (leftDescription != null)
            {
                Graphics g = Graphics.FromImage(bit);
                g.Clear(Color.White);
                Pen p = new Pen(Color.White,2);
                foreach (var item in leftDescription)
                {
                    float lastX=0;
                    float lastY=0;
                    foreach (var it in item.Value.Item2)
                    {
                        Brush b = new SolidBrush(it.c);
                        g.FillRectangle(b, it.sX, it.sY, it.width, it.height);
                        lastX = it.sX;
                        lastY = it.sY + it.height;
                    }
                    if(leftDescription.Count>1)
                    {
                        g.DrawLine(p, lastX, 0, lastX, lastY);
                    }
                }
                if (labelNum.Length > 0)
                {
                    Size sizeOfText = TextRenderer.MeasureText(labelNum, drawFont);
                    Rectangle rect = new Rectangle(new Point(mouseX + 5, mouseY + 5), sizeOfText);
                    g.FillRectangle(Brushes.White, rect);
                    g.DrawString(labelNum, drawFont, brush, mouseX + 5, mouseY + 5);
                }

            }
            return bit;

        }        
        private void pictureBox6_Paint(object sender, PaintEventArgs e)
        {
            Bitmap annotation = new Bitmap(pictureBox6.Width, pictureBox6.Height);
            Bitmap bit =PaintLeftAnnotation(annotation);
            e.Graphics.DrawImage(bit, 0, 0);
        }

        private void pictureBox6_MouseMove(object sender, MouseEventArgs e)
        {
            /*if (leftDescription != null)
            {
                mouseX = e.X + 5;
                mouseY = e.Y;

                foreach (var item in leftDescription)
                {
                    for (int j = 0; j < item.Value.Item2.Count; j++)
                    {
                        ColorRect r = item.Value.Item2[j];
                        if (r.text != null)
                            if (e.X >= r.sX && e.X <= r.sX + r.width)
                                if (e.Y >= r.sY && e.Y <= r.sY + r.height)
                                    labelNum = r.text.ToString();
                    }
                }
                pictureBox6.Invalidate();
            }*/
        }

        private void selectGenesToolStripMenuItem_Click(object sender, EventArgs e)
        {

            List<string> genes = new List<string>();
            foreach (var item in upperLeaves)
                genes.Add(item.refStructure);
            GeneSelection sel = new GeneSelection(genes);
            DialogResult res = sel.ShowDialog();
            if (res == DialogResult.OK)
            {
                geneSelection = sel.selection;
                pictureBoxHeatMap.Invalidate();
            }
        }
        Dictionary<string, Color> GetDistinctColors(Tuple<Dictionary<string, Tuple<Color, string>>, List<ColorRect>> item)
        {
            Dictionary<string, Color> x = new Dictionary<string, Color>();

            foreach (var it in item.Item1)
            {
                var y = it.Value;
                if (!x.ContainsKey(y.Item2.ToString()))
                {
                    x.Add(y.Item2.ToString(), y.Item1);
                }
            }
            return x;
        }
        Bitmap PaintRightAnnotation(Bitmap bit)
        {
            Dictionary<string, Color> x = new Dictionary<string, Color>();
            if (leftDescription != null)
            {
                Graphics g = Graphics.FromImage(bit);
                g.Clear(Color.White);
                int posy = 15;
                foreach (var item in leftDescription)
                {
                        x = GetDistinctColors(item.Value);

                    int posx = 0;
                    Brush stringB = new SolidBrush(Color.Black);
                    List<string> keys = new List<string>(x.Keys);

                    if (showSelectedLabels)
                    {
                        if (keys.Count != 3)
                            continue;
                    }


                    try
                    {
                        if(keys.Count==3)
                        {
                            keys = new List<string>();
                            keys.Add("Stage1");
                            keys.Add("Stage3");
                            keys.Add("Metastatic");
                        }
                        else
                            keys.Sort((a, b) => Convert.ToInt32(a).CompareTo(Convert.ToInt32(b)));
                    }
                    catch (Exception ex)
                    {
                        keys.Sort();
                    }
                        foreach (var it in keys)
                        {
                            Brush b = new SolidBrush(x[it]);

                            g.FillRectangle(b, posx, posy, 10, 10);
                            g.DrawString(it, drawFont, stringB, posx + 10, posy - 3);
                            posy += 20;
                        }
                        //posx += 21;
                        posy += 40;
                }
            }
            return bit;

        }
        private void pictureBox7_Paint(object sender, PaintEventArgs e)
        {
            
            Bitmap bit= new Bitmap(pictureBox7.Width, pictureBox7.Height);
            Graphics g = Graphics.FromImage(bit);
            g.Clear(Color.White);
            PaintRightAnnotation(bit);
            e.Graphics.DrawImage(bit, 0, 0);
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked)
                avrRowAnnotations = true;
            else
                avrRowAnnotations = false;

            PaintLeftDescription(pictureBox6.Width);
            pictureBox6.Refresh();
            pictureBox7.Refresh();
        }

        private void histogramsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PlotForm pHist = new PlotForm(FilterOmics.memoryFilteredData);
            pHist.Show();
        }

        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            cutHorizontalLine = !cutHorizontalLine;
        }


        private void HeatMap_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                cutHorizontalLine = false;
                cutVerticalLine = false;
            }
        }

        private void pictureBoxUpper_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
                cutHorizontalLine = false;
        }
        class ColorRect
        {
            public float sX;
            public float sY;
            public float width;
            public float height;
            public Color c;
            public string text;
        };

        class ColorDist
        {
            public static Color[] colorTable;
            static string[] strColor = new string[]{ "#000000", "#FFFF00", "#1CE6FF", "#FF34FF", "#FF4A46", "#008941", "#006FA6", "#A30059",
        "#FFDBE5", "#7A4900", "#0000A6", "#63FFAC", "#B79762", "#004D43", "#8FB0FF", "#997D87",
        "#5A0007", "#809693", "#FEFFE6", "#1B4400", "#4FC601", "#3B5DFF", "#4A3B53", "#FF2F80",
        "#61615A", "#BA0900", "#6B7900", "#00C2A0", "#FFAA92", "#FF90C9", "#B903AA", "#D16100",
        "#DDEFFF", "#000035", "#7B4F4B", "#A1C299", "#300018", "#0AA6D8", "#013349", "#00846F",
        "#372101", "#FFB500", "#C2FFED", "#A079BF", "#CC0744", "#C0B9B2", "#C2FF99", "#001E09",
        "#00489C", "#6F0062", "#0CBD66", "#EEC3FF", "#456D75", "#B77B68", "#7A87A1", "#788D66",
        "#885578", "#FAD09F", "#FF8A9A", "#D157A0", "#BEC459", "#456648", "#0086ED", "#886F4C",

        "#34362D", "#B4A8BD", "#00A6AA", "#452C2C", "#636375", "#A3C8C9", "#FF913F", "#938A81",
        "#575329", "#00FECF", "#B05B6F", "#8CD0FF", "#3B9700", "#04F757", "#C8A1A1", "#1E6E00",
        "#7900D7", "#A77500", "#6367A9", "#A05837", "#6B002C", "#772600", "#D790FF", "#9B9700",
        "#549E79", "#FFF69F", "#201625", "#72418F", "#BC23FF", "#99ADC0", "#3A2465", "#922329",
        "#5B4534", "#FDE8DC", "#404E55", "#0089A3", "#CB7E98", "#A4E804", "#324E72", "#6A3A4C",
        "#83AB58", "#001C1E", "#D1F7CE", "#004B28", "#C8D0F6", "#A3A489", "#806C66", "#222800",
        "#BF5650", "#E83000", "#66796D", "#DA007C", "#FF1A59", "#8ADBB4", "#1E0200", "#5B4E51",
        "#C895C5", "#320033", "#FF6832", "#66E1D3", "#CFCDAC", "#D0AC94", "#7ED379", "#012C58",

        "#7A7BFF", "#D68E01", "#353339", "#78AFA1", "#FEB2C6", "#75797C", "#837393", "#943A4D",
        "#B5F4FF", "#D2DCD5", "#9556BD", "#6A714A", "#001325", "#02525F", "#0AA3F7", "#E98176",
        "#DBD5DD", "#5EBCD1", "#3D4F44", "#7E6405", "#02684E", "#962B75", "#8D8546", "#9695C5",
        "#E773CE", "#D86A78", "#3E89BE", "#CA834E", "#518A87", "#5B113C", "#55813B", "#E704C4",
        "#00005F", "#A97399", "#4B8160", "#59738A", "#FF5DA7", "#F7C9BF", "#643127", "#513A01",
        "#6B94AA", "#51A058", "#A45B02", "#1D1702", "#E20027", "#E7AB63", "#4C6001", "#9C6966",
        "#64547B", "#97979E", "#006A66", "#391406", "#F4D749", "#0045D2", "#006C31", "#DDB6D0",
        "#7C6571", "#9FB2A4", "#00D891", "#15A08A", "#BC65E9", "#FFFFFE", "#C6DC99", "#203B3C",

        "#671190", "#6B3A64", "#F5E1FF", "#FFA0F2", "#CCAA35", "#374527", "#8BB400", "#797868",
        "#C6005A", "#3B000A", "#C86240", "#29607C", "#402334", "#7D5A44", "#CCB87C", "#B88183",
        "#AA5199", "#B5D6C3", "#A38469", "#9F94F0", "#A74571", "#B894A6", "#71BB8C", "#00B433",
        "#789EC9", "#6D80BA", "#953F00", "#5EFF03", "#E4FFFC", "#1BE177", "#BCB1E5", "#76912F",
        "#003109", "#0060CD", "#D20096", "#895563", "#29201D", "#5B3213", "#A76F42", "#89412E",
        "#1A3A2A", "#494B5A", "#A88C85", "#F4ABAA", "#A3F3AB", "#00C6C8", "#EA8B66", "#958A9F",
        "#BDC9D2", "#9FA064", "#BE4700", "#658188", "#83A485", "#453C23", "#47675D", "#3A3F00",
        "#061203", "#DFFB71", "#868E7E", "#98D058", "#6C8F7D", "#D7BFC2", "#3C3E6E", "#D83D66",

        "#2F5D9B", "#6C5E46", "#D25B88", "#5B656C", "#00B57F", "#545C46", "#866097", "#365D25",
        "#252F99", "#00CCFF", "#674E60", "#FC009C", "#92896B"};
            static ColorDist()
            {
                colorTable = new Color[strColor.Length];
                for (int i = 0; i < colorTable.Length; i++)
                    colorTable[i] = ColorTranslator.FromHtml(strColor[i]);
            }
        };
        class LocalButton : Button
        {
            public string tupleName = "";

            private void InitializeComponent()
            {
                this.SuspendLayout();
                this.ResumeLayout(false);

            }
        }

        private void ClearDistance_Click(object sender, EventArgs e)
        {
            cutHorizontalLine = false;
            cutVerticalLine = false;
            horizontalCuttDistance = double.MaxValue;
            verticalCuttDistance = double.MaxValue;
            draw.auxUpper.ClearColors(Color.Black);
            draw.auxLeft.ClearColors(Color.Black);
            draw.upper.MakeAllVisible();
            draw.left.MakeAllVisible();
            draw.upper.PrepareGraphNodes(draw.upperBitMap, horizontalCuttDistance);
            draw.left.PrepareGraphNodes(draw.leftBitMap, verticalCuttDistance);
            pictureBoxLeft.Refresh();
            pictureBoxUpper.Refresh();
            pictureBoxHeatMap.Refresh();
        }

        private void toolStripButton4_Click(object sender, EventArgs e)
        {
            cutBranch = true;
        }

        private void toolStripButton5_Click(object sender, EventArgs e)
        {
           
            //draw.upper.MakeAllVisible();
            upperLeaves = draw.auxUpper.GetLeaves();
            double threshold = Convert.ToDouble(textBox1.Text);
            foreach (var up in upperLeaves)                
            {
                up.visible = true;
                bool test = false;
                foreach (var item in leftLeaves)
                {
                    if (item.setStruct.Count > vScrollBar1.Value)
                    {
                        double res = draw.KLIndex(item, up);
                        if (res > threshold)
                        {
                            test = true;
                            break;
                        }
                    }
                }
                    if (!test)
                        up.visible = false;
                
            }
            
            draw.upper.PrepareGraphNodes(draw.upperBitMap, horizontalCuttDistance);
            pictureBoxUpper.Refresh();
            pictureBoxHeatMap.Refresh();
        }

        private void kruskalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LeftClusterRearange();
            PaintLeftDescription(pictureBox6.Width);
            pictureBox6.Refresh();
            backToHistogramToolStripMenuItem.Enabled = true;
            kruskalToolStripMenuItem.Enabled = false;
            minClusterDistanceToolStripMenuItem.Enabled = true;
        }

        private void backToHistogramToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LeftBackToHistogram();
            PaintLeftDescription(pictureBox6.Width);
            pictureBox6.Refresh();
            backToHistogramToolStripMenuItem.Enabled = false;
            kruskalToolStripMenuItem.Enabled = true;
            minClusterDistanceToolStripMenuItem.Enabled = true;

        }

        private void minClusterDistanceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MST ms = new MST();
            GraphMST gr = ms.CreateGraph(distM, leftLeaves);

            if (nodeForKruskal != null)
            {
                List<List<string>> kruskalOut = ms.Closest(nodeForKruskal).GetClusters();

                remleftRoot = leftRoot;
                leftRoot = HashClusterDendrog.MakeDummyDendrog(kruskalOut).hNode;
                draw.leftNode = leftRoot;
                draw.left.rootNode = leftRoot;
                remAuxLeft = draw.auxLeft;
                draw.auxLeft = leftRoot;
                leftLeaves = leftRoot.GetLeaves();
                draw.left.PrepareGraphNodes(draw.leftBitMap, verticalCuttDistance);
                pictureBoxLeft.Refresh();
                pictureBoxHeatMap.Refresh();

                PaintLeftDescription(pictureBox6.Width);
                pictureBox6.Refresh();
                backToHistogramToolStripMenuItem.Enabled = true;
                minClusterDistanceToolStripMenuItem.Enabled = false;
                kruskalToolStripMenuItem.Enabled = true;

            }

        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            saveFileDialog1.DefaultExt = "png";
            saveFileDialog1.Filter = "Png files|*.png";
            DialogResult res = saveFileDialog1.ShowDialog();
            if (res == DialogResult.OK && saveFileDialog1.FileName.Length > 0)
            {
                Resolution resForm = new Resolution(this.Width, this.Height, Color.Black);
                res = resForm.ShowDialog();
                if (res == DialogResult.OK)
                {


                     double h = ((double)pictureBoxUpper.Height) / (pictureBoxUpper.Height + pictureBoxHeatMap.Height);
                    Bitmap bmp = new Bitmap(resForm.WidthR, resForm.HeightR);

                    byte[] resultingBytes;
                    using (var stream = new MemoryStream())
                    using (var graphicsFromSizedImage = Graphics.FromImage(bmp))
                    using (var metafile = new Metafile(stream, graphicsFromSizedImage.GetHdc()))
                    {
                        int height = (int)(bmp.Height * h);
                        double w = ((double)pictureBoxLeft.Width) / (pictureBoxLeft.Width + pictureBoxHeatMap.Width + pictureBox6.Width + pictureBox7.Width);
                        double annot = ((double)pictureBox6.Width) / (pictureBoxLeft.Width + pictureBoxHeatMap.Width + pictureBox6.Width + pictureBox7.Width);
                        double annotRight = ((double)pictureBox7.Width) / (pictureBoxLeft.Width + pictureBoxHeatMap.Width + pictureBox6.Width + pictureBox7.Width);
                        int width = (int)(bmp.Width * w);
                        int annotWidth = (int)(bmp.Width * annot);
                        int rightAnnotWidth = (int)(bmp.Width * annotRight);
                        Bitmap upperBitMap = new Bitmap(bmp.Width - width - annotWidth - rightAnnotWidth, height);
                        Bitmap leftBitMap = new Bitmap(width, bmp.Height - height);
                        Bitmap heatMap = new Bitmap(upperBitMap.Width, leftBitMap.Height);
                        Bitmap leftAnnotation = new Bitmap(annotWidth, leftBitMap.Height);
                        Bitmap rightAnnotation = new Bitmap(rightAnnotWidth, leftBitMap.Height);
                        Bitmap legend = new Bitmap(leftBitMap.Width, upperBitMap.Height);
                        draw.left.PrepareGraphNodes(leftBitMap, verticalCuttDistance);
                        draw.upper.PrepareGraphNodes(upperBitMap, horizontalCuttDistance);

                        PaintLeft(leftBitMap);
                        PaintUpper(upperBitMap);
                        PaintHeatMap(heatMap);
                        PaintLeftDescription(leftAnnotation.Width);
                        PaintLeftAnnotation(leftAnnotation);
                        PaintRightAnnotation(rightAnnotation);
                        PaintLegend(legend);
                        Graphics g;

                        //if (saveFileDialog1.FileName.Contains(".emf"))
                        // g = Graphics.FromImage(metafile);
                        //else
                        //  g = Graphics.FromImage(bmp);
                        using (saveFileDialog1.FileName.Contains(".emf") ? g = Graphics.FromImage(metafile): g = Graphics.FromImage(bmp))
                        {
                            g.Clear(Color.White);

                            g.DrawImage(upperBitMap, width + annotWidth, 0);
                            g.DrawImage(leftBitMap, 0, height);
                            g.DrawImage(leftAnnotation, width, height);
                            g.DrawImage(heatMap, width + annotWidth, height);
                            g.DrawImage(rightAnnotation, width + annotWidth + heatMap.Width, height);

                        }

                        if (saveFileDialog1.FileName.Contains(".emf"))
                        {
                            graphicsFromSizedImage.ReleaseHdc();
                            resultingBytes = stream.ToArray();
                            File.WriteAllBytes(saveFileDialog1.FileName, resultingBytes);
                        }
                        else
                            bmp.Save(saveFileDialog1.FileName, ImageFormat.Jpeg);
                    }
                    //g.DrawImage(legend, 0, 0);
                    //
                    //drawH.PrepareGraphNodes(bmp);
                    //draw.upperBitMap(bmp, resForm.ShowLegend, resForm.LineThickness, resForm.LinesColor);
                    //SavePicture(saveFileDialog1.FileName, bmp);
                    //drawH.PrepareGraphNodes(buffer);
                    draw.left.PrepareGraphNodes(draw.leftBitMap, verticalCuttDistance);
                    draw.upper.PrepareGraphNodes(draw.upperBitMap, horizontalCuttDistance);

                }
                //       this.SavePicture(saveFileDialog1.FileName);          
            }

        }

        private void LongLeaves_Click(object sender, EventArgs e)
        {
            draw.upper.MakeAllVisible();
            upperLeaves = draw.auxUpper.GetLeaves();
            for (int i=0;i<upperLeaves.Count;i++)
            {
                if (upperLeaves[i].refStructure.Contains("G2M"))
                    Console.WriteLine("Ups");
                if (upperLeaves[i].parent.realDist > 0.5)
                    upperLeaves[i].visible = false;
                if (upperLeaves[i].parent.parent != null)
                {
                    int l = upperLeaves[i].parent.GetLeaves().Count;
                    if (l < 3 && upperLeaves[i].parent.parent.realDist > 0.5 && upperLeaves[i].parent.realDist>0.2)
                        upperLeaves[i].visible = false;
                }
            }
            draw.upper.PrepareGraphNodes(draw.upperBitMap, horizontalCuttDistance);
            pictureBoxUpper.Refresh();
            pictureBoxHeatMap.Refresh();
        }
        void FinSelectedGenesPositions()
        {
            upperLeaves = draw.auxUpper.GetLeaves();
            List<string> keys = new List<string>(selectedGeneNames.Keys);
            foreach (var item in keys)
            {
                string name = selectedGeneNames[item].Item1;
                selectedGeneNames[item]= new Tuple<string, int, int>(name, -1,-1);                
            }

            foreach (var item in upperLeaves)
            {

                if (selectedGeneNames.ContainsKey(item.refStructure))
                {
                    string name = selectedGeneNames[item.refStructure].Item1;
                    selectedGeneNames[item.refStructure] = new Tuple<string,int, int>(name,item.gNode.x, item.gNode.y);
                }
            }
        }
        private void noteGenesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            
            DialogResult res=openFileDialog1.ShowDialog();
            if(res==DialogResult.OK)
            {
                selectedGeneNames = new Dictionary<string, Tuple<string, int, int>>();
                //selectedGeneNames.Clear();
                StreamReader wr = new StreamReader(openFileDialog1.FileName);
                string line = wr.ReadLine();
                while(line!=null)
                {
                    string[] aux = line.Split(';');
                    selectedGeneNames.Add(aux[0],new Tuple<string, int, int>(aux[1],-1,-1));
                    line = wr.ReadLine();
                }
                wr.Close();
                
                showSelectedLabels = true;
                pictureBoxUpper.Refresh();
            }
        }

        private void toolStripButton7_Click(object sender, EventArgs e)
        {
            swap = !swap;
        }

        private void toolStripButton8_Click(object sender, EventArgs e)
        {
            showSelectedLabels = !showSelectedLabels;
            pictureBoxUpper.Refresh();
            pictureBoxLeft.Refresh();
        }

        private void toolStripButton9_Click(object sender, EventArgs e)
        {
            horizontalLines = !horizontalLines;
            pictureBoxHeatMap.Refresh();
        }
    }
}
