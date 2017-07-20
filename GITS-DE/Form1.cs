using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Miyako;
using DDSViewer;
using System.IO;
using FastColoredTextBoxNS;
using System.Threading;
using System.Diagnostics;

namespace GITS_DE
{


    public partial class Form1 : Form
    {
        // Initialize variables
        static readonly string[] SizeSuffixes =
           { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
        private string selectedFile;
        private string selectedFileName;
        private string selectedArchive;
        private string fOffset;
        private string cacheFolder = Path.GetTempPath() + "\\Archive Data\\";
        private long fOffsetLong;
        public string ddsFormat;
        public static SolidBrush solarizedBase1 = new SolidBrush(Color.FromArgb(255, 147, 161, 161));
        public static SolidBrush solarizedRed = new SolidBrush(Color.FromArgb(255, 220, 50, 47));
        public static SolidBrush solarizedYellow = new SolidBrush(Color.FromArgb(255, 181, 137, 0));
        TextStyle infoStyle = new TextStyle(solarizedBase1, null, FontStyle.Regular);
        TextStyle warningStyle = new TextStyle(solarizedYellow, null, FontStyle.Regular);
        TextStyle errorStyle = new TextStyle(solarizedRed, null, FontStyle.Regular);

        public Form1()
        {
            InitializeComponent();
            ResDll.ExtractDll("ufmod.dll", Properties.Resources.ufmod);
            ResDll.LoadDll("ufmod.dll");
            Process updater = new Process();
            updater.StartInfo.FileName = ".\\updater.exe";
            updater.Start();
        }

        // Clear the file cache
        private void clearFolder(string FolderName)
        {
            DirectoryInfo dir = new DirectoryInfo(FolderName);

            foreach (FileInfo fi in dir.GetFiles())
            {
                fi.Delete();
            }

            foreach (DirectoryInfo di in dir.GetDirectories())
            {
                clearFolder(di.FullName);
                di.Delete();
            }
        }

        // Make the file sizes human readable
        static string SizeSuffix(Int64 value)
        {
            if (value < 0) { return "-" + SizeSuffix(-value); }
            if (value == 0) { return "0.0 bytes"; }

            int mag = (int)Math.Log(value, 1024);
            decimal adjustedSize = (decimal)value / (1L << (mag * 10));

            return string.Format("{0:n1} {1}", adjustedSize, SizeSuffixes[mag]);
        }

        // Build the explorer tree
        private void BuildTree(DirectoryInfo directoryInfo, TreeNodeCollection addInMe)
        {
            TreeNode curNode = addInMe.Add(directoryInfo.Name);

            foreach (FileInfo file in directoryInfo.GetFiles())
            {
                // if (file.Name.EndsWith(".dds")) { 
                curNode.Nodes.Add(file.FullName, file.Name); //}
            }
            foreach (DirectoryInfo subdir in directoryInfo.GetDirectories())
            {
                BuildTree(subdir, curNode.Nodes);
            }
        }

        private TreeNode m_OldSelectNode;
        private void nativeTreeView1_MouseUp(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            // Show menu only if the right mouse button is clicked.
            if (e.Button == MouseButtons.Right)
            {

                // Point where the mouse is clicked.
                Point p = new Point(e.X, e.Y);

                // Get the node that the user has clicked.
                TreeNode node = nativeTreeView1.GetNodeAt(p);
                if (node != null)
                {

                    // Select the node the user has clicked.
                    // The node appears selected until the menu is displayed on the screen.
                    m_OldSelectNode = nativeTreeView1.SelectedNode;
                    nativeTreeView1.SelectedNode = node;

                    // Find the appropriate ContextMenu depending on the selected node.
                    //MessageBox.Show(node.Name);

                    contextMenuStrip1.Show(nativeTreeView1, p);

                    // Highlight the selected node.
                    //nativeTreeView1.SelectedNode = m_OldSelectNode;
                    //m_OldSelectNode = null;
                }
            }
        }

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

        private void renderDDS(string DDSpath)
        {
            if (pictureBox1.Image != null) { pictureBox1.Image.Dispose(); }
            try
            {
                DDSImage img = new DDSImage(File.ReadAllBytes(DDSpath)); // Returns Exception: "This is not a DDS!"
                pictureBox1.Image = img.images[0];
                if (Form1.ActiveForm.Text.Contains("Format: "))
                {
                    Form1.ActiveForm.Text = Form1.ActiveForm.Text;
                }
                else {
                    Form1.ActiveForm.Text = Form1.ActiveForm.Text + " (Format: " + img.ddsFormat + " Resolution: " + img.ddsWidth + "x" + img.ddsHeight + ")";
                }
            }
            catch
            {
                debugLog("[Error] Invalid DDS!\r\n", errorStyle);
            }
        }
        private void renderPNG(string PNGpath)
        {
    
                if (pictureBox1.Image != null) { pictureBox1.Image.Dispose(); }
                try
                {
                    pictureBox1.Image = Image.FromFile(PNGpath);
                }
                catch
                {
                    debugLog("[Error] Invalid PNG!\r\n", errorStyle);
                }
        }
        private void renderBMP(string BMPpath)
        {

                if (pictureBox1.Image != null) { pictureBox1.Image.Dispose(); }
                try
                {
                    pictureBox1.Image = Image.FromFile(BMPpath);
                }
                catch
                {
                    debugLog("[Error] Invalid BMP!\r\n", errorStyle);
                }  
        }

        // Select a file to edit
        private void nativeTreeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            selectedFile = Path.GetTempPath() + e.Node.FullPath;
            selectedFileName = (selectedFile).Split(new char[] { @"\"[0], "/"[0] }).Last();
            Form1.ActiveForm.Text = "GITS:FA Data Editor - Viewing: " + selectedFileName;


            if (e.Node.Name.EndsWith(".dds") || e.Node.Name.EndsWith(".DDS"))
            {
                fastColoredTextBox2.Visible = false;
                //panel1.Visible = false;
                renderDDS(selectedFile);
            }
            if (e.Node.Name.EndsWith(".png") || e.Node.Name.EndsWith(".PNG"))
            {
                fastColoredTextBox2.Visible = false;
                //panel1.Visible = false;
                renderPNG(selectedFile);
            }
            if (e.Node.Name.EndsWith(".bmp") || e.Node.Name.EndsWith(".BMP"))
            {
                fastColoredTextBox2.Visible = false;
                //panel1.Visible = false;
                renderBMP(selectedFile);
            }
            /*
            if (e.Node.Name.EndsWith(".tga") || e.Node.Name.EndsWith(".TGA"))
            {
                Form1.ActiveForm.Text = "GITS:FA Data Editor - Viewing: " + selectedFileName;
                fastColoredTextBox2.Visible = false;
                //panel1.Visible = false;
                //renderTGA(selectedFile);

            }
            if (e.Node.Name.EndsWith(".swf") || e.Node.Name.EndsWith(".SWF"))
            {
                Form1.ActiveForm.Text = "GITS:FA Data Editor - Viewing: " + selectedFileName;
                fastColoredTextBox2.Visible = false;
                //panel1.Visible = true;
                //axShockwaveFlash1.Stop();
                //axShockwaveFlash1.ScaleMode = 0;
                //axShockwaveFlash1.Movie = selectedFile;
                //axShockwaveFlash1.Play();
                //renderPNG(selectedFile);
            }
            */
            else if (e.Node.Name.EndsWith(".ini") || e.Node.Name.EndsWith(".txt") || e.Node.Name.EndsWith(".bat") || e.Node.Name.EndsWith(".xml") || e.Node.Name.EndsWith(".INI") || e.Node.Name.EndsWith(".TXT") || e.Node.Name.EndsWith(".BAT") || e.Node.Name.EndsWith(".XML"))
            {
                fastColoredTextBox2.Clear();
                fastColoredTextBox2.Visible = true;
                //panel1.Visible = false;
                fastColoredTextBox2.Text = (File.ReadAllText(Path.GetTempPath() + e.Node.FullPath));
            }
            else {
                return;
            }
            
        }

        // Open an archive file
        private void openFile()
        {
            Form1.ActiveForm.Text = "GITS:FA Data Editor";
            nativeTreeView1.Nodes.Clear();
            dataGridView1.Rows.Clear();
            fastColoredTextBox1.Clear();
            debugLog("[Info] Processing Archive " + selectedArchive + "\r\n", infoStyle);
            using (FileStream fs = new FileStream(selectedArchive, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                try
                {
                    foreach (DatArchiveFile file in new DatArchive(fs).Files)
                    {
                        string fType = "";
                        string fName = (file.Name).Split(new char[] { @"\"[0], "/"[0] }).Last();
                        string fExt = fName.Split(new char[] { @"."[0] }).Last();
                        if (fExt == "dds" || fExt == "DDS")
                        {
                            fType = "Texture 2D";
                        }
                        else if (fExt == "nif")
                        {
                            fType = "Gamembryo Nif Model";
                        }
                        else if (fExt == "ini")
                        {
                            fType = "INI Config";
                        }
                        else if (fExt == "txt")
                        {
                            fType = "Plain Text File";
                        }
                        else if (fExt == "bat")
                        {
                            fType = "MSDOS Batch File";
                        }
                        else if (fExt == "ttf" || fExt == "TTF")
                        {
                            fType = "True Type Font";
                        }
                        else if (fExt == "swf")
                        {
                            fType = "Adobe Flash File";
                        }
                        else if (fExt == "png")
                        {
                            fType = "Portable Network Graphic";
                        }
                        else if (fExt == "gfx")
                        {
                            fType = "Gamebryo Graphics File";
                        }
                        else if (fExt == "slm")
                        {
                            fType = "Gamebryo Material Library";
                        }
                        else if (fExt == "sl9")
                        {
                            fType = "Gamebryo Shader Library";
                        }
                        else if (fExt == "tga")
                        {
                            fType = "Targa Image File";
                        }
                        else
                        {
                            fType = "Not Yet Implemented";
                        }
                        string offset = Convert.ToString(file._dataOffset, 16).ToUpper();

                        debugLog("[Info] Processing file " + fName + " at offset 0x" + offset + "\r\n", infoStyle);
                        dataGridView1.Rows.Add(file.Name, SizeSuffix(file.Size), fType, "0x" + offset);
                        
                        string destinationPath = Path.Combine(cacheFolder, file.Name);
                        string destinationFolder = Path.GetDirectoryName(destinationPath);
                        Directory.CreateDirectory(destinationFolder);
                        Path.GetFileName(file.Name);
                        try
                        {
                            file.Save(destinationPath);
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
                catch (Exception)
                {
                    debugLog("[Error] Invalid file .dat archive!", errorStyle);
                    return;
                }

                debugLog("[Info] Archive " + selectedArchive + " Processed\r\n", infoStyle);
                DirectoryInfo directoryInfo = new DirectoryInfo(cacheFolder);
                if (directoryInfo.Exists)
                {
                    nativeTreeView1.AfterSelect += nativeTreeView1_AfterSelect;
                    BuildTree(directoryInfo, nativeTreeView1.Nodes);
                    nativeTreeView1.Nodes[0].Expand();
                }
            }
        }

        private void openArchiveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openFileDialog1.Title = "Browse";
            openFileDialog1.FileName = "";
            openFileDialog1.Filter = "GITS:FA Archive|*.dat";
            openFileDialog1.FilterIndex = 1;
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                if (Directory.Exists(Path.GetTempPath() + "\\Archive Data\\"))
                {
                    clearFolder(Path.GetTempPath() + "\\Archive Data\\");
                    Directory.Delete(Path.GetTempPath() + "\\Archive Data\\", true);
                }
                else
                {
                    Directory.CreateDirectory(Path.GetTempPath() + "\\Archive Data\\");
                }
                selectedArchive = openFileDialog1.FileName;
                openFile();
            }
        }

        private void exportArchiveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (selectedArchive == null)
            {
                return;
            }
            else {
                string selectedArchiveName = (selectedArchive).Split(new char[] { @"\"[0], "/"[0] }).Last();
                folderBrowserDialog1.Description = "Export Archive";
                folderBrowserDialog1.ShowNewFolderButton = true;
                if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
                {
                    using (FileStream fs = new FileStream(selectedArchive, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        try
                        {
                            foreach (DatArchiveFile file in new DatArchive(fs).Files)
                            {
                                string destinationPath = Path.Combine(folderBrowserDialog1.SelectedPath, file.Name);
                                string destinationFolder = Path.GetDirectoryName(destinationPath);
                                Directory.CreateDirectory(destinationFolder);
                                Path.GetFileName(file.Name);
                                try
                                {
                                    file.Save(destinationPath);
                                }
                                catch (Exception)
                                {
                                }
                            }
                            debugLog("[Info] Export Complete\r\n", warningStyle);
                        }
                        catch (Exception)
                        {
                            debugLog("[Error] Invalid .dat archive!\r\n", errorStyle);
                        }
                    }
                }
            }
        }

        private void importFile()
        {
            if (selectedFile == null)
            {
                return;
            }
            else {
                // Open New File
                openFileDialog2.Title = "Browse";
                openFileDialog2.FileName = "";
                if (openFileDialog2.ShowDialog() == DialogResult.OK)
                {
                    // Compare File Sizes
                    FileInfo newFile = new FileInfo(openFileDialog2.FileName);
                    long newFileLength = newFile.Length;
                    FileInfo oldFile = new FileInfo(selectedFile);
                    long oldFileLenth = oldFile.Length;

                    if (newFileLength != oldFileLenth)
                    {
                        debugLog("[Error] File Size Mismatch!\r\n", errorStyle);
                        return;
                    }
                    else {
                        // Get Data Offset of Old File
                        string selectedRow = nativeTreeView1.SelectedNode.FullPath.Replace("Archive Data\\", "");

                        int rowIndex = -1;

                        dataGridView1.ClearSelection();
                        try
                        {
                            foreach (DataGridViewRow row in dataGridView1.Rows)
                            {
                                if (row.Cells["colFile"].Value.ToString().EndsWith(selectedRow))
                                {
                                    rowIndex = row.Index;
                                    dataGridView1.CurrentCell = dataGridView1.Rows[rowIndex].Cells[0];
                                    dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Selected = true;
                                    fOffset = Convert.ToString(dataGridView1.Rows[rowIndex].Cells[3].Value);
                                    fOffsetLong = Convert.ToInt64((fOffset).Split(new char[] { @"x"[0] }).Last(), 16);
                                    //toolStripStatusLabel1.Text = fOffset;
                                    break;
                                }
                            }
                        }
                        catch (Exception exc)
                        {
                            debugLog("[Error] " + exc.Message + "\r\n", errorStyle);
                        }

                        // Write Files
                        // Copy the upk to a new folder
                        try
                        {
                            File.Copy(selectedArchive, selectedArchive + ".bak", true);
                        }
                        catch   // Unable to copy
                        {
                            debugLog("[Error] Failed to backup file!\r\n", errorStyle);
                            return;
                        }
                        // Store our new string as a byte array

                        byte[] patch = File.ReadAllBytes(openFileDialog2.FileName);

                        try
                        {
                            // Open the target upk
                            BinaryWriter bw = new BinaryWriter(File.Open(selectedArchive, FileMode.Open, FileAccess.ReadWrite));

                            // Seek to the offset our data is located at
                            bw.BaseStream.Seek(fOffsetLong, SeekOrigin.Begin);

                            // Write our new bytes
                            bw.Write(patch);

                            // Close the file handle
                            bw.Close();

                            // Tell the user we are done
                            debugLog("[Info] File Successfully Imported!\r\n", warningStyle);
                            //openFile();
                            pictureBox1.Image.Dispose();
                            File.Delete(selectedFile);
                            File.Copy(openFileDialog2.FileName, selectedFile);
                            if (selectedFile.EndsWith(".dds") || selectedFile.EndsWith(".DDS"))
                            {
                                //richTextBox1.Visible = false;
                                //panel1.Visible = false;
                                renderDDS(selectedFile);
                            }
                            if (selectedFile.EndsWith(".png") || selectedFile.EndsWith(".PNG"))
                            {
                                //richTextBox1.Visible = false;
                                //panel1.Visible = false;
                                renderPNG(selectedFile);
                            }
                            if (selectedFile.EndsWith(".tga") || selectedFile.EndsWith(".TGA"))
                            {
                                // richTextBox1.Visible = false;
                                //panel1.Visible = false;
                                // renderTGA(selectedFile);
                            }
                        }
                        catch   // Unable to patch
                        {
                            debugLog("[Error] Failed to import file!\r\n", errorStyle);
                            return;
                        }

                    }
                }
            }
        }

        private void exportFile()
        {
            if (selectedFile == null)
            {
                return;
            }
            else {
                string selectedFileName = (selectedFile).Split(new char[] { @"\"[0], "/"[0] }).Last();
                saveFileDialog1.Title = "Export File";
                saveFileDialog1.OverwritePrompt = true;
                saveFileDialog1.FileName = selectedFileName;
                if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    File.Copy(selectedFile, saveFileDialog1.FileName, true);
                }
                debugLog("[Info] File Successfully Exported!\r\n", warningStyle);
            }
        }

        private void exportFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            exportFile();
        }

        private void importFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            importFile();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            about frmAbout = new about();
            frmAbout.Show();
        }

        private void tutorialToolStripMenuItem_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://youtu.be/N50Y6tSz4FM");
        }
       

        private void supportChatToolStripMenuItem_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://discord.gg/tAHVcNQ");
        }

        private void supportForumToolStripMenuItem_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("http://gamemodding.club/Thread-Tutorial-How-to-mod-Ghost-in-the-Shell-First-Assault");
        }

        private void modManagerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            mods frmMods = new mods();
            frmMods.Show();
        }

        private void exportFileToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            exportFile();
        }

        private void importFileToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            importFile();
        }


        private void previewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            dataGridView1.Visible = false;
            fileTableToolStripMenuItem.Enabled = true;
            previewToolStripMenuItem.Enabled = false;
        }

        private void fileTableToolStripMenuItem_Click(object sender, EventArgs e)
        {
            dataGridView1.Visible = true;
            fileTableToolStripMenuItem.Enabled = false;
            previewToolStripMenuItem.Enabled = true;
        }

        private void supportForumToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("http://gamemodding.club/Thread-Tutorial-How-to-mod-Ghost-in-the-Shell-First-Assault");
        }
    }
}
