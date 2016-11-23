﻿using Lab4hw.classes;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows.Forms;
using System.Xml;

namespace Lab4hw
{
    public partial class Form1 : Form
    {
        private BindingSource bs;
        private int threadNo;

        public Form1()
        {
            InitializeComponent();
            threadNo = 0;
            textBox2.Text = "http://";

            bs = new BindingSource();
            bs.DataSource = typeof(WordCounter);

            bs.Add(new WordCounter("http://www.google.com"));
            bs.Add(new WordCounter("http://www.facebook.com"));
            bs.Add(new WordCounter("http://www.emag.ro"));
            bs.Add(new WordCounter("http://www.pcgarage.ro"));

            dataGridView1.DataSource = bs;
            dataGridView1.AutoGenerateColumns = true;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            String url = textBox2.Text;
            checkAndAddUrl(url);
            dataGridView1.Refresh();
        }

        private void checkAndAddUrl(String url)
        {
            try
            {
                HttpWebRequest request = HttpWebRequest.Create(url) as HttpWebRequest;
                request.Timeout = 5000;

                HttpWebResponse response = request.GetResponse() as HttpWebResponse;

                int statusCode = (int) response.StatusCode;

                if (statusCode >= 100 && statusCode < 400) //Good requests
                {
                    bs.Add(new WordCounter(url));
                    return;
                }
                else if (statusCode >= 500 && statusCode <= 510) //Server Errors
                {
                    MessageBox.Show("Url is not valid", "Error");
                }
            }
            catch (WebException ex)
            {
                MessageBox.Show("Url is not valid!", "Error");
            }
        }

        private void startButton_Click(object sender, EventArgs e)
        {
            threadNo = bs.List.Count;
            String word = textBox1.Text;
            progressBar1.Refresh();
            progressBar1.Step = 100/threadNo;
            
            var wcs = new WordCounter[threadNo];
            var dlg = new MyDelegate(Work);

            int i;
            for (i = 0; i < threadNo; ++i)
            {
                WordCounter wc = (WordCounter)bs.List[i];
                wc.Word = word;
                wcs[i] = wc;
            }

            var asyncResults = from wc in wcs select dlg.BeginInvoke(wc, null, null);
            var results = new List<WordCounter>();
            var sw = System.Diagnostics.Stopwatch.StartNew();
            foreach (var asyncRes in asyncResults.ToArray())
                results.Add(dlg.EndInvoke(asyncRes));

            label1.Text = "Done!";
            PrintXml();
        }

        private void UpdateProgressBar(WordCounter wordCounter)
        {
            label1.Text = "Done with " + wordCounter.Url + "!";
            progressBar1.PerformStep();
            dataGridView1.Refresh();
        }

        public delegate WordCounter MyDelegate(WordCounter wc);

        private WordCounter Work(WordCounter wc)
        {
            DateTime startTime = DateTime.Now;
            wc.Compute();
            DateTime endTime = DateTime.Now;

            TimeSpan ts = endTime - startTime;
            wc.Duration = ts.Seconds;

            UpdateProgressBar(wc);

            return wc;
        }

        private void PrintXml()
        {
            using (XmlWriter writer = XmlWriter.Create("urls.xml"))
            {
                writer.WriteStartDocument();
                writer.WriteStartElement("Items");

                foreach (WordCounter item in bs.List)
                {
                    writer.WriteStartElement("Item");

                    writer.WriteElementString("Url", item.Url);   // <-- These are new
                    writer.WriteElementString("Word", item.Word);
                    writer.WriteElementString("Findings", item.Findings.ToString());
                    writer.WriteElementString("Duration", item.Duration.ToString());

                    writer.WriteEndElement();
                }

                writer.WriteEndElement();
                writer.WriteEndDocument();
            }
        }

    }
}
