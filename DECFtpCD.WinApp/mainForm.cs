using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.FtpClient;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

namespace DECFtpCD.WinApp
{
    public partial class mainForm : Form
    {
        private int intOriginalExStyle = -1;
        private bool bEnableAntiFlicker = true;
        private readonly string _sqlConnection;
        private DataSet dsResult;

        public mainForm()
        {
            ToggleAntiFlicker(false);
            InitializeComponent();
            _sqlConnection = ConfigurationManager.AppSettings["DbConnection"];

            // SetDoubleBuffered(dataGridView1, true);
            this.ResizeBegin += Form1_ResizeBegin;
            this.ResizeEnd += Form1_ResizeEnd;
            this.progressBar1.Style = ProgressBarStyle.Blocks;
            initDataGridViewColumns(dataGridView1);

            // txtPeriod.Text = "201701";
            // txtTaxNumber.Text = "0105541000784";
            txtTotalRecord.Text = "";
        }

        //public void SetDoubleBuffered(this Control control, bool setting)
        //{
        //    Type dgvType = control.GetType();
        //    PropertyInfo pi = dgvType.GetProperty("DoubleBuffered",
        //          BindingFlags.Instance | BindingFlags.NonPublic);
        //    pi.SetValue(control, setting, null);
        //}

        private void Form1_ResizeEnd(object sender, EventArgs e)
        {
            ToggleAntiFlicker(false);
        }

        private void Form1_ResizeBegin(object sender, EventArgs e)
        {
            ToggleAntiFlicker(true);
        }

        private void ToggleAntiFlicker(bool Enable)
        {
            bEnableAntiFlicker = Enable;
            this.MaximizeBox = true;
        }

        protected override CreateParams CreateParams
        {
            get
            {
                if (intOriginalExStyle == -1)
                {
                    intOriginalExStyle = base.CreateParams.ExStyle;
                }
                CreateParams cp = base.CreateParams;

                if (bEnableAntiFlicker)
                {
                    cp.ExStyle |= 0x02000000; //WS_EX_COMPOSITED
                }
                else
                {
                    cp.ExStyle = intOriginalExStyle;
                }

                return cp;
            }
        }

        private void mainForm_Load(object sender, EventArgs e)
        {
        }

        private DataGridViewTextBoxColumn createDataGridViewTextBoxColumn(
            string title, string propertyName, int width,
            DataGridViewContentAlignment alignment = DataGridViewContentAlignment.MiddleLeft)
        {
            var col = new DataGridViewTextBoxColumn
            {
                DataPropertyName = propertyName,
                HeaderText = title.ToUpper(),
                Name = "__col__" + propertyName,
                ReadOnly = true,
                Width = width
            };

            col.DefaultCellStyle.Alignment = alignment;
            return col;
        }

        private void initDataGridViewColumns(DataGridView dgv)
        {
            dgv.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
            dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dgv.EnableHeadersVisualStyles = false;
            dgv.ColumnHeadersHeight = 28;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Tahoma", 8.25f, FontStyle.Regular);
            dgv.AutoGenerateColumns = false;
            dgv.MultiSelect = false;
            dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgv.RowTemplate.Height = 24;
            dgv.RowTemplate.MinimumHeight = 22;
            dgv.RowHeadersVisible = false;
            dgv.AllowUserToResizeRows = false;

            //DataGridViewCheckBoxColumn chkCol = new DataGridViewCheckBoxColumn();
            //chkCol.Name = "__col__checkbox";
            //chkCol.HeaderText = "";
            //chkCol.DataPropertyName = "CheckState";
            //chkCol.Width = 50;
            //chkCol.ReadOnly = true;
            //chkCol.FillWeight = 10;
            //chkCol.Resizable = DataGridViewTriState.False;

            //CheckBox checkboxHeader = new CheckBox();
            //checkboxHeader.Name = "checkboxHeader";
            //checkboxHeader.Size = new Size(18, 18);
            //checkboxHeader.Location = new Point(20, 5);
            //checkboxHeader.BackColor = Color.Transparent;
            //checkboxHeader.CheckedChanged += CheckboxHeader_CheckedChanged;
            //dgv.Columns.Add(chkCol);
            //dgv.Controls.Add(checkboxHeader);

            dgv.Columns.Add(createDataGridViewTextBoxColumn("DecNO", "DecNO", 150, DataGridViewContentAlignment.MiddleCenter));
            dgv.Columns.Add(createDataGridViewTextBoxColumn("Invoices", "InvNOList", 600, DataGridViewContentAlignment.MiddleCenter));
            dgv.Columns.Add(createDataGridViewTextBoxColumn("Status", "Status", 100, DataGridViewContentAlignment.MiddleCenter));

            for (int i = 0; i < dgv.Columns.Count; i++)
            {
                dgv.Columns[i].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            if (txtPeriod.Text == "" || txtTaxNumber.Text == "" || cboShipmentType.Text == "")
            {
                MessageBox.Show("Please enter your period and tax number.!!!");
                return;
            }

            if (backgroundWorker1.IsBusy) return;
            backgroundWorker1.RunWorkerAsync(new object[] { txtPeriod.Text, txtTaxNumber.Text, cboShipmentType.Text });
            progressBar1.Style = ProgressBarStyle.Marquee;
            txtTotalRecord.Text = "";
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                var parms = e.Argument as object[];
                var period = (string)parms[0];
                var taxNumber = (string)parms[1];
                var shipmentType = (string)parms[2];
                var commandText = "";

                if (shipmentType == "EXPORT")
                {
                    commandText = @"select RefNO, DecNO, InvNOList, '' as Status from DecX_Declare where ExporterTaxNo = @taxno
                    and Convert(Varchar(6),UDateDeclare,112) = @period
                    and DocStatus <= 4
                    order by DecNO asc";
                }
                else
                {
                    commandText = @"select RefNO, DecNO, InvNOList, '' as Status from DecI_Declare where ExporterTaxNo = @taxno
                    and Convert(Varchar(6),UDateDeclare,112) = @period
                    and DocStatus <= 4
                    order by DecNO asc";
                }

                e.Result = getDeclarations(commandText, period, taxNumber);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Worker Error : " + ex.Message);
            }
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            progressBar1.Style = ProgressBarStyle.Blocks;
            dsResult = e.Result as DataSet;
            dataGridView1.DataSource = dsResult.Tables[0];
            txtTotalRecord.Text = string.Format("Total {0} rec.", dataGridView1.Rows.Count);
        }

        private DataSet getDeclarations(string commandText, string period, string taxNumber)
        {
            DataSet ds = new DataSet();

            using (SqlConnection conn = new SqlConnection(this._sqlConnection))
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = commandText;
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("@period", period);
                cmd.Parameters.AddWithValue("@taxno", taxNumber);

                var adapter = new SqlDataAdapter(cmd);
                adapter.Fill(ds);
            }

            return ds;
        }

        private void btnExportForCustomer_Click(object sender, EventArgs e)
        {
            if (dataGridView1.Rows.Count == 0)
            {
                MessageBox.Show("Find not found data to export.");
                return;
            }

            if (backgroundWorker2.IsBusy) return;

            var dlg = folderBrowserDialog1.ShowDialog();

            if (dlg == DialogResult.OK)
            {
                backgroundWorker2.RunWorkerAsync(new object[] {
                folderBrowserDialog1.SelectedPath, cboShipmentType.Text });
                progressBar1.Style = ProgressBarStyle.Marquee;
            }
        }

        private void backgroundWorker2_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                var parms = e.Argument as object[];
                var path = (string)parms[0];
                var shipment = (string)parms[1];
                var ftpLocation = "";

                if (shipment == "EXPORT")
                    ftpLocation = ConfigurationManager.AppSettings["ftpEXPGnlPath"];
                else
                    ftpLocation = ConfigurationManager.AppSettings["ftpIMPGnlPath"];

                for (int i = 0; i < dsResult.Tables[0].Rows.Count; i++)
                {
                    var dr = dsResult.Tables[0].Rows[i];
                    var decno = dr["DecNo"].ToString();

                    //download
                    var result = DownloadPdf(path, ftpLocation, decno);
                    dr["Status"] = result == true ? "OK" : "NOT FOUND";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Export Pdf Error : " + ex.Message);
            }
        }

        private bool DownloadPdf(string destFolder, string location, string decno)
        {
            var destinationFile = Path.Combine(destFolder, decno.TrimEnd() + ".pdf");

            using (var ftpClient = new FtpClient())
            {
                ftpClient.Host = ConfigurationManager.AppSettings["ftpHost"];
                ftpClient.Credentials = new NetworkCredential(ConfigurationManager.AppSettings["ftpUser"],
                    ConfigurationManager.AppSettings["ftpPass"]);

                ftpClient.Connect();

                var uri = string.Format("{0}/{1}.pdf", location, decno.TrimEnd());

                try
                {
                    using (var ftpStream = ftpClient.OpenRead(uri))
                    using (var fileStream = File.Create(destinationFile, (int)ftpStream.Length))
                    {
                        var buffer = new byte[8 * 1024];
                        int count;
                        while ((count = ftpStream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            fileStream.Write(buffer, 0, count);
                        }
                    }
                }
                catch
                {
                    MessageBox.Show("Find not found " + uri);
                }
            }

            if (File.Exists(destinationFile))
                return true;
            else
                return false;
        }

        private void backgroundWorker2_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            progressBar1.Style = ProgressBarStyle.Blocks;
            MessageBox.Show("Download completed.");

            dataGridView1.DataSource = dsResult.Tables[0];
            dataGridView1.Refresh();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            DownloadPdf(@"c:\temp", @"/dmc/exp/gnl", "A0241600102443");
        }
    }
}