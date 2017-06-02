using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
//添加shp文件对应增加的引入，有三个：DataSourcesFile、Geodatabase和Carto
using ESRI.ArcGIS.DataSourcesFile;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.esriSystem;  
using System.Runtime.InteropServices;
//执行SQL语句需要的引用
using Microsoft.SqlServer;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Types;

//加入正则表达式，去除字符串中的字母
using System.Text.RegularExpressions;



namespace Test0311
{

    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        //指定了连接的数据库，数据库名字是school,且在SQLServer中已经存在
        //Initial Catalog后面跟你数据库的名字——OK
        SqlConnection myconnection = new SqlConnection("Data Source=(local);Initial Catalog=school;Integrated Security=True");
        private void Form1_Load(object sender, EventArgs e)
        {

           
        }

        
        //连接SQL数据库菜单的点击事件——OK
        private void 连接SQL数据库ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                myconnection.Open();     //打开数据库
                label1.Text = "数据库连接成功！";
            }catch (Exception ee)
            {

                MessageBox.Show("数据库连接失败！" + ee.ToString());

            }
        }
        //关闭数据库对应函数
        public string DisConnect()
        {
            string Result;
            try
            {
                myconnection.Close();

                Result = "数据连接已断开！";

            }
            catch (Exception e)
            {

                MessageBox.Show("数据库断开失败！" + e.ToString());

                Result = "连接成功！";

            }
            return Result;
        }
        private void 关闭数据库ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //调用关闭数据库的函数实现关闭数据库
            label1.Text = DisConnect();
        }
        
        
        //读取到的WKT信息
        private string wktString;
        public string WktString
        {
            get { return wktString; }
            set { wktString = value; }
        }
        // 读取Shapefile信息
        //借助GDAL开源工具
        int insertPointNum = 0;
        int insertLineNum = 0;
        int insertPolygonNum = 0;
        //关键的函数，读取shp文件，将数据转换为WKT数据——OK
        public string ReadSHP(string path)
        {
            string strMessage = "";
            OSGeo.OGR.Ogr.RegisterAll();
            OSGeo.OGR.Driver dr = OSGeo.OGR.Ogr.GetDriverByName("ESRI shapefile");
            if (dr == null)
            {
                MessageBox.Show("文件不能打开，请检查");
                return "";
            }

            OSGeo.OGR.DataSource ds = dr.Open(path, 0);
            int layerCount = ds.GetLayerCount();

            OSGeo.OGR.Layer layer = ds.GetLayerByIndex(0);

            //投影信息
            OSGeo.OSR.SpatialReference coord = layer.GetSpatialRef();
            string coordString;
            coord.ExportToWkt(out coordString);
            OSGeo.OGR.Feature feat;
            string wkt;
            string strWkt = string.Empty;
            //读取shp文件
            string createSQL = "";
            string strSQL1 = "";
            int num = 1;
            int num1 = 1;
            int num2 = 1;
            int num3 = 1;
            //读取数据，同时实现创建数据库表和插入表中数据
            while ((feat = layer.GetNextFeature()) != null)
            {
                OSGeo.OGR.Geometry geometry = feat.GetGeometryRef();
                OSGeo.OGR.wkbGeometryType goetype = geometry.GetGeometryType();
                geometry.ExportToWkt(out wkt);
                //POINT (40.646160125732422 64.520668029785156)
                strWkt += wkt + "\n";
                //点，插入数据到点对应数据库             
                string typeName = layer.GetGeomType().ToString();
                //得到数据的空间数据类型：wkbPoint、wkbLineString或者wkbPolygon
                if (typeName.Equals("wkbPoint"))
                {

                    if (num1 == 1)
                    {
                        //只会执行一次：创建点对应数据库表
                        createSQL = "create table point(id int IDENTITY(1,1),location geometry);";

                        SqlDataAdapter objDataAdpter = new SqlDataAdapter();

                        SqlCommand thisCommand = new SqlCommand(createSQL, myconnection);

                        thisCommand.ExecuteNonQuery();
                        MessageBox.Show("点，对应数据库表已经创建完成！");
                        num1++;
                    }
                    insertPointNum = layer.GetFeatureCount(0);
                    if (num <= layer.GetFeatureCount(0))
                    {
                        //将所有点数据插入数据库,向其指派相应的空间引用标识（SRID）,即num，和id一样。
                        strSQL1 = "insert into point(location) values(geometry::STGeomFromText('" + wkt + "'," + num + "));";
                        SqlDataAdapter objDataAdpter = new SqlDataAdapter();

                        SqlCommand thisCommand = new SqlCommand(strSQL1, myconnection);

                        thisCommand.ExecuteNonQuery();

                        num++;
                    }


                }

                //是线，插入数据到线对应表中
                if (typeName.Equals("wkbLineString"))
                {

                    if (num2 == 1)
                    {
                        //只会执行一次：创建线对应数据库表
                        createSQL = "create table polyline(id int IDENTITY(1,1),location geometry);";
                        SqlDataAdapter objDataAdpter = new SqlDataAdapter();

                        SqlCommand thisCommand = new SqlCommand(createSQL, myconnection);

                        thisCommand.ExecuteNonQuery();
                        MessageBox.Show("线，对应数据库表已经创建完成！");
                        num2++;
                    }
                    insertLineNum = layer.GetFeatureCount(0);
                    if (num <= layer.GetFeatureCount(0))
                    {
                        //将所有线数据插入数据库
                        strSQL1 = "insert into polyline(location) values(geometry::STGeomFromText('" + wkt + "'," + num + "))";
                        SqlDataAdapter objDataAdpter = new SqlDataAdapter();

                        SqlCommand thisCommand = new SqlCommand(strSQL1, myconnection);

                        thisCommand.ExecuteNonQuery();

                        num++;
                    }
                }
                //是多边形（多边形集合）
                if (typeName.Equals("wkbPolygon"))
                {
                    if (num3 == 1)
                    {
                        //只会执行一次：创建多边形对应数据库表
                        createSQL = "create table polygon(id int IDENTITY(1,1),location geometry);";

                        SqlDataAdapter objDataAdpter = new SqlDataAdapter();

                        SqlCommand thisCommand = new SqlCommand(createSQL, myconnection);

                        thisCommand.ExecuteNonQuery();
                        MessageBox.Show("多边形，对应数据库表已经创建完成！");
                        num3++;
                    }
                    insertPolygonNum = layer.GetFeatureCount(0);
                    if (num <= layer.GetFeatureCount(0))
                    {
                        //将所有多边形数据插入数据库
                        strSQL1 = "insert into polygon(location) values(geometry::STGeomFromText('" + wkt + "'," + num + "))";
                        SqlDataAdapter objDataAdpter = new SqlDataAdapter();

                        SqlCommand thisCommand = new SqlCommand(strSQL1, myconnection);

                        thisCommand.ExecuteNonQuery();

                        num++;
                    }
                }



            }
            strMessage += "该文件有：" + layerCount + "层";
            strMessage += Environment.NewLine;
            strMessage += "该文件坐标信息为:" + coordString;
            strMessage += Environment.NewLine;
            strMessage += "几何类型:" + layer.GetGeomType();//shp的类型
            strMessage += Environment.NewLine;
            strMessage += "该文件共有:" + layer.GetFeatureCount(0).ToString() + "记录";
            strMessage += Environment.NewLine;
            strMessage += strWkt;
            return strMessage;
        }


        //添加shp的菜单项对应的点击事件：代码如下——OK
        private void menuAddShp_Click(object sender, EventArgs e)
        {
            //1、创建工作空间工厂
            IWorkspaceFactory pWorkspaceFactory = new ShapefileWorkspaceFactory();
            //C:\Program Files (x86)\ArcGIS\DeveloperKit10.1\Samples\data\World   这是ArcGIS按照目录下面的数据
            //2、打开ShapeFile文件名对应的工作空间

            //新：增加新的功能：文件属性过滤
            openFileDialog1.Filter = "ShapeFile文件(*.shp)|*.shp";
            //新：设定文件对话框的初始化路径
            openFileDialog1.InitialDirectory = @"E:\毕业论文资料\河南交通图";
            //新：实例数据文件夹
            openFileDialog1.Multiselect = false;
            DialogResult pDialogResult = openFileDialog1.ShowDialog();
            if (pDialogResult != DialogResult.OK)
            {
                return;
            }
            //新：得到文件名对应的路径、文件等
            string pPath = openFileDialog1.FileName;
            string pFolder = System.IO.Path.GetDirectoryName(pPath);
            string pFileName = System.IO.Path.GetFileName(pPath);
            txtInput.Text = pPath;
            IWorkspace pWorkspace = pWorkspaceFactory.OpenFromFile(pFolder, 0);
            IFeatureWorkspace pFeatureWorkspace = pWorkspace as IFeatureWorkspace;
            IFeatureClass pFc = pFeatureWorkspace.OpenFeatureClass(pFileName);
            // IWorkspace pWorksapce1 = pWorkspaceFactory.OpenFromFile(@"C:\Program Files (x86)\ArcGIS\DeveloperKit10.1\Samples\data\World", 0);
            // IFeatureWorkspace pFeatureWorkspace = pWorksapce1 as IFeatureWorkspace;    
            //3、打开要素类，这里指定了数据 Word30.shp
            //IFeatureClass pFc = pFeatureWorkspace.OpenFeatureClass("world30.shp");
            //4、创建要素图层
            IFeatureLayer pFLayer = new FeatureLayerClass();
            pFLayer.FeatureClass = pFc;
            pFLayer.Name = pFc.AliasName;
            //5、关联图层和要素类
            ILayer pLayer = pFLayer as ILayer;
            IMap pMap = axMapControl1.Map;
            //6、添加到地图控件中
            pMap.AddLayer(pLayer);
            //刷新一下
            axMapControl1.ActiveView.Refresh();
        }

        //退出按钮对应事件,关闭窗体
        private void button2_Click(object sender, EventArgs e)
        {
            //整个窗体进行关闭操作
            this.Dispose();

        }
        //查询菜单项，在dataGridView1控件中显示数据库表的信息,但没有效果，下拉列表有“点、线、面”,点击点、线或者面才有效果——OK
        private void 查询数据库ToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        //按钮显示WKT事件：出现弹框显示信息——OK
        private void 生成wkt数据ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {

                //调用读取Shapefile shp信息函数，返回前面定义好的字符串数据，在执行这里，已经读取了WKT，并创建了数据库表，并向表中增加完成了数据。
                wktString = ReadSHP(txtInput.Text);
                InfoFrm info = new InfoFrm(wktString);
                //弹出新的窗体显示WKT信息。
                info.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);

            }
        }
        string lineText = "";
        string polygonText = "";
        //查询点数据——OK
        private void 点ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                //对应点查询方法显示坐标：select location.STX,location.STY from point;
                string SQL = "select * From point";

                SqlDataAdapter objDataAdpter = new SqlDataAdapter();

                objDataAdpter.SelectCommand = new SqlCommand(SQL, myconnection);

                DataSet ds = new DataSet();

                objDataAdpter.Fill(ds, "城市表");

                dataGridView1.DataSource = ds.Tables[0];
                //数据库操作类
                SqlCommand cmd = myconnection.CreateCommand();
              
                cmd.CommandText = "select location.STX as X,location.STY as Y from point  ";
                //从数据库中读取数据流存入reader中                      
                SqlDataReader reader1 = cmd.ExecuteReader();

                int a = 0;
                //循环之前生成一个点图层
                ILayer ly = CreatePointShapeFile();
                while (reader1.Read())
                {
                    //得到XY坐标
                    Double douX = reader1.GetDouble(0);
                    Double douY = reader1.GetDouble(1);
                    //执行addPoint函数，在已经生成的点图层中循环添加点坐标，a用来记录点数
                    if (addPoint(ly, douX, douY))
                    {
                        a++;
                    }

                }
                MessageBox.Show("点图层生成成功,点的数量是"+a+"。");
                //所有的点数据都放到了IPointCollection中了
                axMapControl1.Extent = axMapControl1.FullExtent;
                reader1.Close();
            }
            catch (Exception ee)
            {

                MessageBox.Show("查询失败！" + ee.ToString());

            }
        }
        //查询线数据——OK 
        private void 线ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                //对应点查询方法显示坐标：select location.STAsText() from stringline;
                string SQL = "select * From polyline";

                SqlDataAdapter objDataAdpter = new SqlDataAdapter();

                objDataAdpter.SelectCommand = new SqlCommand(SQL, myconnection);

                DataSet ds = new DataSet();

                objDataAdpter.Fill(ds, "高速公路表");

                dataGridView1.DataSource = ds.Tables[0];
               

                //创建数据库命令  
                SqlCommand cmd = myconnection.CreateCommand();
                //创建查询语句  
                cmd.CommandText = "select location.STAsText() as location from polyline";
                //从数据库中读取数据流存入reader中                      
                SqlDataReader reader1 = cmd.ExecuteReader();
                String str1 = "";
                String str2 = "";
                String str3 = "";
                String str4 = "";
                String str5 = "";
                String str6 = "";
                int num = 1;
                ILayer ly = CreateLineShapeFile();
                IGeometryCollection pGeometryCollection = new PolylineClass();  
                while (reader1.Read())
                {
                    //多边形数据返回直接是字符类型
                    lineText = reader1.GetString(0);
                    //将字符串中的的英文字母和首尾空格去掉
                    str1 = Regex.Replace(lineText, "[a-zA-Z]", "");
                    str2 = str1.Trim();
                    str3 = str2.Replace("(", "");
                    str4 = str3.Replace(")", "");
                    str5 = str4.Replace(", ", "\n");
                    str6 = str5.Replace(" ", ",");
                    str6 += ",公路" + num + "";           
                    //按照行读取，一次读取一行           
                                      
                    ILine pLine = new LineClass();                   
                    object missing = Type.Missing;
                    using (StringReader sr = new StringReader(str6))
                    {
                        string line = "";
                        int a = 0;
                        IPointCollection pPointCollection = new PathClass();  
                                         
                        while ((line = sr.ReadLine()) != null)
                        {

                            //此时line就已经是一个点坐标和地点1包含的文本了
                            String[] xyandname = line.Split(',');
                            String x = xyandname[0];
                            string y = xyandname[1];                        
                            //获取到了点的XY坐标和点所属于的面的名称

                            double dx = Convert.ToDouble(x);
                            double dy = Convert.ToDouble(y);
                            IPoint pPoint = new PointClass();
                            pPoint.X = dx;
                            pPoint.Y = dy;
                            pPointCollection.AddPoint(pPoint,ref missing,ref missing);                          
                            a++;                                                                                                                            
                        }
                        pGeometryCollection.AddGeometry(pPointCollection as IGeometry, ref missing, ref missing); 
                    }

                    IPolyline pline = pGeometryCollection as IPolyline;
                    addLine(ly, pline);                  
                    num++;                 
                }
                MessageBox.Show("线图层生成成功,线的数量是"+num+"。");
               
                reader1.Close();
            }catch (Exception ee)
            {

                MessageBox.Show("查询失败！" + ee.ToString());

            }
        }
        //查询多边形数据
        string shapeFileFullName = "";
        string surveyDataFullName = string.Empty;
        List<string> pColumns = new List<string>();
        List<CPoint> pCPointList = new List<CPoint>();
        private void 面ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                //对应点查询方法显示坐标：select location.STAsText() from stringline;
                string SQL = "select * From polygon";

                SqlDataAdapter objDataAdpter = new SqlDataAdapter();

                objDataAdpter.SelectCommand = new SqlCommand(SQL, myconnection);

                DataSet ds = new DataSet();

                objDataAdpter.Fill(ds, "多边形表");

                dataGridView1.DataSource = ds.Tables[0];

                //为了得到shp文件的名字

                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "Shape文件(*.shp)|*.shp";
                DialogResult dialogresult = saveFileDialog.ShowDialog();
                if (dialogresult == DialogResult.OK)
                {
                    shapeFileFullName = saveFileDialog.FileName;
                }
                else
                {
                    shapeFileFullName = null;
                    return;
                }

                //创建数据库命令  
                SqlCommand cmd = myconnection.CreateCommand();
                //创建查询语句  
                cmd.CommandText = "select location.STAsText() as location from polygon";
                //从数据库中读取数据流存入reader中                      
                SqlDataReader reader1 = cmd.ExecuteReader();
                String str1 = "";
                String str2 = "";
                String str3 = "";
                String str4 = "";
                String str5 = "";
                String str6 = "";
                int num =1;
                //点结合，存放点             
                object o = Type.Missing;
                ILayer ly;
                while (reader1.Read())
                {
                    //多边形数据返回直接是字符类型
                    polygonText = reader1.GetString(0);
                    //将字符串中的的英文字母和首尾空格去掉
                    str1 = Regex.Replace(polygonText, "[a-zA-Z]", "");
                    str2 = str1.Trim();
                    str3 = str2.Replace("(", "");
                    str4 = str3.Replace(")", "");
                    str5 = str4.Replace(", ", ",多边形" + num + "\n");
                    str6 = str5.Replace(" ", ",");
                    str6 += ",多边形" + num + "";
                   
                    //循环一次，就是一个点集合，才能保证上一个多边形的最后一个点不和下一个多边形的第一个点产生链接
                    //按照行读取，一次读取一行
                    using (StringReader sr = new StringReader(str6))
                    {
                        string line;
                        while ((line = sr.ReadLine()) != null)
                        {
                                         
                          //此时line就已经是一个点坐标和地点1包含的文本了
                            String[] xyandname = line.Split(',');
                            for (int j = 0; j < xyandname.Length; j++)
                            {
                                pColumns.Add(xyandname[j]);      //将字符串数组中的内容复制给 列List
                            }
                            String x = xyandname[0];
                            string y = xyandname[1];
                            string name = xyandname[2];

                            //获取到了点的XY坐标和点所属于的面的名称

                            double dx = Convert.ToDouble(x);
                            double dy = Convert.ToDouble(y);
                            CPoint pCpoint = new CPoint();
                            pCpoint.x = dx;
                            pCpoint.y = dy;
                            pCpoint.name = name;
                            pCPointList.Add(pCpoint);

                        }

                    }
                    num++;

                }
                ly = CreateShpFromPoints(shapeFileFullName);
                MessageBox.Show("面图层生成成功,面的数量是"+num+"。");
                string fileFullPath = saveFileDialog.FileName;
                int index = fileFullPath.LastIndexOf("\\");
                string fileName = fileFullPath.Substring(index + 1);
                string filePath = fileFullPath.Substring(0, index);
                //关联图层显示,两个参数：文件路径和文件名称
                axMapControl1.AddShapeFile(filePath, fileName);
                reader1.Close();
            }
            catch (Exception ee)
            {

                MessageBox.Show("查询失败！" + ee.ToString());

            }
        }
             
        //0416：成功创建点图层,但没有数据——OK
        private ILayer CreatePointShapeFile()
        {
            //保存文件的对话框
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            //设定文件类型
            saveFileDialog.Filter = "Shape文件（*.shp）|*.shp";
           // saveFileDialog.Title = "新建点形shp文件";
            //不进行文件是否存在检查
            saveFileDialog.CheckFileExists = false;

            DialogResult dialogResult = saveFileDialog.ShowDialog();
            IWorkspaceFactory pWorkspaceFactory = new ShapefileWorkspaceFactory();
            int index;
            string fileFullPath;
            string fileName;
            string filePath;
            //当用户选择OK，窗体关闭，ShowDialog函数执行完毕，它（模态对话框）的DialogResult值就是DialogResult.OK。
            if (dialogResult == DialogResult.OK)
            {
                //获取用户保存的文件的名字和路径
                fileFullPath = saveFileDialog.FileName;
                index = fileFullPath.LastIndexOf("\\");
                fileName = fileFullPath.Substring(index + 1);
                filePath = fileFullPath.Substring(0, index);
                if (System.IO.File.Exists(saveFileDialog.FileName))//检查文件是否存在
                {
                    if (MessageBox.Show("该文件夹下已经有同名文件，替换原文件？", "询问", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK)
                    {
                        IFeatureWorkspace FWS = pWorkspaceFactory.OpenFromFile(filePath, 0) as IFeatureWorkspace;
                        IFeatureClass pFeatureClass = FWS.OpenFeatureClass(fileName);
                        IDataset pDataset = pFeatureClass as IDataset;
                        pDataset.Delete();
                    }
                    //System.IO.File.Delete(saveFileDialog.FileName);
                    else
                        return null;
                }

            }
            else
            {
                fileFullPath = null;
                return null;
            }

            //创建图层
            //定义一个字段集合对象 
            IFields pFields = new FieldsClass();
            IFieldsEdit pFieldsEdit = pFields as IFieldsEdit;
            //定义单个的字段 
            IField pField = new FieldClass();
            IFieldEdit pFieldEdit = pField as IFieldEdit;
            //定义单个的字段，并添加到字段集合中 
            pFieldEdit.Name_2 = "Shape";
            pFieldEdit.Type_2 = esriFieldType.esriFieldTypeGeometry;
            //GeometryDef是用来设计几何字段的
            IGeometryDef pGeometryDef = new GeometryDef();
            IGeometryDefEdit pGeometryDefEdit = pGeometryDef as IGeometryDefEdit;
            pGeometryDefEdit.GeometryType_2 = esriGeometryType.esriGeometryPoint;//点、线、面
            pGeometryDefEdit.SpatialReference_2 = axMapControl1.SpatialReference;
            pFieldEdit.GeometryDef_2 = pGeometryDef;

            pFieldsEdit.AddField(pField);
            //管理基于矢量数据
            IFeatureWorkspace pFeatureWorkspace = pWorkspaceFactory.OpenFromFile(filePath, 0) as IFeatureWorkspace;

            int i = fileName.IndexOf(".shp");
            //0表示第一个字符,1表示第二个字符依此类推,如果说没有找到则返回 -1 
            if (i == -1)
                pFeatureWorkspace.CreateFeatureClass(fileName + ".shp", pFields, null, null, esriFeatureType.esriFTSimple, "Shape", "");
            else
                pFeatureWorkspace.CreateFeatureClass(fileName, pFields, null, null, esriFeatureType.esriFTSimple, "Shape", "");

            axMapControl1.AddShapeFile(filePath, fileName);

            int count = this.axMapControl1.LayerCount;
            //最新的图层第一位显示，会覆盖下面的图层
            return this.axMapControl1.get_Layer(0);

        }
        //0416：成功创建线图层,但没有数据——OK
        private ILayer CreateLineShapeFile()
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Shape文件（*.shp）|*.shp";
            saveFileDialog.Title = "新建点形shp文件";
            saveFileDialog.CheckFileExists = false;

            DialogResult dialogResult = saveFileDialog.ShowDialog();
            IWorkspaceFactory pWorkspaceFactory = new ShapefileWorkspaceFactory();
            int index;
            string fileFullPath;
            string fileName;
            string filePath;
            if (dialogResult == DialogResult.OK)
            {
                fileFullPath = saveFileDialog.FileName;
                index = fileFullPath.LastIndexOf("\\");
                fileName = fileFullPath.Substring(index + 1);
                filePath = fileFullPath.Substring(0, index);
                if (System.IO.File.Exists(saveFileDialog.FileName))//检查文件是否存在
                {
                    if (MessageBox.Show("该文件夹下已经有同名文件，替换原文件？", "询问", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK)
                    {
                        IFeatureWorkspace FWS = pWorkspaceFactory.OpenFromFile(filePath, 0) as IFeatureWorkspace;
                        IFeatureClass pFeatureClass = FWS.OpenFeatureClass(fileName);
                        IDataset pDataset = pFeatureClass as IDataset;
                        pDataset.Delete();
                    }
                    //System.IO.File.Delete(saveFileDialog.FileName);
                    else
                        return null;
                }

            }
            else
            {
                fileFullPath = null;
                return null;
            }

            //创建图层
            IFields pFields = new FieldsClass();
            IFieldsEdit pFieldsEdit = pFields as IFieldsEdit;
            IField pField = new FieldClass();
            IFieldEdit pFieldEdit = pField as IFieldEdit;

            pFieldEdit.Name_2 = "Shape";
            pFieldEdit.Type_2 = esriFieldType.esriFieldTypeGeometry;

            IGeometryDef pGeometryDef = new GeometryDef();
            IGeometryDefEdit pGeometryDefEdit = pGeometryDef as IGeometryDefEdit;
            pGeometryDefEdit.GeometryType_2 = esriGeometryType.esriGeometryPolyline;//点、线、面
            pGeometryDefEdit.SpatialReference_2 = axMapControl1.SpatialReference;
            pFieldEdit.GeometryDef_2 = pGeometryDef;

            pFieldsEdit.AddField(pField);

            IFeatureWorkspace pFeatureWorkspace = pWorkspaceFactory.OpenFromFile(filePath, 0) as IFeatureWorkspace;

            int i = fileName.IndexOf(".shp");
            if (i == -1)
                pFeatureWorkspace.CreateFeatureClass(fileName + ".shp", pFields, null, null, esriFeatureType.esriFTSimple, "Shape", "");
            else
                pFeatureWorkspace.CreateFeatureClass(fileName, pFields, null, null, esriFeatureType.esriFTSimple, "Shape", "");

            axMapControl1.AddShapeFile(filePath, fileName);
            int count = this.axMapControl1.LayerCount;
            return this.axMapControl1.get_Layer(0);
        }      
        //0416成功实现：向点图层里面增加点——OK
        private bool addPoint(ILayer layer, double x, double y)
        {


            IFeatureLayer fLayer = layer as IFeatureLayer;

            IFeatureClass fc = fLayer.FeatureClass;

            IFeatureClassWrite fr = fc as IFeatureClassWrite;

            IWorkspaceEdit workSpace = (fc as IDataset).Workspace as IWorkspaceEdit;

            IFeature feature;

            IPoint pt;
            IPointCollection pPointColl = new PolygonClass();

            workSpace.StartEditing(true);

            workSpace.StartEditOperation();

            feature = fc.CreateFeature();

            IFields fields = feature.Fields;
            pt = new PointClass();

            pt.X = x;

            pt.Y = y;
            pPointColl.AddPoint(pt);

            feature.Shape = pt;

            fr.WriteFeature(feature);

            workSpace.StopEditOperation();

            workSpace.StopEditing(true);

            this.axMapControl1.ActiveView.Refresh();

            return true;

        }
        //0418:自定义的类CPoint,具有和IPoint一样的X和Y,同时增加name属性，作为判断多边形的标识。
        struct CPoint
        {
            public double x;
            public double y;
            public string name;
        }
        //0418:成功创建面Shp文件
        private IFeatureLayer CreateShpFromPoints(string shapeFileFullName)
        {

            int index = shapeFileFullName.LastIndexOf('\\');
            string folder = shapeFileFullName.Substring(0, index);
            //获取shp文件夹
            shapeFileFullName = shapeFileFullName.Substring(index + 1);
            //获取shp文件名
            IWorkspaceFactory pWSF = new ShapefileWorkspaceFactoryClass();
            IFeatureWorkspace pFWS = pWSF.OpenFromFile(folder, 0) as IFeatureWorkspace;
            //如果shapefile存在替换它
            if (File.Exists(shapeFileFullName))
            {
                IFeatureClass featureClass = pFWS.OpenFeatureClass(shapeFileFullName);
                IDataset pDataset = (IDataset)featureClass;
                pDataset.Delete();                           //将里面数据删除
            }
            IFields pFields = new FieldsClass();
            IFieldsEdit pFieldsEdit;
            pFieldsEdit = (IFieldsEdit)pFields;
            IField pField = new FieldClass();
            IFieldEdit pFieldEdit = (IFieldEdit)pField;
            pFieldEdit.Name_2 = "Shape";
            pFieldEdit.Type_2 = esriFieldType.esriFieldTypeGeometry;
            IGeometryDef pGeometryDef = new GeometryDefClass();
            IGeometryDefEdit pGDefEdit = (IGeometryDefEdit)pGeometryDef;
            pGDefEdit.GeometryType_2 = esriGeometryType.esriGeometryPolygon;
            pFieldEdit.GeometryDef_2 = pGeometryDef;
            pFieldsEdit.AddField(pField);
            pField = new FieldClass();
            pFieldEdit = (IFieldEdit)pField;
            pFieldEdit.Length_2 = 20;
            pFieldEdit.Name_2 = pColumns[2];
            pFieldEdit.Type_2 = esriFieldType.esriFieldTypeString;
            pFieldsEdit.AddField(pField);
            IFeatureClass pFeatureClass;
            pFeatureClass = pFWS.CreateFeatureClass(shapeFileFullName, pFields, null, null,
                esriFeatureType.esriFTSimple, "Shape", "");
            List<string> pBuildingList = new List<string>();
            for (int i = 0; i < pCPointList.Count; i++)
            {
                if (pBuildingList.Contains(pCPointList[i].name.Trim()) == false)
                {
                    pBuildingList.Add(pCPointList[i].name.Trim());  //将不一样的名字加了进来
                }
            }
            for (int i = 0; i < pBuildingList.Count; i++)   //遍历不同的名字
            {
                IPointCollection pPointColl = new PolygonClass();
                object o = Type.Missing;
                for (int j = 0; j < pCPointList.Count; j++)
                {
                    if (pCPointList[j].name.Trim() == pBuildingList[i].Trim())
                    {
                        IPoint pPoint = new PointClass();
                        pPoint.X = pCPointList[j].x;
                        pPoint.Y = pCPointList[j].y;
                        pPointColl.AddPoint(pPoint, ref o, ref o);  //相同的名字添加到同一个多边形中
                    }
                }
                if (pPointColl.PointCount > 0)
                {
                    IClone pClone = pPointColl.get_Point(0) as IClone;
                    IPoint pEndPoint = pClone.Clone() as IPoint;        //将第一个点拷贝到最后一个点
                    pPointColl.AddPoint(pEndPoint, ref o, ref o);
                }

                IFeature pFeature = pFeatureClass.CreateFeature();
                pFeature.Shape = pPointColl as IPolygon;
                pFeature.Store();
                pFeature.set_Value(pFeature.Fields.FindField(pColumns[2]), pBuildingList[i].Trim());
                pFeature.Store();
            }
            IFeatureLayer pFeaturelayer = new FeatureLayerClass();
            pFeaturelayer.FeatureClass = pFeatureClass;
            return pFeaturelayer;
        }
        //0419：成功向线图层中增加数据——OK
        private bool addLine(ILayer layer, IPolyline pline)
        {
            IFeatureLayer fLayer = layer as IFeatureLayer;

            IFeatureClass fc = fLayer.FeatureClass;

            IFeatureClassWrite fr = fc as IFeatureClassWrite;

            IWorkspaceEdit workSpace = (fc as IDataset).Workspace as IWorkspaceEdit;
            IFeature feature;       
            workSpace.StartEditing(true);

            workSpace.StartEditOperation();

            feature = fc.CreateFeature();

            IFields fields = feature.Fields;

            feature.Shape = pline;

            fr.WriteFeature(feature);

            workSpace.StopEditOperation();

            workSpace.StopEditing(true);           
            this.axMapControl1.ActiveView.Refresh();
            return true;
        }
    }
}
