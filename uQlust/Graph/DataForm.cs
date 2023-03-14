using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using phiClustCore;

namespace Graph
{


    public partial class DataForm : Form
    {
        char[] separators;
        string[] tempData = new string[52];
        public OmicsDataSet data;
        int rowPos;
        int colPos;
        int geneP;
        int samP;
        string fileName = "";

        string[] keyTab;
        float [][]tabNum;
        Dictionary<string, float[]> locData = new Dictionary<string, float[]>(3000);
        List<string> labRow;
        List<string> labCol;


        public DataForm(string fileName)
        {
            InitializeComponent();
            geneLabels.SelectedIndex = 1;
            sampleLabels.SelectedIndex = 0;
            rowPos = (int)Row.Value;
            colPos = (int)Column.Value;
            geneP = (int)genePos.Value;
            samP = (int)samplePos.Value;
            this.fileName = fileName;
            data = new OmicsDataSet(Path.GetFileName(fileName));
            StreamReader sr = new StreamReader(fileName);
            string line = sr.ReadLine();

            int counter = 0;
            while (line != null)
            {
                tempData[counter] = line;
                dataGridView1.Rows.Add(line);
                if (counter > 50)
                    break;
                counter++;
                line = sr.ReadLine();
            }
            sr.Close();

        }
        void ParseLines(int start,int end,string []tabString)
        {
            int startCol = 0;
            int labelRow = 0;
            if ((string)geneLabels.SelectedItem == "Column")
            {
                startCol = (int)genePos.Value;
                labelRow = (int)samplePos.Value;
            }
            else
            {
                startCol = (int)samplePos.Value;
                labelRow = (int)genePos.Value;
            }


            string[] aux;
            for (int l = start; l < end; l++)
            {
                string line = tabString[l].Replace("\"", "");
                if (l == (int)labelRow)
                {                 
                    aux = line.Split(separators);
                    lock (labRow)
                    {
                        for (int i = (int)Column.Value; i < aux.Length; i++)
                            labRow.Add(aux[i]);
                    }
                }

                if (l >= (int)Row.Value)
                {
                    aux = line.Split(separators);
                    float[] d = new float[aux.Length - (int)Column.Value];
                    NumberFormatInfo provider = new NumberFormatInfo();
                    provider.NumberDecimalSeparator = ".";
                    for (int i = (int)Column.Value, k = 0; i < aux.Length; i++, k++)
                    {
                        int z = 0;
                       // for (int s = 0; s < aux[i].Length; s++)
                       //     z = z * 10 + (aux[i][s] - '0');
                       
                        //d[k] = (short)z;// Convert.ToInt16(aux[i],provider);
                        d[k] =  Convert.ToSingle(aux[i],provider);
//                        if (d[k] < 0)
//                            Console.WriteLine("Ups");
                    }
                    lock (tabNum)
                    {
                        tabNum[l] = d;
                    }
                    lock (keyTab)
                    {

                        keyTab[l] = aux[startCol];
                    }
                   

                    /*                    lock (locData)
                                        {
                                            locData.Add(aux[startCol], d);
                                            labCol.Add(aux[startCol]);
                                        }*/
                }
            }
        }
        public OmicsDataSet GetOmicsSpareFile(string fileName)
        {
            string[] tabString = new string[20000];
            StreamReader sr = new StreamReader(fileName, Encoding.UTF8, true, 1024);
            data = new OmicsDataSet(Path.GetFileName(fileName));
            //locData.Clear();

            string line = sr.ReadLine();
            string[] aux = null;            
            tabString[0] = line;
            int x = 1;
            while (!sr.EndOfStream)
            {
                tabString[x] = sr.ReadLine();
                x++;
            }
            sr.Close();
            data.sampleLabels = new List<string>(1000);
            aux = tabString[0].Split('\t');
            for (int i = 1; i < aux.Length; i++)
                data.sampleLabels.Add(aux[i]);


            SpareArray sp = new SpareArray(data.sampleLabels.Count);

            data.geneLabels = new List<string>(20000);
            for (int i=1;i<x;i++)
            {
                aux = tabString[i].Split('\t');
                data.geneLabels.Add(aux[0]);
                
                for(int j=1;j<aux.Length;j++)
                {
                    if (aux[j].Contains(","))
                    {
                        string[] aux2 = aux[j].Split(',');
                        int index = int.Parse(aux2[0]);
                        sp[index,i-1] = int.Parse(aux2[1]);
                    }
                }
            }

            data.data = sp;
            data.data.columns = data.geneLabels.Count;
            
            return data;
        }
        public OmicsDataSet GetOmicsFile(string fileName)
        {
            string[] tabString = new string[20000];
            StreamReader sr = new StreamReader(fileName, Encoding.UTF8, true, 1024);
            data = new OmicsDataSet(Path.GetFileName(fileName));
            //locData.Clear();

            labRow=new List<string>(1000);
            labCol = new List<string>(1000);
            string line = sr.ReadLine();
            string[] aux = null;
            int count = 0;
            int startCol = 0;
            int labelRow = 0;
            if ((string)geneLabels.SelectedItem == "Column")
            {
                startCol = (int)genePos.Value;
                labelRow = (int)samplePos.Value;
            }
            else
            {
                startCol = (int)samplePos.Value;
                labelRow = (int)genePos.Value;
            }
            tabString[0] = line;
            int x = 1;
            while (!sr.EndOfStream)
            {
                tabString[x] = sr.ReadLine();
                x++;
            }
            int cores = 5;
            Task[] tabTask = new Task[cores];
            keyTab = new string[x];
            tabNum = new float[x][];

            float step = x / ((float)cores);
            for(int i=0;i<cores;i++)
            {
                int s = (int)step * i;
                int k = (int)step * (i + 1);

                tabTask[i] = Task.Run(() => ParseLines(s, k, tabString));
            }
            Task.WaitAll(tabTask);

            /*while (line != null)
            {
                line = line.Replace("\"", "");
                if (count == (int)labelRow)
                {
                    aux = line.Split(separators);
                    for (int i = (int)Column.Value; i < aux.Length; i++)
                        labRow.Add(aux[i]);
                }


                if (count >= (int)Row.Value)
                {
                    aux = line.Split(separators);
                    Int16[] d = new Int16[aux.Length - (int)Column.Value];
                    NumberFormatInfo provider = new NumberFormatInfo();
                    provider.NumberDecimalSeparator = ".";
                    for (int i = (int)Column.Value,k=0; i < aux.Length; i++,k++)
                    {
                        int y = 0;
                        for (int s = 0; s < aux[i].Length; s++)
                            y = y * 10 + (aux[i][s] - '0');

                        d[k] =(Int16) y;// Convert.ToInt16(aux[i],provider);
                        //d[k] =  Convert.ToInt16(aux[i],provider);
                    }
                    locData.Add(aux[startCol], d);
                    labCol.Add(aux[startCol]);
                }

                count++;
                line = sr.ReadLine();
            }*/
            sr.Close();
            List<string> locKeys = new List<string>(locData.Keys);
            int rem = -1;
            for (int i = 0; i < keyTab.Length; i++)
                if (keyTab[i] != null)
                {
                    locKeys.Add(keyTab[i]);
                    labCol.Add(keyTab[i]);
                    rem = i;
                }

            if ((string)geneLabels.SelectedItem == "Column")
            {
                data.geneLabels = labCol;
                data.sampleLabels = labRow;
            }
            else
            {
                data.geneLabels = labRow;
                data.sampleLabels = labCol;
            }

            
            //Debug.WriteLine("samples="+data.sampleLabels.Count);
            data.data = new SpareArray(data.sampleLabels.Count, data.geneLabels.Count, false);
            if (labCol.Contains(data.geneLabels[0]))
            {                                
                for (int i = 0; i < tabNum[rem].Length; i++)
                {
                    int counter = 0;
                    for (int j = 0; j < tabNum.Length; j++)
                    {
                        if (tabNum[j] == null)
                            continue;
                        data.data[i, counter++] = tabNum[j][i];
                    }
                }

            }
            else
            {
                for (int j = 0; j < locKeys.Count; j++)
                {
                    if(tabNum[j]!=null)
                    for (int i = 0; i < tabNum[rem].Length; i++)
                        if(locData[locKeys[j]][i]>0)
                            data.data[j, i] = tabNum[j][i];
                }

            }
            
/*            StreamWriter wr = new StreamWriter("test",true);
            for (int i = 0; i < data.data.rows; i++)
            {
                for (int j = 0; j < data.data.columns; j++)
                {
                    wr.Write(" " + data.data[i, j]);
                }
                wr.WriteLine() ;
            }

            wr.Close();*/
            return data;
        }
        void SplitData()
        {

            List<char> listSep = new List<char>();
            if (Tab.Checked)
                listSep.Add('\t');
            if (Comma.Checked)
                listSep.Add(',');
            if (Semicolon.Checked)
                listSep.Add(';');
            if (Space.Checked)
                listSep.Add(' ');

            int cColumn = dataGridView1.Columns.Count - 1;

            for (int i = cColumn; i >= 1; i--)
                dataGridView1.Columns.RemoveAt(i);
            for (int i = 0; i < tempData.Length; i++)
                dataGridView1.Rows[i].Cells[0].Value = tempData[i];
            if (listSep.Count > 0)
            {
                separators = listSep.ToArray();
                string[] aux = tempData[3].Split(separators);
                int maxV = Math.Min(20, aux.Length);

                for (int j = 1; j < maxV; j++)
                {
                    dataGridView1.Columns.Add(j.ToString(), j.ToString());
                    dataGridView1.Columns[dataGridView1.Columns.Count - 1].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                    dataGridView1.Columns[dataGridView1.Columns.Count - 1].MinimumWidth = 100;
                }
                for (int i = 0; i < tempData.Length; i++)
                {

                    if (tempData[i] != null)
                    {
                        aux = tempData[i].Split(separators);
                        int maxX = Math.Min(maxV, aux.Length);
                        for (int j = 0; j < maxX; j++)
                            dataGridView1.Rows[i].Cells[j].Value = aux[j];
                    }
                }
            }

            for (int i = 0; i < dataGridView1.Columns.Count; i++)
                if(dataGridView1.Rows.Count> (int)Row.Value)
                    dataGridView1.Rows[(int)Row.Value].Cells[i].Style.BackColor = Color.Red;

            for (int i = 0; i < dataGridView1.Rows.Count; i++)
                if(dataGridView1.Rows[i].Cells.Count> (int)Column.Value)
                    dataGridView1.Rows[i].Cells[(int)Column.Value].Style.BackColor = Color.Red;            

        }

        private void Tab_CheckedChanged(object sender, EventArgs e)
        {
            SplitData();
        }

        private void Space_CheckedChanged(object sender, EventArgs e)
        {
            SplitData();
        }

        private void Row_ValueChanged(object sender, EventArgs e)
        {
            SelectRow(rowPos, (int)Row.Value, Color.Red);
            rowPos = (int)Row.Value;
        }

        private void Column_ValueChanged(object sender, EventArgs e)
        {
            SelectColumn(colPos, (int)Column.Value, Color.Red);
            colPos = (int)Column.Value;
        }

        private void geneLabels_SelectedIndexChanged(object sender, EventArgs e)
        {
            sampleLabels.SelectedIndex = 1 - geneLabels.SelectedIndex;
        }

        private void sampleLabels_SelectedIndexChanged(object sender, EventArgs e)
        {
            geneLabels.SelectedIndex = 1 - sampleLabels.SelectedIndex;
        }

        void SelectColumn(int prevCol, int col, Color color)
        {
            for (int i = 0; i < dataGridView1.Rows.Count; i++)
                if(dataGridView1.Rows[i].Cells[prevCol].Style.BackColor==color)
                    dataGridView1.Rows[i].Cells[prevCol].Style.BackColor = Color.White;

            for (int i = 0; i < dataGridView1.Rows.Count; i++)
                dataGridView1.Rows[i].Cells[col].Style.BackColor = color;

        }

        void SelectRow(int prevRow, int row, Color color)
        {
            for (int i = 0; i < dataGridView1.Columns.Count; i++)
                if(dataGridView1.Rows[prevRow].Cells[i].Style.BackColor==color)
                    dataGridView1.Rows[prevRow].Cells[i].Style.BackColor = Color.White;


            for (int i = 0; i < dataGridView1.Columns.Count; i++)
                dataGridView1.Rows[row].Cells[i].Style.BackColor = color;
        }

        private void genePos_ValueChanged(object sender, EventArgs e)
        {
            if (geneLabels.SelectedIndex == 0)
                SelectRow(geneP, (int)genePos.Value, Color.Gray);
            else
                SelectColumn(geneP, (int)genePos.Value, Color.Gray);

            geneP = (int)genePos.Value;
        }

        private void samplePos_ValueChanged(object sender, EventArgs e)
        {
            if (sampleLabels.SelectedIndex == 0)
                SelectRow(samP, (int)samplePos.Value, Color.Gray);
            else
                SelectColumn(geneP, (int)samplePos.Value, Color.Gray);
            samP = (int)samplePos.Value;
        }

        private void Import_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            this.Close();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }

    
  
}
