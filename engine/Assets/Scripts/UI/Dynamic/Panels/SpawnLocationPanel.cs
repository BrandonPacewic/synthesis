using System;
using System.Collections.Generic;
using System.Linq;
using Synthesis.UI.Dynamic;
using Synthesis.PreferenceManager;
using UnityEngine;
using Synthesis.Gizmo;
using Synthesis.Runtime;

namespace Synthesis.UI.Dynamic {
    public class SpawnLocationPanel : PanelDynamic {
        private static float width  = 300f;
        private static float height = 200f;

        private const float VERTICAL_PADDING = 15f;

        public Func<UIComponent, UIComponent> VerticalLayout = (u) => {
            var offset = (-u.Parent!.RectOfChildren(u).yMin) + VERTICAL_PADDING;
            u.SetTopStretch<UIComponent>(anchoredY: offset, leftPadding: 0f); // used to be 15f
            return u;
        };

        public SpawnLocationPanel() : base(new Vector2(width, height)) {}

        Label location;

        public override bool Create() {
            Title.SetText("Set Spawn").SetFontSize(25f);
            PanelImage.RootGameObject.SetActive(false);
            Content panel = new Content(null, UnityObject, null);
            panel.SetBottomStretch<Content>(Screen.width / 2 - width / 2 - 40f, Screen.width / 2 - width / 2 - 40f, 0);

            AcceptButton.StepIntoLabel(label => label.SetText("Start")).AddOnClickedEvent(b => {
                if (!matchStarted) {
                    matchStarted = true;
                    StartMatch();
                }
            });
            CancelButton.StepIntoLabel(label => label.SetText("Cancel")).AddOnClickedEvent(b => {
                DynamicUIManager.CreateModal<MatchModeModal>();
            });

            location = MainContent.CreateLabel(30f)
                           .ApplyTemplate(VerticalLayout)
                           .SetFontSize(30)
                           .SetHorizontalAlignment(TMPro.HorizontalAlignmentOptions.Center)
                           .SetVerticalAlignment(TMPro.VerticalAlignmentOptions.Bottom)
                           .SetTopStretch(leftPadding: 10f, anchoredY: 130f)
                           .SetText("(0.00, 0.00, 0.00)");

            return true;
        }

        private void StartMatch() {
            if (RobotSimObject.CurrentlyPossessedRobot != string.Empty) {
                Vector3 p = RobotSimObject.GetCurrentlyPossessedRobot().RobotNode.transform.position;
                PreferenceManager.PreferenceManager.SetPreference(
                    MatchMode.PREVIOUS_SPAWN_LOCATION, new float[] { p.x, p.y, p.z });
                Quaternion q = RobotSimObject.GetCurrentlyPossessedRobot().RobotNode.transform.rotation;
                PreferenceManager.PreferenceManager.SetPreference(
                    MatchMode.PREVIOUS_SPAWN_ROTATION, new float[] { q.x, q.y, q.z, q.w });
                PreferenceManager.PreferenceManager.Save();
            }

            // TEMPORARY: FOR POWERUP ONLY

            Scoring.CreatePowerupScoreZones();
            DynamicUIManager.CloseAllPanels(true);
            DynamicUIManager.CreatePanel<Synthesis.UI.Dynamic.ScoreboardPanel>(true);

            GizmoManager.ExitGizmo();
        }

        private bool matchStarted = false;

        public override void Update() {
            Vector3 robotPosition = new Vector3();
            if (RobotSimObject.CurrentlyPossessedRobot != string.Empty) {
                robotPosition = RobotSimObject.GetCurrentlyPossessedRobot().RobotNode.transform.position;
            }

            location.SetText(
                $"({String.Format("{0:0.00}", robotPosition.x)}, {String.Format("{0:0.00}", robotPosition.y)}, {String.Format("{0:0.00}", robotPosition.z)})");

            if ((SimulationRunner.HasContext(SimulationRunner.GIZMO_SIM_CONTEXT) ||
                    SimulationRunner.HasContext(SimulationRunner.PAUSED_SIM_CONTEXT)) &&
                !matchStarted) {
                matchStarted = true;
                StartMatch();
            }
        }

        public override void Delete() {}
    }
}