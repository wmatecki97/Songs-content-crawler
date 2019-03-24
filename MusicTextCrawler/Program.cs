using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;

namespace MusicTextCrawler
{
    class Program
    {
        static void Main(string[] args)
        {
            string file = "Categories";
            var files = Directory.GetFiles(file);

            foreach (var f in files)
            {
                Thread t = new Thread(() => ProcessFile(f));
                t.Start();
            }

            Console.ReadKey();
        }

        public static void ProcessFile(string file)
        {
            List<string> artists = new List<string>();
            try
            {
                using (StreamReader sr = new StreamReader(file))
                {
                    String line = sr.ReadToEnd();
                    artists = line.Replace('\r', ' ').Split('\n').ToList();
                }
            }
            catch (IOException e)
            {
                Console.WriteLine("The file could not be read:");
                Console.WriteLine(e.Message);
            }

            WebClient client = new WebClient();
            client.Encoding = Encoding.UTF8;
            List<string> texts = GetTexts(artists, client);

            file = SaveResultFile(file, texts);
        }

        private static string SaveResultFile(string file, List<string> texts)
        {
            file = file.Replace(".txt", "-result.txt").Replace("Categories","Results");
            if (!File.Exists(file))
                File.Create(file).Dispose();

            using (var sw = new StreamWriter(file, true))
            {
                foreach (var text in texts)
                {
                    sw.WriteLine(text);
                }
            }

            return file;
        }

        private static List<string> GetTexts(List<string> artists, WebClient client)
        {
            IEnumerable<string> songsContent = new List<string>();
            //for (int i = 0; i < artists.Count(); i++)
            for (int i = 0; i < 10; i++)
            {
                Console.WriteLine("Artist: " + i + " of " + artists.Count());
                string url = "https://www.google.com/search?q=" + artists[i].Replace(' ', '+') + "tekstowo";
                string downloadString = "";
                try { downloadString = client.DownloadString(url); }
                catch (Exception) { }
                int startIndex = downloadString.IndexOf("https://www.tekstowo.pl/");
                if (startIndex > 0)
                {
                    int endIndex = downloadString.IndexOf("\"", startIndex);
                    if (endIndex > 0)
                    {
                        string listUrl = downloadString.Substring(startIndex, endIndex - startIndex);
                        var songsUrls = GetSongsUrls(listUrl, client);
                        songsContent = songsContent.Concat(GetSongsContent(songsUrls, client));
                    }
                }
            }
            return songsContent.ToList();
        }

        private static List<string> GetSongsContent(List<string> songsUrls, WebClient client)
        {
            List<string> contents = new List<string>();
            int songNr = 0;
            foreach (var url in songsUrls)
            {
                songNr++;
                Console.WriteLine("Song: " + songNr + " of " + songsUrls.Count());

                string downloadString = "";
                try { downloadString = client.DownloadString(url); }
                catch (Exception) { }
                int start = downloadString.IndexOf("<h2>Tekst piosenki:</h2>");//24
                if (start > 0)
                {
                    int end = downloadString.IndexOf("</div>", start);
                    if (end > 0)
                    {
                        string content = downloadString.Substring(start + 24, end - start - 24).Replace("<br />", "");
                        contents.Add(content);
                    }
                }
            }
            return contents;
        }

        private static List<string> GetSongsUrls(string listUrl, WebClient client)
        {
            List<string> songs = new List<string>();
            string downloadString = "";
            try { downloadString = client.DownloadString(listUrl); }
            catch (Exception) { }
            int startIndex = downloadString.IndexOf("ranking-lista");
            if (startIndex > 0)
            {
                int end = downloadString.IndexOf("padding", startIndex);
                if (end > 0)
                {
                    string list = downloadString.Substring(startIndex, end - startIndex);
                    songs = list.Replace("box-przeboje", "\r").Split('\r').ToList();
                    songs.RemoveAt(0);//remove list title

                    for (int i = 0; i < songs.Count(); i++)
                    {
                        startIndex = songs[i].IndexOf("href=\"");
                        if (startIndex > 0)
                        {
                            end = songs[i].IndexOf("\"", startIndex + 6);
                            if (end > 0)
                                songs[i] = "https://www.tekstowo.pl" + songs[i].Substring(startIndex + 6, end - startIndex - 6).Replace("/piosenka", "/drukuj");
                            else
                                songs[i] = "";
                        }
                    }
                }
            }

            return songs;
        }
    }
}
