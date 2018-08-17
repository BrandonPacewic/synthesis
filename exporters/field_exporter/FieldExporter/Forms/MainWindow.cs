﻿using System;
using System.Windows.Forms;
using System.Collections.Generic;
using FieldExporter.Components;
using System.Diagnostics;

namespace FieldExporter
{
    public partial class MainWindow : Form
    {   
        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            try
            {
                if (Program.ASSEMBLY_DOCUMENT != null)
                {
                    Exporter.FieldProperties fieldProps;
                    List<PropertySet> propSets;

                    Exporter.SaveManager.Load(Program.ASSEMBLY_DOCUMENT, out fieldProps, out propSets);

                    fieldMeta.SetSpawnpoints(fieldProps.spawnpoints);
                    GetPropertySetsTabControl().ApplyPropertySets(propSets);
                    GetPropertySetsTabControl().ApplyGamepieces(fieldProps.gamepieces);
                }
            }
            catch (Exporter.FailedToLoadException)
            {
                // Failed to load config
            }

            Text = "Synthesis Field Exporter - " + Program.ASSEMBLY_DOCUMENT.DisplayName;
        }

        /// <summary>
        /// Returns the physicsGroupsTabControl instance.
        /// </summary>
        /// <returns></returns>
        public PropertySetsTabControl GetPropertySetsTabControl()
        {
            return propertySetsTabControl;
        }

        /// <summary>
        /// Prepares the window.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainWindow_Load(object sender, EventArgs e)
        {
            menuStrip.Renderer = new ToolStripProfessionalRenderer(new SynthesisColorTable());
        }

        /// <summary>
        /// Closes the window when the exitToolStripMenuItem is clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        /// <summary>
        /// Saves the current configuration to the field assembly.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (Program.ASSEMBLY_DOCUMENT != null)
                {
                    Exporter.FieldProperties fieldProps = new Exporter.FieldProperties(FieldMetaForm.GetSpawnpoints(),
                                                                                       GetPropertySetsTabControl().TranslateToGamepieces());
                    List<PropertySet> propSets = GetPropertySetsTabControl().TranslateToPropertySets();
                    Exporter.SaveManager.Save(Program.ASSEMBLY_DOCUMENT, fieldProps, propSets);
                }
            }
            catch (Exporter.FailedToSaveException er)
            {
                MessageBox.Show("Failed to save field configuration. The following error occurred:\n" + er.InnerException.ToString(), "Error", MessageBoxButtons.OK);
            }
        }

        /// <summary>
        /// Resets the size of the window.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void resetSizeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            WindowState = FormWindowState.Normal;
            Size = MinimumSize;
        }

        /// <summary>
        /// Toggles the TopMost property for the window.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void alwaysOnTopToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (alwaysOnTopToolStripMenuItem.Checked)
            {
                TopMost = true;
            }
            else
            {
                TopMost = false;
            }
        }

        /// <summary>
        /// Opens the user's browser and brings them to the field exporter tutorial page.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tutorialsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start("http://bxd.autodesk.com/synthesis/?page=tutorialFieldExporter");
        }

        /// <summary>
        /// Prevents the user from switching tabs when the exporter is running.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tabControl_Selecting(object sender, TabControlCancelEventArgs e)
        {
            if (exportForm.IsExporting)
                e.Cancel = true;
        }
    }
}
