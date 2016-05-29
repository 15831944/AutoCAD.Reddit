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
                PromptPointOptions prmptPtOptions = new PromptPointOptions("\n\nPick insertion point....");            
                PromptPointResult result =  ed.GetPoint(prmptPtOptions);              
                PromptCornerOptions prmptCnrOptions = new PromptCornerOptions("\n\n Click on bottom corner..", result.Value);
                PromptPointResult prmptCnrResult;
                prmptCnrResult = ed.GetCorner(prmptCnrOptions);
                PromptStringOptions prmptStrOpt = new PromptStringOptions("\n\n Type subreddit name. Do not include '/r/' ");
                PromptResult prmpRes = ed.GetString(prmptStrOpt);                
                string chosenSubReddit = prmpRes.StringResult;
                RedditCAD.FormatRedditDim(dimStyles, result.Value, prmptCnrResult.Value);
                
                if(RedditCAD.PlotSubReddit(dimStyles, chosenSubReddit) == "FAILED")
                {
                    ed.WriteMessage("\n\nFAILED");
                }
            }
        }
        [CommandMethod("redditgetpost")]
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
        [CommandMethod("hidereddit")]
        public void HideReddit()
        {
            string layerName = "";
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            ObjectIdCollection objCollection = DrawEntity.SelectByLayer(layerName);
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                
            }
        }


    }

}
