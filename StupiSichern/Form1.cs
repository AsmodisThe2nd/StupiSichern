using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Web;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections;
using System.Data.SqlClient;
using System.Xml;
using DotNetWikiBot;

namespace StupiSichern
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        private string lastarticle;
        private List<string> errors = new List<string> { };
        private bool sw = false;


        private void button1_Click(object sender, EventArgs e)
        {
            sw = !sw;
            if (sw)
            {
                if (textBox1.Text != "" && textBox2.Text != "")
                {

                    progressBar1.Maximum = (int)numericUpDown1.Value;
                    backgroundWorker1.RunWorkerAsync();
                    button1.Text = "Abbrechen";
                    textBox1.ReadOnly = true;
                    textBox2.ReadOnly = true;
                }
                else
                {
                    sw = !sw;
                }
            }
            else
            {
                backgroundWorker1.CancelAsync();
                button1.Text = "Starten";
                textBox1.ReadOnly = false;
                textBox2.ReadOnly = false;
            }

        }


        private static string RemoveIllegalCharacters(string fileName)
        {
            string illegal = fileName;
            string invalid = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());

            foreach (char c in invalid)
            {
                illegal = illegal.Replace(c.ToString(), "");
            }

            return illegal;
        }



        private void button2_Click(object sender, EventArgs e)
        {
            textBox3.Text = lastarticle;
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            int num = 0;

            try
            {
                SqlConnection con = new SqlConnection(Properties.Settings.Default.StupidataConnectionString);
                con.Open();
                SqlCommand cmd = con.CreateCommand();

                backgroundWorker1.ReportProgress(-1, new String[] { "Logging in" });
                Site site = new Site("http://www.stupidedia.org", textBox1.Text, textBox2.Text);
                backgroundWorker1.ReportProgress(-1, new String[] { "Done" });
                if (backgroundWorker1.CancellationPending)
                    return;
                backgroundWorker1.ReportProgress(-1, new String[] { "Fetching " + (int)numericUpDown1.Value + " Pages" });
                PageList pl = new PageList(site);
                pl.FillFromAllPages(textBox3.Text, 0, true, (int)numericUpDown1.Value);
                backgroundWorker1.ReportProgress(-1, new String[] { "Done" });

                string sql = "INSERT INTO Artikel (Titel, Namespace, Content) VALUES ( @tit, @ns , @txt)";

                foreach (Page p in pl)
                {
                    if (backgroundWorker1.CancellationPending)
                        return;
                    try
                    {
                        p.Load();

                        lastarticle = p.title;
                        cmd.CommandText = sql;
                        cmd.Parameters.Clear();
                        cmd.Parameters.AddWithValue("tit", p.title);
                        cmd.Parameters.AddWithValue("ns", p.GetNamespace());
                        cmd.Parameters.AddWithValue("txt", p.text);
                        
                        //cmd.Parameters.AddWithValue("tit", p.title);
                        //cmd.Parameters.AddWithValue("ns", p.GetNamespace());
                        //cmd.Parameters.AddWithValue("txt", p.text);
                        cmd.ExecuteNonQuery();

                        string ns = "";
                        if (!site.namespaces.TryGetValue(p.GetNamespace(), out ns))
                        {
                            ns = "Artikelnamensraum";
                        }
                        backgroundWorker1.ReportProgress(num, new String[] { p.title, ns });
                        num += 1;
                    }
                    catch (Exception ex2)
                    {
                        Console.WriteLine(ex2.Message);
                        backgroundWorker1.ReportProgress(-2, new String[] { p.title });
                    }

                }
                con.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (e.ProgressPercentage >= 0)
            {
                lastarticle = ((string[])e.UserState)[0];
                progressBar1.Value = e.ProgressPercentage;
                label6.Text = e.ProgressPercentage + "/" + numericUpDown1.Value.ToString();
                label5.Text = "Last: " + lastarticle;
                label7.Text = "Namespace: " + ((string[])e.UserState)[1];

            }
            else if (e.ProgressPercentage == -1)
            {
                label6.Text = ((string[])e.UserState)[0];
            }
            else if (e.ProgressPercentage == -2)
            {
                errors.Add(((string[])e.UserState)[0]);
            }
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            progressBar1.Value = 0;
            label6.Text = "";
            label7.Text = "Namespace:";
            button1.Text = "Starten";
            textBox1.ReadOnly = false;
            textBox2.ReadOnly = false;
            sw = !sw;
            if (!(errors.Count == 0))
            {
                File.WriteAllLines("Articles\\errors.txt", errors.ToArray());
                MessageBox.Show("Es gab Fehler während des Speicherns. Die betroffenen Artikel sind unter \"Articles\\errors.txt\" nachzulesen");
            }
            if (checkBox1.Checked)
            {
                textBox3.Text = lastarticle;
                button1_Click(sender, (EventArgs)e);
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            DialogResult dr = MessageBox.Show("Soll der zuletzt geladene Artikel gespeichert werden, damit später fortgefahren werden kann?", "Speichern?", MessageBoxButtons.YesNo);
            if (dr == System.Windows.Forms.DialogResult.Yes)
            {
                Properties.Settings.Default.last = lastarticle;
            }
            Properties.Settings.Default.Username = textBox1.Text;
            Properties.Settings.Default.Password = textBox2.Text;
            Properties.Settings.Default.Number = (int)numericUpDown1.Value;
            Properties.Settings.Default.Save();

        }

        private void Form1_Load(object sender, EventArgs e)
        {

            try
            {
                Properties.Settings.Default.Reload();

                textBox3.Text = Properties.Settings.Default.last;
                textBox1.Text = Properties.Settings.Default.Username;
                textBox2.Text = Properties.Settings.Default.Password;
                numericUpDown1.Value = (decimal)Properties.Settings.Default.Number;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}

////OLD - WORKS WITH FILES
        //private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        //{
        //    int num = 0;
        //    try
        //    {
        //        backgroundWorker1.ReportProgress(-1, new String[] { "Logging in" });
        //        Site site = new Site("http://www.stupidedia.org", textBox1.Text, textBox2.Text);
        //        backgroundWorker1.ReportProgress(-1, new String[] { "Done" });
        //        if (backgroundWorker1.CancellationPending)
        //            return;
        //        backgroundWorker1.ReportProgress(-1, new String[] { "Fetching " + (int)numericUpDown1.Value + " Pages" });
        //        PageList pl = new PageList(site);
        //        pl.FillFromAllPages(textBox3.Text, 0, true, (int)numericUpDown1.Value);
        //        backgroundWorker1.ReportProgress(-1, new String[] { "Done" });
        //        foreach (Page p in pl)
        //        {
        //            if (backgroundWorker1.CancellationPending)
        //                return;
        //            try
        //            {
        //                p.Load();

        //                lastarticle = p.title;
        //                string ns = "";
        //                if (!site.namespaces.TryGetValue(p.GetNamespace(), out ns))
        //                {
        //                    ns = "Artikelnamensraum";
        //                }
        //                ns = RemoveIllegalCharacters(ns);
        //                if (!Directory.Exists("Articles\\" + ns))
        //                {

        //                    Directory.CreateDirectory("Articles\\" + ns);
        //                }
        //                p.SaveToFile(Path.Combine("Articles\\" + ns, RemoveIllegalCharacters(p.title) + ".txt"));
        //                backgroundWorker1.ReportProgress(num, new String[] { p.title, ns });
        //                num += 1;
        //            }
        //            catch (Exception ex2)
        //            {
        //                backgroundWorker1.ReportProgress(-2, new String[] { p.title});
        //            }

        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show(ex.Message);
        //    }
        //}