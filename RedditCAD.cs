using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.IO;
using Autodesk.AutoCAD.Geometry;
using HtmlAgilityPack;

namespace AutoCADReddit
{
    class RedditCAD
    {

        public static string PlotSubReddit(EntityData dimstyle, string pickedSubReddit)
        {
            string result = "OK";
            try
            {
                DrawEntity.DrawBox(dimstyle.origin, dimstyle.cornerBase, "REDDIT.BORDER", 20);
                RedditSharp.Reddit reddit = new RedditSharp.Reddit();

                var subreddit = reddit.GetSubreddit(pickedSubReddit);
                if (subreddit != null)
                {
                    Tuple<double, double> widthHeight;
                    int i = -1;
                    double yTextLocation = dimstyle.origin.Y ;
                    double mtextActualHeight = 0;
                    DrawEntity.DrawDim(dimstyle.origin, new Point3d(dimstyle.cornerBase.X, dimstyle.origin.Y, 0), "/r/" + pickedSubReddit, "REDDIT.HEADINGS", dimstyle.headingTxtSize, 1.025);
                    foreach (var post in subreddit.Hot.Take(20))                  
                    {
                        
                        string postId = GeneratePostId(PostMarker.postmarker);
                        yTextLocation -=(mtextActualHeight - ( dimstyle.yLength * .025)); //adds 5% height padding
                        Point2d headingLocation = new Point2d(dimstyle.origin.X, yTextLocation);

                        widthHeight = DrawEntity.DrawText(headingLocation, dimstyle.headingTxtSize, dimstyle.textWidth, post.Title, "REDDIT.HEADINGS", 7);
                        mtextActualHeight = widthHeight.Item2;
                        Point2d subHeadingLocation = new Point2d(dimstyle.origin.X, yTextLocation - mtextActualHeight);
                        string subHeading = string.Format("By:{0} | No. Of Comments: {1} | Submitted: {2} | NSFW: {3}", 
                                                          post.Author.ToString(), post.CommentCount.ToString(), post.Created.ToString(), post.NSFW ? "YES":"NO");
                        widthHeight = DrawEntity.DrawText(subHeadingLocation, dimstyle.subHeadingTxtSize, dimstyle.textWidth, subHeading, "REDDIT.SUBHEADINGS", 251);
                        Point2d postIdLocation = new Point2d((dimstyle.origin.X + widthHeight.Item1) + ((dimstyle.cornerBase.X - dimstyle.origin.X) * 0.001), yTextLocation - mtextActualHeight);
                        string postIdFormat = string.Format("[REDDITCAD POST ID: {0} ]", postId);
                        DrawEntity.DrawText(postIdLocation, dimstyle.subHeadingTxtSize, dimstyle.textWidth, postIdFormat, "REDDIT.POSTID", 10);
                        PostIds.AddPost(postId, post);
                        i--;                  
                    }
                }
            }
            catch(Exception e)
            {
                System.Windows.MessageBox.Show(e.Message);
                result = "FAILED";
            }
            return result;
        }
        /// <summary>
        /// Plots reddit post based on Post Id
        /// </summary>
        /// <param name="dimstyle">object containing dim scales based on the bounding box drawn by user</param>
        /// <param name="id">Reddit Post ID</param>
        /// <returns></returns>
        public static string PlotPost(EntityData dimstyle, string id)
        {
            string result = "OK";         
            try
            {
                RedditSharp.Things.Post post = (RedditSharp.Things.Post)PostIds.GetPost(id);
                if (IsLinkedImg(post.Url.AbsoluteUri))
                {
                    //Will insert imageif link is a direct link to the image
                    string filename = GetImage(post.Url.AbsoluteUri);
                    DrawEntity.DrawImg(dimstyle.origin, filename, "REDDIT.IMGS", dimstyle.yLength * .05, dimstyle.yLength * .05);
                }
                //if it's only a link to imgur page...
                List<string> imgurLinks = GetImgurImgs(post.Url.AbsoluteUri);
                for(int i = 0; i < imgurLinks.Count; i++)                
                {
                    Point3d imgLocation = new Point3d(dimstyle.origin.X + (i * (dimstyle.cornerBase.X - dimstyle.origin.X)), dimstyle.origin.Y, 0);
                    double heightNWith = dimstyle.yLength * 0.025; //Basing height and with on the height of the border drawn by user. if width & height are seperated then img will be distorted
                    string image = GetImage(imgurLinks[i]);
                    DrawEntity.DrawImg(imgLocation, image,"REDDIT.IMGS", heightNWith, heightNWith);
                }

                //DrawEntity.DrawBox(dimstyle.origin, dimstyle.cornerBase, "REDDIT.BORDER", 20);
                RedditSharp.Reddit reddit = new RedditSharp.Reddit();
                if (post != null)
                {
                    //Post generated looks like this with indentation 
                    int i = -1; //Multiplier to help to align text in the Y axis.
                    double yTextLocation = dimstyle.origin.Y;
                    double mtextActualHeight = (dimstyle.yLength * .0025);
                    Point2d subHeadingLocation = new Point2d(dimstyle.origin.X, dimstyle.origin.Y);
                    foreach (var comment in post.Comments.Take(50))
                    {
                        Tuple<double, double> widthHeight; // A*C*T*U*A*L width & height of the mtext created
                        yTextLocation -= (mtextActualHeight - (dimstyle.yLength * .0025)); //adds height padding
                        Point2d commentLocation = new Point2d(dimstyle.origin.X, yTextLocation);
                        widthHeight = DrawEntity.DrawText(commentLocation, dimstyle.commentsSize, dimstyle.textWidth, comment.Body, "REDDIT.COMMENTS", 7);
                        
                        mtextActualHeight = widthHeight.Item2;
                        subHeadingLocation = new Point2d(dimstyle.origin.X, yTextLocation - mtextActualHeight);
                        string subHeading = string.Format("By:{0} | Upvotes: {1} | Submitted: {2}", 
                                                          comment.Author, comment.Upvotes, post.Created.ToString());
                        DrawEntity.DrawText(subHeadingLocation, dimstyle.subCommentsSize, dimstyle.textWidth, subHeading, "REDDIT.COMMENTS.SUBHEADINGS", 251);                      
                        i--;
                        foreach (var subcomment in comment.Comments.Take(20))
                        {                          
                            yTextLocation -= (mtextActualHeight - (dimstyle.yLength * .0025)); //adds height padding
                            commentLocation = new Point2d(dimstyle.origin.X + ((dimstyle.cornerBase.X - dimstyle.origin.X) * 0.025), yTextLocation);
                            widthHeight = DrawEntity.DrawText(commentLocation, (dimstyle.commentsSize * 0.75), dimstyle.textWidth, subcomment.Body, "REDDIT.COMMENTS", 7);
                            mtextActualHeight = widthHeight.Item2;
                            subHeadingLocation = new Point2d(dimstyle.origin.X + ((dimstyle.cornerBase.X - dimstyle.origin.X) * 0.025), yTextLocation - mtextActualHeight);
                            subHeading = string.Format("By:{0} | Upvotes: {1} | Submitted: {2}", 
                                                       subcomment.Author, subcomment.Upvotes, subcomment.Created.ToString());
                            DrawEntity.DrawText(subHeadingLocation, (dimstyle.subCommentsSize * 0.75), dimstyle.textWidth, subHeading, "REDDIT.COMMENTS.SUBHEADINGS", 251);
                            i--;
                            foreach (var subsubcomment in subcomment.Comments.Take(15))
                            {
                                yTextLocation -= (mtextActualHeight - (dimstyle.yLength * .0025)); //adds height padding
                                commentLocation = new Point2d(dimstyle.origin.X + ((dimstyle.cornerBase.X - dimstyle.origin.X) * 0.05), yTextLocation);
                                widthHeight = DrawEntity.DrawText(commentLocation, (dimstyle.commentsSize * 0.5), dimstyle.textWidth, subsubcomment.Body, "REDDIT.COMMENTS", 7);
                                mtextActualHeight = widthHeight.Item2;
                                subHeadingLocation = new Point2d(dimstyle.origin.X + ((dimstyle.cornerBase.X - dimstyle.origin.X) * 0.05), yTextLocation - mtextActualHeight);
                                subHeading = string.Format("By:{0} | Upvotes: {1} | Submitted: {2}", subsubcomment.Author, subsubcomment.Upvotes, subsubcomment.Created.ToString());
                                DrawEntity.DrawText(subHeadingLocation, (dimstyle.subCommentsSize * 0.5), dimstyle.textWidth, subHeading, "REDDIT.COMMENTS.SUBHEADINGS", 251);
                                i--;
                                foreach (var subsubsubcomment in subsubcomment.Comments.Take(10))
                                {
                                    yTextLocation -= (mtextActualHeight - (dimstyle.yLength * .0025)); //adds height padding
                                    commentLocation = new Point2d(dimstyle.origin.X + ((dimstyle.cornerBase.X - dimstyle.origin.X) * 0.075), yTextLocation);
                                    widthHeight = DrawEntity.DrawText(commentLocation, (dimstyle.commentsSize * .25), dimstyle.textWidth, subsubsubcomment.Body, "REDDIT.COMMENTS", 7);
                                    mtextActualHeight = widthHeight.Item2;
                                    subHeadingLocation = new Point2d(dimstyle.origin.X + ((dimstyle.cornerBase.X - dimstyle.origin.X) * 0.075), yTextLocation - mtextActualHeight);
                                    subHeading = string.Format("By:{0} | Upvotes: {1} | Submitted: {2}", subsubsubcomment.Author, subsubsubcomment.Upvotes, subsubsubcomment.Created.ToString());
                                    DrawEntity.DrawText(subHeadingLocation, (dimstyle.subCommentsSize * 0.25), dimstyle.textWidth, subHeading, "REDDIT.COMMENTS.SUBHEADINGS", 251);
                                    i--;
                                }
                            }
                        }
                    }
                    Point3d endPt = new Point3d(dimstyle.cornerBase.X - ((dimstyle.cornerBase.X - dimstyle.origin.X) /2), subHeadingLocation.Y - (subHeadingLocation.Y * 0.0025) , 0);
                    DrawEntity.DrawBox(dimstyle.origin, endPt, "REDDIT.BORDER", 20);
                    DrawEntity.DrawDim(dimstyle.origin, new Point3d(endPt.X, dimstyle.origin.Y, 0), "POST ID : " + id, "REDDIT.COMMENTS", dimstyle.subHeadingTxtSize, 1.005);
                }
            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show(e.Message);
                result = "FAILED";
            }
            return result;
        }
        /// <summary>
        /// Checks if url is a direct link to an image
        /// </summary>
        /// <param name="url">Url that could contain an image</param>
        /// <returns>bool True if it's a direct link to an image.</returns>
        private static bool IsLinkedImg(string url)
        {
            bool isImg = false;
            List<string> extensions = new List<string>()
            {
                "png",
                "jpg",
                "jpeg",
                "gif",
                "bmp"
            };
            foreach(string extension in extensions)
            {
                if(url.EndsWith(extension))
                {
                    return isImg = true;
                }
            }
            return isImg;
        }

        /// <summary>
        /// Parses imgurs html and looks for the div element containing the direct link to the image
        /// </summary>
        /// <param name="url">imgur link</param>
        /// <returns>Direct url link to the image</returns>
        private static List<string> GetImgurImgs(string url)
        {
            List<string> imgsLinks = new List<string>();
            if(url.Contains("imgur.com"))
            {
                HtmlWeb imgur = new HtmlWeb();
                imgur.Load(url);
                HtmlDocument doc = imgur.Load(url);
                HtmlNodeCollection nodes = doc.DocumentNode.SelectNodes("//*/div[@class='post-image']//img"); //node that contains the link & description of images
                if (nodes != null)
                {
                    foreach (HtmlNode node in nodes)
                    {
                        imgsLinks.Add(node.GetAttributeValue("src", ""));
                    }
                }
            }
            return imgsLinks;
        }
        /// <summary>
        /// Formats Reddit txt sizes and location based on a bounding box
        /// </summary>
        /// <param name="ent">Class containing RedditCAD data</param>
        /// <param name="origin">Top left corner of the bounding box</param>
        /// <param name="cornerBase">Bottom right corner of the bounding box</param>
        /// <returns></returns>
        public static EntityData FormatRedditDim(EntityData ent, Point3d origin, Point3d cornerBase)
        {
            ent.origin = origin;
            ent.cornerBase = cornerBase;
            ent.headingTxtSize = (origin.Y - cornerBase.Y) * 0.015;
            ent.subHeadingTxtSize = ((origin.Y - cornerBase.Y) * 0.015) * 0.6;
            ent.commentsSize = (origin.Y - cornerBase.Y) * 0.002;
            ent.subCommentsSize = ((origin.Y - cornerBase.Y) * 0.0020) * 0.6;
            ent.textWidth = (cornerBase.X - origin.X);
            ent.yLength = (cornerBase.Y - origin.Y);
            ent.xLength = (cornerBase.X - origin.X);
            return ent;            
        }
        /// <summary>
        /// Gets image from the url and saves it to the temporary folder
        /// </summary>
        /// <param name="url"></param>
        /// <returns>Path to the image in the temp folderr</returns>
        private static string GetImage(string url)
        {
            string savePath = Path.GetTempPath();
            string fileName = Path.GetTempFileName();
            fileName = fileName.Replace(".tmp", ".jpg"); //Temporary file names have a ".tmp" extension. Change to a jpg(doesn't matter if it's gif or etc as long as AutoCAD can insert it
            using (WebClient webclient = new WebClient())
            {
                if(!url.StartsWith("http:"))
                {
                    //links doesn't start with http. Not sure why. Webclient thinks it's a network path 
                    url = "http:" + url;
                }
                webclient.DownloadFile(url,fileName);
            }
            return fileName;
        }
        /// <summary>
        /// Generates RedditCAD Post Id. Post Id is always 3 digits
        /// The method converts each digit to a letter.
        /// </summary>
        /// <param name="idNumber">Global Post Id Counter. Each post increments this counter in the range of 1 - 8</param>
        /// <returns>Three digit Post Id</returns>
        public static string GeneratePostId(int idNumber)
        {
            Random randomNumber = new Random();
            PostMarker.postmarker += randomNumber.Next(1, 8);
            string id = "";
            if (PostMarker.postmarker > 999)
            {
                //max val 999.
                PostMarker.postmarker = 1;
            }                     
            int rem;
            int firstNum = (idNumber / 100) + 65; //add 65 as UTF & ASCII Chars starts at 65
            rem = idNumber % 100;
            int secondNum = (rem / 10) + 65;
            int thirdNum = (rem % 10) + 65 ;
            StringBuilder sb = new StringBuilder();
            sb.Append(Convert.ToChar(firstNum));
            sb.Append(Convert.ToChar(secondNum));
            sb.Append(Convert.ToChar(thirdNum));
            id = sb.ToString();

            return id;
        }
    }
    /// <summary>
    /// Class containing reddit dim scales
    /// </summary>
    public class EntityData
    {
        public double yLength { get; set; }
        public double xLength { get; set; }
        public double headingTxtSize { get; set; }
        public double subHeadingTxtSize { get; set; }
        public double commentsSize { get; set; }
        public double subCommentsSize { get; set; }
        public double textWidth { get; set; }
        public Point3d origin { get; set; }
        public Point3d cornerBase { get; set; }
    }
    /// <summary>
    /// Global post counter. Used to generate unique post Id. Max val = 999
    /// If you need more then you should start doing more drafting that redditing ;)
    /// </summary>
    public static class PostMarker
    {
        public static int postmarker = 1;
    }
    /// <summary>
    /// List of layer names used by AutoCAD.Reddit
    /// </summary>
    public static class LayerNameList
    {
        private static List<string> _layerlist = new List<string>()
        {
            "REDDIT.BORDER",
            "REDDIT.COMMENTS",
            "REDDIT.COMMENTS.SUBHEADINGS",
            "REDDIT.IMGS",
            "REDDIT.HEADINGS",
            "REDDIT.POSTID",
            "REDDIT.SUBHEADINGS"

        };
        public static void AddLayer(string layname)
        {
            if(!_layerlist.Contains(layname))
            {
                _layerlist.Add(layname);
            }
        }
        public static List<string> GetLayerNames()
        {
            return _layerlist;
        }
    }
    /// <summary>
    /// Post Id creator. Dictionary holds the Class 'RedditSharp.Post' along with the post id
    /// </summary>
    public static class PostIds
    {
        static Dictionary<string, object> dictionary = new Dictionary<string, object>();
        public static object GetPost(string id)
        {
            object result;
            if (dictionary.TryGetValue(id, out result))
            {
                return result;
            }
            else
            {
                return null; 
            }
        }
        public static void AddPost(string id, object obj)
        {
            dictionary.Add(id, obj);
        }
        public static bool Contains(string id)
        {
            bool result = false;
            if(dictionary.ContainsKey(id))
            {
                result = true;
            }
            return result;
        }
    }
}
