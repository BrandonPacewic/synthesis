﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using InventorRobotExporter.Managers;
using InventorRobotExporter.Utilities;

namespace InventorRobotExporter.GUI.Editors.SimpleJointEditor
{
    public partial class SimpleEditor : Form
    {
        private readonly RobotDataManager robotDataManager;

        public SimpleEditor(RobotDataManager robotDataManager)
        {
            this.robotDataManager = robotDataManager;

            InitializeComponent();
            LoadJointsNavigator();

            jointTypeInput.SelectedIndex = 0;
            jointDriverInput.SelectedIndex = 0;
            driveSideInput.SelectedIndex = 0;
            wheelTypeInput.SelectedIndex = 0;
        }

        private void ClearType()
        {
            // Hides all elements to return the window to its default state
            weightBox.Visible = false;
            weightAmountInput.Visible = false;
            jointDriverBox.Visible = false;
            jointDriverInput.Visible = false;
            driveSideBox.Visible = false;
            driveSideInput.Visible = false;
            wheelTypeBox.Visible = false;
            wheelTypeInput.Visible = false;
            limitsBox.Visible = false;
            limitStartCheckbox.Visible = false;
            limitEndCheckbox.Visible = false;
            limitStartInput.Visible = false;
            limitEndInput.Visible = false;
            animateMovementButton.Visible = false;
            advancedButton.Visible = false;
        }

        private void ShowMechanismType()
        {
            // Show relevant elements to the Mechanism joint type
            weightBox.Visible = true;
            weightAmountInput.Visible = true;
            jointDriverBox.Visible = true;
            jointDriverInput.Visible = true;
            limitsBox.Visible = true;
            limitStartCheckbox.Visible = true;
            limitEndCheckbox.Visible = true;
            limitStartInput.Visible = true;
            limitEndInput.Visible = true;
            animateMovementButton.Visible = true;
            advancedButton.Visible = true;

            ResizeToMechanism();
        }

        private void ResizeToMechanism()
        {
            this.Height = 409;

            advancedButton.Location = new Point(9, 338);
            advancedButton.BringToFront();
            okButton.Location = new Point(208, 338);
            okButton.BringToFront();
            cancelButton.Location = new Point(284, 338);
            cancelButton.BringToFront();
        }

        private void ShowDrivetrainType()
        {
            // Show relevant elements to the Drivetrain joint type
            driveSideBox.Visible = true;
            driveSideInput.Visible = true;
            wheelTypeBox.Visible = true;
            wheelTypeInput.Visible = true;
            advancedButton.Visible = true;

            ResizeToDrivetrain();
        }

        private void ResizeToDrivetrain()
        {
            this.Height = 346;

            advancedButton.Location = new Point(9, 275);
            advancedButton.BringToFront();
            okButton.Location = new Point(208, 275);
            okButton.BringToFront();
            cancelButton.Location = new Point(284, 275);
            cancelButton.BringToFront();
        }

        private void LoadJointsNavigator()
        {
            // Loads all node names of components with joints into the Joints Navigator
            foreach (RigidNode_Base node in robotDataManager.RobotBaseNode.ListAllNodes())
            {
                if (node.GetSkeletalJoint() != null)
                {
                    jointNavigator.Items.Add(ToStringUtils.NodeNameString(node));
                }
            }
            jointNavigator.SelectedIndex = 0;
        }

        // Switch joints from joint list
        private void JointNavigator_SelectedIndexChanged(object sender, EventArgs e)
        {
            // TODO: Create logic for updating data
        }

        // Change between Drivetrain Wheel and Mechanism Joint
        private void JointTypeInput_SelectedIndexChanged(object sender, EventArgs e)
        {
            ClearType();
            if (jointTypeInput.SelectedIndex == 0)
            {
                ResizeToDrivetrain();
            } else if (jointTypeInput.SelectedIndex == 1)
            {
                ShowDrivetrainType();
            } else if (jointTypeInput.SelectedIndex == 2)
            {
                ShowMechanismType();
            }
        }

        private void AnimateMovementButton_Click(object sender, EventArgs e)
        {
            // TODO: Animate the selected joint
        }

        private void AdvancedButton_Click(object sender, EventArgs e)
        {
            new AdvancedJointSettings().ShowDialog();
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void OkButton_Click(object sender, EventArgs e)
        {
            // TODO: Implement saving
            Close();
        }

    }
}
