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



namespace Graph
{ 
    class LocalButton : Button
    {
        public string tupleName = "";
    }
    public partial class HeatMap : Form,IVisual
    {

        Random rand = new Random();
        bool avrRowAnnotations = false;
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
        bool showLabels = false;
        bool fullImage = false;
        List<HClusterNode> upperLeaves;
        List<HClusterNode> leftLeaves;
        HashSet<string> geneSelection;
        Dictionary<string,Tuple<Dictionary<string, Tuple<Color, string>>,List<ColorRect>>> leftDescription;
        HClusterNode colorNodeUpper = null;
        HClusterNode colorNodeLeft = null;
        Bitmap heatmap=null;
        public void ToFront()
        {
            this.BringToFront();
        }

        public HeatMap(HClusterNode upperNode,HClusterNode leftNode,Dictionary<string,string> labels,ClusterOutput outp)
        {
            
            upperLeaves = upperNode.GetLeaves() ;
            leftLeaves = leftNode.GetLeaves();
            upperNode.ClearColors(Color.Black);
            leftNode.ClearColors(Color.Black);
            InitializeComponent();
            this.Text = outp.name;
            //draw=new HeatMapDraw(new Bitmap(tableLayoutPanel1.Width, tableLayoutPanel1.Height), upperNode, leftNode, labels, outp);
            // draw = new HeatMapDraw(new Bitmap(pictureBox2.Width,pictureBox2.Height),new Bitmap(pictureBox3.Width,pictureBox3.Height),
            //     new Bitmap(pictureBox1.Width,pictureBox1.Height),new Bitmap(pictureBox4.Width,pictureBox4.Height),upperNode, leftNode, labels, outp);
            draw = new HeatMapDraw(new Bitmap(pictureBox2.Width, pictureBox2.Height), new Bitmap(pictureBox3.Width, pictureBox3.Height),
                new Bitmap(pictureBox1.Width, pictureBox1.Height), new Bitmap(pictureBox4.Width,pictureBox4.Height),upperNode, leftNode, labels, outp);

            label1.Text = "Gene clusters: " + upperNode.GetLeaves().Count;
            label2.Text = "Sample clusters: " + leftNode.GetLeaves().Count;

            int max = 0;
            foreach (var item in leftNode.GetLeaves())
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
            
                        
            for (int i = 1; i < draw.outp.aux2.Count; i++)
                comboBox1.Items.Add(draw.outp.aux2[i]);

            if (comboBox1.Items.Count > 0)
                comboBox1.SelectedIndex = 0;
            else
                comboBox1.Visible = false;


            pictureBox2.Refresh();
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
        private void pictureBox2_Paint(object sender, PaintEventArgs e)
        {
            if (showLabels == true)
            {

                Graphics g = Graphics.FromImage(draw.upperBitMap);
                g.Clear(pictureBox2.BackColor);
                Brush drawBrush = new System.Drawing.SolidBrush(Color.Black);
                drawFont = new System.Drawing.Font("Arial", draw.upper.labelSize);
                foreach (var item in upperLeaves)
                {
                    SizeF ss = g.MeasureString(item.refStructure, drawFont);
                    g.TranslateTransform(item.gNode.x, pictureBox2.Height);                    
                    g.RotateTransform(-45);

                    g.DrawString(item.refStructure, drawFont, drawBrush,5,-5);
                    g.ResetTransform();
                }

                e.Graphics.DrawImage(draw.upperBitMap, 0, 0);
            }
            else
            {
                pictureBox2.SizeMode = PictureBoxSizeMode.AutoSize;
                Graphics g = Graphics.FromImage(draw.upperBitMap);
                PrepareBarRegions(e.Graphics);
                g.Clear(this.BackColor);
                //e.Graphics.Clear(pictureBox2.BackColor);

                draw.upper.DrawOnBuffer(draw.upperBitMap, false, 1, Color.Empty);
                e.Graphics.DrawImage(draw.upperBitMap, 0, 0);
                //pictureBox1.Image = draw.upperBitMap;
                if (draw.upper.labColor != null && draw.upper.labColor.Count > 0)
                {
                    Font drawFont = new System.Drawing.Font("Arial", 8);
                    foreach (var item in regionBarColor)
                    {
                        RectangleF rec = item.Key.GetBounds(e.Graphics);
                        SolidBrush drawBrush = new System.Drawing.SolidBrush(draw.upper.labColor[item.Value.Value]);
                        e.Graphics.FillRectangle(drawBrush, rec.X, rec.Y, rec.Width, rec.Height);

                        SizeF textSize = e.Graphics.MeasureString(item.Value.Value, drawFont);
                        drawBrush = new System.Drawing.SolidBrush(Color.Black);

                        e.Graphics.DrawString(item.Value.Value, drawFont, drawBrush, rec.X + 20, rec.Y);
                        if (item.Value.Key)
                        {
                            Pen p = new Pen(Color.Black);
                            e.Graphics.DrawLine(p, rec.X, rec.Y, rec.X + rec.Width, rec.Y + rec.Height);
                            e.Graphics.DrawLine(p, rec.X + rec.Width, rec.Y, rec.X, rec.Y + rec.Height);
                        }
                    }

                }
            }
        }

        private void pictureBox3_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.Clear(this.BackColor);
            draw.left.posStart = 5;
            draw.left.DrawOnBuffer(draw.leftBitMap, false, 1, Color.Empty);
            e.Graphics.DrawImage(draw.leftBitMap, 0, 0);
            //draw.leftBitMap.Save("test.jpg");
        }


        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            //e.Graphics.Clear(this.BackColor);
            //draw.DrawHeatMap(e.Graphics);
//            if (pictureBox1.BackgroundImage == null)
            {
                Graphics g = Graphics.FromImage(draw.heatMap);
                g.Clear(BackColor);
                draw.DrawHeatMapN();

                
                if (interactiveLabelColumn.Checked || interactiveLabelRow.Checked)
                {
                    string label="";
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
                e.Graphics.DrawImage(draw.heatMap, x_pos, y_pos);
                if (geneSelection != null && geneSelection.Count > 0)
                {
                    Color newColor = Color.FromArgb(50, 255, 0, 0);
                    Brush br = new SolidBrush(newColor);
                    foreach (var item in upperLeaves)
                    {
                        if (geneSelection.Contains(item.refStructure))
                        {
                            Rectangle r = new Rectangle(item.gNode.areaLeft, 0, item.gNode.areaRight - item.gNode.areaLeft, pictureBox1.Height);
                            e.Graphics.FillRectangle(br, r);
                        }
                    }
                }
            }
        }
        void PaintLeftDescription()
        {            
            if (leftDescription != null)
            {                             
                int width = pictureBox6.Width / leftDescription.Count;                
                Tuple<Dictionary<string, Tuple<Color, string>>, List<ColorRect>> element;
                List<string> keys = new List<string>(leftDescription.Keys);
                for(int i=0;i<keys.Count;i++)
                {
                    int pos = i * width;
                    Dictionary<string, Tuple<Color, string>> x = leftDescription[keys[i]].Item1;                    
                    if (avrRowAnnotations)
                        element=CalcLeftBarsClusterAvr(x, pos, width);
                    else
                        element=CalcLeftBars(x, pos, width);

                    leftDescription[keys[i]] = element;
                    
                }
                pictureBox6.Refresh();
                pictureBox7.Refresh();
            }

        }

        private void HeatMap_ResizeEnd(object sender, EventArgs e)
        {
            
            if (!fullImage)
            {
                draw.upperBitMap = new Bitmap(pictureBox2.Width, pictureBox2.Height);
                draw.leftBitMap = new Bitmap(pictureBox3.Width, pictureBox3.Height);
                draw.heatMap = new Bitmap(pictureBox2.Width, pictureBox3.Height);
            }
            draw.left.PrepareGraphNodes(draw.leftBitMap);
            draw.upper.PrepareGraphNodes(draw.upperBitMap);
            pictureBox1.Refresh();
            pictureBox2.Refresh();
            pictureBox3.Refresh();
            pictureBox5.Refresh();
            PaintLeftDescription();
            //this.Invalidate();
        }

        private void pictureBox4_Paint(object sender, PaintEventArgs e)
        {
            SolidBrush b=new SolidBrush(Color.Black);
            int xPos,yPos;
            xPos=5;yPos=5;
            System.Drawing.Font drawFont = new System.Drawing.Font("Arial", 10);
            System.Drawing.SolidBrush drawBrush = new System.Drawing.SolidBrush(System.Drawing.Color.Black);
            System.Drawing.StringFormat drawFormat = new System.Drawing.StringFormat();

            List<double> ordered = new List<double>(draw.profilesColorMap.Keys);
            ordered.Sort();
            foreach (var item in ordered)
            {
                b.Color = draw.profilesColorMap[item];
                e.Graphics.FillRectangle(b, xPos, yPos, 15, 10);
                //e.Graphics.DrawString(item.ToString(), drawFont, drawBrush, xPos+25,yPos-3);
                e.Graphics.DrawString(item.ToString(), drawFont, drawBrush, xPos + 25, yPos - 3);
                yPos += 25;
                if (yPos > pictureBox4.Height)
                {
                    yPos = 5;
                    xPos += 40;
                }
            }
            //test.Paint();             
        }

        private void tableLayoutPanel1_Paint_1(object sender, PaintEventArgs e)
        {

        }

        private void pictureBox2_MouseMove(object sender, MouseEventArgs e)
        {
            bool test = false;
            if (colorNodeUpper != null)
            {
                draw.upper.ChangeColors(colorNodeUpper, Color.Black);
                test = true;
            }

            colorNodeUpper = draw.upper.FindClosestNode(e.X, e.Y);

            if(colorNodeUpper!=null)
            {
                draw.upper.ChangeColors(colorNodeUpper, Color.Red);
                test = true;
            }
            if (test)
            {
                Graphics g = Graphics.FromImage(draw.upperBitMap);
                g.Clear(pictureBox2.BackColor);
               
                if (colorNodeUpper != null)
                {
                    float v = ((float)colorNodeUpper.setStruct.Count) / draw.upperNode.setStruct.Count * 360;
                    g.FillPie(new SolidBrush(Color.Black), new Rectangle(e.X - 20, e.Y, 15, 15), v, 360 - v);
                    g.FillPie(new SolidBrush(Color.Red), new Rectangle(e.X - 20, e.Y, 15, 15), 0, v);
                }
                pictureBox2.Refresh(); 
              
            }
        }
        private void pictureBox2_MouseClick(object sender, MouseEventArgs e)
        {

            
            switch(e.Button)
            {

                case MouseButtons.Left:

                    if (regionBarColor.Count > 0)
                    {
                        List<Region> l = new List<Region>(regionBarColor.Keys);
                        bool testFlag = false;
                        foreach (var item in l)
                            if (item.IsVisible(e.X, e.Y))
                            {
                                regionBarColor[item] = new KeyValuePair<bool, string>(!regionBarColor[item].Key, regionBarColor[item].Value) ;
                                draw.upper.labColor = new Dictionary<string, Color>();
                                foreach (var itemColor in labelToColor)
                                    draw.upper.labColor.Add(itemColor.Key, itemColor.Value);
                                testFlag = true;                         
                            }
                        if (testFlag)
                        {
                            pictureBox2.Refresh();
                            return;
                        }
                    }
                    HClusterNode nodeC = colorNodeUpper;//upper.CheckClick(upper.rootNode,e.X,e.Y);
                    if (nodeC != null && nodeC.joined == null)
                    {
                        TextBoxView rr = new TextBoxView(nodeC.setStruct);
                       
                        rr.Show();

                        DrawPanel pn = new DrawPanel("Leave profile: "+nodeC.refStructure,null);
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
                        if (colorNodeUpper != null && colorNodeUpper.joined != null)
                            draw.auxUpper = colorNodeUpper;
                    }
                    break;      
                case MouseButtons.Right:
                        draw.auxUpper = draw.upperNode;
                        break;
            }
            if (draw.auxUpper != null)
            {
                if(colorNodeUpper!=null)
                    draw.upper.ChangeColors(colorNodeUpper, Color.Black);
                colorNodeUpper = null;
                draw.upper.rootNode = draw.auxUpper;
                draw.upper.PrepareGraphNodes(draw.upperBitMap);
                Graphics g = Graphics.FromImage(draw.upperBitMap);
                g.Clear(pictureBox2.BackColor);
                pictureBox2.Refresh();
                pictureBox1.Refresh();
            }
        }

        private void pictureBox3_MouseMove(object sender, MouseEventArgs e)
        {
            bool test = false;
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
                g.Clear(pictureBox3.BackColor);
                
                if(colorNodeLeft!=null)
                { 
                    float v=((float)colorNodeLeft.setStruct.Count)/draw.leftNode.setStruct.Count*360;
                    g.FillPie(new SolidBrush(Color.Black), new Rectangle(e.X - 20, e.Y, 15, 15), v,360-v);
                    g.FillPie(new SolidBrush(Color.Red), new Rectangle(e.X-20, e.Y, 15, 15), 0, v);
                }
                //g.DrawArc(new Pen(Color.DarkGreen),new Rectangle(e.X,e.Y,10,10), 0.0, v);
                pictureBox3.Refresh();
            }
        }
       
        private void pictureBox3_MouseClick(object sender, MouseEventArgs e)
        {
            switch (e.Button)
            {

                case MouseButtons.Left:
                    HClusterNode nodeC = colorNodeLeft;//  left.CheckClick(left.rootNode,e.X,e.Y);
                    
                    if (nodeC != null && nodeC.joined==null)
                    {
                        TextBoxView rr = new TextBoxView(nodeC.setStruct);
                        rr.Show();
                        DrawPanel pn = new DrawPanel("Leave profile: " + nodeC.refStructure,null);
                         pn.CreateBMP();
                         List<HClusterNode> upperLeaves = draw.auxUpper.GetLeaves();
                         upperLeaves = upperLeaves.OrderByDescending(o => o.gNode.y).Reverse().ToList();
                         List<string> refList = new List<string>();
                        foreach(var item in upperLeaves)
                            refList.Add(item.refStructure);
                        pn.drawPic = delegate {draw.DrawHeatMapNode(pn.bmp, nodeC.setStruct, refList); pn.Text = nodeC.consistency.ToString(); };
                         pn.Show();
                        draw.DrawHeatMapN();
                        pictureBox1.BackgroundImage = draw.heatMap;
                        //pn.pictureBox1.Invalidate();
                    }
                    else
                        if (colorNodeLeft != null && colorNodeLeft.joined!=null)
                            draw.auxLeft = colorNodeLeft;
                    break;
                case MouseButtons.Right:
                    draw.auxLeft = draw.leftNode;
                    break;
            }
            if (draw.auxLeft != null)
            {
                if (colorNodeLeft != null)
                    draw.left.ChangeColors(colorNodeLeft, Color.Black);
                colorNodeLeft = null;
                draw.left.rootNode = draw.auxLeft;
                leftLeaves = draw.left.rootNode.GetLeaves();
                draw.left.PrepareGraphNodes(draw.leftBitMap);
                Graphics g = Graphics.FromImage(draw.leftBitMap);
                g.Clear(pictureBox3.BackColor);
                pictureBox3.Refresh();
                pictureBox1.Refresh();
                PaintLeftDescription();
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
            //draw.left.showLabels = showLabels;
            pictureBox2.Refresh();
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            draw.left.labelSize = (int)numericUpDown1.Value;
            draw.upper.labelSize = (int)numericUpDown1.Value;

            Graphics g = Graphics.FromImage(draw.upperBitMap);
            g.Clear(pictureBox2.BackColor);
            g = Graphics.FromImage(draw.leftBitMap);
            g.Clear(pictureBox3.BackColor);

            pictureBox3.Refresh();
            pictureBox2.Refresh();

        }

        private void pictureBox2_MouseLeave(object sender, EventArgs e)
        {
            if (colorNodeUpper != null)
            {             
                draw.upper.ChangeColors(colorNodeUpper, Color.Black);
                colorNodeUpper = null;
                Graphics g = Graphics.FromImage(draw.upperBitMap);
                g.Clear(pictureBox2.BackColor);
                pictureBox2.Refresh();                 
            }
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            DialogResult res=saveFileDialog1.ShowDialog();
            if(res==DialogResult.OK)
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
                ClusterOutput.Save(clusters,cons,wS,true);
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
                ClusterOutput.Save(clusters, cons,wS, true);
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
                g.Clear(pictureBox3.BackColor);

                //g.DrawArc(new Pen(Color.DarkGreen),new Rectangle(e.X,e.Y,10,10), 0.0, v);
                pictureBox3.Refresh();
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

            List<HClusterNode> leaveas = draw.upperNode.GetLeaves();                
            int index=1;
            labelToColor.Clear();
            regionBarColor.Clear();
            foreach(var item in leaveas)
            {                
                if(item.refStructure.Contains(";"))
                {
                    string[] aux = item.refStructure.Split(';');
                    if (!labelToColor.ContainsKey(aux[comboBox1.SelectedIndex + 1]))
                    {
                        labelToColor.Add(aux[comboBox1.SelectedIndex + 1], draw.barMap[0]);
                    }
                }
            }
            List<string> labels = new List<string>(labelToColor.Keys);
            labelToColor.Clear();
            foreach (var item in labels)
                labelToColor.Add(item, draw.barMap[index++]);

            draw.upper.labColor = new Dictionary<string, Color>();
            foreach (var item in labelToColor)            
                draw.upper.labColor.Add(item.Key,item.Value);
            
            
            draw.upper.currentLabelIndex = comboBox1.SelectedIndex+1;
            
            
            //int step = (barMap.Count-1) / labels.Count;

            if (labels.Count > draw.barMap.Count)
            {
                MessageBox.Show("To many colors need to be used!");
                return;
            }
            

            pictureBox2.Refresh();
        }

        private void panel2_Scroll(object sender, ScrollEventArgs e)
        {
            x_pos = -e.NewValue;
            pictureBox1.Invalidate();
        }

        private void panel3_Scroll(object sender, ScrollEventArgs e)
        {

            if (e.ScrollOrientation == ScrollOrientation.VerticalScroll)
            {
                y_pos = -e.NewValue;
                pictureBox1.Invalidate();
            }
            pictureBox3.Invalidate();

        }

        private void toolStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            fullImage = !fullImage;
            if (fullImage)
            {
                List<HClusterNode> upperLeaves = draw.auxUpper.GetLeaves();
                List<HClusterNode> leftLeaves = draw.auxLeft.GetLeaves();
                double stepH = 5;
                if (leftLeaves.Count * stepH < pictureBox3.Height)
                {
                    stepH=((double)pictureBox3.Height)/leftLeaves.Count;
                }
                double stepW = 5;
                if(upperLeaves.Count * stepW < draw.upperBitMap.Width)
                    stepW=((double)draw.upperBitMap.Width)/upperLeaves.Count;
                draw.ChangeBitmpSizes(pictureBox3.Width, (int)(leftLeaves.Count * stepH), (int)(upperLeaves.Count * stepW), draw.upperBitMap.Height);

            }
            else
                draw.ChangeBitmpSizes(pictureBox3.Width, pictureBox3.Height, pictureBox2.Width, pictureBox2.Height);
            draw.left.PrepareGraphNodes(draw.leftBitMap);
            draw.upper.PrepareGraphNodes(draw.upperBitMap);
            pictureBox2.Refresh();
            pictureBox3.Refresh();
            pictureBox1.Refresh();
        }

        private void vScrollBar1_ValueChanged(object sender, EventArgs e)
        {
            draw.left.clusterSize = vScrollBar1.Value;
            label3.Text = vScrollBar1.Value.ToString();
            pictureBox3.Invalidate();
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
        string GetLabel(int x,int y,bool row, List<HClusterNode> nodes)
        {
            int pos=y;
            if (!row)
                pos = x;
            
                foreach(var item in nodes)
                {
                    if(pos>=item.gNode.areaLeft && pos<item.gNode.areaRight)
                    {
                        return item.refStructure;
                    }
                }
            return "";
        }

        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if(interactiveLabelColumn.Checked || interactiveLabelRow.Checked)
            {
                mouseX = e.X + 5;
                mouseY = e.Y;
                pictureBox1.Invalidate();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            splitContainer1.SplitterDistance += 20;
            draw.leftBitMap = new Bitmap(pictureBox3.Width, pictureBox3.Height);
            draw.heatMap = new Bitmap(pictureBox2.Width, pictureBox3.Height);
            draw.left.PrepareGraphNodes(draw.leftBitMap);

            pictureBox3.Invalidate();
            pictureBox2.Invalidate();
                        
        }

        private void labelsColumnToolStripMenuItem_Click(object sender, EventArgs e)
        {
            labelsColumnToolStripMenuItem.Checked = !labelsColumnToolStripMenuItem.Checked;
            if (labelsColumnToolStripMenuItem.Checked)
                pictureBox5.Refresh();

            List<string> geneNums = new List<string>();
            int num = 1;
            foreach (var item in upperLeaves)
            {
                string s = num + "\t" + item.refStructure;
                num++;
                geneNums.Add(s);
            }
            TextBoxView t = new TextBoxView(geneNums);
            t.Show();

        }

        private void pictureBox5_Paint(object sender, PaintEventArgs e)
        {
            if (labelsColumnToolStripMenuItem.Checked)
            {
                int count = 1;
                Brush b = new SolidBrush(Color.Black);
                e.Graphics.Clear(BackColor);
                foreach (var item in upperLeaves)
                {
                    SizeF textSize = e.Graphics.MeasureString(count.ToString(), drawFont);
                    int posx = (int)(item.gNode.x - textSize.Width / 2);
                    e.Graphics.DrawString(count.ToString(), drawFont, b, posx, textSize.Height / 2);
                    count++;
                }
            }
        }
        Dictionary<string,Tuple<Color,string>> ReadClusterFile(string fileName)
        {
            StreamReader r = new StreamReader(fileName);
            Dictionary<string, Tuple<Color,string>> clDic = new Dictionary<string, Tuple<Color, string>>();
            Dictionary<string, string> labels = new Dictionary<string, string>();
            HashSet<string> distinc = new HashSet<string>() ;
            string line = r.ReadLine();
            while(line!=null)
            {
                string[] tmp = line.Split(new char[] { ' ', '\t' });
                if (tmp.Length == 2)
                {
                    labels.Add(tmp[0], tmp[1]);
                    distinc.Add(tmp[1]);                    
                }

                line = r.ReadLine();
            }
            r.Close();
            int i = 0;
            Dictionary<string, Color> toColor = new Dictionary<string, Color>(); ;
            foreach(var item in distinc)
            {
                toColor.Add(item, ColorDist.colorTable[i]);
                i++;
            }
            foreach(var item in labels)
                clDic.Add(item.Key, new Tuple<Color, string>(toColor[item.Value], item.Value));

            return clDic;
        }
        Tuple<Dictionary<string, Tuple<Color, string>>, List<ColorRect>> CalcLeftBarsClusterAvr(Dictionary<string, Tuple<Color, string>> colorMap, int pos = 0, int width=20)
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
                    z.sX = 0+pos;
                    z.width = width;
                    z.height = item.gNode.areaRight - item.gNode.areaLeft;
                    z.c = mappings[mappings.Count - 1].Key;
                    r.Add(z);        
                }                                
            }            
            return new Tuple<Dictionary<string, Tuple<Color, string>>, List<ColorRect>>(colorMap, r);
        }

        Tuple<Dictionary<string, Tuple<Color, string>>, List<ColorRect>> CalcLeftBars(Dictionary<string, Tuple<Color, string>> colorMap,int pos=0,int width=20)
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
                        x.sX = 0+pos;
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
                        if (mappings.Count > 0)
                        {
                            mappings.Sort((x, y) => x.Value.CompareTo(y.Value));
                            for (int i = 0; i < temp.Count; i++)
                                if ((int)Math.Floor(temp[i].sY) == it)
                                {
                                    temp[i].c = mappings[mappings.Count - 1].Key;
                                    temp[i].height = 1;
                                    if (step>1)
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
            return new Tuple<Dictionary<string, Tuple<Color, string>>, List<ColorRect>>(colorMap,r);
        }
        private void rowAnnotationClick(object sender, EventArgs e)
        {
            DialogResult res = openFileDialog1.ShowDialog();
            if(res==DialogResult.OK)
            {
                checkBox1.Visible = true;
                if (leftDescription == null)
                    leftDescription = new Dictionary<string,Tuple<Dictionary<string, Tuple<Color, string>>, List<ColorRect>>>();
                var colorMap=ReadClusterFile(openFileDialog1.FileName);
                int pos = 1;
                if (leftDescription.Count>0)
                    pos = 35;
                
                foreach(var item in leftDescription)
                {
                    pos +=GetDistinctColors(item.Value).Count*20;
                }
                string NameT = "Name" + rand.Next();
                leftDescription.Add(NameT,new Tuple<Dictionary<string, Tuple<Color, string>>, List<ColorRect>>(colorMap, null));
                LocalButton b = new LocalButton();
                
                b.Location = new Point(1, pos);
                b.Text = "X";
                b.Size = new Size(10, 10);               
                PaintLeftDescription();
                b.tupleName = NameT;
                b.Click += (s, ev) => {
                    leftDescription.Remove(((LocalButton)s).tupleName);
                    pictureBox7.Controls.Remove((LocalButton)s);
                    pictureBox6.Invalidate();
                    pictureBox7.Invalidate();
                };
                pictureBox7.Controls.Add(b);
            }
        }

        private void pictureBox6_Paint(object sender, PaintEventArgs e)
        {
            if (leftDescription != null)
            {                
                e.Graphics.Clear(pictureBox6.BackColor);

                foreach (var item in leftDescription)
                {
                    foreach (var it in item.Value.Item2)
                    {
                        Brush b = new SolidBrush(it.c);
                        e.Graphics.FillRectangle(b, it.sX, it.sY, it.width, it.height);
                    }
                }
                if (labelNum.Length > 0)
                {
                    Size sizeOfText = TextRenderer.MeasureText(labelNum, drawFont);
                    Rectangle rect = new Rectangle(new Point(mouseX+5, mouseY+5), sizeOfText);
                    e.Graphics.FillRectangle(Brushes.White, rect);
                    e.Graphics.DrawString(labelNum, drawFont, brush, mouseX+5, mouseY+5);
                }

            }
        }

        private void pictureBox6_MouseMove(object sender, MouseEventArgs e)
        {
            if(leftDescription!=null)
            {
                mouseX = e.X + 5;
                mouseY = e.Y;

                foreach(var item in leftDescription)
                {
                    for(int j=0;j<item.Value.Item2.Count;j++)
                    {
                        ColorRect r = item.Value.Item2[j];
                        if(r.text!=null)
                            if (e.X >= r.sX && e.X <= r.sX + r.width)
                                if (e.Y >= r.sY && e.Y <= r.sY + r.height)                                                               
                                    labelNum = r.text.ToString();
                    }
                }
                pictureBox6.Invalidate();
            }
        }

        private void selectGenesToolStripMenuItem_Click(object sender, EventArgs e)
        {

            List<string> genes = new List<string>();
            foreach (var item in upperLeaves)
                genes.Add(item.refStructure);
            GeneSelection sel = new GeneSelection(genes);
            DialogResult res=sel.ShowDialog();
            if(res==DialogResult.OK)
            {
                geneSelection = sel.selection;
                pictureBox1.Invalidate();
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
        private void pictureBox7_Paint(object sender, PaintEventArgs e)
        {
            Dictionary<string, Color> x = new Dictionary<string, Color>();
            if(leftDescription!=null)
            {

                int posy = 15;
                foreach (var item in leftDescription)
                {

                    x = GetDistinctColors(item.Value);

                    int posx = 0;
                    Brush stringB = new SolidBrush(Color.Black);
                    List<string> keys = new List<string>(x.Keys);
                    try
                    {
                        keys.Sort((a, b) => Convert.ToInt32(a).CompareTo(Convert.ToInt32(b)));
                    }
                    catch(Exception ex)
                    {
                        keys.Sort();
                    }

                    foreach (var it in keys)
                    {
                        Brush b = new SolidBrush(x[it]);

                        e.Graphics.FillRectangle(b, posx, posy, 10, 10);
                        e.Graphics.DrawString(it, drawFont, stringB, posx + 10, posy - 3);
                        posy += 20;
                    }
                    //posx += 21;
                    posy += 40;
                }
            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked)
                avrRowAnnotations = true;
            else
                avrRowAnnotations = false;

            PaintLeftDescription();

            pictureBox6.Invalidate();
        }

        private void histogramsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PlotForm pHist=new PlotForm(FilterOmics.memoryFilteredData);
            pHist.Show();
        }
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

}
