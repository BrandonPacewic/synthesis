﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;

partial class ControlGroups : System.Windows.Forms.Form
{

    //Form overrides dispose to clean up the component list.
    [System.Diagnostics.DebuggerNonUserCode()]
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
    }

    //NOTE: The following procedure is required by the Windows Form Designer
    //It can be modified using the Windows Form Designer.  
    //Do not modify it using the code editor.
    [System.Diagnostics.DebuggerStepThrough()]
    private void InitializeComponent()
    {
            this.btnExport = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.lstJoints = new System.Windows.Forms.ListView();
            this.item_chType = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.item_chParent = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.item_chChild = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.item_chDrive = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.tabsMain = new System.Windows.Forms.TabControl();
            this.tabGroups = new System.Windows.Forms.TabPage();
            this.lstGroups = new System.Windows.Forms.ListView();
            this.groups_chName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.groups_chFaceColor = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.groups_chHighRes = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.tabJoints = new System.Windows.Forms.TabPage();
            this.groups_chGrounded = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.tabsMain.SuspendLayout();
            this.tabGroups.SuspendLayout();
            this.tabJoints.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnExport
            // 
            this.btnExport.Location = new System.Drawing.Point(818, 439);
            this.btnExport.Name = "btnExport";
            this.btnExport.Size = new System.Drawing.Size(130, 42);
            this.btnExport.TabIndex = 1;
            this.btnExport.Text = "Export";
            this.btnExport.UseVisualStyleBackColor = true;
            this.btnExport.Click += new System.EventHandler(this.btnExport_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(12, 439);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(130, 42);
            this.btnCancel.TabIndex = 2;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // lstJoints
            // 
            this.lstJoints.AutoArrange = false;
            this.lstJoints.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.item_chType,
            this.item_chParent,
            this.item_chChild,
            this.item_chDrive});
            this.lstJoints.FullRowSelect = true;
            this.lstJoints.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this.lstJoints.Location = new System.Drawing.Point(6, 6);
            this.lstJoints.MultiSelect = false;
            this.lstJoints.Name = "lstJoints";
            this.lstJoints.ShowGroups = false;
            this.lstJoints.Size = new System.Drawing.Size(915, 378);
            this.lstJoints.TabIndex = 3;
            this.lstJoints.UseCompatibleStateImageBehavior = false;
            this.lstJoints.View = System.Windows.Forms.View.Details;
            this.lstJoints.SelectedIndexChanged += new System.EventHandler(this.lstJoints_SelectedIndexChanged);
            this.lstJoints.DoubleClick += new System.EventHandler(this.lstJoints_DoubleClick);
            // 
            // item_chType
            // 
            this.item_chType.Text = "Joint Type";
            this.item_chType.Width = 138;
            // 
            // item_chParent
            // 
            this.item_chParent.Text = "Fixed";
            this.item_chParent.Width = 127;
            // 
            // item_chChild
            // 
            this.item_chChild.Text = "Child";
            this.item_chChild.Width = 135;
            // 
            // item_chDrive
            // 
            this.item_chDrive.Text = "Driver";
            this.item_chDrive.Width = 422;
            // 
            // tabsMain
            // 
            this.tabsMain.Controls.Add(this.tabGroups);
            this.tabsMain.Controls.Add(this.tabJoints);
            this.tabsMain.Location = new System.Drawing.Point(13, 13);
            this.tabsMain.Name = "tabsMain";
            this.tabsMain.SelectedIndex = 0;
            this.tabsMain.Size = new System.Drawing.Size(935, 419);
            this.tabsMain.TabIndex = 4;
            // 
            // tabGroups
            // 
            this.tabGroups.Controls.Add(this.lstGroups);
            this.tabGroups.Location = new System.Drawing.Point(4, 25);
            this.tabGroups.Name = "tabGroups";
            this.tabGroups.Padding = new System.Windows.Forms.Padding(3);
            this.tabGroups.Size = new System.Drawing.Size(927, 390);
            this.tabGroups.TabIndex = 0;
            this.tabGroups.Text = "Object Groups";
            this.tabGroups.UseVisualStyleBackColor = true;
            // 
            // lstGroups
            // 
            this.lstGroups.AutoArrange = false;
            this.lstGroups.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.groups_chName,
            this.groups_chGrounded,
            this.groups_chFaceColor,
            this.groups_chHighRes});
            this.lstGroups.FullRowSelect = true;
            this.lstGroups.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this.lstGroups.Location = new System.Drawing.Point(6, 6);
            this.lstGroups.MultiSelect = false;
            this.lstGroups.Name = "lstGroups";
            this.lstGroups.ShowGroups = false;
            this.lstGroups.Size = new System.Drawing.Size(915, 378);
            this.lstGroups.TabIndex = 4;
            this.lstGroups.UseCompatibleStateImageBehavior = false;
            this.lstGroups.View = System.Windows.Forms.View.Details;
            this.lstGroups.SelectedIndexChanged += new System.EventHandler(this.lstGroups_SelectedIndexChanged);
            this.lstGroups.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.lstGroups_MouseDoubleClick);
            // 
            // groups_chName
            // 
            this.groups_chName.Text = "Name";
            this.groups_chName.Width = 138;
            // 
            // groups_chFaceColor
            // 
            this.groups_chFaceColor.Text = "Multicolor Parts";
            this.groups_chFaceColor.Width = 127;
            // 
            // groups_chHighRes
            // 
            this.groups_chHighRes.Text = "High Resolution";
            this.groups_chHighRes.Width = 135;
            // 
            // tabJoints
            // 
            this.tabJoints.Controls.Add(this.lstJoints);
            this.tabJoints.Location = new System.Drawing.Point(4, 25);
            this.tabJoints.Name = "tabJoints";
            this.tabJoints.Padding = new System.Windows.Forms.Padding(3);
            this.tabJoints.Size = new System.Drawing.Size(927, 390);
            this.tabJoints.TabIndex = 1;
            this.tabJoints.Text = "Joint Drivers";
            this.tabJoints.UseVisualStyleBackColor = true;
            // 
            // groups_chGrounded
            // 
            this.groups_chGrounded.Text = "Grounded";
            this.groups_chGrounded.Width = 91;
            // 
            // ControlGroups
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(960, 493);
            this.Controls.Add(this.tabsMain);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnExport);
            this.Name = "ControlGroups";
            this.Text = "Control Groups";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.ControlGroups_FormClosed);
            this.Load += new System.EventHandler(this.ControlGroups_Load);
            this.tabsMain.ResumeLayout(false);
            this.tabGroups.ResumeLayout(false);
            this.tabJoints.ResumeLayout(false);
            this.ResumeLayout(false);

    }
    internal System.Windows.Forms.Button btnExport;
    internal System.Windows.Forms.Button btnCancel;
    public ControlGroups()
    {
        InitializeComponent();
    }

    private System.Windows.Forms.ListView lstJoints;
    private System.Windows.Forms.ColumnHeader item_chType;
    private System.Windows.Forms.ColumnHeader item_chParent;
    private System.Windows.Forms.ColumnHeader item_chChild;
    private System.Windows.Forms.ColumnHeader item_chDrive;
    private System.Windows.Forms.TabControl tabsMain;
    private System.Windows.Forms.TabPage tabGroups;
    private System.Windows.Forms.TabPage tabJoints;
    private System.Windows.Forms.ListView lstGroups;
    private System.Windows.Forms.ColumnHeader groups_chName;
    private System.Windows.Forms.ColumnHeader groups_chFaceColor;
    private System.Windows.Forms.ColumnHeader groups_chHighRes;
    private System.Windows.Forms.ColumnHeader groups_chGrounded;
}