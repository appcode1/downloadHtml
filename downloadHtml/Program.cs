using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Linq.Expressions;


namespace downloadHtml
{
    internal class Program
    {
        static string baseUrl = "http://bible.kyhs.net/xdzw/";
        static HttpClient client = new HttpClient();
        static int Min = 1;
        static int Max = 66; //39+27
        static async Task Main(string[] args)
        {
            if (args.Length < 3)
            {
                Console.WriteLine("downloadHtml orderId startNumber endNumber");
                return;
            }

            int orderId = 1;
            int start = 1047;
            int end = 1096;

            if (!int.TryParse(args[0], out orderId))
            {
                Console.WriteLine("specify the orderId of the sub collection: 1, 2, ....");
                return;
            }
            if (orderId < Min || orderId > Max)
            {
                Console.WriteLine("specify the orderId of the sub collection: 1, 2, ...., 66, total 66 chapters!!!");
                return;
            }
            if (!int.TryParse(args[1], out start))
            {
                Console.WriteLine("specify the start html file number");
                return;
            }
            if (!int.TryParse(args[2], out end))
            {
                Console.WriteLine("specify the end html file number");
                return;
            }

            if (end < start)
            {
                Console.WriteLine("the end html file number should be greater than start number");
                return;
            }

            string subfolder = $"{orderId:D2}";
            if (!Directory.Exists(subfolder))
            {
                Directory.CreateDirectory(subfolder);
            }
            if (Directory.GetFiles(subfolder).Length > 0)
            {
                Console.WriteLine($"There are files existed in the subfolder - {subfolder} !!!");
                return;
            }

            List<string> downloadedFiles = new List<string>();
            for (int i = start; i <= end; i++)
            {
                string url = $"{baseUrl}{i}.htm";
                Console.WriteLine($"downloading {url} ...");
                bool flag = await DownloadHtmlFile(url, $"{subfolder}\\{i}.htm");
                if (flag)
                    downloadedFiles.Add($"{subfolder}\\{i}.htm");
            }

            foreach (var fileName in downloadedFiles)
            {
                Console.WriteLine($"updating {fileName} ...");

                string htmlText = File.ReadAllText(fileName);
                htmlText = UpdateHtmlText(htmlText);

                int contentStart = htmlText.IndexOf("<div id=\"content\">");
                int contentEnd = htmlText.LastIndexOf("<div id=\"footlink\">");
                contentStart += "<div id=\"content\">".Length + 1;

                int n1 = 0, n2 = 0;
                n1 = htmlText.IndexOf("<a href=\"zj/", contentStart);
                while (n1 > contentStart && n1 < contentEnd && n1 > n2 + 1)
                {
                    n1 += "<a href=\"".Length;
                    n2 = htmlText.IndexOf("title=", n1 + 1);
                    if (n2 > n1 + 1 && n2 < contentEnd)
                    {
                        string aHref = htmlText.Substring(n1, n2 - n1 + 5);
                        htmlText = htmlText.Replace(aHref, "#t\" data-bs-toggle=\"tooltip\" data-bs-title"); 
                        //do not use <a href="#"...>, use <a href="#t"...>, fix the issue to scroll to the window top after clicking the Tooltip
                        n1 = htmlText.IndexOf("<a href=\"zj/", n2 + 10);
                    }
                    else
                    {
                        break;
                    }
                }

                File.WriteAllText(fileName, htmlText);
            }

            if (orderId > 1)
                UpdateFirstHtml(orderId, start);

            if (orderId < Max)
                UpdateLastHtml(orderId, end);
        }

        static async Task DownloadFile(string url, string fileName)
        {
            using (Stream s = await client.GetStreamAsync(url))
            {
                using (FileStream fs = File.Create(fileName))
                {
                    s.CopyTo(fs);
                }
            }
        }

        /// <summary>
        /// download the GB2312 content, save it as UTF-8 content
        /// </summary>
        /// <param name="url"></param>
        /// <param name="fileName"></param>
        /// <returns>true if download success</returns>
        static async Task<bool> DownloadHtmlFile(string url, string fileName)
        {
            using (var response = await client.GetAsync(url))
            {
                if (response.IsSuccessStatusCode)
                {
                    Stream s = await response.Content.ReadAsStreamAsync();
                    using (StreamReader sr = new StreamReader(s, Encoding.GetEncoding(936))) //Chinese GB2312
                    {
                        using (StreamWriter sw = new StreamWriter(fileName, false, Encoding.UTF8)) //UTF8
                        {
                            sw.Write(sr.ReadToEnd());
                        }
                    }

                    return true;
                }
                else
                {
                    Console.WriteLine($"{url}: {response.StatusCode} - {response.ReasonPhrase}");
                    return false;
                }
            }
        }

        static void UpdateFirstHtml(int orderId, int fileId)
        {
            string subfolder = $"{orderId:D2}";
            string htmlText = File.ReadAllText($"{subfolder}\\{fileId}.htm");

            string searchedText = "<div id=\"footlink\"><a href=\"";
            htmlText = htmlText.Replace(searchedText, $"{searchedText}../{(orderId - 1):D2}/");
            File.WriteAllText($"{subfolder}\\{fileId}.htm", htmlText);

        }
        static void UpdateLastHtml(int orderId, int fileId)
        {
            string subfolder = $"{orderId:D2}";
            string htmlText = File.ReadAllText($"{subfolder}\\{fileId}.htm");

            int n1 = htmlText.IndexOf("<span class=\"arrow-up\"/>");
            n1 += "<span class=\"arrow-up\"/>".Length;
            int n2 = htmlText.LastIndexOf("\" title=\"Next\"");
            n1 = htmlText.IndexOf("<a href=\"", n1);
            n1 += "<a href=\"".Length;
            string r = htmlText.Substring(n1, n2 - n1);
            htmlText = htmlText.Replace(r, $"../{(orderId + 1):D2}/{r}");
            File.WriteAllText($"{subfolder}\\{fileId}.htm", htmlText);
        }
        static string UpdateHtmlText(string htmlText)
        {
            return htmlText.Replace("<!DOCTYPE HTML PUBLIC \"-//W3C//DTD HTML 4.01//EN\" \"http://www.w3.org/TR/html4/strict.dtd\">\r\n<html>", "<!doctype html>\r\n<html lang=\"zh-CN\">")
            .Replace("<meta http-equiv=\"Content-Type\" content=\"text/html; charset=gb2312\">", "<meta charset=\"utf-8\">\r\n<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">")
            .Replace("<link rel=\"stylesheet\" href=\"tcvbg.css\" type=\"text/css\">", "<link href=\"../main.css\" rel=\"stylesheet\">")
            .Replace("<a name=\"Top\"></a>", "<a name=\"Top\"></a><div class=\"mb-3\"><a href=\"../\"><button type=\"button\" class=\"btn btn-primary\">Home</button></a></div>")
            .Replace("</body>", "<div class=\"mt-3\"><a href=\"../\"><button type=\"button\" class=\"btn btn-primary\">Home</button></a></div>\r\n<script src=\"../main.js\"></script>\r\n</body>")
            .Replace("><img src=\"left.gif\" alt=\"上一章\" width=\"18\" height=\"17\" border=\"0\">", " title=\"Prev\"><span class=\"arrow-left\"/>")
            .Replace("><img src=\"up.gif\" alt=\"回顶部\" width=\"16\" height=\"17\" border=\"0\">", " title=\"Top\"><span class=\"arrow-up\"/>")
            .Replace("><img src=\"right.gif\" alt=\"下一章\" width=\"18\" height=\"17\" border=\"0\">", " title=\"Next\"><span class=\"arrow-right\"/>");
        }
    }
}
