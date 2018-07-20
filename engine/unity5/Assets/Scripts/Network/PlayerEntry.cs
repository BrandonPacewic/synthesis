﻿using Synthesis.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Synthesis.Network
{
    public class PlayerEntry : MonoBehaviour
    {
        /// <summary>
        /// The <see cref="Network.PlayerIdentity"/> used to updated this <see cref="PlayerEntry"/>.
        /// </summary>
        public PlayerIdentity PlayerIdentity { get; set; }

        private Text playerTagText;
        private Text robotText;
        private Text readyText;
        private Button robotButton;
        private Button readyButton;

        /// <summary>
        /// Establishes references to various components of this <see cref="PlayerEntry"/>.
        /// </summary>
        private void Awake()
        {
            playerTagText = transform.Find("PlayerTagText").GetComponent<Text>();
            robotText = transform.Find("RobotButton").GetComponent<Text>();
            readyText = transform.Find("ReadyButton").GetComponent<Text>();
            robotButton = transform.Find("RobotButton").GetComponent<Button>();
            readyButton = transform.Find("ReadyButton").GetComponent<Button>();

            NetworkMultiplayerUI.Instance.RegisterButtonCallback(robotButton);
            NetworkMultiplayerUI.Instance.RegisterButtonCallback(readyButton);
        }

        /// <summary>
        /// Sets the initial properties of the entry.
        /// </summary>
        private void Start()
        {
            if (!PlayerIdentity.isLocalPlayer)
            {
                robotButton.enabled = false;
                readyButton.enabled = false;
            }
        }

        /// <summary>
        /// Updates information on the entry to the associated <see cref="Network.PlayerIdentity"/>.
        /// </summary>
        private void OnGUI()
        {
            if (PlayerIdentity == null)
                return;

            playerTagText.color = PlayerIdentity.isLocalPlayer ? Color.green : Color.white;
            playerTagText.text = PlayerIdentity.PlayerTag;
            
            if (PlayerIdentity.isLocalPlayer)
                playerTagText.text += " (You)";

            robotText.text = string.IsNullOrEmpty(PlayerIdentity.RobotName) ?
                "(Choosing...)" : PlayerIdentity.RobotName;

            if (PlayerIdentity.Ready)
            {
                readyText.text = "READY";
                readyText.color = Color.green;
            }
            else
            {
                readyText.text = "NOT READY";
                readyText.color = Color.red;
            }
        }
    }
}
