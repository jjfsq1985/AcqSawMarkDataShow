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
            this.resultGrid = new System.Windows.Forms.PropertyGrid();
            this.DataGraph = new GraphComponent.LineGraph();
            ((System.ComponentModel.ISupportInitialize)(this.GraphContainer)).BeginInit();
            this.GraphContainer.Panel1.SuspendLayout();
            this.GraphContainer.Panel2.SuspendLayout();
            this.GraphContainer.SuspendLayout();
            this.LineGraphContextMenu.SuspendLayout();
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
            this.GraphContainer.Panel2.Controls.Add(this.resultGrid);
            this.GraphContainer.Size = new System.Drawing.Size(786, 457);
            this.GraphContainer.SplitterDistance = 583;
            this.GraphContainer.TabIndex = 0;
            // 
            // LineGraphContextMenu
            // 
            this.LineGraphContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.OpenFileMenuItem});
            this.LineGraphContextMenu.Name = "LineGraphContextMenu";
            this.LineGraphContextMenu.Size = new System.Drawing.Size(149, 26);
            // 
            // OpenFileMenuItem
            // 
            this.OpenFileMenuItem.Name = "OpenFileMenuItem";
            this.OpenFileMenuItem.Size = new System.Drawing.Size(148, 22);
            this.OpenFileMenuItem.Text = "打开数据文件";
            this.OpenFileMenuItem.Click += new System.EventHandler(this.OpenFileMenuItem_Click);
            // 
            // resultGrid
            // 
            this.resultGrid.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.resultGrid.Location = new System.Drawing.Point(3, 3);
            this.resultGrid.Name = "resultGrid";
            this.resultGrid.Size = new System.Drawing.Size(193, 451);
            this.resultGrid.TabIndex = 0;
            // 
            // DataGraph
            // 
            this.DataGraph.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.DataGraph.ContextMenuStrip = this.LineGraphContextMenu;
            this.DataGraph.Location = new System.Drawing.Point(3, 3);
            this.DataGraph.Name = "DataGraph";
            this.DataGraph.Size = new System.Drawing.Size(577, 451);
            this.DataGraph.TabIndex = 0;
            // 
            // AcqDataForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(786, 457);
            this.Controls.Add(this.GraphContainer);
            this.Name = "AcqDataForm";
            this.Text = "数据显示";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.GraphContainer.Panel1.ResumeLayout(false);
            this.GraphContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.GraphContainer)).EndInit();
            this.GraphContainer.ResumeLayout(false);
            this.LineGraphContextMenu.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer GraphContainer;
        private System.Windows.Forms.PropertyGrid resultGrid;
        private System.Windows.Forms.ContextMenuStrip LineGraphContextMenu;
        private System.Windows.Forms.ToolStripMenuItem OpenFileMenuItem;
        private GraphComponent.LineGraph DataGraph;
    }
}

