﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using BxDRobotExporter.ControlGUI;
using BxDRobotExporter.GUI.Editors;
using BxDRobotExporter.OGLViewer;
using BxDRobotExporter.SkeletalStructure;
using Inventor;

namespace BxDRobotExporter
{
    public class RobotData
    {
        // Robot
        public string RobotName;
        public float RobotWeightKg;
        public RigidNode_Base RobotBaseNode = null;
        public List<BXDAMesh> RobotMeshes = null;

        // Robot export settings
        public bool ExportWithColors = true;
        public string ExportDefaultField;
        public bool PreferMetric = false;

        public RobotData()
        {
            RigidNode_Base.NODE_FACTORY = guid => new OGL_RigidNode(guid); // TODO: Remove this and refactor BXDJReader versions
        }

        /// <summary>
        /// Build the node tree of the robot from Inventor
        /// </summary>
        public bool LoadRobotSkeleton()
        {
            try
            {
                var exporterThread = new Thread(() =>
                {
                    var loadingSkeleton = new LoadingSkeletonForm(this);
                    loadingSkeleton.ShowDialog();
                });

                exporterThread.SetApartmentState(ApartmentState.STA);
                exporterThread.Start();
                exporterThread.Join();
                GC.Collect();
            }
            catch (InvalidComObjectException) // TODO: Don't do this
            {
            }
            catch (TaskCanceledException)
            {
                return false;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
                return false;
            }

            if (RobotBaseNode == null)
                return false; // Skeleton export failed

            return true;
        }

        /// <summary>
        /// Load meshes of a robot from Inventor
        /// </summary>
        private bool LoadMeshes()
        {
            var liteExporter = new LiteExporterForm(this);
            try
            {
                var exporterThread = new Thread(() =>
                {
                    if (RobotBaseNode == null)
                    {
                        var loadingSkeleton = new LoadingSkeletonForm(this);
                        loadingSkeleton.ShowDialog();
                    }

                    if (RobotBaseNode != null)
                    {
                        liteExporter.ShowDialog(); // Remove node building
                    }
                });

                exporterThread.SetApartmentState(ApartmentState.STA);
                exporterThread.Start();

                exporterThread.Join();

                GC.Collect();

                ExportWithColors = RobotExporterAddInServer.Instance.AddInSettings.GeneralUseFancyColors;
            }
            catch (InvalidComObjectException) // TODO: Don't do this
            {
            }
            catch (TaskCanceledException)
            {
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
                return false;
            }

            return RobotMeshes != null && liteExporter.DialogResult == DialogResult.OK;
        }

        /// <summary>
        /// Iterates over all the joints in the skeleton and writes the corrosponding Inventor limit into the internal joint limit
        /// Necessary to pull the limits into the joint as the exporter exports. Where the joint is actually written to the .bxdj,
        /// we are unable to access RobotExporterAPI or BxDRobotExporter, so writing the limits here is a workaround to that issue.
        /// </summary>
        /// <param name="skeleton">Skeleton to write limits to</param>
        private static void WriteLimits(RigidNode_Base skeleton)
        {
            var nodes = new List<RigidNode_Base>();
            skeleton.ListAllNodes(nodes);
            var parentId = new int[nodes.Count];

            for (var i = 0; i < nodes.Count; i++)
            {
                if (nodes[i].GetParent() != null)
                {
                    parentId[i] = nodes.IndexOf(nodes[i].GetParent());

                    if (parentId[i] < 0) throw new Exception("Can't resolve parent ID for " + nodes[i].ToString());
                }
                else
                {
                    parentId[i] = -1;
                }
            }

            for (var i = 0; i < nodes.Count; i++)
            {
                if (parentId[i] >= 0)
                {
                    var inventorJoint = nodes[i].GetSkeletalJoint() as InventorSkeletalJoint;
                    if (inventorJoint != null)
                        inventorJoint.ReloadInventorJoint();
                }
            }
        }

        /// <summary>
        /// Saves the robot to the directory it was loaded from or the default directory
        /// </summary>
        /// <returns></returns\>
        /// 
        public bool ExportRobot()
        {
            try
            {
                WriteLimits(RobotBaseNode); // write the limits from Inventor to the skeleton
                
                if (string.IsNullOrEmpty(RobotName)) // If robot has not been named, cancel
                    return false;

                var robotFolderPath = RobotExporterAddInServer.Instance.AddInSettings.GeneralSaveLocation + "\\" + RobotName;
                Directory.CreateDirectory(robotFolderPath); // CreateDirectory checks if the folder already exists

                if (!LoadMeshes()) // Re-export every time because we don't detect changes to the robot CAD
                    return false;
                
                BXDJSkeleton.SetupFileNames(RobotBaseNode);
                BXDJSkeleton.WriteSkeleton(robotFolderPath + "\\skeleton.bxdj", RobotBaseNode);

                for (var i = 0; i < RobotMeshes.Count; i++)
                {
                    RobotMeshes[i].WriteToFile(robotFolderPath + "\\node_" + i + ".bxda");
                }
                return true;
            }
            catch (Exception e)
            {
                //TODO: Create a form that displays a simple error message with an option to expand it and view the exception info
                MessageBox.Show("Unable to export robot: " + e.Message, "Export Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>
        /// Loads the joint information from the Inventor assembly file. Returns false if fails.
        /// </summary>
        /// <param name="asmDocument">Assembly document to load data from. Data will be saved to this document when <see cref="SaveRobotData"/> is called.</param>
        /// <returns>True if all data was loaded successfully.</returns>
        public bool LoadRobotData(Document asmDocument)
        {
            if (asmDocument == null)
                return false;

            if (RobotBaseNode == null)
                return false;

            var propertySets = asmDocument.PropertySets;

            // Load Robot Data
            try
            {
                // Load global robot data
                var propertySet = InventorDocumentIoUtils.GetPropertySet(propertySets, "bxd-robotdata", false);

                if (propertySet != null)
                {
                    RobotName = InventorDocumentIoUtils.GetProperty(propertySet, "robot-name", "");
                    RobotWeightKg = InventorDocumentIoUtils.GetProperty(propertySet, "robot-weight-kg", 0) / 10.0f; // Stored at x10 for better accuracy
                    PreferMetric = InventorDocumentIoUtils.GetProperty(propertySet, "robot-prefer-metric", false);
                    RobotBaseNode.driveTrainType = (RigidNode_Base.DriveTrainType) InventorDocumentIoUtils.GetProperty(propertySet, "robot-driveTrainType", (int) RigidNode_Base.DriveTrainType.NONE);
                }

                // Load joint data
                return LoadJointData(propertySets, RobotBaseNode) && (propertySet != null);
            }
            catch (Exception e)
            {
                MessageBox.Show("Robot data could not be loaded from the inventor file. The following error occured:\n" + e.Message);
                return false;
            }
        }

        /// <summary>
        /// Recursive utility for JointDataLoad.
        /// </summary>
        /// <param name="propertySets">Group of property sets to add any new property sets to.</param>
        /// <param name="currentNode">Current node to save joint data of.</param>
        /// <returns>True if all data was loaded successfully.</returns>
        private static bool LoadJointData(PropertySets propertySets, RigidNode_Base currentNode)
        {
            var allSuccessful = true;

            foreach (var connection in currentNode.Children)
            {
                var joint = connection.Key;
                var child = connection.Value;

                // Name of the property set in inventor
                var setName = "bxd-jointdata-" + child.GetModelID();

                // Attempt to open the property set
                var propertySet = InventorDocumentIoUtils.GetPropertySet(propertySets, setName, false);

                // If the property set does not exist, stop loading data
                if (propertySet == null)
                    return false;

                joint.weight = InventorDocumentIoUtils.GetProperty(propertySet, "weight", 10);

                // Get joint properties from set
                // Get driver information
                if (InventorDocumentIoUtils.GetProperty(propertySet, "has-driver", false))
                {
                    if (joint.cDriver == null)
                        joint.cDriver = new JointDriver((JointDriverType) InventorDocumentIoUtils.GetProperty(propertySet, "driver-type", (int) JointDriverType.MOTOR));
                    var driver = joint.cDriver;

                    joint.cDriver.motor = (MotorType) InventorDocumentIoUtils.GetProperty(propertySet, "motor-type", (int) MotorType.GENERIC);
                    joint.cDriver.port1 = InventorDocumentIoUtils.GetProperty(propertySet, "driver-port1", 0);
                    joint.cDriver.port2 = InventorDocumentIoUtils.GetProperty(propertySet, "driver-port2", -1);
                    joint.cDriver.isCan = InventorDocumentIoUtils.GetProperty(propertySet, "driver-isCan", false);
                    joint.cDriver.lowerLimit = InventorDocumentIoUtils.GetProperty(propertySet, "driver-lowerLimit", 0.0f);
                    joint.cDriver.upperLimit = InventorDocumentIoUtils.GetProperty(propertySet, "driver-upperLimit", 0.0f);
                    joint.cDriver.InputGear = InventorDocumentIoUtils.GetProperty(propertySet, "driver-inputGear", 0.0f); // writes the gearing that the user last had in the exporter to the current gearing value
                    joint.cDriver.OutputGear = InventorDocumentIoUtils.GetProperty(propertySet, "driver-outputGear", 0.0f); // writes the gearing that the user last had in the exporter to the current gearing value
                    joint.cDriver.hasBrake = InventorDocumentIoUtils.GetProperty(propertySet, "driver-hasBrake", false);

                    // Get other properties stored in meta
                    // Wheel information
                    if (InventorDocumentIoUtils.GetProperty(propertySet, "has-wheel", false))
                    {
                        if (driver.GetInfo<WheelDriverMeta>() == null)
                            driver.AddInfo(new WheelDriverMeta());
                        var wheel = joint.cDriver.GetInfo<WheelDriverMeta>();

                        wheel.type = (WheelType) InventorDocumentIoUtils.GetProperty(propertySet, "wheel-type", (int) WheelType.NORMAL);
                        wheel.isDriveWheel = InventorDocumentIoUtils.GetProperty(propertySet, "wheel-isDriveWheel", false);
                        wheel.SetFrictionLevel((FrictionLevel) InventorDocumentIoUtils.GetProperty(propertySet, "wheel-frictionLevel", (int) FrictionLevel.MEDIUM));
                    }

                    // Pneumatic information
                    if (InventorDocumentIoUtils.GetProperty(propertySet, "has-pneumatic", false))
                    {
                        if (driver.GetInfo<PneumaticDriverMeta>() == null)
                            driver.AddInfo(new PneumaticDriverMeta());
                        var pneumatic = joint.cDriver.GetInfo<PneumaticDriverMeta>();

                        pneumatic.width = InventorDocumentIoUtils.GetProperty(propertySet, "pneumatic-diameter", (double) 0.5);
                        pneumatic.pressureEnum = (PneumaticPressure) InventorDocumentIoUtils.GetProperty(propertySet, "pneumatic-pressure", (int) PneumaticPressure.MEDIUM);
                    }

                    // Elevator information
                    if (InventorDocumentIoUtils.GetProperty(propertySet, "has-elevator", false))
                    {
                        if (driver.GetInfo<ElevatorDriverMeta>() == null)
                            driver.AddInfo(new ElevatorDriverMeta());
                        var elevator = joint.cDriver.GetInfo<ElevatorDriverMeta>();

                        elevator.type = (ElevatorType) InventorDocumentIoUtils.GetProperty(propertySet, "elevator-type", (int) ElevatorType.NOT_MULTI);
                        if (((int) elevator.type) > 7)
                        {
                            elevator.type = ElevatorType.NOT_MULTI;
                        }
                    }

                    for (var i = 0; i < InventorDocumentIoUtils.GetProperty(propertySet, "num-sensors", 0); i++)
                    {
                        RobotSensor addedSensor;
                        addedSensor = new RobotSensor((RobotSensorType) InventorDocumentIoUtils.GetProperty(propertySet, "sensorType" + i, (int) RobotSensorType.ENCODER));
                        addedSensor.portA = ((int) InventorDocumentIoUtils.GetProperty(propertySet, "sensorPortA" + i, 0));
                        addedSensor.portB = ((int) InventorDocumentIoUtils.GetProperty(propertySet, "sensorPortB" + i, 0));
                        addedSensor.conTypePortA = ((SensorConnectionType) InventorDocumentIoUtils.GetProperty(propertySet, "sensorPortConA" + i, (int) SensorConnectionType.DIO));
                        addedSensor.conTypePortB = ((SensorConnectionType) InventorDocumentIoUtils.GetProperty(propertySet, "sensorPortConB" + i, (int) SensorConnectionType.DIO));
                        addedSensor.conversionFactor = InventorDocumentIoUtils.GetProperty(propertySet, "sensorConversion" + i, 0.0);
                        joint.attachedSensors.Add(addedSensor);
                    }
                }

                // Recur along this child
                if (!LoadJointData(propertySets, child))
                    allSuccessful = false;
            }

            // Save was successful
            return allSuccessful;
        }

        /// <summary>
        /// Saves the joint information to the most recently loaded assembly file. Returns false if fails.
        /// </summary>
        /// <returns>True if all data was saved successfully.</returns>
        public bool SaveRobotData(Document asmDocument)
        {
            if (asmDocument == null)
                return false;

            if (RobotBaseNode == null)
                return false;

            var propertySets = asmDocument.PropertySets;

            // Save Robot Data
            try
            {
                // Save global robot data
                var propertySet = InventorDocumentIoUtils.GetPropertySet(propertySets, "bxd-robotdata");

                if (RobotName != null)
                    InventorDocumentIoUtils.SetProperty(propertySet, "robot-name", RobotName);
                InventorDocumentIoUtils.SetProperty(propertySet, "robot-weight-kg", RobotWeightKg * 10.0f); // x10 for better accuracy
                InventorDocumentIoUtils.SetProperty(propertySet, "robot-prefer-metric", PreferMetric);
                InventorDocumentIoUtils.SetProperty(propertySet, "robot-driveTrainType", (int) RobotBaseNode.driveTrainType);

                // Save joint data
                return SaveJointData(propertySets, RobotBaseNode);
            }
            catch (Exception e)
            {
                MessageBox.Show("Robot data could not be save to the inventor file. The following error occured:\n" + e.Message);
                return false;
            }
        }

        /// <summary>
        /// Recursive utility for JointDataSave.
        /// </summary>
        /// <returns>True if all data was saved successfully.</returns>
        private static bool SaveJointData(PropertySets assemblyPropertySets, RigidNode_Base currentNode)
        {
            var allSuccessful = true;

            foreach (var connection in currentNode.Children)
            {
                var joint = connection.Key;
                var child = connection.Value;

                // Name of the property set in inventor
                var setName = "bxd-jointdata-" + child.GetModelID();

                // Create the property set if it doesn't exist
                var propertySet = InventorDocumentIoUtils.GetPropertySet(assemblyPropertySets, setName);

                // Add joint properties to set
                // Save driver information
                var driver = joint.cDriver;
                InventorDocumentIoUtils.SetProperty(propertySet, "has-driver", driver != null);
                InventorDocumentIoUtils.SetProperty(propertySet, "weight", joint.weight);
                if (driver != null)
                {
                    InventorDocumentIoUtils.SetProperty(propertySet, "driver-type", (int) driver.GetDriveType());
                    InventorDocumentIoUtils.SetProperty(propertySet, "motor-type", (int) driver.GetMotorType());
                    InventorDocumentIoUtils.SetProperty(propertySet, "driver-port1", driver.port1);
                    InventorDocumentIoUtils.SetProperty(propertySet, "driver-port2", driver.port2);
                    InventorDocumentIoUtils.SetProperty(propertySet, "driver-isCan", driver.isCan);
                    InventorDocumentIoUtils.SetProperty(propertySet, "driver-lowerLimit", driver.lowerLimit);
                    InventorDocumentIoUtils.SetProperty(propertySet, "driver-upperLimit", driver.upperLimit);
                    InventorDocumentIoUtils.SetProperty(propertySet, "driver-inputGear", driver.InputGear); // writes the input gear to the .IAM file incase the user wants to reexport their robot later
                    InventorDocumentIoUtils.SetProperty(propertySet, "driver-outputGear", driver.OutputGear); // writes the ouotput gear to the .IAM file incase the user wants to reexport their robot later
                    InventorDocumentIoUtils.SetProperty(propertySet, "driver-hasBrake", driver.hasBrake);

                    // Save other properties stored in meta
                    // Wheel information
                    var wheel = joint.cDriver.GetInfo<WheelDriverMeta>();
                    InventorDocumentIoUtils.SetProperty(propertySet, "has-wheel", wheel != null);

                    if (wheel != null)
                    {
                        InventorDocumentIoUtils.SetProperty(propertySet, "wheel-type", (int) wheel.type);
                        InventorDocumentIoUtils.SetProperty(propertySet, "wheel-isDriveWheel", wheel.isDriveWheel);
                        InventorDocumentIoUtils.SetProperty(propertySet, "wheel-frictionLevel", (int) wheel.GetFrictionLevel());
                    }

                    // Pneumatic information
                    var pneumatic = joint.cDriver.GetInfo<PneumaticDriverMeta>();
                    InventorDocumentIoUtils.SetProperty(propertySet, "has-pneumatic", pneumatic != null);

                    if (pneumatic != null)
                    {
                        InventorDocumentIoUtils.SetProperty(propertySet, "pneumatic-diameter", (double) pneumatic.width);
                        InventorDocumentIoUtils.SetProperty(propertySet, "pneumatic-pressure", (int) pneumatic.pressureEnum);
                    }

                    // Elevator information
                    var elevator = joint.cDriver.GetInfo<ElevatorDriverMeta>();


                    InventorDocumentIoUtils.SetProperty(propertySet, "has-elevator", elevator != null);

                    if (elevator != null)
                    {
                        InventorDocumentIoUtils.SetProperty(propertySet, "elevator-type", (int) elevator.type);
                    }
                }

                for (var i = 0; i < InventorDocumentIoUtils.GetProperty(propertySet, "num-sensors", 0); i++) // delete existing sensors
                {
                    InventorDocumentIoUtils.RemoveProperty(propertySet, "sensorType" + i);
                    InventorDocumentIoUtils.RemoveProperty(propertySet, "sensorPortA" + i);
                    InventorDocumentIoUtils.RemoveProperty(propertySet, "sensorPortConA" + i);
                    InventorDocumentIoUtils.RemoveProperty(propertySet, "sensorPortB" + i);
                    InventorDocumentIoUtils.RemoveProperty(propertySet, "sensorPortConB" + i);
                    InventorDocumentIoUtils.RemoveProperty(propertySet, "sensorConversion" + i);
                }

                InventorDocumentIoUtils.SetProperty(propertySet, "num-sensors", joint.attachedSensors.Count);
                for (var i = 0; i < joint.attachedSensors.Count; i++)
                {
                    InventorDocumentIoUtils.SetProperty(propertySet, "sensorType" + i, (int) joint.attachedSensors[i].type);
                    InventorDocumentIoUtils.SetProperty(propertySet, "sensorPortA" + i, joint.attachedSensors[i].portA);
                    InventorDocumentIoUtils.SetProperty(propertySet, "sensorPortConA" + i, (int) joint.attachedSensors[i].conTypePortA);
                    InventorDocumentIoUtils.SetProperty(propertySet, "sensorPortB" + i, joint.attachedSensors[i].portB);
                    InventorDocumentIoUtils.SetProperty(propertySet, "sensorPortConB" + i, (int) joint.attachedSensors[i].conTypePortB);
                    InventorDocumentIoUtils.SetProperty(propertySet, "sensorConversion" + i, joint.attachedSensors[i].conversionFactor);
                }

                // Recur along this child
                if (!SaveJointData(assemblyPropertySets, child))
                    allSuccessful = false;
            }

            // Save was successful
            return allSuccessful;
        }

        /// <summary>
        /// Opens the <see cref="DrivetrainWeightForm"/> form
        /// </summary>
        /// <returns>True if robot weight was changed.</returns>
        public bool PromptRobotWeight()
        {
            try
            {
                var weightForm = new DrivetrainWeightForm(this);

                weightForm.ShowDialog();

                if (weightForm.DialogResult == DialogResult.OK)
                {
                    RobotWeightKg = weightForm.TotalWeightKg;
                    PreferMetric = weightForm.PreferMetric;
                    return true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                throw;
            }

            return false;
        }

        /// <summary>
        /// Merges a node into the parent. Used during the one click export and the wizard.
        /// </summary>
        /// <param name="node"></param>
        public void MergeNodeIntoParent(RigidNode_Base node)
        {
            if (node.GetParent() == null)
                throw new ArgumentException("ERROR: Root node passed to MergeNodeIntoParent(RigidNode_Base)", "node");

            node.GetParent().ModelFullID += node.ModelFullID;

            //Get meshes for each node
            var childMesh = GetMesh(node);
            var parentMesh = GetMesh(node.GetParent());

            //Merge submeshes and colliders
            parentMesh.meshes.AddRange(childMesh.meshes);
            parentMesh.colliders.AddRange(childMesh.colliders);

            //Merge physics
            parentMesh.physics.Add(childMesh.physics.mass, childMesh.physics.centerOfMass);

            //Remove node from the children of its parent
            node.GetParent().Children.Remove(node.GetSkeletalJoint());
            RobotMeshes.Remove(childMesh);
        }

        private BXDAMesh GetMesh(RigidNode_Base node)
        {
            return RobotMeshes[RobotBaseNode.ListAllNodes().IndexOf(node)];
        }
    }
}