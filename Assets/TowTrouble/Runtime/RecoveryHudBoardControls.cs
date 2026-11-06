using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace TowTrouble
{
    public sealed partial class RecoveryHud
    {
        private readonly Dictionary<int, UnityEngine.UI.Button> boardCards = new Dictionary<int, UnityEngine.UI.Button>();
        private List<int> boardEntries = new List<int>();
        private readonly List<BoardHint> boardHints = new List<BoardHint>();
        private TMP_Text boardSelectionHint;
        private UnityEngine.UI.ScrollRect boardScroll;
        private UnityEngine.UI.Button[] boardDialogButtons;
        private EventSystem boardEventSystem;
        private bool boardOwnsNavigation, boardPreviousNavigation, boardSubmitRelease;
        private int boardOpenFrame, boardMoveDirection, boardDialogIndex;
        private float boardNextMove;

        private sealed class BoardHint
        {
            public UnityEngine.UI.Button Button;
            public RectTransform Cap;
            public TMP_Text Key, Action;
            public int Kind; // 0 previous tab, 1 next tab, 2 primary, 3 back
            public bool Right;
        }

        private void SetBoardNavigation(bool enabled)
        {
            if (enabled && !boardOwnsNavigation && EventSystem.current != null)
            {
                boardEventSystem = EventSystem.current;
                boardPreviousNavigation = boardEventSystem.sendNavigationEvents;
                boardEventSystem.sendNavigationEvents = false;
                boardOwnsNavigation = true;
            }
            else if (!enabled && boardOwnsNavigation)
            {
                if (boardEventSystem != null) boardEventSystem.sendNavigationEvents = boardPreviousNavigation;
                boardEventSystem = null; boardOwnsNavigation = false;
            }
        }

        private void BeginBoardPage(bool entering)
        {
            SetBoardNavigation(true);
            if (entering)
            { boardOpenFrame = Time.frameCount; boardSubmitRelease = true; boardMoveDirection = 0; }
            boardCards.Clear(); boardHints.Clear(); boardScroll = null;
        }

        private void FocusBoardRecovery()
        {
            if (EventSystem.current == null || !boardCards.TryGetValue(boardIndex, out var card) || card == null) return;
            if (EventSystem.current.currentSelectedGameObject != card.gameObject)
                EventSystem.current.SetSelectedGameObject(card.gameObject);
        }

        private void SelectBoardRecovery(int id)
        {
            float speed = boardScroll is RecoverySmoothScrollRect previous ? previous.ScrollSpeed : 0;
            RememberBoardScroll(); boardIndex = id; Show("Board");
            if (boardScroll is RecoverySmoothScrollRect current) current.ScrollSpeed = speed;
        }
        private void RememberBoardScroll()
        { if (boardScroll != null) boardCompletedScrollY = boardScroll.content.anchoredPosition.y; }

        private bool CanUseBoardPrimary()
        {
            var world = session.World;
            if (!session.IsHost) return false;
            if (world.HasContract) return true;
            return boardIndex >= 0 && boardIndex < world.AvailableContracts.Length &&
                world.Phase == RecoveryPhase.Explore && !world.CrewInGarage;
        }
        private void BoardPrimaryAction()
        {
            if (!CanUseBoardPrimary()) return;
            session.ConsumeMenuInput();
            if (session.World.HasContract)
                Confirm("Abandon \"" + session.World.Contract.TitleFor(P) + "\" without payment?",
                    () => { session.ConsumeMenuInput(); session.FinishExplorationContract(); Show("Gameplay"); }, "Board");
            else session.AcceptBoardContract(boardIndex);
        }
        private void CloseBoard()
        { session.ConsumeMenuInput(); session.CloseOverlay(); Show("Gameplay"); }

        private void ConfigureBoardDialog()
        {
            SetBoardNavigation(true); boardSubmitRelease = true; boardMoveDirection = 0; boardDialogIndex = 0;
            boardHints.Clear();
            boardDialogButtons = pageRoot.GetComponentsInChildren<UnityEngine.UI.Button>();
            if (boardDialogButtons.Length > 0) AddBoardHint(boardDialogButtons[0], 2, false);
            if (boardDialogButtons.Length > 1) AddBoardHint(boardDialogButtons[1], 3, false);
            RefreshBoardHints();
        }
        private void FocusBoardDialog()
        {
            if (EventSystem.current == null || boardDialogButtons == null || boardDialogButtons.Length == 0) return;
            var button = boardDialogButtons[Mathf.Clamp(boardDialogIndex, 0, boardDialogButtons.Length - 1)];
            if (button != null && EventSystem.current.currentSelectedGameObject != button.gameObject)
                EventSystem.current.SetSelectedGameObject(button.gameObject);
        }

        // Board input owns navigation/submit; pointer clicks and scroll events still use uGUI.
        private bool HandleBoardControls(bool online)
        {
            bool dialog = page == "Confirm" && confirmationReturn == "Board";
            if (!online || page != "Board" && !dialog) { SetBoardNavigation(false); return false; }
            SetBoardNavigation(true);
            RefreshBoardHints();
            var keyboard = Keyboard.current; var pad = RecoveryInputDevices.Pad;
            bool cancel = keyboard != null && keyboard.escapeKey.wasPressedThisFrame ||
                RecoveryInputPrompts.Cancel(pad)?.wasPressedThisFrame == true;
            if (cancel)
            {
                session.ConsumeMenuInput();
                if (dialog) Show("Board");
                else CloseBoard();
                return true;
            }
            if (page == "Board")
            {
                if (boardRenderedCompletedMask != P.completedMask) Show("Board");
                if (startButton != null) startButton.interactable = CanUseBoardPrimary();
                FocusBoardRecovery();
                if (Time.frameCount == boardOpenFrame) return true;
                if (keyboard != null && keyboard.tabKey.wasPressedThisFrame && P.ReadKey(P.boardKey, Key.Tab) == Key.Tab ||
                    pad != null && pad.leftStickButton.wasPressedThisFrame)
                { CloseBoard(); return true; }
                bool previous = keyboard != null && keyboard.qKey.wasPressedThisFrame || pad != null && pad.leftShoulder.wasPressedThisFrame;
                bool next = keyboard != null && keyboard.eKey.wasPressedThisFrame || pad != null && pad.rightShoulder.wasPressedThisFrame;
                if (previous != next)
                { RememberBoardScroll(); SelectBoardTab(next); boardMoveDirection = 0; return true; }
            }

            int direction = BoardMoveDirection(keyboard, pad, dialog);
            if (direction == 0) boardMoveDirection = 0;
            else if (direction != boardMoveDirection || Time.unscaledTime >= boardNextMove)
            {
                bool first = direction != boardMoveDirection;
                boardMoveDirection = direction; boardNextMove = Time.unscaledTime + (first ? .35f : .12f);
                if (dialog)
                { boardDialogIndex = Mathf.Clamp(boardDialogIndex + direction, 0, boardDialogButtons.Length - 1); FocusBoardDialog(); }
                else if (boardEntries.Count > 0)
                {
                    int current = Mathf.Max(0, boardEntries.IndexOf(boardIndex));
                    int next = Mathf.Clamp(current + direction, 0, boardEntries.Count - 1);
                    if (next != current) SelectBoardRecovery(boardEntries[next]);
                }
            }
            if (dialog) FocusBoardDialog();
            bool held = keyboard != null && (keyboard.enterKey.isPressed || keyboard.numpadEnterKey.isPressed) ||
                RecoveryInputPrompts.Confirm(pad)?.isPressed == true;
            bool pressed = keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame) ||
                RecoveryInputPrompts.Confirm(pad)?.wasPressedThisFrame == true;
            if (boardSubmitRelease) { if (!held) boardSubmitRelease = false; }
            else if (pressed)
            {
                boardSubmitRelease = true;
                session.ConsumeMenuInput();
                if (dialog) boardDialogButtons[boardDialogIndex].onClick.Invoke();
                else BoardPrimaryAction();
            }
            return true;
        }

        private static int BoardMoveDirection(Keyboard keyboard, Gamepad pad, bool dialog)
        {
            bool up = keyboard != null && (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed || dialog && keyboard.leftArrowKey.isPressed);
            bool down = keyboard != null && (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed || dialog && keyboard.rightArrowKey.isPressed);
            if (!up && !down && pad != null)
            {
                up = pad.dpad.up.isPressed || pad.leftStick.y.ReadValue() > .55f || dialog && (pad.dpad.left.isPressed || pad.leftStick.x.ReadValue() < -.55f);
                down = pad.dpad.down.isPressed || pad.leftStick.y.ReadValue() < -.55f || dialog && (pad.dpad.right.isPressed || pad.leftStick.x.ReadValue() > .55f);
            }
            return up == down ? 0 : up ? -1 : 1;
        }

        private void AddBoardHint(UnityEngine.UI.Button button, int kind, bool right)
        {
            var navigation = button.navigation; navigation.mode = UnityEngine.UI.Navigation.Mode.None; button.navigation = navigation;
            foreach (var icon in button.GetComponentsInChildren<RecoveryIconGraphic>()) icon.gameObject.SetActive(false);
            var action = button.GetComponentInChildren<TMP_Text>();
            var cap = Panel(button.transform, 8, 4, 26, 22, Paper);
            cap.name = "Control hint";
            var text = Text(cap, "", 0, 0, 26, 22, 13, Ink, true); text.alignment = TextAlignmentOptions.Center;
            boardHints.Add(new BoardHint { Button = button, Cap = cap, Key = text, Action = action, Kind = kind, Right = right });
        }
        private void RefreshBoardHints()
        {
            bool gamepad = session.UsingGamepad;
            var family = RecoveryInputPrompts.Family(RecoveryInputDevices.Pad);
            foreach (var hint in boardHints)
            {
                string key = hint.Kind == 0 ? gamepad ? RecoveryInputPrompts.PreviousTabLabel(family) : "Q"
                    : hint.Kind == 1 ? gamepad ? RecoveryInputPrompts.NextTabLabel(family) : "E"
                    : hint.Kind == 2 ? gamepad ? RecoveryInputPrompts.ConfirmLabel(family) : "Enter"
                    : gamepad ? RecoveryInputPrompts.CancelLabel(family) : "Esc";
                if (hint.Key.text == key) continue;
                hint.Key.text = key;
                var rect = (RectTransform)hint.Button.transform;
                float width = Mathf.Max(26f, Mathf.Ceil(hint.Key.GetPreferredValues(key).x) + 12f);
                float height = Mathf.Min(30f, rect.rect.height - 8f);
                bool gold = hint.Button.GetComponentInChildren<RecoveryPlateGraphic>().Style == RecoveryPlateStyle.Gold;
                hint.Cap.GetComponent<UnityEngine.UI.Image>().color = gold ? Ink : Paper;
                hint.Key.color = gold ? Paper : Ink;
                hint.Cap.anchoredPosition = new Vector2(hint.Right ? rect.rect.width - width - 8f : 12f, -(rect.rect.height - height) * .5f);
                hint.Cap.sizeDelta = hint.Key.rectTransform.sizeDelta = new Vector2(width, height);
                float left = hint.Right ? 16f : width + 24f;
                hint.Action.rectTransform.anchoredPosition = new Vector2(left, 0);
                hint.Action.rectTransform.sizeDelta = new Vector2(rect.rect.width - left - (hint.Right ? width + 20f : 14f), rect.rect.height);
            }
            if (boardSelectionHint != null) boardSelectionHint.text = gamepad ? "D-pad / Left stick · Select" : "W / S or Up / Down · Select";
        }
    }
}
