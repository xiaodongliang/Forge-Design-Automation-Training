using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.ApplicationServices.Core;
using Autodesk.AutoCAD.EditorInput;

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.DatabaseServices;
using System.IO;
using Newtonsoft.Json;

namespace PackageNetPlugin
{

    public class CircleParameters
    {
        public double centerX { get; set; }
        public double centerY { get; set; }
        public double radius { get; set; } 
    }


    public class Class1
    {
        void CommonTest()
        {

            Database db = Application.DocumentManager.MdiActiveDocument.Database;
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;


            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead, false);
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite, false);

                DBText dbtxt = new DBText();
                dbtxt.TextString = "Shanghai AutoCAD Design Automation Training!";

                Circle oCircle = new Circle(new Autodesk.AutoCAD.Geometry.Point3d(0, 0, 0), new Autodesk.AutoCAD.Geometry.Vector3d(0, 0, 1), 50);


                btr.AppendEntity(dbtxt);
                btr.AppendEntity(oCircle);

                tr.AddNewlyCreatedDBObject(dbtxt, true);
                tr.AddNewlyCreatedDBObject(oCircle, true);
                tr.Commit();
            }
        }

        void MultiOutputs()
        {
            CommonTest();

            Database db = Application.DocumentManager.MdiActiveDocument.Database;
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;


            // 测试导出zip
           
            var pr = ed.GetString("\nSpecify output folder");
            if (pr.Status != PromptStatus.OK)
                return;

            string outFolder = pr.StringResult;


            ed.WriteMessage("Output Folder:" + outFolder);

            var dwgOut = Path.Combine(outFolder, "MyTest.dwg");
            var dxfOut = Path.Combine(outFolder, "MyTest.dxf");
            var pngOut = Path.Combine(outFolder, "MyTest.png");

            db.SaveAs(dwgOut, DwgVersion.Current);
            db.DxfOut(dxfOut, 16, DwgVersion.Current);

            ed.Command("_grid", "_off");
            ed.Command("_zoom", "_extents");
            ed.Command("_pngout", pngOut, ""); 
           

        }

        void VariousInputs()
        {
            //测试嵌入式参数以及外链参数

            Database db = Application.DocumentManager.MdiActiveDocument.Database;
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;

            
            var pfnr = ed.GetFileNameForOpen("\n输入第一个圆的参数");
            if (pfnr.Status != PromptStatus.OK)
                return;
            var paramFile = pfnr.StringResult;

            var contents = File.ReadAllText(paramFile);
            ed.WriteMessage("\n第一个圆的参数:" + contents.ToString());

            var parameters = JsonConvert.DeserializeObject<CircleParameters>(contents);

            //获取圆心
            var cir1_X = parameters.centerX;
            var cir1_Y = parameters.centerY;
            //获取半径
            var cir1_r = parameters.radius;

            var pfnr2 = ed.GetFileNameForOpen("\n输入第二个圆的参数");
            if (pfnr2.Status != PromptStatus.OK)
                return;
            paramFile = pfnr2.StringResult;

            contents = File.ReadAllText(paramFile);

            parameters = JsonConvert.DeserializeObject<CircleParameters>(contents);
            ed.WriteMessage("\n第二个圆的参数:" + contents.ToString());


            //获取圆心
            var cir2_X = parameters.centerX;
            var cir2_Y = parameters.centerY;
            //获取半径
            var cir2_r = parameters.radius;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead, false);
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite, false);

                DBText dbtxt = new DBText();
                dbtxt.TextString = "Shanghai AutoCAD Design Automation Training!";

                Circle oCircle1 = new Circle(new Autodesk.AutoCAD.Geometry.Point3d(cir1_X , cir1_Y , 0), 
                    new Autodesk.AutoCAD.Geometry.Vector3d(0, 0, 1),
                    cir1_r );

                Circle oCircle2 = new Circle(new Autodesk.AutoCAD.Geometry.Point3d(cir2_X, cir2_Y, 0),
                   new Autodesk.AutoCAD.Geometry.Vector3d(0, 0, 1),
                   cir2_r);


                btr.AppendEntity(dbtxt);
                btr.AppendEntity(oCircle1);
                btr.AppendEntity(oCircle2);

                tr.AddNewlyCreatedDBObject(dbtxt, true);
                tr.AddNewlyCreatedDBObject(oCircle1, true);
                tr.AddNewlyCreatedDBObject(oCircle2, true);

                tr.Commit();
            }

        }

        [CommandMethod("MyPluginCommand")]
        public void MyPluginCommand()
        {
            CommonTest();

            //MultiOutputs();

            //VariousInputs();




        }
    }
}
