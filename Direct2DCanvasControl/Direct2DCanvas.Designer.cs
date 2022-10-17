// <copyright file="DirectXCanvas.Designer.cs" company="Shkyrockett" >
//     Copyright © 2017 - 2022 Shkyrockett. All rights reserved.
// </copyright>
// <author id="shkyrockett">Shkyrockett</author>
// <license>
//     Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </license>
// <summary></summary>
// <remarks>
// </remarks>

namespace Direct2DCanvasControl
{
    /// <summary>
    /// The DirectX canvas control.
    /// </summary>
    partial class Direct2DCanvas
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Using a <see cref="System.Timers.Timer"/> to keep from pausing when the form is resized.
        /// </summary>
        private System.Timers.Timer timer;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            timer.Stop();
            timer.Dispose();

            if (disposing && (components is not null))
            {
                components.Dispose();
            }

            base.Dispose(disposing);
        }

        #region Component Designer generated code
        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.timer = new System.Timers.Timer();
            ((System.ComponentModel.ISupportInitialize)(this.timer)).BeginInit();
            this.SuspendLayout();
            // 
            // timer
            // 
            this.timer.Enabled = true;
            this.timer.SynchronizingObject = this;
            this.timer.Elapsed += new System.Timers.ElapsedEventHandler(this.Timer_Tick);
            // 
            // DirectXCanvas
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.DoubleBuffered = true;
            this.Name = "DirectXCanvas";
            this.Resize += new System.EventHandler(this.DirectXCanvas_Resize);
            ((System.ComponentModel.ISupportInitialize)(this.timer)).EndInit();
            this.ResumeLayout(false);
        }
        #endregion
    }
}
