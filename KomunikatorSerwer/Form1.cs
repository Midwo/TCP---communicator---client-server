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
using System.Net.Sockets;
using System.Net;

namespace KomunikatorSerwer
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            webBrowser1.Navigate("about:blank");
            webBrowser1.Document.Write("<html><head><style>body,table { font-size: 10pt; font - family: Verdana; margin: 3px 3px 3px 3px; font - color: black; }</style></head><body width =\"" + (webBrowser1.ClientSize.Width - 20).ToString() + "\">");
            IPHostEntry adresyIP = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress pozycja in adresyIP.AddressList)
                comboBox1.Items.Add(pozycja.ToString());


        }
        private int PozycjaKursora;
        private TcpListener serwer = null;
        private TcpClient klient = null;
        private string adresIP = "127.0.0.1";
        private BinaryReader czytanie = null;
        private BinaryWriter pisanie = null;
        private bool polaczenieAktywne = false;

        delegate void SetTextCallBack(string tekst);
        delegate void SetScrollCallBack();

        private void SetText(string tekst)
        {
            if (listBox1.InvokeRequired)
            {
                SetTextCallBack f = new SetTextCallBack(SetText);
                this.Invoke(f, new object[] { tekst });
            }
            else
            {
                this.listBox1.Items.Add(tekst);
            }
        }

        private void SetTextHTML(string tekst)
        {
            if (webBrowser1.InvokeRequired)
            {
                SetTextCallBack f = new SetTextCallBack(SetTextHTML);
                this.Invoke(f, new object[] { tekst });
            }
            else
            {
                this.webBrowser1.Document.Write(tekst);

            }
        }

        private void SetScroll()
        {
            if (webBrowser1.InvokeRequired)
            {
                SetScrollCallBack s = new SetScrollCallBack(SetScroll);
                this.Invoke(s);
            }
            else
            {
                webBrowser1.Document.Window.ScrollTo(0, webBrowser1.Document.Body.ScrollRectangle.Height);
            }
        }

        private void WpiszTekst(string kto, string wiadomosc)
        {
            SetTextHTML("<table><tr><td width=\"10%\"><b>" + kto + "</b></td><td width =\"90%\">(" + DateTime.Now.ToShortTimeString() + "):</td></tr>");
            SetTextHTML("<tr><td colspan=2>" + wiadomosc + "</td></tr></table>");
            SetTextHTML("<hr>");
            SetScroll();
        }

        private void WprowadzTag(string tag)
        {
            try
            {
                string kod = textBox1.Text;
                textBox1.Text = kod.Insert(PozycjaKursora, tag);
                textBox1.Focus();
                if (tag == "<br>" || tag == "<hr>")
                {
                    textBox1.Select(PozycjaKursora + tag.Length, 0);
                    PozycjaKursora += tag.Length;
                }
                else
                {
                    textBox1.Select(PozycjaKursora + tag.Length / 2, 0);
                    PozycjaKursora += tag.Length / 2;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Nie zaznaczono pola: " + ex.Message, "Error");
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            WprowadzTag("<b></b>");
        }

        private void button4_Click(object sender, EventArgs e)
        {
            WprowadzTag("<i></i>");
        }

        private void textBox1_KeyUp(object sender, KeyEventArgs e)
        {
            PozycjaKursora = textBox1.SelectionStart;
        }

        private void textBox1_MouseUp(object sender, MouseEventArgs e)
        {
            PozycjaKursora = textBox1.SelectionStart;
        }

        private void wyczyśćToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.webBrowser1.Navigate("about:blank");
        }

        private void zapiszToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                using (StreamWriter sw = new StreamWriter(saveFileDialog1.FileName))
                    try
                    {
                        sw.Write(webBrowser1.DocumentText);
                    }
                    catch
                    {
                        MessageBox.Show("Nie można zapisać pliku: " + saveFileDialog1.FileName);
                    }
            }
        }

       

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            IPAddress serwerIP;
            try
            {
                serwerIP = IPAddress.Parse(adresIP);
            }
            catch
            {
                MessageBox.Show("Błędny adres IP");
                polaczenieAktywne = false;
                return;
            }
            serwer = new TcpListener(serwerIP, (int)numericUpDown1.Value);
            try
            {
                serwer.Start();
                SetText("Oczekuje na połączenie ...");
                klient = serwer.AcceptTcpClient();
                NetworkStream ns = klient.GetStream();
                SetText("Klient próbuje się połączyć");
                czytanie = new BinaryReader(ns);
                pisanie = new BinaryWriter(ns);
                if (czytanie.ReadString() == "###HI###")
                {
                    SetText("Klient połączony");
                    backgroundWorker2.RunWorkerAsync();
                }
                else
                {
                    SetText("Klient nie wykonał wymaganej autoryzacji. Połączenie przerwane");
                    klient.Close();
                    serwer.Stop();
                    polaczenieAktywne = false;
                }
            }
            catch
            {
                SetText("Połączenie zostało przerwane");
                polaczenieAktywne = false;
            }
        }

        private void backgroundWorker2_DoWork(object sender, DoWorkEventArgs e)
        {
            string wiadomosc;
            try
            {
                while ((wiadomosc = czytanie.ReadString()) != "###BYE###")
                {
                    encryptDecrypt decrypt = new encryptDecrypt();
                    string decryptstring = decrypt.decrypt(wiadomosc);
                    WpiszTekst("ktoś", decryptstring);
                }
                klient.Close();
                serwer.Stop();
                SetText("Połączenie zostało przerwane przez klienta");
            }
            catch
            {
                SetText("Klient rozłączony");
                polaczenieAktywne = false;
                klient.Close();
                serwer.Stop();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (polaczenieAktywne == false)
            {
                polaczenieAktywne = true;
                backgroundWorker1.RunWorkerAsync();
            }
            else
            {
                polaczenieAktywne = false;
                if (klient != null)
                    klient.Close();
                serwer.Stop();
                backgroundWorker1.CancelAsync();
                if (backgroundWorker2.IsBusy)
                    backgroundWorker2.CancelAsync();
            }

        }

        private void button2_Click(object sender, EventArgs e)
        {
            WpiszTekst("ja", textBox1.Text);
            if (polaczenieAktywne)
            {
                encryptDecrypt encrypt = new encryptDecrypt();
                string encryptString = encrypt.encrypt(textBox1.Text);
                pisanie.Write(encryptString);
            }
              
            textBox1.Text = "";
         
        }

        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)13)
                this.button2_Click(sender, e);
        }

        private void comboBox1_TextChanged(object sender, EventArgs e)
        {
            adresIP = comboBox1.Text;
        }
    }
}
