using System;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Web;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace 编码转换工具
{
    public partial class Form1 : Form
    {
        public static string BytesToBase32(byte[] bytes)
        {
            const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
            string output = "";
            for (int bitIndex = 0; bitIndex < bytes.Length * 8; bitIndex += 5)
            {
                int dualbyte = bytes[bitIndex / 8] << 8;
                if (bitIndex / 8 + 1 < bytes.Length)
                    dualbyte |= bytes[bitIndex / 8 + 1];
                dualbyte = 0x1f & (dualbyte >> (16 - bitIndex % 8 - 5));
                output += alphabet[dualbyte];
            }

            return output;
        }
        public static byte[] Base32ToBytes(string base32)
        {
            const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
            List<byte> output = new List<byte>();
            char[] bytes = base32.ToCharArray();
            for (int bitIndex = 0; bitIndex < base32.Length * 5; bitIndex += 8)
            {
                int dualbyte = alphabet.IndexOf(bytes[bitIndex / 5]) << 10;
                if (bitIndex / 5 + 1 < bytes.Length)
                    dualbyte |= alphabet.IndexOf(bytes[bitIndex / 5 + 1]) << 5;
                if (bitIndex / 5 + 2 < bytes.Length)
                    dualbyte |= alphabet.IndexOf(bytes[bitIndex / 5 + 2]);

                dualbyte = 0xff & (dualbyte >> (15 - bitIndex % 5 - 8));
                output.Add((byte)(dualbyte));
            }
            return output.ToArray();
        }
        public Form1()
        {
            InitializeComponent();
        }
        private void Form1_Load(object sender, EventArgs e)
        {

        }
        private void button2_Click(object sender, EventArgs e) //转换
        {
            // ASCII
            byte[] ascii = Encoding.UTF8.GetBytes(textBox5.Text);
            for (int i = 0; i < ascii.Length; i++)
            {
                int str = (int)(ascii[i]);
                textBox6.Text += Convert.ToString(str+" ");
            }
            // URL编码
            StringBuilder sb = new StringBuilder();
            byte[] url = Encoding.UTF8.GetBytes(textBox5.Text);
            for (int i = 0; i < url.Length; i++)
            {
                sb.Append(@"%" + Convert.ToString(url[i], 16));
            }
            textBox7.Text = sb.ToString();

            // MD5
            var MD5 = new MD5CryptoServiceProvider();
            byte[] md5 = Encoding.UTF8.GetBytes(textBox5.Text);
            textBox8.Text = BitConverter.ToString(MD5.ComputeHash(md5), 4, 8).Replace("-", "");
            textBox9.Text = BitConverter.ToString(MD5.ComputeHash(md5)).Replace("-", "").ToLower();

            // HEX
            byte[] hex = Encoding.UTF8.GetBytes(textBox5.Text);
            textBox10.Text = "0x"+BitConverter.ToString(hex).Replace("-", "").ToLower();
            
            // Base64
            byte[] bytes = Encoding.Default.GetBytes(textBox5.Text);
            textBox11.Text = Convert.ToBase64String(bytes);
            
        }

        private void button3_Click(object sender, EventArgs e) // base 加密
        {
            byte[] bytes = Encoding.Default.GetBytes(textBox4.Text);
            textBox1.Text = Convert.ToBase64String(bytes);
            textBox2.Text = BytesToBase32(bytes);

        }

        private void button1_Click(object sender, EventArgs e) // base64 解密
        {
            byte[] base64 = Convert.FromBase64String(textBox1.Text);
            textBox4.Text = Encoding.UTF8.GetString(base64);

        }

        private void button4_Click(object sender, EventArgs e)
        {
            string base32 = textBox2.Text;
            textBox4.Text = Encoding.Default.GetString(Base32ToBytes(base32));
        }

        private void textBox1_TextChanged(object sender, EventArgs e) // base 64
        {

        }

        private void textBox2_TextChanged(object sender, EventArgs e) // base 32
        {

        }

        private void textBox4_TextChanged(object sender, EventArgs e)
        {

        }

        private void label7_Click(object sender, EventArgs e)
        {

        }

        private void textBox5_TextChanged(object sender, EventArgs e) // 原始数据
        {

        }

        private void textBox6_TextChanged(object sender, EventArgs e) //ascii
        {

        }

        private void textBox7_TextChanged(object sender, EventArgs e) //URL
        {

        }

        private void textBox8_TextChanged(object sender, EventArgs e) //md5_16
        {

        }

        private void textBox9_TextChanged(object sender, EventArgs e) //md5_32
        {

        }

        private void textBox10_TextChanged(object sender, EventArgs e)  // hex
        {

        }
        private void textBox11_TextChanged(object sender, EventArgs e) //base64
        {

        }
    }
}
