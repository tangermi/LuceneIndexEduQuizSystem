using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Documents; // for Document and Field
using Lucene.Net.Index; //for Index Writer
using Lucene.Net.Store; //for Directory
using Lucene.Net.Search; // for IndexSearcher
using Newtonsoft.Json;
using System.IO;
using Syn.WordNet;
//using HttpWebRequest;
using System.Net;
using System.Text.RegularExpressions;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.QueryParsers.Classic;
using System.Windows.Forms;
using System.Diagnostics;


namespace LuceneIndexEduQuizSystem
{
    class Indexing
    {
        Lucene.Net.Store.Directory luceneIndexDirectory;
        Lucene.Net.Analysis.Analyzer analyzer;
        public Lucene.Net.Index.IndexWriter writer;
        IndexSearcher searcher;
        MultiFieldQueryParser parser;
        public WordNetEngine wordNet;
        private List<String> txtCont;
        private int topicID;

        public Indexing()
        {
            luceneIndexDirectory = null;
            writer = null;
            analyzer = new StandardAnalyzer(AppLuceneVersion);
            txtCont = new List<String>();
            topicID = 0;
        }

        const Lucene.Net.Util.LuceneVersion AppLuceneVersion = Lucene.Net.Util.LuceneVersion.LUCENE_48;

        public void CreateIndex(string indexPath)
        {
            var dir = FSDirectory.Open(indexPath);

            luceneIndexDirectory = Lucene.Net.Store.FSDirectory.Open(indexPath);
            var indexConfig = new IndexWriterConfig(AppLuceneVersion, analyzer);
            indexConfig.Similarity = new Lucene.Net.Search.Similarities.BM25Similarity((float)1.2, (float)0.75);
            writer = new IndexWriter(dir, indexConfig);
        }


        public void CleanUpIndexer()
        {
            //writer.Optimize();
            writer.Flush(true, true);
            writer.Dispose();
        }

        //---------------------------------------------------------------------------------------------------------------------------------------
        public void CreateSearcher()
        {
            searcher = new IndexSearcher(DirectoryReader.Open(luceneIndexDirectory));
            searcher.Similarity = new Lucene.Net.Search.Similarities.BM25Similarity((float)1.2, (float)0.75);
            //searcher.setSimilarity(new BM25Similarity(1.2, 0.75));
        }

        public List<SynSet> GettingSynSets(string word)
        {

            var directory = System.IO.Directory.GetCurrentDirectory();

            wordNet = new WordNetEngine();
            //Console.WriteLine(directory);
            //Console.WriteLine("Loading database...");
            wordNet.LoadFromDirectory(directory);
            //Console.WriteLine("Load completed.");

            if (true)
            {
                var synSetList = wordNet.GetSynSets(word);
                return synSetList;
            }
        }

        public static string[] TokeniseString(string text)
        {
            char[] splitters = new char[] { ' ', '\t', '\'', '"', '-', '(', ')', ',', '’', '\n', ':', ';', '?', '.', '!','_','/' };
            return text.ToLower().Split(splitters, StringSplitOptions.RemoveEmptyEntries);
        }

        public static string GetWebTitleFromUrl(string url)
        {
            Uri uri = new Uri(url);
            int lastone = uri.Segments.Length - 1;
            string rawTitle = uri.Segments[lastone];
            rawTitle = rawTitle.Split(new char[] { '.' })[0];
            string[] tokenized = TokeniseString(rawTitle);
            string title = "";
            foreach(string token in tokenized)
            {
                title += token + " ";
            }
            return title;
        }

        public static string GetWebPageTitle(string url)

        {
            string title = "";

            // Create a request to the url

            HttpWebRequest request = HttpWebRequest.Create(url) as HttpWebRequest;



            // If the request wasn't an HTTP request (like a file), ignore it

            if (request == null) return GetWebTitleFromUrl(url);



            // Use the user's credentials

            request.UseDefaultCredentials = true;



            // Obtain a response from the server, if there was an error, return nothing

            HttpWebResponse response = null;

            try { response = request.GetResponse() as HttpWebResponse; }

            catch (WebException) { return GetWebTitleFromUrl(url); }

            // Regular expression for an HTML title

            //string regex = @"(?<=<title.*>)([\s\S]*)(?=</title>)";

            using (Stream stream = response.GetResponseStream())
            {
                // compiled regex to check for <title></title> block
                Regex titleCheck = new Regex(@"<title>\s*(.+?)\s*</title>", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                int bytesToRead = 8092;
                byte[] buffer = new byte[bytesToRead];
                string contents = "";
                int length = 0;
                while ((length = stream.Read(buffer, 0, bytesToRead)) > 0)
                {
                    // convert the byte-array to a string and add it to the rest of the
                    // contents that have been downloaded so far
                    contents += Encoding.UTF8.GetString(buffer, 0, length);

                    Match m = titleCheck.Match(contents);
                    if (m.Success)
                    {
                        // we found a <title></title> match =]
                        title = m.Groups[1].Value.ToString();
                        break;
                    }
                    else if (contents.Contains("</head>"))
                    {
                        // reached end of head-block; no title found =[
                        break;
                    }
                }
                return title;
            }
        }


        public List<String> SearchAndDisplayResults(string querytext)
        {
            topicID += 1;
            List<String> resultList = new List<String>();
            txtCont.Clear();
            resultList.Add(querytext);

            querytext = querytext.ToLower();

            Dictionary<String, float> boosts = new Dictionary<string, float>();
            boosts["passage_text"] = 2;
            boosts["title"] = 10;

            parser = new MultiFieldQueryParser(AppLuceneVersion, new String[] { "passage_text", "title" }, analyzer,boosts);

            Query query = parser.Parse(querytext);

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            TopDocs results = searcher.Search(query,100000);
            stopwatch.Stop();

            int hits = results.TotalHits;
            resultList.Add(hits.ToString());
            resultList.Add(stopwatch.ElapsedMilliseconds.ToString());

            int rank = 0;
            foreach (ScoreDoc scoreDoc in results.ScoreDocs)
            {
                rank++;
                Lucene.Net.Documents.Document doc = searcher.Doc(scoreDoc.Doc);
                string url = doc.Get("url");
                string passage_text = doc.Get("passage_text");
                string title = doc.Get("title");
                string id = doc.Get("passage_id");
                resultList.Add("\nRank " + rank + "\ntitle: " + title + "\nurl: " + url + "\npassage_text: " + passage_text + "\n");
                string txt = topicID.ToString().PadLeft(3,'0') + " Q0 " + id + " " + rank + " " + scoreDoc.Score + " " + "n10057862_" +  "n10296255_"
                    + "n10056084_" + "n10153853_" + "HyperGloryTeam" + "\n";
                txtCont.Add(txt);
            }
            return resultList;
        }

        public void SaveFile(string fileDirect)
        {
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(fileDirect, true))
            {
                foreach (string line in txtCont)
                {
                    file.WriteLine(line);
                }
            }
        }

        public string GetWeightedExpandedQuery(string query)
        {
            string expandedQuery = "";
            if (true)
            {
                string[] tokens = TokeniseString(query);
                foreach(string token in tokens)
                {
                    expandedQuery += " " + token + "^5";

                    var synSetList = GettingSynSets(token);
                    string[] words = { };
                    foreach (var synSet in synSetList)
                    {
                        words = words.Concat(synSet.Words).ToArray();
                    }
                    var unique_words = new HashSet<string>(words);
                    foreach (string word in unique_words)
                    {
                        if (word != token)
                        {
                            expandedQuery += " " + word;
                        }
                    }
                }
            }
            return expandedQuery;
        }

        public void CleanUpSearcher()
        {
            //searcher.Dispose();
        }

        public void CreateCollection(string collectionPath)
        {
            using (StreamReader r = new StreamReader(collectionPath))
            {
                string json = r.ReadToEnd();
                dynamic array = JsonConvert.DeserializeObject(json);
                foreach (var item in array)
                {
                    foreach (var passage in item.passages)
                    {
                        Lucene.Net.Documents.Document document = new Document();
                        string title = GetWebTitleFromUrl(passage.url.ToString());
                        document.Add(new TextField("title", title, Field.Store.YES));
                        document.Add(new StringField("url", passage.url.ToString(), Field.Store.YES));
                        document.Add(new TextField("passage_text", passage.passage_text.ToString(), Field.Store.YES));
                        document.Add(new StringField("passage_id", passage.passage_ID.ToString(), Field.Store.YES));
                        writer.UpdateDocument(new Term("url", passage.url.ToString()), document);
                        //writer.AddDocument(document);
                    }
                }
                System.Console.WriteLine("All documents added.");
            }
        }
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}



// #changes to baseline system: ##INDEX: 1. Field normalisation 2.  term vectors 3.query expansion by wordnet 4.field-level-boosting 5.standard analyser 6.extract webpages' titles from their urls. 7. using Okapi 25 similarity