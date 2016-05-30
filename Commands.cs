// (C) Copyright 2016 by Jericho Masigan
using System;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using RedditSharp;
using System.Linq;
using System.IO;

// This line is not mandatory, but improves loading performances
[assembly: CommandClass(typeof(AutoCADReddit.MyCommands))]

namespace AutoCADReddit
{
    public class MyCommands
    {
        [CommandMethod("reddit")]
        public void CreateReddit()
        {
            EntityData dimStyles = new EntityData();
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed;
            if (doc != null)
            {
                ed = doc.Editor;
                PromptStringOptions prmptStrOpt = new PromptStringOptions("\n\n Type subreddit name. Do not include '/r/' ");
                PromptResult prmpRes = ed.GetString(prmptStrOpt);
                PromptPointOptions prmptPtOptions = new PromptPointOptions("\n\nPick insertion point....");            
                PromptPointResult result =  ed.GetPoint(prmptPtOptions);              
                PromptCornerOptions prmptCnrOptions = new PromptCornerOptions("\n\n Click on bottom corner..", result.Value);
                PromptPointResult prmptCnrResult;
                prmptCnrResult = ed.GetCorner(prmptCnrOptions);           
                string chosenSubReddit = prmpRes.StringResult;
                RedditCAD.FormatRedditDim(dimStyles, result.Value, prmptCnrResult.Value);
                
                if(RedditCAD.PlotSubReddit(dimStyles, chosenSubReddit) == "FAILED")
                {
                    ed.WriteMessage("\n\nFAILED");
                }
            }
        }
        [CommandMethod("rpost")]
        public void GetPost()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            PromptStringOptions prmptStrOpt = new PromptStringOptions("\n\n Type RedditCAD Post ID : ");
            PromptResult prmpRes = ed.GetString(prmptStrOpt);
            string postId = prmpRes.StringResult.ToUpper();
            if (postId.Length < 4)
            {
                EntityData dimStyles = new EntityData();
                PromptPointOptions prmptPtOptions = new PromptPointOptions("\n\nPick insertion point....");
                PromptPointResult result = ed.GetPoint(prmptPtOptions);
                PromptCornerOptions prmptCnrOptions = new PromptCornerOptions("\n\n Click on bottom corner..", result.Value);
                PromptPointResult prmptCnrResult;
                prmptCnrResult = ed.GetCorner(prmptCnrOptions);
                RedditCAD.FormatRedditDim(dimStyles, result.Value, prmptCnrResult.Value);
                RedditCAD.PlotPost(dimStyles, postId);
            }

        }
        [CommandMethod("rfreeze")]
        public void HideReddit()
        {
            string layerName = "";
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                LayerTable layerTable;
                layerTable = tr.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;
                foreach(string layername in LayerNameList.GetLayerNames())
                {
                    if(layerTable.Has(layername))
                    {
                        LayerTableRecord layerTableRec = tr.GetObject(layerTable[layername], OpenMode.ForWrite) as LayerTableRecord;
                        layerTableRec.IsFrozen = true;
                    }
                }
                tr.Commit();
            }
        }
        [CommandMethod("rthaw")]
        public void ThawReddit()
        {
            string layerName = "";
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;          
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                LayerTable layerTable;
                layerTable = tr.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;
                foreach (string layername in LayerNameList.GetLayerNames())
                {
                    if (layerTable.Has(layername))
                    {
                        LayerTableRecord layerTableRec = tr.GetObject(layerTable[layername], OpenMode.ForWrite) as LayerTableRecord;
                        layerTableRec.IsFrozen = false;
                    }
                }
                tr.Commit();
            }
        }
        [CommandMethod("rdel")]
        public void DeleteReddit()
        {
            string layerName = "";
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                LayerTable layerTable;
                layerTable = tr.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;
                foreach (string layername in LayerNameList.GetLayerNames())
                {
                    //Select entities from layer names 
                    ObjectIdCollection objects = DrawEntity.SelectByLayer(layername);
                    foreach(ObjectId obj in objects)
                    {
                        var ent = tr.GetObject(obj, OpenMode.ForWrite);
                        ent.Erase(true);
                    }
                    //Now delete the layer
                    LayerTableRecord layerTableRec = tr.GetObject(layerTable[layername], OpenMode.ForWrite) as LayerTableRecord;
                    layerTableRec.Erase(true);
                }
                tr.Commit();
            }
        }


    }

}
