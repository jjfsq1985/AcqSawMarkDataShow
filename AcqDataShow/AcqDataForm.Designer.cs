namespace AcqDataShow
{
    partial class AcqDataForm
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.GraphContainer = new System.Windows.Forms.SplitContainer();
            this.LineGraphContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.OpenFileMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.WaveLineData = new System.Windows.Forms.ToolStripMenuItem();
            this.SpectrumData = new System.Windows.Forms.ToolStripMenuItem();
            this.CalcPeakValleyMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.InfoContainer = new System.Windows.Forms.SplitContainer();
            this.resultGrid = new System.Windows.Forms.PropertyGrid();
            this.labelProbe = new System.Windows.Forms.Label();
            this.textProbe = new System.Windows.Forms.TextBox();
            this.btnMonitor = new System.Windows.Forms.Button();
            this.BatchProgressBar = new System.Windows.Forms.ProgressBar();
            this.btnBatchTest = new System.Windows.Forms.Button();
            this.backgroundBatchCalc = new System.ComponentModel.BackgroundWorker();
            this.ProbeMonitorWorker = new System.ComponentModel.BackgroundWorker();
            this.LineType = new System.Windows.Forms.CheckBox();
            this.DataGraph = new GraphComponent.LineGraph();
            ((System.ComponentModel.ISupportInitialize)(this.GraphContainer)).BeginInit();
            this.GraphContainer.Panel1.SuspendLayout();
            this.GraphContainer.Panel2.SuspendLayout();
            this.GraphContainer.SuspendLayout();
            this.LineGraphContextMenu.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.InfoContainer)).BeginInit();
            this.InfoContainer.Panel1.SuspendLayout();
            this.InfoContainer.Panel2.SuspendLayout();
            this.InfoContainer.SuspendLayout();
            this.SuspendLayout();
            // 
            // GraphContainer
            // 
            this.GraphContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.GraphContainer.Location = new System.Drawing.Point(0, 0);
            this.GraphContainer.Name = "GraphContainer";
            // 
            // GraphContainer.Panel1
            // 
            this.GraphContainer.Panel1.Controls.Add(this.DataGraph);
            // 
            // GraphContainer.Panel2
            // 
            this.GraphContainer.Panel2.Controls.Add(this.InfoContainer);
            this.GraphContainer.Size = new System.Drawing.Size(964, 714);
            this.GraphContainer.SplitterDistance = 768;
            this.GraphContainer.TabIndex = 0;
            // 
            // LineGraphContextMenu
            // 
            this.LineGraphContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.OpenFileMenuItem,
            this.WaveLineData,
            this.SpectrumData,
            this.CalcPeakValleyMenuItem});
            this.LineGraphContextMenu.Name = "LineGraphContextMenu";
            this.LineGraphContextMenu.Size = new System.Drawing.Size(149, 92);
            // 
            // OpenFileMenuItem
            // 
            this.OpenFileMenuItem.Name = "OpenFileMenuItem";
            this.OpenFileMenuItem.Size = new System.Drawing.Size(148, 22);
            this.OpenFileMenuItem.Text = "打开数据文件";
            this.OpenFileMenuItem.Click += new System.EventHandler(this.OpenFileMenuItem_Click);
            // 
            // WaveLineData
            // 
            this.WaveLineData.Name = "WaveLineData";
            this.WaveLineData.Size = new System.Drawing.Size(148, 22);
            this.WaveLineData.Text = "波形分析";
            this.WaveLineData.Click += new System.EventHandler(this.WaveLineData_Click);
            // 
            // SpectrumData
            // 
            this.SpectrumData.Name = "SpectrumData";
            this.SpectrumData.Size = new System.Drawing.Size(148, 22);
            this.SpectrumData.Text = "频谱分析";
            this.SpectrumData.Click += new System.EventHandler(this.SpectrumData_Click);
            // 
            // CalcPeakValleyMenuItem
            // 
            this.CalcPeakValleyMenuItem.Name = "CalcPeakValleyMenuItem";
            this.CalcPeakValleyMenuItem.Size = new System.Drawing.Size(148, 22);
            this.CalcPeakValleyMenuItem.Text = "计算峰谷值";
            this.CalcPeakValleyMenuItem.Click += new System.EventHandler(this.CalcPeakValleyMenuItem_Click);
            // 
            // InfoContainer
            // 
            this.InfoContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.InfoContainer.Location = new System.Drawing.Point(0, 0);
            this.InfoContainer.Name = "InfoContainer";
            this.InfoContainer.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // InfoContainer.Panel1
            // 
            this.InfoContainer.Panel1.Controls.Add(this.resultGrid);
            // 
            // InfoContainer.Panel2
            // 
            this.InfoContainer.Panel2.Controls.Add(this.LineType);
            this.InfoContainer.Panel2.Controls.Add(this.labelProbe);
            this.InfoContainer.Panel2.Controls.Add(this.textProbe);
            this.InfoContainer.Panel2.Controls.Add(this.btnMonitor);
            this.InfoContainer.Panel2.Controls.Add(this.BatchProgressBar);
            this.InfoContainer.Panel2.Controls.Add(this.btnBatchTest);
            this.InfoContainer.Size = new System.Drawing.Size(192, 714);
            this.InfoContainer.SplitterDistance = 631;
            this.InfoContainer.TabIndex = 1;
            // 
            // resultGrid
            // 
            this.resultGrid.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.resultGrid.Location = new System.Drawing.Point(3, 3);
            this.resultGrid.Name = "resultGrid";
            this.resultGrid.Size = new System.Drawing.Size(186, 625);
            this.resultGrid.TabIndex = 0;
            // 
            // labelProbe
            // 
            this.labelProbe.AutoSize = true;
            this.labelProbe.Location = new System.Drawing.Point(5, 42);
            this.labelProbe.Name = "labelProbe";
            this.labelProbe.Size = new System.Drawing.Size(65, 12);
            this.labelProbe.TabIndex = 4;
            this.labelProbe.Text = "监视探头号";
            // 
            // textProbe
            // 
            this.textProbe.Location = new System.Drawing.Point(72, 37);
            this.textProbe.Name = "textProbe";
            this.textProbe.Size = new System.Drawing.Size(36, 21);
            this.textProbe.TabIndex = 3;
            // 
            // btnMonitor
            // 
            this.btnMonitor.Location = new System.Drawing.Point(112, 35);
            this.btnMonitor.Name = "btnMonitor";
            this.btnMonitor.Size = new System.Drawing.Size(75, 23);
            this.btnMonitor.TabIndex = 2;
            this.btnMonitor.Text = "探头监视";
            this.btnMonitor.UseVisualStyleBackColor = true;
            this.btnMonitor.Click += new System.EventHandler(this.btnMonitor_Click);
            // 
            // BatchProgressBar
            // 
            this.BatchProgressBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.BatchProgressBar.Location = new System.Drawing.Point(3, 62);
            this.BatchProgressBar.Name = "BatchProgressBar";
            this.BatchProgressBar.Size = new System.Drawing.Size(185, 10);
            this.BatchProgressBar.TabIndex = 1;
            // 
            // btnBatchTest
            // 
            this.btnBatchTest.Location = new System.Drawing.Point(112, 4);
            this.btnBatchTest.Name = "btnBatchTest";
            this.btnBatchTest.Size = new System.Drawing.Size(75, 23);
            this.btnBatchTest.TabIndex = 0;
            this.btnBatchTest.Text = "批量处理";
            this.btnBatchTest.UseVisualStyleBackColor = true;
            this.btnBatchTest.Click += new System.EventHandler(this.btnBatchTest_Click);
            // 
            // backgroundBatchCalc
            // 
            this.backgroundBatchCalc.WorkerReportsProgress = true;
            this.backgroundBatchCalc.WorkerSupportsCancellation = true;
            this.backgroundBatchCalc.DoWork += new System.ComponentModel.DoWorkEventHandler(this.backgroundBatchCalc_DoWork);
            this.backgroundBatchCalc.ProgressChanged += new System.ComponentModel.ProgressChangedEventHandler(this.backgroundBatchCalc_ProgressChanged);
            this.backgroundBatchCalc.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.backgroundBatchCalc_RunWorkerCompleted);
            // 
            // ProbeMonitorWorker
            // 
            this.ProbeMonitorWorker.WorkerReportsProgress = true;
            this.ProbeMonitorWorker.WorkerSupportsCancellation = true;
            this.ProbeMonitorWorker.DoWork += new System.ComponentModel.DoWorkEventHandler(this.ProbeMonitorWorker_DoWork);
            this.ProbeMonitorWorker.ProgressChanged += new System.ComponentModel.ProgressChangedEventHandler(this.ProbeMonitorWorker_ProgressChanged);
            this.ProbeMonitorWorker.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.ProbeMonitorWorker_RunWorkerCompleted);
            // 
            // LineType
            // 
            this.LineType.AutoSize = true;
            this.LineType.Location = new System.Drawing.Point(5, 8);
            this.LineType.Name = "LineType";
            this.LineType.Size = new System.Drawing.Size(48, 16);
            this.LineType.TabIndex = 5;
            this.LineType.Text = "滤波";
            this.LineType.UseVisualStyleBackColor = true;
            // 
            // DataGraph
            // 
            this.DataGraph.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.DataGraph.ContextMenuStrip = this.LineGraphContextMenu;
            this.DataGraph.Font = new System.Drawing.Font("宋体", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.DataGraph.Location = new System.Drawing.Point(3, 3);
            this.DataGraph.Name = "DataGraph";
            this.DataGraph.Size = new System.Drawing.Size(762, 708);
            this.DataGraph.TabIndex = 0;
            // 
            // AcqDataForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(964, 714);
            this.Controls.Add(this.GraphContainer);
            this.Name = "AcqDataForm";
            this.Text = "数据显示";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.GraphContainer.Panel1.ResumeLayout(false);
            this.GraphContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.GraphContainer)).EndInit();
            this.GraphContainer.ResumeLayout(false);
            this.LineGraphContextMenu.ResumeLayout(false);
            this.InfoContainer.Panel1.ResumeLayout(false);
            this.InfoContainer.Panel2.ResumeLayout(false);
            this.InfoContainer.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.InfoContainer)).EndInit();
            this.InfoContainer.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer GraphContainer;
        private System.Windows.Forms.PropertyGrid resultGrid;
        private System.Windows.Forms.ContextMenuStrip LineGraphContextMenu;
        private System.Windows.Forms.ToolStripMenuItem OpenFileMenuItem;
        private GraphComponent.LineGraph DataGraph;
        private System.Windows.Forms.ToolStripMenuItem CalcPeakValleyMenuItem;
        private System.Windows.Forms.SplitContainer InfoContainer;
        private System.Windows.Forms.Button btnBatchTest;
        private System.Windows.Forms.ProgressBar BatchProgressBar;
        private System.ComponentModel.BackgroundWorker backgroundBatchCalc;
        private System.Windows.Forms.Button btnMonitor;
        private System.ComponentModel.BackgroundWorker ProbeMonitorWorker;
        private System.Windows.Forms.TextBox textProbe;
        private System.Windows.Forms.Label labelProbe;
        private System.Windows.Forms.ToolStripMenuItem SpectrumData;
        private System.Windows.Forms.ToolStripMenuItem WaveLineData;
        private System.Windows.Forms.CheckBox LineType;
    }
}

