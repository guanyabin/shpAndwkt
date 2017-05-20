# shpAndwkt
Windows窗体应用程序：解析shp图层得到wkt信息，并将wkt存入SQLServer数据库，之后再次读取并还原为shp。
说明：
1、软件版本：ArcGIS Engine10.1,SQL Server 2012,VS 2010,是基于VS2010进行二次开发的Windows窗体应用程序。
2、准备实验数据：点、线和多边形的三个shp图层文件。
全部功能：
1、加载shp并显示，同时还有放大、缩小、平移和全图等小功能。
2、SQLServer数据库的连接和关闭。
3、解析当前加载的shp图层（位于第一个显示的），得到其中的WKT信息，例如点图层得到的WKT信息是POINT （x,y）。
4、将得到的WKT信息存入数据库，此时已经连接了数据库，并指定了操作的数据库名称。wkt存入数据库有两步，一是对应创建数据库表，二是循环插入数据。
5、点线和多边形对应point、polyline和polygon三张数据库表。
6、将shp图层中所有的WKT信息解析并存入数据库后，进入后半部分，再次从数据库读取WKT信息，仅依靠wkt信息还原shp。
7、本实验使用的WKT是几何对象的wkt,是文本格式的。使用SQL语句读取对应表的数据，将读取到的字符串进行剥离，得到最基本的x和y坐标，创建IPoint、IPolyline和IPolygon对象，同时对应生成点、线和多边形的ILayer对象，将点、线和多边形对象放ILayer对象中，生成新的shp文件，还原成功。
8、实验数据对应的WKT采用了Point,LineString和polygon,没有使用线集合MultiLinestring和面集合Multipolygon。
