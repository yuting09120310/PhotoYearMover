using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace PhotoYearMover
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            init();
        }

        
        public void init()
        {
            int nowYear = DateTime.Now.Year;

            for (int i = nowYear - 10; i <= nowYear; i++)
            {
                this.comboBox1.Items.Add(i.ToString());
            }

            this.comboBox1.SelectedItem = nowYear.ToString();
        }

        private void guna2TextBox1_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                dialog.Description = "請選擇存放資料夾";
                dialog.ShowNewFolderButton = false;

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    guna2TextBox1.Text = dialog.SelectedPath;
                }
            }
        }

        private void guna2Button1_Click(object sender, EventArgs e)
        {
            // 取得選擇的年份
            var selectedYear = comboBox1.SelectedItem;

            // 檢查年份是否有選
            if (selectedYear == null)
            {
                MessageBox.Show("請選擇年份", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            MessageBox.Show("準備開始處理照片！", "OK", MessageBoxButtons.OK, MessageBoxIcon.Information);

            ScanPhotosFromCDrive();

            MessageBox.Show("掃描完成！確認後請按下複製作業", "OK", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ScanPhotosFromCDrive()
        {
            listLogs.Items.Clear();

            //找出電腦上所有邏輯磁碟
            DriveInfo[] drives = DriveInfo.GetDrives();

            foreach (DriveInfo drive in drives)
            {
                if (drive.IsReady && drive.DriveType == DriveType.Fixed)
                {
                    ScanDirectory(drive.RootDirectory.FullName);
                }
            }
        }

        private void ScanDirectory(string path)
        {
            // 要排除的系統資料夾（可自行擴充）
            string[] ignoreFolders = { "Windows", "Program Files", "Program Files (x86)", "ProgramData" };

            try
            {
                foreach (string dir in Directory.GetDirectories(path))
                {
                    string folderName = new DirectoryInfo(dir).Name;

                    // 忽略系統資料夾
                    if (ignoreFolders.Any(x => string.Equals(x, folderName, StringComparison.OrdinalIgnoreCase)))
                        continue;

                    // 遞迴繼續掃子資料夾
                    ScanDirectory(dir);
                }

                foreach (string file in Directory.GetFiles(path))
                {
                    string ext = Path.GetExtension(file).ToLower();
                    if (ext == ".jpg" || ext == ".jpeg")
                    {
                        AddToListView(file);
                    }
                }
            }
            catch
            {
            }
        }

        private void AddToListView(string filePath)
        {
            try
            {
                FileInfo fileInfo = new FileInfo(filePath);
                string createdDate = fileInfo.CreationTime.ToString("yyyy/MM/dd");

                string takenDate = GetPhotoTakenDate(filePath);

                string selectedYear = comboBox1.SelectedItem?.ToString();
                if (!string.IsNullOrEmpty(selectedYear) && takenDate.StartsWith(selectedYear))
                {
                    ListViewItem item = new ListViewItem(filePath);
                    item.SubItems.Add(takenDate.Substring(0,10));
                    listLogs.Items.Add(item);
                    listLogs.Refresh();
                }
            }
            catch { }
        }

        private string GetPhotoTakenDate(string filePath)
        {
            try
            {
                using (var img = System.Drawing.Image.FromFile(filePath))
                {
                    const int PropertyTagExifDTOrig = 0x9003;
                    if (img.PropertyIdList.Contains(PropertyTagExifDTOrig))
                    {
                        var prop = img.GetPropertyItem(PropertyTagExifDTOrig);
                        string dateTakenStr = Encoding.ASCII.GetString(prop.Value).Trim();

                        return Regex.Replace(dateTakenStr, ":", "/");
                    }
                }
            }
            catch { }

            return "無拍攝日期";
        }

        private void btn_Copy_Click(object sender, EventArgs e)
        {
            string targetRoot = guna2TextBox1.Text.Trim();

            if (string.IsNullOrEmpty(targetRoot))
            {
                MessageBox.Show("請選擇存放資料夾", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            foreach (ListViewItem item in listLogs.Items)
            {
                try
                {
                    string sourceFilePath = item.Text; 
                    string takenDate = item.SubItems[1].Text; 

                    string subFolderName = DateTime.Parse(takenDate).ToString("yyyy-MM");
                    string targetFolder = Path.Combine(targetRoot, subFolderName);

                    if (!Directory.Exists(targetFolder))
                    {
                        Directory.CreateDirectory(targetFolder);
                    }

                    string fileName = Path.GetFileName(sourceFilePath);
                    string destPath = Path.Combine(targetFolder, fileName);

                    File.Copy(sourceFilePath, destPath, true);

                }
                catch (Exception ex)
                {
                    MessageBox.Show($"複製失敗：{ex.Message}", "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            MessageBox.Show("複製完成！", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

    }
}
