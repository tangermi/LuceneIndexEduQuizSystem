using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using LuceneIndexEduQuizSystem;
using Lucene.Net.Documents;
using System.Diagnostics;

namespace LuceneIndexEduQuizSystem
{
    public partial class Form1 : Form
    {
        private Indexing myLuceneApp;
        public Form1()
        {
            InitializeComponent();
            System.Console.WriteLine("Hello Lucene.Net");
            myLuceneApp = new Indexing();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            
        }

        private void button1_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                textBoxIndex.Text = fbd.SelectedPath;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            var indextime = Task<string>.Factory.StartNew(()=> {
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                string indexPath = textBoxIndex.Text;
                string collectionPath = textBoxCollection.Text;
                if (indexPath == "")
                {
                    MessageBox.Show("Invalid index path!", "Error");
                    return "0";
                }
                if (collectionPath == "")
                {
                    MessageBox.Show("Invalid collection path!", "Error");
                    return "0";
                }
                myLuceneApp.CreateIndex(indexPath); myLuceneApp.CreateCollection(collectionPath); myLuceneApp.CleanUpIndexer();
                stopwatch.Stop();

                return stopwatch.ElapsedMilliseconds.ToString();
            });

            textBoxTime.Text = indextime.Result;
            MessageBox.Show("Indexing completed!", "Success");
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }

        private void button3_Click(object sender, EventArgs e)
        {
            // Index code
            if (textBoxTime.Text == "")
            {
                MessageBox.Show("Please create index first!", "Error");
                return;
            }

            List<String> resultList = new List<String>();
            string queryText = textBoxSearch.Text;
            if (queryText == "")
            {
                MessageBox.Show("Search cannot be empty!", "Error");
                return;
            }
            myLuceneApp.CreateSearcher();

            // box
            var expansion = checkBox1.Checked;
            if (!expansion)
            {
                queryText = myLuceneApp.GetWeightedExpandedQuery(queryText);
            }

            resultList = myLuceneApp.SearchAndDisplayResults(queryText);
            resultTextBox.Text = "Search for " + resultList[0] + "\n" + "About " + resultList[1] + " results " + "(" + resultList[2] + " milliseconds" + ")" + "\n";
            for (int i = 0; i < 10; i++)
            {
                if (i + 3 > resultList.Count)
                {
                    break;
                }

                resultTextBox.Text += resultList[i+3] + "\n";
            }
            myLuceneApp.CleanUpSearcher();
        }

        private void textBox1_TextChanged_1(object sender, EventArgs e)
        {

        }

        private void textBoxTime_TextChanged(object sender, EventArgs e)
        {

        }

        private void openFileDialog1_FileOk(object sender, CancelEventArgs e)
        {

        }

        private void button4_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Json files (*.json)|*.json";
            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                textBoxCollection.Text = ofd.FileName;
            }
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void resultTextBox_TextChanged(object sender, EventArgs e)
        {

        }

        private void resultTextBox_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            Process.Start(e.LinkText);
        }

        private void button5_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                textBoxSave.Text = fbd.SelectedPath;
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            string fileName = textBoxFileName.Text;
            string fileDir = textBoxSave.Text;
            if (fileName == "")
            {
                MessageBox.Show("File name cannot be empty!", "Error");
                return;
            }

            if (fileDir == "")
            {
                MessageBox.Show("File directory cannot be empty!", "Error");
                return;
            }
            fileName += ".txt";
            fileDir += @"\" + fileName;

            myLuceneApp.SaveFile(fileDir);
            MessageBox.Show("File has already been saved", "Success");
        }
    }
}
