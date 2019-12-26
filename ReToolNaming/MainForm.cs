﻿using System;
using System.ComponentModel;
using System.Windows.Forms;
using System.IO;
using System.Reflection;
using Classes;

namespace MatchSubs4Vids
{
    public partial class MainForm : Form
    {
        private readonly RenameUtils renameUtils;
        private readonly ConsoleLogForm consoleLogForm;
        
        private bool pathViaAppParams = false;
                
        public MainForm(string path)
        {
            InitializeComponent();

            consoleLogForm = new ConsoleLogForm
            {
                StartPosition = FormStartPosition.CenterParent
            };
            consoleLogForm.Hide();

            //Handle version title.
            string VersionNumber = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            Text = "MatchSubs4Vids (v" + VersionNumber + ")";

            renameUtils = new RenameUtils();
            renameUtils.InitializeUtils() ;
            tbSearchPatternLeft.Text = "*.avi|*.mkv|*.mp4|*.m2ts";
            tbSearchPatternRight.Text = "*.srt|*.sub|*.str";

            renameUtils.LoadRegexesIntoComboBox(cbRegexes);

            lvFilesLeft.AllowDrop = true;
            lvFilesRight.AllowDrop = true;
            if (path.Length > 0 && Directory.Exists(path))
            {
                DisplayStatus("Directory from startup parameter /path exists - " + path + " - preloading data", false);
                this.pathViaAppParams = true;
                folderBrowserDialog1.SelectedPath = path;
                LoadTargetDirectory();
            }
        }

        private void StartAutomaticDownloadSubtitles()
        {
            pbProgress.Value = 0;
            consoleLogForm.lConsoleLog.Text = "Wait. Procesing...";
            consoleLogForm.Show();

            bgWorker.RunWorkerAsync();
        }

        private void bgWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            string[] args = new string[3];

            args[0] = folderBrowserDialog1.SelectedPath.ToString();
            args[1] = Properties.Settings.Default.OPEN_SUBTITLES_DOWNLOAD_LANGUAGES;
            args[2] = Properties.Settings.Default.OPEN_SUBTITLES_USERNAME;
            args[3] = Properties.Settings.Default.OPEN_SUBTITLES_PASSWORD;

            e.Result = AutoSubtitleDownloader.ASD.Start(args) + "\r\nPres SPACE / ESC / ENTER to close this message";
        }

        private void bgWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            consoleLogForm.lConsoleLog.Text = e.Result.ToString();
            
            consoleLogForm.lConsoleLog.SelectionStart = consoleLogForm.lConsoleLog.TextLength;
            consoleLogForm.lConsoleLog.ScrollToCaret();

            pbProgress.Value = 100;
            bFilterRight.PerformClick();
        }

        private void bgWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            pbProgress.Value++;
        }

        private void LoadTargetDirectory()
        {
            RenameUtils.PopulateFiles(lvFilesLeft, tbSearchPatternLeft.Text, folderBrowserDialog1);
            RenameUtils.PopulateFiles(lvFilesRight, tbSearchPatternRight.Text, folderBrowserDialog1);
            LogUtils.AddLogTextLine("Populating form with content from path: " + RenameUtils.GetFullPath(folderBrowserDialog1).ToString());
            AutoDetectAutoMatchListViews();
        }

        private void DisplayStatus(string message, bool append)
        {
            lStatus.Text = append ? (!lStatus.Text.Contains(message) ? lStatus.Text + message : lStatus.Text) : message;
        }
      
        private void MainForm_Load(object sender, EventArgs e)
        {
            LogUtils.AddLogTextLine("Application opened.");
            LoadTargetDirectory();
        }

        private void bFilterLeft_Click(object sender, EventArgs e)
        {
            RenameUtils.PopulateFiles(lvFilesLeft, tbSearchPatternLeft.Text, folderBrowserDialog1);
        }

        private void bFilterRight_Click(object sender, EventArgs e)
        {
            RenameUtils.PopulateFiles(lvFilesRight, tbSearchPatternRight.Text, folderBrowserDialog1);
        }

        private void bMatch_Click(object sender, EventArgs e)
        {
            Match();
        }

        private void Match()
        {
            renameUtils.MatchLeftRight(lvFilesLeft, lvFilesRight);
            LogUtils.AddLogTextLine("Manually matched files");
        }

        private void bRenameFromLeft_Click(object sender, EventArgs e)
        {
            renameUtils.Rename(RenameUtils.DIRECTION_LEFT, lvFilesLeft, lvFilesRight, folderBrowserDialog1, tbSearchPatternLeft.Text, tbSearchPatternRight.Text);
        }

        private void bRenameFromRight_Click(object sender, EventArgs e)
        {
            renameUtils.Rename(RenameUtils.DIRECTION_RIGHT, lvFilesLeft, lvFilesRight, folderBrowserDialog1, tbSearchPatternLeft.Text, tbSearchPatternRight.Text);
        }

        private void bAutoDetect_Click(object sender, EventArgs e)
        {
            AutoDetectAutoMatchListViews();
        }

        private void AutoDetectAutoMatchListViews()
        {
            string regex = cbRegexes.SelectedValue == null ? cbRegexes.Text : cbRegexes.SelectedValue.ToString();
            renameUtils.AutoMatchListViews(lvFilesLeft, lvFilesRight, pbProgress, cbUseComplexRegex.Checked, regex, cbSpeed.Checked);
            lHelp.Hide();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            folderBrowserDialog1.ShowDialog();
            LoadTargetDirectory();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            About aboutBox = new About();
            aboutBox.ShowDialog();
            aboutBox.Dispose();
        }

        private void cbUseComplexRegex_CheckedChanged(object sender, EventArgs e)
        {
            cbRegexes.Enabled = cbUseComplexRegex.Checked;
        }

        private void button1_Click(object sender, EventArgs e)//Clear matches.
        {
            RenameUtils.ClearMathingLeftRight(lvFilesLeft, lvFilesRight);
        }

        private void lvFilesLeft_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            RenameUtils.BehaveLikeRadioCheckBox(lvFilesLeft, e.Item);
        }

        private void lvFilesRight_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            RenameUtils.BehaveLikeRadioCheckBox(lvFilesRight, e.Item);
        }

        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

            foreach (string file in files)
            {
                bool flagPass = false;
                if (Directory.Exists(file.ToString()))
                {
                    folderBrowserDialog1.SelectedPath = file.ToString();
                    flagPass = true;
                }
                if (File.Exists(file.ToString()))
                {
                    FileInfo ddFile = new FileInfo(file.ToString());
                    folderBrowserDialog1.SelectedPath = ddFile.Directory.FullName.ToString();
                    flagPass = true;
                }
                if (flagPass)
                {
                    RenameUtils.PopulateFiles(lvFilesLeft, tbSearchPatternLeft.Text, folderBrowserDialog1);
                    RenameUtils.PopulateFiles(lvFilesRight, tbSearchPatternRight.Text, folderBrowserDialog1);
                    LogUtils.AddLogTextLine("Populating form with content from path: " + RenameUtils.GetFullPath(folderBrowserDialog1).ToString());
                    break;
                }
            }
            
        }

        private void Form1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop, false) == true)
            {
                e.Effect = DragDropEffects.All;
            }  
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            LogUtils.AddLogTextLine("Application closed.");
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F4)
            {
                folderBrowserDialog1.ShowDialog();
                LoadTargetDirectory();
            }

            if (e.KeyCode == Keys.F1)
            {
                AutoDetectAutoMatchListViews();
            }

            if (e.KeyCode == Keys.F2)
            {
                AutoDetectAutoMatchListViews();
            }

            if (e.KeyCode == Keys.F8)
            {
                RenameUtils.ClearMathingLeftRight(lvFilesLeft, lvFilesRight);
            }

            if (e.KeyCode == Keys.F12)
            {
                StartAutomaticDownloadSubtitles();
            }

            if (e.KeyCode == Keys.Enter)
            {
                    Match();
            }
        }

        private void Form1_Activated(object sender, EventArgs e)
        {
            if (this.pathViaAppParams)
            {
                this.pathViaAppParams = false;
                return;
            }

            string clipboardText = Clipboard.GetText();

            if (Directory.Exists(clipboardText))
            {
                if (folderBrowserDialog1.SelectedPath != clipboardText)
                {
                    DisplayStatus("Directory from clipboard exists - " + clipboardText + " - preloading data", false);
                    folderBrowserDialog1.SelectedPath = clipboardText;
                    LoadTargetDirectory();
                }
            }
            else
            {
                DisplayStatus("  Clipboard text is not a path.", true);
            }
        }

        private void lHelp_Click(object sender, EventArgs e)
        {
            lHelp.Hide();
        }

        private void tbSearchPatternLeft_KeyUp(object sender, KeyEventArgs e)
        {
            bFilterLeft.PerformClick();
        }

        private void tbSearchPatternRight_KeyUp(object sender, KeyEventArgs e)
        {
            bFilterRight.PerformClick();
        }

        private void helpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            lHelp.Show();
        }

        private void bASD_Click(object sender, EventArgs e)
        {
            StartAutomaticDownloadSubtitles();
        }

        private void MainForm_Click(object sender, EventArgs e)
        {
            if (consoleLogForm.Visible) consoleLogForm.Hide();
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            splitContainer1.SplitterDistance = Size.Height - 140;
        }
    }
}
