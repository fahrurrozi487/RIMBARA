using Rimbara.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Rimbara.Editor
{
    internal static class HudFactory
    {
        private static readonly Color Ink = new Color(0.045f, 0.10f, 0.08f, 0.94f);
        private static readonly Color Light = new Color(0.90f, 0.95f, 0.88f);
        private static readonly Color Muted = new Color(0.62f, 0.76f, 0.65f);
        private static readonly Color Green = new Color(0.42f, 0.83f, 0.52f);
        private static readonly Color Amber = new Color(0.98f, 0.74f, 0.36f);
        private static TMP_FontAsset font;
        private static Sprite rounded;
        private static Sprite circle;

        public static HudReferences Create(TMP_FontAsset typeface, Sprite roundedSprite, Sprite circleSprite)
        {
            font = typeface;
            rounded = roundedSprite;
            circle = circleSprite;
            var references = new HudReferences();
            GameObject canvasObject = new GameObject("UI - RIMBARA", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.layer = 5;
            references.Canvas = canvasObject.GetComponent<Canvas>();
            references.Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600f, 900f);
            scaler.matchWidthOrHeight = 0.5f;
            GameObject events = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            references.Gameplay = Layer(canvasObject.transform, "Gameplay HUD");
            Transform hud = references.Gameplay.transform;

            Image mission = Panel(hud, "Mission card", new Vector2(-541f, 331f), new Vector2(470f, 190f), Ink);
            references.MissionNumber = Label(mission.transform, "Mission number", new Vector2(22f, -17f), new Vector2(425f, 20f), "FIELD ASSIGNMENT    01 / 04", 12f, Muted);
            references.MissionNumber.characterSpacing = 1.8f;
            references.MissionTitle = Label(mission.transform, "Mission title", new Vector2(22f, -45f), new Vector2(425f, 40f), "FIRST STEPS", 29f, Green, FontStyles.Bold);
            references.MissionDescription = Label(mission.transform, "Mission description", new Vector2(22f, -94f), new Vector2(425f, 78f), "Inspect the forest.", 18f, Light);

            Image counters = Panel(hud, "Restoration counters", new Vector2(614f, 328f), new Vector2(324f, 196f), Ink);
            references.Seedlings = Label(counters.transform, "Seedling counter", new Vector2(20f, -19f), new Vector2(290f, 45f), "SEEDLINGS   0 / 5", 16f, Light);
            references.Trees = Label(counters.transform, "Tree counter", new Vector2(20f, -67f), new Vector2(286f, 26f), "TREES PLANTED   0 / 5", 17f, Light);
            references.Fires = Label(counters.transform, "Fire counter", new Vector2(20f, -101f), new Vector2(286f, 26f), "FIRES OUT   0 / 2", 17f, Light);
            references.Restoration = Label(counters.transform, "Restoration percent", new Vector2(20f, -143f), new Vector2(286f, 22f), "0% RESTORED", 12f, Green, FontStyles.Bold);
            Image progressBack = ChildPanel(counters.transform, "Restoration bar background", new Vector2(20f, -171f), new Vector2(284f, 7f), new Color(0.16f, 0.25f, 0.19f));
            references.RestorationFill = Fill(progressBack.transform, "Restoration bar fill", Green);

            Image waypoint = Panel(hud, "Next destination", new Vector2(0f, 407f), new Vector2(490f, 40f), new Color(0.045f, 0.10f, 0.08f, 0.82f));
            references.Waypoint = Label(waypoint.transform, "Waypoint instruction", new Vector2(12f, -7f), new Vector2(466f, 27f), "INSPECT THE FOREST", 14f, Amber, FontStyles.Bold);
            references.Waypoint.alignment = TextAlignmentOptions.Center;
            references.Waypoint.characterSpacing = 0.8f;

            Image action = Panel(hud, "Interaction prompt", new Vector2(0f, -357f), new Vector2(760f, 52f), Ink);
            references.Interaction = Label(action.transform, "Interaction text", new Vector2(20f, -12f), new Vector2(720f, 30f), "[E]  Collect native seedling", 20f, Light, FontStyles.Bold);
            references.Interaction.alignment = TextAlignmentOptions.Center;
            Image toast = Panel(hud, "Feedback toast", new Vector2(0f, -285f), new Vector2(830f, 58f), new Color(0.07f, 0.18f, 0.11f, 0.95f));
            references.Toast = Label(toast.transform, "Feedback text", new Vector2(22f, -12f), new Vector2(786f, 37f), "", 18f, Light);
            references.Toast.alignment = TextAlignmentOptions.Center;
            toast.gameObject.SetActive(false);
            Image controlsPill = Panel(hud, "Controls reminder", new Vector2(-476f, -414f), new Vector2(600f, 31f), new Color(0.045f, 0.10f, 0.08f, 0.84f));
            Label(controlsPill.transform, "Controls text", new Vector2(13f, -6f), new Vector2(575f, 22f), "WASD move   /   SHIFT run   /   E interact   /   LEFT MOUSE water   /   ESC pause", 12f, Muted);

            Image map = Panel(hud, "Arunika field map", new Vector2(681f, -315f), new Vector2(190f, 220f), Ink);
            TMP_Text mapTitle = Label(map.transform, "Map title", new Vector2(16f, -13f), new Vector2(158f, 20f), "ARUNIKA / FIELD MAP", 11f, Muted, FontStyles.Bold);
            mapTitle.characterSpacing = 1f;
            RectTransform mapArea = Point(map.transform, "Map coordinates", new Vector2(18f, -43f), new Vector2(154f, 165f));
            mapArea.gameObject.AddComponent<RectMask2D>();
            MapCircle(mapArea, "Zone A", new Vector2(0f, -57f), 47f, new Color(0.50f, 0.38f, 0.21f, 0.30f));
            MapCircle(mapArea, "Zone B", new Vector2(0f, -4f), 57f, new Color(0.30f, 0.50f, 0.28f, 0.30f));
            MapCircle(mapArea, "Zone C", new Vector2(0f, 54f), 48f, new Color(0.61f, 0.31f, 0.20f, 0.30f));
            Image line = Panel(mapArea, "Trail", new Vector2(0f, 0f), new Vector2(2f, 131f), new Color(0.71f, 0.72f, 0.56f, 0.28f));
            line.sprite = null;
            CenterLabel(mapArea, "A label", new Vector2(33f, -57f), "A", Muted);
            CenterLabel(mapArea, "B label", new Vector2(37f, -4f), "B", Muted);
            CenterLabel(mapArea, "C label", new Vector2(33f, 54f), "C", Amber);
            references.TargetDot = MapCircle(mapArea, "Destination dot", Vector2.zero, 10f, Amber).rectTransform;
            references.PlayerDot = MapCircle(mapArea, "Volunteer dot", Vector2.zero, 9f, Green).rectTransform;

            Panel(hud, "Crosshair horizontal", Vector2.zero, new Vector2(12f, 2f), new Color(1f, 1f, 1f, 0.9f)).sprite = null;
            Panel(hud, "Crosshair vertical", Vector2.zero, new Vector2(2f, 12f), new Color(1f, 1f, 1f, 0.9f)).sprite = null;
            references.FireProgress = Panel(hud, "Water contact progress", new Vector2(0f, -27f), new Vector2(156f, 7f), new Color(0.06f, 0.1f, 0.08f, 0.85f)).gameObject;
            references.FireFill = Fill(references.FireProgress.transform, "Water contact fill", new Color(0.43f, 0.80f, 0.96f));

            references.Dimmer = Layer(canvasObject.transform, "Menu dimmer");
            var dim = references.Dimmer.AddComponent<Image>();
            dim.color = new Color(0.015f, 0.04f, 0.035f, 0.40f);
            dim.raycastTarget = true;

            references.Menu = Layer(canvasObject.transform, "Main menu");
            Image menu = Panel(references.Menu.transform, "Main menu card", new Vector2(-414f, 8f), new Vector2(660f, 672f), Ink);
            TMP_Text eyebrow = Label(menu.transform, "Menu eyebrow", new Vector2(38f, -34f), new Vector2(584f, 24f), "ARUNIKA / VOLUNTEER RESTORATION PROGRAM", 12f, Muted, FontStyles.Bold);
            eyebrow.characterSpacing = 2f;
            TMP_Text logo = Label(menu.transform, "RIMBARA title", new Vector2(34f, -78f), new Vector2(594f, 99f), "RIMBARA", 78f, Green, FontStyles.Bold);
            logo.characterSpacing = 5f;
            Label(menu.transform, "Game subtitle", new Vector2(38f, -178f), new Vector2(584f, 30f), "RISE OF THE FORGOTTEN FOREST", 17f, Light, FontStyles.Bold).characterSpacing = 2f;
            Label(menu.transform, "Menu story", new Vector2(38f, -243f), new Vector2(584f, 115f), "A forest scarred by fire.\nA young volunteer. A chance to begin again.\n\nCollect native seedlings, plant new roots, and let Arunika breathe.", 21f, Light);
            Label(menu.transform, "Menu scope", new Vector2(38f, -382f), new Vector2(584f, 27f), "3 ZONES     /     4 MISSIONS     /     ONE NEW BEGINNING", 13f, Amber, FontStyles.Bold).characterSpacing = 1f;
            references.Start = Button(menu.transform, "Start restoration", new Vector2(38f, -435f), new Vector2(584f, 62f), "BEGIN RESTORATION", true);
            references.OpenControls = Button(menu.transform, "Controls button", new Vector2(38f, -512f), new Vector2(282f, 52f), "HOW TO PLAY", false);
            Button menuQuit = Button(menu.transform, "Menu quit", new Vector2(340f, -512f), new Vector2(282f, 52f), "QUIT", false);
            Label(menu.transform, "Menu footer", new Vector2(38f, -604f), new Vector2(584f, 38f), "A small act of care can change what grows next.\nSOLO MVP  /  RIMBARA", 12f, Muted);

            references.Briefing = Layer(canvasObject.transform, "Briefing screen");
            Image briefing = Panel(references.Briefing.transform, "Volunteer briefing", Vector2.zero, new Vector2(820f, 660f), Ink);
            Label(briefing.transform, "Briefing eyebrow", new Vector2(46f, -34f), new Vector2(728f, 25f), "VOLUNTEER POST / ARUNIKA FOREST", 13f, Amber, FontStyles.Bold).characterSpacing = 2f;
            Label(briefing.transform, "Briefing title", new Vector2(46f, -80f), new Vector2(728f, 67f), "YOUR FIRST DAY", 48f, Green, FontStyles.Bold);
            Label(briefing.transform, "Briefing story", new Vector2(46f, -163f), new Vector2(728f, 99f), "The fire has passed, but the forest still needs you. From this volunteer post, follow the amber marker to inspect the damage. Then bring native trees back to the planting area and put out the last flames.", 22f, Light);
            Label(briefing.transform, "Briefing steps", new Vector2(46f, -295f), new Vector2(728f, 130f), "01   FIRST STEPS  /  Inspect the forest\n02   SEEDS OF HOPE  /  Collect and plant\n03   STOP THE FIRE  /  Aim and hold left mouse\n04   A NEW BEGINNING  /  Return to this post", 20f, Light);
            references.BriefingTargets = Label(briefing.transform, "Briefing targets", new Vector2(46f, -451f), new Vector2(728f, 30f), "5 NATIVE SEEDLINGS / 5 TREES / 2 FIRES", 15f, Amber, FontStyles.Bold);
            Label(briefing.transform, "No fail promise", new Vector2(46f, -493f), new Vector2(728f, 32f), "No timer. No damage. No water limit. Take the time you need.", 17f, Muted);
            references.Begin = Button(briefing.transform, "Enter Arunika", new Vector2(46f, -555f), new Vector2(546f, 59f), "ENTER ARUNIKA", true);
            references.BriefingBack = Button(briefing.transform, "Briefing back", new Vector2(610f, -555f), new Vector2(164f, 59f), "BACK", false);

            references.Controls = Layer(canvasObject.transform, "Controls screen");
            Image help = Panel(references.Controls.transform, "How to play card", Vector2.zero, new Vector2(760f, 656f), Ink);
            Label(help.transform, "Help eyebrow", new Vector2(45f, -34f), new Vector2(670f, 25f), "FIELD GUIDE", 13f, Amber, FontStyles.Bold);
            Label(help.transform, "Help title", new Vector2(45f, -79f), new Vector2(670f, 58f), "HOW TO PLAY", 45f, Green, FontStyles.Bold);
            Label(help.transform, "Control list", new Vector2(45f, -173f), new Vector2(670f, 240f), "WASD                      Move through the forest\nMOUSE                   Turn the third-person camera\nSHIFT                      Run\nE                              Collect seedlings / plant trees / read the board\nLEFT MOUSE          Hold to spray water at the crosshair\nESC                          Pause or resume", 21f, Light);
            Label(help.transform, "Help tip", new Vector2(45f, -435f), new Vector2(670f, 80f), "Follow the amber guide and the field map. Green markers show seedlings and planting beds. Move within reach to interact; approach each fire before spraying.", 19f, Muted);
            references.ControlsBack = Button(help.transform, "Help back", new Vector2(45f, -555f), new Vector2(670f, 58f), "BACK TO MENU", true);

            references.Pause = Layer(canvasObject.transform, "Pause screen");
            Image pause = Panel(references.Pause.transform, "Pause card", Vector2.zero, new Vector2(510f, 485f), Ink);
            Label(pause.transform, "Pause eyebrow", new Vector2(38f, -30f), new Vector2(434f, 26f), "TAKE A BREATH", 13f, Amber, FontStyles.Bold);
            Label(pause.transform, "Pause title", new Vector2(38f, -76f), new Vector2(434f, 61f), "FIELD PAUSE", 41f, Green, FontStyles.Bold);
            Label(pause.transform, "Pause description", new Vector2(38f, -152f), new Vector2(434f, 42f), "Your mission is safe. Resume when you are ready.", 19f, Light);
            Label(pause.transform, "Volume label", new Vector2(38f, -217f), new Vector2(434f, 24f), "MASTER VOLUME", 12f, Muted, FontStyles.Bold);
            references.Volume = Slider(pause.transform, new Vector2(38f, -258f), new Vector2(434f, 25f));
            references.Resume = Button(pause.transform, "Resume mission", new Vector2(38f, -318f), new Vector2(434f, 58f), "RESUME MISSION", true);
            references.PauseMenu = Button(pause.transform, "Pause return menu", new Vector2(38f, -391f), new Vector2(266f, 51f), "RETURN TO MENU", false);
            Button pauseQuit = Button(pause.transform, "Pause quit", new Vector2(322f, -391f), new Vector2(150f, 51f), "QUIT", false);

            references.Victory = Layer(canvasObject.transform, "Ending screen");
            Image victory = Panel(references.Victory.transform, "Restoration ending", Vector2.zero, new Vector2(800f, 632f), Ink);
            Label(victory.transform, "Victory eyebrow", new Vector2(46f, -34f), new Vector2(708f, 26f), "A NEW BEGINNING / MISSION COMPLETE", 14f, Amber, FontStyles.Bold).characterSpacing = 1.5f;
            Label(victory.transform, "Victory title", new Vector2(46f, -86f), new Vector2(708f, 78f), "ARUNIKA RESTORED", 48f, Green, FontStyles.Bold);
            Label(victory.transform, "Victory story", new Vector2(46f, -189f), new Vector2(708f, 96f), "New roots hold the soil. The last flames are silent.\nWhat began as a small act of care has become a new beginning for this forgotten forest.", 23f, Light);
            references.VictoryResult = Label(victory.transform, "Mission results", new Vector2(46f, -329f), new Vector2(708f, 142f), "5 trees planted / 2 fires extinguished", 22f, Light);
            references.Replay = Button(victory.transform, "Play again", new Vector2(46f, -524f), new Vector2(320f, 58f), "RESTORE AGAIN", true);
            references.VictoryMenu = Button(victory.transform, "Victory return menu", new Vector2(384f, -524f), new Vector2(210f, 58f), "MENU", false);
            Button victoryQuit = Button(victory.transform, "Victory quit", new Vector2(612f, -524f), new Vector2(142f, 58f), "QUIT", false);
            references.Quit = new[] { menuQuit, pauseQuit, victoryQuit };
            references.Menu.SetActive(true);
            references.Briefing.SetActive(false);
            references.Controls.SetActive(false);
            references.Pause.SetActive(false);
            references.Victory.SetActive(false);
            references.Gameplay.SetActive(false);
            return references;
        }

        private static GameObject Layer(Transform parent, string name)
        {
            RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.gameObject.layer = 5;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect.gameObject;
        }

        private static RectTransform Point(Transform parent, string name, Vector2 position, Vector2 size, bool centered = false)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.gameObject.layer = 5;
            rect.anchorMin = rect.anchorMax = centered ? new Vector2(0.5f, 0.5f) : new Vector2(0f, 1f);
            rect.pivot = centered ? new Vector2(0.5f, 0.5f) : new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        private static Image Panel(Transform parent, string name, Vector2 center, Vector2 size, Color color)
        {
            RectTransform rect = Point(parent, name, center, size, true);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = rounded;
            image.type = Image.Type.Sliced;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static Image ChildPanel(Transform parent, string name, Vector2 position, Vector2 size, Color color)
        {
            RectTransform rect = Point(parent, name, position, size);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = rounded;
            image.type = Image.Type.Sliced;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static Image Fill(Transform parent, string name, Color color)
        {
            GameObject layer = Layer(parent, name);
            Image fill = layer.AddComponent<Image>();
            fill.sprite = rounded;
            fill.color = color;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillAmount = 0f;
            fill.raycastTarget = false;
            return fill;
        }

        private static TMP_Text Label(Transform parent, string name, Vector2 position, Vector2 size, string text, float fontSize,
            Color color, FontStyles style = FontStyles.Normal)
        {
            var label = Point(parent, name, position, size).gameObject.AddComponent<TextMeshProUGUI>();
            label.font = font;
            label.fontSize = fontSize;
            label.fontStyle = style;
            label.color = color;
            label.text = text;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.overflowMode = TextOverflowModes.Overflow;
            label.raycastTarget = false;
            label.lineSpacing = 8f;
            return label;
        }

        private static Button Button(Transform parent, string name, Vector2 position, Vector2 size, string text, bool primary)
        {
            Image background = ChildPanel(parent, name, position, size, primary ? Green : new Color(0.15f, 0.25f, 0.19f));
            background.raycastTarget = true;
            Button button = background.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.12f, 1.12f, 1.07f);
            colors.pressedColor = new Color(0.75f, 0.90f, 0.77f);
            colors.selectedColor = Color.white;
            button.colors = colors;
            TMP_Text label = Label(background.transform, name + " label", new Vector2(12f, -10f), size - new Vector2(24f, 20f), text, 18f,
                primary ? new Color(0.04f, 0.13f, 0.08f) : Light, FontStyles.Bold);
            label.alignment = TextAlignmentOptions.Center;
            label.characterSpacing = 0.8f;
            return button;
        }

        private static Slider Slider(Transform parent, Vector2 position, Vector2 size)
        {
            RectTransform root = Point(parent, "Volume slider", position, size);
            Slider slider = root.gameObject.AddComponent<Slider>();
            Image back = Panel(root, "Volume background", Vector2.zero, new Vector2(size.x, 7f), new Color(0.21f, 0.33f, 0.25f));
            RectTransform fillArea = Point(root, "Volume fill area", Vector2.zero, new Vector2(size.x, 7f), true);
            Image filled = Fill(fillArea, "Volume filled", Green);
            RectTransform handleArea = Point(root, "Volume handle area", Vector2.zero, new Vector2(size.x - 22f, 22f), true);
            Image handle = MapCircle(handleArea, "Volume handle", Vector2.zero, 22f, Light);
            // Slider stretches the cross-axis anchors itself. A dedicated slide
            // area and zero cross-axis size keep this handle circular, not oval.
            handle.rectTransform.sizeDelta = new Vector2(22f, 0f);
            slider.fillRect = filled.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            back.raycastTarget = true;
            handle.raycastTarget = true;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0.65f;
            return slider;
        }

        private static Image MapCircle(Transform parent, string name, Vector2 center, float diameter, Color color)
        {
            Image image = Panel(parent, name, center, Vector2.one * diameter, color);
            image.sprite = circle;
            image.type = Image.Type.Simple;
            return image;
        }

        private static void CenterLabel(Transform parent, string name, Vector2 center, string text, Color color)
        {
            var label = Point(parent, name, center, new Vector2(20f, 20f), true).gameObject.AddComponent<TextMeshProUGUI>();
            label.font = font;
            label.fontSize = 12f;
            label.fontStyle = FontStyles.Bold;
            label.text = text;
            label.color = color;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
        }
    }
}
