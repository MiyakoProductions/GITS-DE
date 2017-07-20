using FastColoredTextBoxNS;
using Microsoft.Win32;
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

namespace GITS_DE
{
    public partial class mods : Form
    {
        public mods()
        {
            InitializeComponent();
            if ((string)Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Miyako\GITS-DE", "mods", ",") == null ) {
                Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Miyako\GITS-DE", "mods", ",", RegistryValueKind.String);
            }
            if ((string)Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Miyako\GITS-DE", "ModPath", "") == null)
            {
                Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Miyako\GITS-DE", "ModPath", "Please Set Path", RegistryValueKind.String);
            }
        }
        string ModPath = (string)Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Miyako\GITS-DE", "ModPath", "Please Set Path");
        string EnabledMods = (string)Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Miyako\GITS-DE", "mods", ",");
        string SelectedMod = "";
        public static SolidBrush solarizedBase1 = new SolidBrush(Color.FromArgb(255, 147, 161, 161));
        public static SolidBrush solarizedRed = new SolidBrush(Color.FromArgb(255, 220, 50, 47));
        public static SolidBrush solarizedYellow = new SolidBrush(Color.FromArgb(255, 181, 137, 0));
        TextStyle infoStyle = new TextStyle(solarizedBase1, null, FontStyle.Regular);
        TextStyle warningStyle = new TextStyle(solarizedYellow, null, FontStyle.Regular);
        TextStyle errorStyle = new TextStyle(solarizedRed, null, FontStyle.Regular);
        // Log debug output
        private void debugLog(string text, Style style)
        {
            //some stuffs for best performance
            fastColoredTextBox1.BeginUpdate();
            fastColoredTextBox1.Selection.BeginUpdate();
            //remember user selection
            var userSelection = fastColoredTextBox1.Selection.Clone();
            //add text with predefined style
            fastColoredTextBox1.TextSource.CurrentTB = fastColoredTextBox1;
            fastColoredTextBox1.AppendText(text, style);
            fastColoredTextBox1.GoEnd();//scroll to end of the text
            //
            fastColoredTextBox1.Selection.EndUpdate();
            fastColoredTextBox1.EndUpdate();
        }

        private const int WM_NCLBUTTONDBLCLK = 0x00A3; //double click on a title bar a.k.a. non-client area of the form

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_NCLBUTTONDBLCLK)
            {
                m.Result = IntPtr.Zero;
                return;
            }
            base.WndProc(ref m);
        }

        private void mods_Load(object sender, EventArgs e)
        {
            textBox1.Text = (string)Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Miyako\GITS-DE", "ModPath", "Please Set Path");

            if (!Directory.Exists(@".\\backup\\"))
            {
                Directory.CreateDirectory(@".\\backup\");
            }
            if (!Directory.EnumerateFiles(@".\\backup\").Any())
            {
                revertToolStripMenuItem.Enabled = false;
            }
            if (Directory.EnumerateFiles(@".\\backup\").Any())
            {
                revertToolStripMenuItem.Enabled = true;
            }

            if (!Directory.Exists(@".\\mods\\"))
            {
                Directory.CreateDirectory(@".\\mods\\");
            }
            try
            {
                DirectoryInfo dinfo = new DirectoryInfo(@".\\mods\\");
                DirectoryInfo[] Files = dinfo.GetDirectories();
                foreach (DirectoryInfo file in Files)
                {
                    checkedListBox1.Items.Add(file.Name);
                }
                checkedListBox1.SelectedIndex = 0;
            }
            catch
            {
                debugLog("[Error] Mods folder is empty!\r\n", errorStyle);
            }

            // Select Mods
            try
            {
                EnabledMods.Split(',').ToList().ForEach(item =>
                {
                    var index = checkedListBox1.Items.IndexOf(item);
                    checkedListBox1.SetItemChecked(index, true);
                });
            }
            catch
            {
            }
        }

        private void checkedListBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            SelectedMod = checkedListBox1.SelectedItem.ToString();
            try
            {
                richTextBox1.Text = File.ReadAllText(".\\mods\\" + SelectedMod + "\\desc.txt");
            }
            catch
            {
                richTextBox1.Text = "No Description Found";
            }
            try
            {
                pictureBox1.Image = Image.FromFile(".\\mods\\" + SelectedMod + "\\preview.png");
            }
            catch
            {
                pictureBox1.Image = null;
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            folderBrowserDialog1.Description = "Path to Data folder";
            folderBrowserDialog1.ShowNewFolderButton = true;
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                textBox1.Text = folderBrowserDialog1.SelectedPath;
                Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Miyako\GITS-DE", "ModPath", folderBrowserDialog1.SelectedPath, RegistryValueKind.String);
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Miyako\GITS-DE", "ModPath", textBox1.Text, RegistryValueKind.String);
        }

        private void backupToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ModPath = (string)Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Miyako\GITS-DE", "ModPath", "Please Set Path");
            if (ModPath == "Please Set Path")
            {
                debugLog("[Error] Please set a path to the Data folder!", errorStyle);
                return;
            }
            debugLog("[Info] Backing up Data folder.\r\n", infoStyle);
            try {
                foreach (var file in Directory.GetFiles(ModPath, "*.dat"))
                {
                    if (File.Exists(Path.Combine(@".\\backup\\", Path.GetFileName(file))))
                    {
                        File.Delete(Path.Combine(@".\\backup\\", Path.GetFileName(file)));
                        //break;
                    }
                    File.Copy(file, Path.Combine(@".\\backup\\", Path.GetFileName(file)), true);
                }
                revertToolStripMenuItem.Enabled = true;
                debugLog("[Info] Successfully backed up Data folder!\r\n", warningStyle);
            }
            catch
            {
                debugLog("[Error] Unable to backup Data folder!\r\n", errorStyle);
            }
        }

        private void revertToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Restore Mods
            ModPath = (string)Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Miyako\GITS-DE", "ModPath", "Please Set Path");
            if (ModPath == "Please Set Path")
            {
                debugLog("[Error] Please set a path to the Data folder!", errorStyle);
                return;
            }
            debugLog("[Info] Restoring Data folder.\r\n", infoStyle);
            try {
                foreach (var file in Directory.GetFiles(@".\\backup\\", "*.dat"))
                {
                    if (File.Exists(Path.Combine(ModPath, Path.GetFileName(file))))
                    {
                        File.Delete(Path.Combine(ModPath, Path.GetFileName(file)));
                        //break;
                    }
                    File.Copy(file, Path.Combine(ModPath, Path.GetFileName(file)), true);
                }


                foreach (int i in checkedListBox1.CheckedIndices)
                {
                    checkedListBox1.SetItemCheckState(i, CheckState.Unchecked);
                }
                EnabledMods = String.Join(",", checkedListBox1.CheckedItems.Cast<string>().ToArray());
                Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Miyako\GITS-DE", "mods", EnabledMods + ",", RegistryValueKind.String);
                debugLog("[Info] Data folder restored!\r\n", warningStyle);
            }
            catch
            {
                debugLog("[Error] Unable to restore Data folder!\r\n", errorStyle);
            }
        }

        private void applyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Apply Mods
            ModPath = (string)Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Miyako\GITS-DE", "ModPath", "Please Set Path");
            if (ModPath == "Please Set Path")
            {
                debugLog("[Error] Please set a path to the Data folder!", errorStyle);
                return;
            }
            debugLog("[Info] Applying Mods.\r\n", infoStyle);
            try
            {
                foreach (var dir in checkedListBox1.CheckedItems.Cast<string>().ToArray())
                {
                    foreach (var file in Directory.GetFiles(@".\\mods\\" + dir + "\\", "*.dat"))
                    {
                        if (File.Exists(Path.Combine(ModPath, Path.GetFileName(file))))
                        {
                            File.Delete(Path.Combine(ModPath, Path.GetFileName(file)));
                            //break;
                        }
                            File.Copy(file, Path.Combine(ModPath, Path.GetFileName(file)), true);
                    }
                }
                EnabledMods = String.Join(",", checkedListBox1.CheckedItems.Cast<string>().ToArray());
                Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Miyako\GITS-DE", "mods", EnabledMods + ",", RegistryValueKind.String);
                debugLog("[Info] Successfully applied mods!\r\n", warningStyle);
            }
            catch
            {
                debugLog("[Error] Unable to apply mods!\r\n", errorStyle);
            }
        }
    }
}
