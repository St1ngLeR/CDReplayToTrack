using System;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Resources;
using System.Text;
using System.Windows.Forms;

namespace CDReplayToTrack
{
    public partial class Form1 : Form
    {
        private string filePath2;
        private string trackname;
        private int tracknameoffset = 262226;
        byte[] fileBytes;
        byte[] extractedBytes;
        string[] _args;
        public Form1(string[] args)
        {
            InitializeComponent();
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            this.CenterToScreen();
            _args = args;

            try
            {
                if (_args.Length > 0)
                {
                    StartExtract(_args[0]);
                    File.WriteAllBytes(Path.GetDirectoryName(_args[0]) + @"\" + trackname, extractedBytes);
                    Environment.Exit(0);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("The specified file is not a replay file!", "Error!",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(0);
            }
        }
        
        void StartExtract(string filePath)
        {
            byte[] buffer = new byte[1];
            byte[] cdtrkBytes = System.Text.Encoding.ASCII.GetBytes("CDTRK");
            byte[] nullBytes = new byte[32];

            using (StreamReader sr = new StreamReader(filePath))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    if (line.Contains("RP#2"))
                    {
                        tracknameoffset = 262226+4;
                        break;  // No need to continue reading once we find "RP#2"
                    }
                    if (line.Contains("RP#3"))
                    {
                        tracknameoffset = 262226+4;
                        break;  // No need to continue reading once we find "RP#3"
                    }
                    else
                    {
                        tracknameoffset = 262226;
                        break;
                    }
                }
            }
            byte[] fileBytes = File.ReadAllBytes(filePath);
            // Find the index of the zero byte starting from rpltracknameoffset
            int zeroByteIndex = -1;
            for (int i = tracknameoffset; i < fileBytes.Length; i++)
            {
                if (fileBytes[i] == 0)
                {
                    zeroByteIndex = i;
                    break;
                }
            }
            if (zeroByteIndex != -1)
            {
                // Convert the bytes from rpltracknameoffset to zeroByteIndex into a string
                trackname = System.Text.Encoding.GetEncoding(1251).GetString(fileBytes, tracknameoffset, zeroByteIndex - tracknameoffset);
            }
            using (BinaryReader reader = new BinaryReader(File.Open(filePath, FileMode.Open)))
            {
                fileBytes = reader.ReadBytes((int)reader.BaseStream.Length);
            }

            // Search for the "CDTRK" text
            int startIndex = 0;
            for (int i = 0; i < fileBytes.Length - 5; i++)
            {
                if (Encoding.ASCII.GetString(fileBytes, i, 5) == "CDTRK")
                {
                    startIndex = i;
                    break;
                }
            }

            // Search for 32 zero bytes from the end of previously read bytes
            int endIndex = fileBytes.Length;
            for (int i = fileBytes.Length - 1; i >= startIndex; i--)
            {
                bool foundNullBytes = true;
                for (int j = i; j > i - 32; j--)
                {
                    if (fileBytes[j] != 0)
                    {
                        foundNullBytes = false;
                        break;
                    }
                }

                if (foundNullBytes)
                {
                    // Set the end index to the position before the 32 null bytes
                    endIndex = i+1;
                    break;
                }
            }

            // Remove all bytes before the start index and after the end index
            extractedBytes = new byte[endIndex - startIndex];
            Array.Copy(fileBytes, startIndex, extractedBytes, 0, extractedBytes.Length);
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.FileName = "";
            ofd.DefaultExt = ".rpl";
            ofd.Filter = "Crashday Replay (*.rpl)|*.rpl";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                filePath2 = ofd.FileName;
                textBox1.Text = filePath2;
                button2.Enabled = true;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            StartExtract(filePath2);

            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "Crashday Track|*.trk";
            sfd.Title = "Save track file as...";
            sfd.FileName = trackname;
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                File.WriteAllBytes(sfd.FileName, extractedBytes);
            }
        }
    }
}