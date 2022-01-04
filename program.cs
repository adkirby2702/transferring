using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using HtmlAgilityPack;
using System.Text.Json;



namespace my_app
{
    public class Pathfinder
    {
        public struct MyStruct
        {
            public List<string> children;
            public string link;
            public string title;
            public string screenshotLocal;
        }

        public class scren{
            public string screenshotLoc { get; set; }
            public string screTitle { get; set; }
        }

        /// A queue of pages to be crawled
        private static Queue<string> queueToCheck = new Queue<string>();
        /// All pages visited
        public static Dictionary<string, string> allCheckedPages = new Dictionary<string, string>();
        private static Dictionary<string, int> pageCount = new Dictionary<string, int>();
        private static Dictionary<string, MyStruct> hierarchy = new Dictionary<string, MyStruct>();

        /// A Url that the crawled page must start with. 
        public static string siteName { get; set; }
        public static int totalCount = 0;
        /// Starting page of crawl.
        public static Uri beginning { get; set; }
        private static List<scren> screens = new List<scren>();
        public static IWebDriver driver = null;
        public static string title = "not provided";
        public static void Main()
        {
            if (File.Exists("C:\\Users\\alexk\\work\\index.html"))
            {
                File.Delete("C:\\Users\\alexk\\work\\index.html");
            }
            File.WriteAllText("C:\\Users\\alexk\\work\\index.html", "<!DOCTYPE html>\n<html>\n<head>\n<title> All Pages </title>\n</head><br/>\n<body>\n<h1> All Pages </h1>\n<div><ul>");
            Pathfinder.beginning = new Uri("https://onrealm.t.ac.st");
            Pathfinder.siteName = "https://onrealm.t.ac.st";
            Pathfinder.driver = new ChromeDriver();
            Pathfinder.driver.Navigate().GoToUrl(Pathfinder.beginning);
            Pathfinder.driver.FindElement(By.Id("emailAddress")).SendKeys("anneconley@example.org");
            Pathfinder.driver.FindElement(By.Id("password")).SendKeys("RealmAcs#2018");
            Pathfinder.driver.FindElement(By.Id("signInButton")).Click();
            Thread.Sleep(7000);
            Pathfinder.driver.FindElement(By.Id("siteList")).Click();
            Thread.Sleep(7000);
            Pathfinder.driver.FindElement(By.XPath("//*[@id='siteDialog']/div[1]/ul/div/li[26]")).Click();
            Thread.Sleep(7000);
            Pathfinder.driver.FindElement(By.Id("selectSite")).Click();
            Thread.Sleep(5000);

            Pathfinder.Start();

        }
        public static void Start()
        {

            if (!queueToCheck.Contains("https://onrealm.t.ac.st/connectdemochurch/Home/Tasks?redirectController=Individual&redirectAction=Info&redirectId=12971bdc-f606-42c4-9352-a95101016acb"))
            {
                queueToCheck.Enqueue("https://onrealm.t.ac.st/connectdemochurch/Home/Tasks?redirectController=Individual&redirectAction=Info&redirectId=12971bdc-f606-42c4-9352-a95101016acb");
            }
            var threads = new ThreadStart(PathfinderThread);
            var thread = new Thread(threads);
            thread.Start();

        }

        private static void PathfinderThread()
        {
            while (true)
            {
                Thread.Sleep(5000);
                //if there is nothing left in queueToCheck
                if (queueToCheck.Count == 0)
                {
                    var max = pageCount.Aggregate((l, r) => l.Value > r.Value ? l : r).Key;
                    Console.WriteLine("Done crawling");
                    using (FileStream indexFile = File.Open("C:\\Users\\alexk\\work\\index.html", FileMode.Append))
                    {
                        Byte[] info = new UTF8Encoding(true).GetBytes("</ul><p>There are " + allCheckedPages.Count().ToString() + " pages contained in the domain of " + Pathfinder.beginning + " <br/> The page with the most links is " + max+ " with " + pageCount[max] + " links</body>\n</html>");
                        // Add some information to the file.  
                        indexFile.Write(info, 0, info.Length);
                    }
                    Pathfinder.driver.Close();
                    var opt = new JsonSerializerOptions(){ WriteIndented=true };
                    string strJson = JsonSerializer.Serialize<IList<scren>>(screens, opt);
                    using (FileStream indexFile = File.Open("C:\\Users\\alexk\\my-app\\public\\screens.json", FileMode.Append))
                    {
                        Byte[] info = new UTF8Encoding(true).GetBytes(strJson);
                        // Add some information to the file.  
                        indexFile.Write(info, 0, info.Length);
                    }
                    Console.WriteLine(strJson);
                    return;
                }
                MyStruct pageinfo = new MyStruct();
                var webUrl = queueToCheck.First();
                
                var http = new WebClient();
                string html;
                try
                {
                    html = http.DownloadString(webUrl);
                }
                catch
                {
                    queueToCheck.Dequeue();
                    continue;
                }
                //remove the url from the queueToCheck of ones to check
                queueToCheck.Dequeue();
                //load the html in the doc
                var doc = new HtmlDocument();
                doc.LoadHtml(html);
                doc.OptionEmptyCollection = true;
                try
                {
                   title = doc.DocumentNode.SelectSingleNode("html/head/title").InnerText;
                }
                catch(System.NullReferenceException)
                {
                    title = "not provided";
                }
                if (allCheckedPages.ContainsKey(webUrl)|| allCheckedPages.ContainsValue(title))
                {
                    continue;
                }

                if (!allCheckedPages.ContainsKey(webUrl)&& !allCheckedPages.ContainsValue(title))
                {
                    pageinfo.link = webUrl;
                    pageinfo.title = title;
                    pageinfo.children = new List<string>();
                    pageCount.Add(webUrl, 1);
                    allCheckedPages.Add(webUrl, title);
                    Pathfinder.Visit(webUrl, title, pageinfo, driver);
                }
                var mimeType = http.ResponseHeaders[HttpResponseHeader.ContentType];
                if (!mimeType.StartsWith("text/html"))
                {
                    continue;
                }
                var root = webUrl.Substring(0, webUrl.LastIndexOf('/'));
                
                //find the links in the page
                foreach (var link in doc.DocumentNode.SelectNodes("//a[@href]"))
                {

                    var newLink = link.Attributes["href"].Value;

                    //a whole bunch of things to skip if the newLink satisfies a condition ie. is outside of what we are looking for
                    if (newLink.ToUpper().StartsWith("JAVASCRIPT:"))
                    {
                        continue;
                    }
                    if (newLink.Contains("#"))
                    {
                        continue;
                    }

                    if (newLink.ToUpper().StartsWith("FTP:"))
                    {
                        continue;
                    }
                    if (newLink.ToUpper().StartsWith("MAILTO:"))
                    {
                        continue;
                    }
                    if (newLink.ToUpper().Contains(".PDF"))
                    {
                        continue;
                    }
                    if (!newLink.ToUpper().StartsWith("HTTP://") && !newLink.ToUpper().StartsWith("HTTPS://"))
                    {

                        if (!newLink.StartsWith("/")) newLink = "/" + newLink;
                        newLink = root + newLink;
                    }
                    


                    //if the site goes to something outside of the given starting url
                    if (!newLink.ToUpper().StartsWith(siteName.ToUpper()))
                    {

                        continue;
                    }
                    //if the page has been visited skip it
                    if (allCheckedPages.ContainsKey(newLink))
                    {
                        pageCount[newLink] = pageCount[newLink] + 1;
                        continue;
                    }

                    //if the page is already queued up to be crawled skip it
                    if (queueToCheck.Contains(newLink))
                    {
                        // Recent Duplicate
                        continue;
                    }

                   
                    queueToCheck.Enqueue(newLink);
                    if (pageinfo.children == null)
                    {
                        Console.WriteLine("list is null");
                    }
                    else
                    {
                        pageinfo.children.Append(newLink);
                    }
                }
                hierarchy[pageinfo.link] = pageinfo;
            }
        }
        public static void Visit(string webUrl, string title, MyStruct pageinfo, IWebDriver driver)
        {
            Console.WriteLine("Visited : " + webUrl);
            string filePath = "File.csv";
            File.AppendAllText(filePath, webUrl + "," + title + "\n");
            Pathfinder.totalCount += 1;
            Console.WriteLine(Pathfinder.totalCount);
            try
            {
                Pathfinder.driver.Navigate().GoToUrl(webUrl);

            }
            catch
            {

                return;
            }
            Screenshot ss = ((ITakesScreenshot)driver).GetScreenshot();
            ss.SaveAsFile("C:\\Users\\alexk\\work\\screenshotTest\\screenshot" + Pathfinder.allCheckedPages.Count().ToString() + ".png", ScreenshotImageFormat.Png);
            pageinfo.screenshotLocal = "C:\\Users\\alexk\\work\\screenshotTest\\screenshot" + Pathfinder.allCheckedPages.Count().ToString() + ".png";
            using (FileStream indexFile = File.Open("C:\\Users\\alexk\\work\\index.html", FileMode.Append))
            {
                Byte[] info = new UTF8Encoding(true).GetBytes("<li><a href='C:\\Users\\alexk\\work\\screenshotTest\\screenshot" + Pathfinder.allCheckedPages.Count().ToString() + ".png'>" + title + " </a></li>\n");
                // Add some information to the file.  
                indexFile.Write(info, 0, info.Length);
            }
            scren dept= new scren() {screenshotLoc = pageinfo.screenshotLocal , screTitle = title};
            screens.Add(dept);
        }
    }

    




}
