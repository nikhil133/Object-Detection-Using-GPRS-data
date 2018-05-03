using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using HDF5DotNet;
using System.Diagnostics;
using Emgu.CV;
using Emgu.CV.Structure;

namespace ObjectDetection
{
    public partial class Form1 : Form
    {
        int hdep = 0, dtpt = 0, dtchnl = 0, dtscn = 0, dtdep = 0, temp = 0;
        public float[, ,] data;
        float[][] x;
        float[][] xb;
        float[][] c1;
        float[][] y1;
        float mn;
        int NOF, NOC, NOS, last = 100, res = 1, leaveFirst = 0, m, n, depth = 0, chnl = 0, hscn = 0;
        int n_o_s;//number of scan files
        int n_o_c;//number of channels
        int n_o_f;//number of Freq.
        int cluster = 0, colseg = 0;
        Bitmap Bscan;
        float[, ,] grid;
        public Form1()
        {
            InitializeComponent();
        }
        public void imagescanner(int scan, int depth, int chnl)
        {
            for (int i = 0; i < m; i++)
            {
                for (int j = 0; j < n; j++)
                {

                    xb[i][j] = grid[scan + i, chnl, depth + j];
                }
            }
            plotBscan(res);
            normalize(ref xb, m, n);
            mn = mean_mat(ref xb, m, n);
            meanrmv2(ref xb, mn, m, n);
            gaussfltr(ref xb, m, n, mn);
            sobel(ref xb, m, n);
            for (int i = 0; i < m; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    x[i][j] = xb[i][j];
                }
            }
            plotBscanout(res);
            c3algo(ref xb, ref x, ref y1, scan, depth, m, n);
            plotCscanout(res);
        }    
        
        public void normalize(ref float[][] N, int m, int n)
        {
            float[] temp = new float[n];
            float sum = 0;
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < m; j++)
                {
                    sum = Convert.ToSingle(sum + Math.Pow(N[j][i], 2));
                }
                temp[i] = Convert.ToSingle(Math.Sqrt(sum));
                sum = 0;
            }
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < m; j++)
                {
                    N[j][i] = N[j][i] / temp[i];
                }
            }

        }
        public float mean_mat(ref float[][] M, int m, int n)
        {
            float sum = 0, size = m * n;

            for (int i = 0; i < m; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    sum = sum + M[i][j];
                }
            }
            sum = sum / size;
            return sum;
        }
        public void meanrmv2(ref float[][] M, float x, int m, int n)
        {
            for (int i = 0; i < m; i++)
                for (int j = 0; j < n; j++)
                    M[i][j] = M[i][j] - x;
        }
        public void gaussfltr(ref float[][] a, int m, int n, float u)
        {
            float sig;;
            float r;
            float sum = 0, xi;
            int i, j;
            /**------------- gauss filter kernel-------------**/
            float[][] q = new float[m][];
            for (i = 0; i < m; i++)
            {
                for (j = 0; j < n; j++)
                {
                    xi = a[i][j] - u;
                    sum = sum + (float)Math.Pow(xi, 2);
                }
            }
            sum = Convert.ToSingle((1 / 31.000) * sum);
            sig = (float)Math.Sqrt(sum);
            float s = 2 * sig * sig;
            sum = 0;
            for (i = 0; i < 3; i++)
                q[i] = new float[3];

            for (i = -1; i < 1; i++)
            {

                for (j = -1; j < 1; j++)
                {
                    r = (float)Math.Sqrt(i * i + j * j);
                    q[i + 1][j + 1] = (float)Math.Exp(-r * r / s) / (float)Math.PI * s;
                    sum += q[i + 1][j + 1];
                }
            }

            for (i = 0; i < 3; i++)
            {

                for (j = 0; j < 3; j++)
                {
                    q[i][j] /= sum;
                }
            }
/*-----------------------------------------------*/
            for (int x = 0; x < m; x++) // Correlating gauss kernel to image
            {
                for (int y = 0; y < n; y++)
                {
                    sum = 0;
                    for (int k = 2; k >=0; k--)
                    {
                        for (j = 2; j >=0; j--)
                        {
                            sum += q[k][j] * a[x][y];
                        }
                    }
                    a[x][y] = sum;
                }
            }
        }
        public void sobel(ref float[][] a, int m, int n)
        {
            int h, w, count = 0;
            float[,] gx = new float[3, 3] { { 1, 0, -1 }, { 2, 0, -2 }, { 1, 0, 1 } }; // sobel kernel for x axis
            float[,] gy = new float[3, 3] { { 1, 2, 1 }, { 0, 0, 0 }, { -1, -2, -1 } };// sobel kernel for y axis

            float valx = 0, valy = 0, val1 = 0, Imax = 0, Imin = 0, sum = 0, th = 0;
            for (int i = 0; i < m; i++)
            {

                for (int j = 0; j < n; j++)
                {
                    if ((i == 0) || (i == m - 1) || (j == 0) || (j == n - 1))
                    {
                        valx = 0;
                        valy = 0;
                    }
                    else
                    {
                        for (h = 2; h >= 2; h--)
                        {
                            for (w = 2; w >= 2; w--)
                            {
                                valx = valx + a[i][j] * gx[h, w];
                                valy = valy + a[i][j] * gy[h, w];
                            }
                        }
                    }
                    val1 = (float)Math.Sqrt(Math.Pow(valx, 2) + Math.Pow(valy, 2));
                    if (val1 > Imax)
                    {
                        Imax = val1;
                        
                    }
                    else if (val1 < Imin)
                    {
                        Imin = val1;
                    }
                   
                    a[i][j] = val1;
                    valx = 0; valy = 0; val1 = 0;
                }
            }
            /***** Adaptive Threshold*****/
            for (int i = 0; i < m; i++)
            {

                for (int j = 0; j < n; j++)
                {
                    if (a[i][j] > 0.3 * Imax) 
                    {
                        sum = sum + a[i][j];
                        count++;
                    }
                    
                }
            }
            th = sum / count;
            /***************************/
            for (int i = 0; i < m; i++)
            {

                for (int j = 0; j < n; j++)
                {
                    if ((th >= a[i][j]) )
                    {
                        a[i][j] = 0;
                    }
                    else
                    {
                        a[i][j] = a[i][j];
                    }

                }
            }

        }
        public void c3algo(ref float[][] a, ref float[][] cell, ref float[][] y1, int chnl, int depth, int m, int n)//clustering Algorithm
        {

            int s = m / 31, count = 0, l = 0, g = 0;
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < m; j++)
                {
                    if (a[j][i] != 0)
                    {
                        count++;
                    }
                    else
                    {
                        l = j - count;
                        if (l < 0)
                            l = 1;
                        g = count + l;
                        if (count >= s)
                        {
                            for (int k = l; k <= g; k++)
                            {
                                y1[k][i] = a[k][i];
                                cell[k][i] = 1;
                            }
                            colseg++;
                            count = 0;
                            l = 0;
                            g = 0;
                        }
                    }
                }
            }
            for (int i = 0; i < m; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    if (cell[i][j] != 0)
                    {
                        y1[i][j] = a[i][j];
                        cell[i][j] = 1;
                            
                        count++;
                        g = j;
                    }
                    else
                    {
                        
                        if (count < s)
                        {
                            if (g < n)
                            y1[i][g] = cell[i][g] = 0;
                        }
                        else
                        {

                            cluster++;
                        }
                        count = 0;
                    }
                }
            }
        }
        public void pattern_match(ref float[][] hyp, ref float[][] a, float[][] r,int chnnl,int dep,int hscn, int m, int n) // pattern matching and object identification
        {
            float k = 0;
            

            for (int i = 0; i < m; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    k = a[i][j];
                    if (((hyp[i][j] * a[i][j]) == 1) && (0.4 <= r[i][j] && r[i][j] <= 0.5))
                    {
                                a[i][j] = 11;

                                    dtpt++;
                                
                            }
                        
                    else
                    {
                        a[i][j] = k;
                    }
                }
            }
            if (dtpt >2)
            {
                if (dtpt > temp)
                {
                    temp = dtpt;
                    dtdep = dep;
                    dtchnl = chnnl;
                    dtscn = hscn;
                 }
                
            }
            dtpt = 0;
        }
        public void hyper_recog(ref float[][] hp,int hscn,int hdep, int m, int n) // hyperbolic pattern generator
        {

            int h = 0;
            for (int i = 0; i < m; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    if (j > 1)
                    {
                        h = ((j - 1) * (j - 1) / 10) - ((i - hscn) * (i - hscn) / 9);
                        if (h == 1 )
                        {
                            hp[i][j] = 1;
                        }
                        else
                        {
                            hp[i][j] = 0;
                        }

                    }
                }
            }


        }
        private void button1_Click(object sender, EventArgs e)
        {
            fileNameTextBox.Text = "";
            string filename = "";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                if ((openFileDialog1.OpenFile()) != null)
                {
                    filename = openFileDialog1.FileName;
                    fileNameTextBox.Text = openFileDialog1.FileName;
                    Debug.WriteLine(filename);
                }

                H5.Open();
                var h5 = H5F.open(filename, H5F.OpenMode.ACC_RDONLY);
                var dataset = H5D.open(h5, "/radarData/data");
                var Space = H5D.getSpace(dataset);
                var size = H5S.getSimpleExtentDims(Space);


                float[, , , ,] dataarray = new float[size[0], size[1], size[2], size[3], size[4] * 2];
                var wrapArray = new H5Array<float>(dataarray);
                NOS = (int)size[2]; //number_of_scan (X)
                NOC = (int)size[3]; //Number of channel (antenna) (Y)
                NOF = (int)size[4]; // Number of frequency (Z)
                if (NOS < NOF)
                {
                    m = NOS;
                    last=n = 50;
                }
                else
                {
                    m = NOS;
                    last =n=50;
                }
                x = new float[m][];
                xb = new float[m][];
                c1 = new float[m][];
                y1 = new float[m][];
                for (int i = 0; i < m; i++)
                {
                   
                    x[i] = new float[n];
                    xb[i] = new float[n];
                    c1[i] = new float[n];
                    y1[i] = new float[n];
                    for (int j = 0; j < n; j++)
                    {
                        y1[i][j] = 0;
                        x[i][j] = 0;
                    }
                }
                textBox1.Text = size[2].ToString();
                textBox2.Text = size[4].ToString();
                textBox3.Text = size[3].ToString();
                var dataType = H5D.getType(dataset);

                H5D.read<float>(dataset, dataType, wrapArray);
                data = new float[size[2], size[3], size[4] * 2];
                var xd = data.Length;

                Debug.WriteLine(xd);

                for (int k = 0; k < size[2]; k++)
                {
                    for (int i = 0; i < size[3]; i++)
                    {
                        for (int j = 0; j < size[4] * 2; j++)
                        {
                            data[k, i, j] = dataarray[0, 0, k, i, j];
                        }
                    }
                }
                // res = 10; //10mm
                res = 1; //100mm
                n_o_s = NOS; //640;// 510;//number of files
                n_o_c = NOC * res;//100;// NOC* res; //* res; //for outdoor =NOC * res for indoor=100; 150 for outdoor as after 15th channel readings were not proper
                n_o_f = NOF;
                grid = new float[NOS, NOC * res, NOF];
                 
                

                for (int k = 0; k < NOS; k++) //100mm
                {
                    for (int i = 0; i < NOC; i++)
                    {
                        for (int j = 0; j < NOF; j++)
                        {
                            grid[k, i, j] = data[k, i, j * 2];
                        }
                    }
                }
                
                H5.Close(); 
            }
            hscn = 0; depth = 0; dtscn = 0; dtdep = 0; chnl = 0; dtchnl = 0;
            imagescanner(0, 0, 0);
           
        }
        public void plotBscan(int y)
        {

            Bscan = new Bitmap(NOS, last - leaveFirst);
            float max = -1000f, min = 1000f;
            float GS = 0;
            for (int i = 0; i < xb.GetLength(0); i++)
            {
                for (int j = leaveFirst; j < last; j++)
                {
                    if (max < xb[i][j])
                        max = xb[i][j];
                    if (min > xb[i][j])
                        min = xb[i][j];
                }
            }
            GS = (max - min) / 256.0f;
            for (int i = 0; i < xb.GetLength(0); i++)
            {
                for (int j = leaveFirst; j < last; j++)
                {
                    int value = (int)((xb[i][j] - min) / GS);
                    if (value > 255)
                        value = 255;
                    if (value < 1)
                        value = 0;
                    Bscan.SetPixel(i, j - leaveFirst, Color.FromArgb(value, value, value));
                }
            }
            Image<Bgr, byte> img = new Image<Bgr, byte>(Bscan).Resize(400, 400, Emgu.CV.CvEnum.INTER.CV_INTER_LINEAR, true);
            imgbraw.Image = img;
        }
        public void plotBscanout(int y)
        {

            Bscan = new Bitmap(NOS, last - leaveFirst);
            float max = -1000f, min = 1000f;
            float GS = 0;
            for (int i = 0; i < xb.GetLength(0); i++)
            {
                for (int j = leaveFirst; j < last; j++)
                {
                    if (max < xb[i][j])
                        max = xb[i][j];
                    if (min > xb[i][j])
                        min = xb[i][j];
                }
            }
            
            GS = (max - min) / 256.0f;
            for (int i = 0; i < xb.GetLength(0); i++)
            {
                for (int j = leaveFirst; j < last; j++)
                {
                    float value = xb[i][j];
                    int v = 0;
                    if (value > 0)
                        v = 255;
                    if (value == 0)
                        v = 0;
                    if (value < 0)
                        v = 255;
                    Bscan.SetPixel(i, j - leaveFirst, Color.FromArgb(v, v, v));
                }
            }
            Image<Bgr, byte> img = new Image<Bgr, byte>(Bscan).Resize(400, 400, Emgu.CV.CvEnum.INTER.CV_INTER_LINEAR, true);
            imgbout.Image = img;
        }
        public void plotCscanout(int y)
        {

            Bscan = new Bitmap(m, last - leaveFirst);
            
            float max = -1000f, min = 1000f;
            float GS = 0;
            for (int i = 0; i < x.GetLength(0); i++)
            {
                for (int j = leaveFirst; j < last; j++)
                {
                    if (max < x[i][j])
                        max = x[i][j];
                    if (min > x[i][j])
                        min = x[i][j];
                }
            }
            GS = (max - min) / 256.0f;
            for (int i = 0; i < x.GetLength(0); i++)
            {
                for (int j = leaveFirst; j < last; j++)
                {
                    float value = x[i][j];
                    float hv = c1[i][j];
                    int r = 0, g = 0, b = 0;
                    if (value == 0)
                    {
                        r = 0; g = 0; b = 0;
                    }
                    if (value == 1)
                    {
                        r = 255; g = 255; b = 255;
                    } if (value == 11)
                    {
                        r = 255; g = 0; b = 0;
                    }
                    Bscan.SetPixel(i, j - leaveFirst, Color.FromArgb(r, g, b));

                }
            }
            Image<Bgr, byte> img = new Image<Bgr, byte>(Bscan).Resize(400, 400, Emgu.CV.CvEnum.INTER.CV_INTER_LINEAR, true);
            imgcout.Image = img;
        }
        public void plotgraphout(int y)
        {
            
            Bscan = new Bitmap(m, last - leaveFirst);
            float max = -1000f, min = 1000f;
            float GS = 0;
            for (int i = 0; i < c1.GetLength(0); i++)
            {
                for (int j = leaveFirst; j < last; j++)
                {
                    if (max < c1[i][j])
                        max = c1[i][j];
                    if (min > c1[i][j])
                        min = c1[i][j];
                }
            }
            GS = (max - min) / 256.0f;
            for (int i = 0; i < c1.GetLength(0); i++)
            {
                for (int j = leaveFirst; j < last; j++)
                {
                    int value = (int)c1[i][j];
                    if (value == 0)
                    {
                        value = 255;
                    }
                    else
                    {
                        value = 0;
                    }
                    Bscan.SetPixel(i, j - leaveFirst, Color.FromArgb(value, value, value));
                }
            }

            Image<Bgr, byte> img = new Image<Bgr, byte>(Bscan).Resize(400, 400, Emgu.CV.CvEnum.INTER.CV_INTER_LINEAR, true);
            imgCraw.Image = img;
        }
        
        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            hscn = 0; depth = 0; dtscn = 0; dtdep = 0; chnl = 0; dtchnl = 0;
            progressBar1.Value = 0; progressBar2.Value = 0; progressBar3.Value = 0;
            /***** image scanning per channel*****/
            while (chnl < 31)
            {
                depth = 0;
                while (depth < 50)
                {
                    imagescanner(0,depth,chnl);
                        hscn = 0;
                    /***** Pattern reconization****/
                        while (hscn < m)
                        {
                            hyper_recog(ref c1, hscn, hdep, m, n);
                            plotgraphout(res);
                            pattern_match(ref c1, ref x, y1,chnl, depth,hscn,m, n);
                            plotCscanout(res);
                            progressBar2.Value = hscn * progressBar2.Maximum / m;
                            Application.DoEvents();
                            hscn++;
                          
                        }
                       
                        Application.DoEvents();
                    progressBar3.Value = depth * progressBar3.Maximum / 49;
                    Application.DoEvents();
                        
                    depth=depth+10;
                }
                progressBar1.Value = chnl * progressBar1.Maximum / 30;
                Application.DoEvents();
                chnl++;
            }
            /*****Object Detection****/
            imagescanner(0, dtdep, dtchnl);
            hyper_recog(ref c1, dtscn, dtdep, m, n);
            plotgraphout(res);
            pattern_match(ref c1, ref x, y1, dtchnl, dtdep, dtscn, m, n);
            plotCscanout(res);
                           
            depthtb.Text = dtdep.ToString();
            chnltb.Text = dtchnl.ToString();
            scantb.Text = dtscn.ToString();
            
               
        }

        private void imgbraw_Click(object sender, EventArgs e)
        {

        }

        private void progressBar1_Click(object sender, EventArgs e)
        {

        }

        private void progressBar3_Click(object sender, EventArgs e)
        {

        }

        private void label8_Click(object sender, EventArgs e)
        {

        }

        private void label6_Click(object sender, EventArgs e)
        {

        }

        private void imgbout_Click(object sender, EventArgs e)
        {

        }

        private void label5_Click(object sender, EventArgs e)
        {

        }

        private void imgCraw_Click(object sender, EventArgs e)
        {

        }

        private void label11_Click(object sender, EventArgs e)
        {

        }
    }
}
