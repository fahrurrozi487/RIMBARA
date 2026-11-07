using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Rimbara.UI
{
    [Serializable]
    public sealed class HudReferences
    {
        public Canvas Canvas;
        public GameObject Gameplay;
        public GameObject Menu;
        public GameObject Briefing;
        public GameObject Controls;
        public GameObject Pause;
        public GameObject Victory;
        public GameObject Dimmer;
        public TMP_Text MissionNumber;
        public TMP_Text MissionTitle;
        public TMP_Text MissionDescription;
        public TMP_Text Seedlings;
        public TMP_Text Trees;
        public TMP_Text Fires;
        public TMP_Text Restoration;
        public TMP_Text Interaction;
        public TMP_Text Waypoint;
        public TMP_Text Toast;
        public TMP_Text VictoryResult;
        public TMP_Text BriefingTargets;
        public Image RestorationFill;
        public Image FireFill;
        public GameObject FireProgress;
        public RectTransform PlayerDot;
        public RectTransform TargetDot;
        public Button Start;
        public Button Begin;
        public Button OpenControls;
        public Button ControlsBack;
        public Button BriefingBack;
        public Button Resume;
        public Button PauseMenu;
        public Button Replay;
        public Button VictoryMenu;
        public Button[] Quit;
        public Slider Volume;
    }
}
