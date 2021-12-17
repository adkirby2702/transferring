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
using OpenQA.Selenium.Support.UI;



namespace ConsoleApp1
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

        public static IWebDriver driver = null;
        public static string title = "not provided";
        public static void Main()
        {
            if (File.Exists("D:\\Users\\adkir\\source\\repos\\ConsoleApp1\\ConsoleApp1\\index.html"))
            {
                File.Delete("D:\\Users\\adkir\\source\\repos\\ConsoleApp1\\ConsoleApp1\\index.html");
            }
            File.WriteAllText("D:\\Users\\adkir\\source\\repos\\ConsoleApp1\\ConsoleApp1\\index.html", "<!DOCTYPE html>\n<html>\n<head>\n<title> All Pages </title>\n<style>\n@import 'D:\\\\Users\\\\adkir\\\\source\\\\repos\\\\ConsoleApp1\\\\StyleSheet1.css';\n</style>\n</head><br/>\n<body>\n<h1> All Pages </h1>\n<div><ul>");
            Pathfinder.beginning = new Uri("https://peanuts.com/");
            Pathfinder.siteName = "https://peanuts.com/";
            Pathfinder.driver = new ChromeDriver();
            Pathfinder.driver.Navigate().GoToUrl(Pathfinder.beginning);
            Pathfinder.driver.FindElement(By.Id("cookie-dismiss")).Click();

            Pathfinder.Start();

        }
        public static void Start()
        {

            if (!queueToCheck.Contains(beginning.ToString()))
            {
                queueToCheck.Enqueue(beginning.ToString());
            }
            var threads = new ThreadStart(PathfinderThread);
            var thread = new Thread(threads);
            thread.Start();

        }

        private static void PathfinderThread()
        {
            while (true)
            {

                //if there is nothing left in queueToCheck
                if (queueToCheck.Count == 0)
                {
                    var max = pageCount.Aggregate((l, r) => l.Value > r.Value ? l : r).Key;
                    Console.WriteLine("Done crawling");
                    using (FileStream indexFile = File.Open("D:\\Users\\adkir\\source\\repos\\ConsoleApp1\\ConsoleApp1\\index.html", FileMode.Append))
                    {
                        Byte[] info = new UTF8Encoding(true).GetBytes("</ul><p>There are " + allCheckedPages.Count().ToString() + " pages contained in the domain of " + Pathfinder.beginning + " <br/> The page with the most links is " + max+ " with " + pageCount[max] + " links</body>\n</html>");
                        // Add some information to the file.  
                        indexFile.Write(info, 0, info.Length);
                    }
                    Pathfinder.driver.Close();
                    foreach( var link in allCheckedPages)
                    {
                        Console.WriteLine(link.Key);
                    }
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
            ss.SaveAsFile("D:\\adkir\\Desktop\\screenshotTest\\screenshot" + Pathfinder.allCheckedPages.Count().ToString() + ".png", ScreenshotImageFormat.Png);
            pageinfo.screenshotLocal = "D:\\adkir\\Desktop\\screenshotTest\\screenshot" + Pathfinder.allCheckedPages.Count().ToString() + ".png";
            using (FileStream indexFile = File.Open("D:\\Users\\adkir\\source\\repos\\ConsoleApp1\\ConsoleApp1\\index.html", FileMode.Append))
            {
                Byte[] info = new UTF8Encoding(true).GetBytes("<li><a href='D:\\adkir\\Desktop\\screenshotTest\\screenshot" + Pathfinder.allCheckedPages.Count().ToString() + ".png'>" + title + " </a></li>\n");
                // Add some information to the file.  
                indexFile.Write(info, 0, info.Length);
            }
        }
    }

    




}