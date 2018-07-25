﻿using Synthesis.FSM;
using Synthesis.States;
using Synthesis.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Synthesis.GUI
{
    public class NetworkMultiplayerUI : MonoBehaviour
    {
        /// <summary>
        /// The global <see cref="NetworkMultiplayerUI"/> instance.
        /// </summary>
        public static NetworkMultiplayerUI Instance { get; private set; }

        private StateMachine uiStateMachine;
        private Canvas canvas;

        /// <summary>
        /// Initializes the global instance reference.
        /// </summary>
        private void Awake()
        {
            Instance = this;
        }

        /// <summary>
        /// Links the <see cref="NetworkMultiplayerUI"/>'s panels to the <see cref="StateMachine"/> and
        /// registers all button callbacks.
        /// </summary>
        private void Start()
        {
            uiStateMachine = GetComponent<StateMachine>();
            canvas = GetComponent<Canvas>();

            LinkPanels();
            RegisterButtonCallbacks();

            uiStateMachine.PushState(new HostJoinState());
        }

        /// <summary>
        /// Runs every frame to update the GUI elements.
        /// </summary>
        void OnGUI()
        {
            UserMessageManager.scale = canvas.scaleFactor;
            UserMessageManager.Render();
        }

        /// <summary>
        /// Registers a click callback from the given <see cref="Button"/> to the active
        /// <see cref="State"/>.
        /// </summary>
        /// <param name="button"></param>
        public void RegisterButtonCallback(Button button)
        {
            button.onClick.AddListener(() => InvokeCallback("On" + button.name + "Pressed"));
        }

        /// <summary>
        /// Pops the active UI <see cref="State"/>.
        /// </summary>
        public void OnBackButtonPressed()
        {
            uiStateMachine.PopState();

            if (uiStateMachine.CurrentState == null)
            {
                Auxiliary.FindGameObject("ExitingPanel").SetActive(true);
                SceneManager.LoadScene("MainMenu");
            }
        }

        /// <summary>
        /// Links individual panels with their respective <see cref="State"/>s.
        /// </summary>
        private void LinkPanels()
        {
            LinkPanel<HostJoinState>("HostJoinPanel");
            LinkPanel<EnterTagState>("EnterTagPanel");
            LinkPanel<EnterInfoState>("EnterInfoPanel");
            LinkPanel<LobbyState>("LobbyPanel");
            LinkPanel<LoadFieldState>("SimLoadField");
            LinkPanel<LoadRobotState>("SimLoadRobot");
            LinkPanel<FetchingMetadataState>("FetchingMetadataPanel");
            LinkPanel<AnalyzingResourcesState>("AnalyzingResourcesPanel");
            LinkPanel<GatheringResourcesState>("GatheringResourcesPanel");
        }

        /// <summary>
        /// Links a panel to the provided <see cref="State"/> type from the panel's name.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="panelName"></param>
        private void LinkPanel<T>(string panelName, bool strict = true) where T : State
        {
            GameObject tab = Auxiliary.FindGameObject(panelName);

            if (tab != null)
                uiStateMachine.Link<T>(tab, true, strict);
        }

        /// <summary>
        /// Finds each Button component in the main menu that doesn't already have a
        /// listener and registers it with a callback.
        /// </summary>
        private void RegisterButtonCallbacks()
        {
            foreach (Button b in GetComponentsInChildren<Button>(true))
                if (b.onClick.GetPersistentEventCount() == 0)
                    RegisterButtonCallback(b);
        }

        /// <summary>
        /// Invokes a method in the active <see cref="State"/> by the given method name.
        /// </summary>
        /// <param name="methodName"></param>
        private void InvokeCallback(string methodName)
        {
            State currentState = uiStateMachine.CurrentState;
            MethodInfo info = currentState.GetType().GetMethod(methodName);

            if (info == null)
                Debug.LogWarning("Method " + methodName + " does not have a listener in " + currentState.GetType().ToString());
            else
                info.Invoke(currentState, null);
        }
    }
}
