namespace GraphComponent
{
    public partial class LineGraph
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

        #region 组件设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // LineGraph
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 14F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.DoubleBuffered = true;
            this.Font = new System.Drawing.Font("宋体", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.Name = "LineGraph";
            this.Size = new System.Drawing.Size(288, 259);
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.LineGraph_Paint);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.LineGraph_KeyDown);
            this.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.LineGraph_KeyPress);
            this.KeyUp += new System.Windows.Forms.KeyEventHandler(this.LineGraph_KeyUp);
            this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.LineGraph_MouseDown);
            this.MouseMove += new System.Windows.Forms.MouseEventHandler(this.LineGraph_MouseMove);
            this.MouseUp += new System.Windows.Forms.MouseEventHandler(this.LineGraph_MouseUp);
            this.ResumeLayout(false);

        }

        #endregion
    }
}
