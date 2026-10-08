using LinePutScript.Localization.WPF;
using Panuon.WPF.UI;
using System;
using System.IO;
using System.Text;
using System.Windows;

namespace VPet_Simulator.Windows
{
    /// <summary>
    /// winReport.xaml 的交互逻辑
    /// </summary>
    public partial class winReport : WindowX
    {
        MainWindow mw;
        public bool IsUniformSizeChanged => false;
        public bool StoreSize => false;
        public winReport(MainWindow mainw, string? errmsg = null)
        {
            mainw.Windows.Add(this);
            InitializeComponent();
            mw = mainw;
            Title = "反馈中心".Translate() + ' ' + mw.PrefixSave;
            if (errmsg != null)
            {
                tType.SelectedIndex = 0;
                tContent.Text = errmsg;
                tContent.IsReadOnly = true;
            }

        }

        private void SaveReport_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Text report (*.txt)|*.txt",
                FileName = $"VPet-report-{DateTime.Now:yyyyMMdd-HHmmss}.txt",
            };
            if (dialog.ShowDialog(this) != true)
                return;
            try
            {
                var content = new StringBuilder();
                content.AppendLine($"VPet Standalone {mw.Version}");
                content.AppendLine(tType.Text);
                content.AppendLine(tDescription.Text);
                content.AppendLine(tContent.Text);
                content.AppendLine(tContact.Text);
                if (IncludeSave.IsChecked == true)
                    content.AppendLine(mw.GameSavesData.ToLPS().ToString() + mw.Set.ToString());
                File.WriteAllText(dialog.FileName, content.ToString(), Encoding.UTF8);
                MessageBoxX.Show("报告已保存到本地".Translate());
            }
            catch (Exception ex)
            {
                MessageBoxX.Show(ex.Message, "保存失败".Translate());
            }
        }

        private void MainGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Height = MainGrid.ActualHeight + 50;
        }

        private void tType_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (tType.SelectedIndex == 5)
            {
                try
                {
                    StringBuilder sb = new StringBuilder();
                    foreach (var v in LocalizeCore.StoreTranslationList)
                    {
                        sb.AppendLine(v.Replace("\n", @"\n").Replace("\r", @"\r"));
                    }
                    tContent.Text = sb.ToString();
                    if (string.IsNullOrEmpty(tContent.Text))
                    {
                        tContent.Text = "没有需要提交的翻译的内容".Translate();
                    }
                    IncludeSave.IsChecked = false;
                }
                catch
                {

                }
            }
        }

        private void WindowX_Closed(object sender, EventArgs e)
        {
            mw.Windows.Remove(this);
        }
    }
}
