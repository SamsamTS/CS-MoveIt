using ColossalFramework;
using ColossalFramework.UI;
using MoveIt.Lang;
using System.Reflection;
using UnityEngine;

namespace MoveIt
{
    public class OptionsKeymappingMain : OptionsKeymapping
    {
        private void Awake()
        {
            AddKeymapping(Str.key_ToggleTool, toggleTool);
            AddKeymapping(Str.key_MoveNorth, moveZpos);
            AddKeymapping(Str.key_MoveSouth, moveZneg);
            AddKeymapping(Str.key_MoveEast, moveXpos);
            AddKeymapping(Str.key_MoveWest, moveXneg);
            AddKeymapping(Str.key_MoveUp, moveYpos);
            AddKeymapping(Str.key_MoveDown, moveYneg);
            AddKeymapping(Str.key_RotateCCW, turnNeg);
            AddKeymapping(Str.key_RotateCW, turnPos);
            AddKeymapping(Str.key_ScaleIn, scaleIn);
            AddKeymapping(Str.key_ScaleOut, scaleOut);
            AddKeymapping(Str.key_DeselectAll, deselectAll);
            AddKeymapping(Str.key_Undo, undo);
            AddKeymapping(Str.key_Redo, redo);
            AddKeymapping(Str.key_Clone, clone);
            AddKeymapping(Str.key_Bulldoze, bulldoze);
            AddKeymapping(Str.key_ToggleGridView, viewGrid);
            AddKeymapping(Str.key_ToggleUndergroundView, viewUnderground);
            //AddKeymapping(Str.key_ToggleNodeMerging, mergeNodes);
            AddKeymapping(Str.key_ToggleDebugPanel, viewDebug);
            AddKeymapping(Str.key_StepOver, stepOverKey);
            AddKeymapping(Str.key_ShowSelectors, viewSelectors);
            AddKeymapping(Str.baseUI_Single_Tooltip, selectSingle);
            AddKeymapping(Str.baseUI_Marquee_Tooltip, selectMarquee);
            AddKeymapping(Str.key_QuickUndergroundView, quickUnderground);
        }
    }

    public class OptionsKeymappingToolbox : OptionsKeymapping
    {
        private void Awake()
        {
            AddKeymapping(Str.key_ToolLineUpSpaced, alignLine);
            AddKeymapping(Str.key_ToolLineUpUnspaced, alignLineUnspaced);
            AddKeymapping(Str.key_ToolMirrorObjects, alignMirror);
            AddKeymapping(Str.key_ToolResetObjects, reset);
            AddKeymapping(Str.key_ToolSetPosition, alignMoveTo);
            AddKeymapping(Str.key_ToolRotateRandomly, alignRandom);
            AddKeymapping(Str.key_ToolRotateAtCentre, alignGroup);
            AddKeymapping(Str.key_ToolRotateInPlace, alignInplace);
            AddKeymapping(Str.key_ToolSlopeObjects, alignSlope);
            AddKeymapping(Str.key_ToolQuickSlopeNode, alignSlopeQuick);
            AddKeymapping(Str.key_ToolFullSlope, alignSlopeFull);
            AddKeymapping(Str.key_ToolToTerrainHeight, alignTerrainHeight);
            AddKeymapping(Str.key_ToolToObjectHeight, alignHeights);
        }
    }

    public class OptionsKeymappingPO : OptionsKeymapping
    {
        private void Awake()
        {
            AddKeymapping("  " + Str.key_TogglePO, activatePO);
            AddKeymapping("  " + Str.key_ConvertToPO, convertToPO);
        }
    }

    public class OptionsKeymapping : UICustomControl
    {
        protected static readonly string kKeyBindingTemplate = "KeyBindingTemplate";

        protected SavedInputKey m_EditingBinding;

        protected string m_EditingBindingCategory;

        public static readonly SavedInputKey toggleTool = new SavedInputKey("toggleTool", Settings.settingsFileName, SavedInputKey.Encode(KeyCode.M, false, false, false), true);

        public static readonly SavedInputKey moveXpos = new SavedInputKey("moveXpos", Settings.settingsFileName, SavedInputKey.Encode(KeyCode.RightArrow, false, false, false), true);
        public static readonly SavedInputKey moveXneg = new SavedInputKey("moveXneg", Settings.settingsFileName, SavedInputKey.Encode(KeyCode.LeftArrow, false, false, false), true);

        public static readonly SavedInputKey moveZpos = new SavedInputKey("moveZpos", Settings.settingsFileName, SavedInputKey.Encode(KeyCode.UpArrow, false, false, false), true);
        public static readonly SavedInputKey moveZneg = new SavedInputKey("moveZneg", Settings.settingsFileName, SavedInputKey.Encode(KeyCode.DownArrow, false, false, false), true);

        public static readonly SavedInputKey moveYpos = new SavedInputKey("moveYpos", Settings.settingsFileName, SavedInputKey.Encode(KeyCode.PageUp, false, false, false), true);
        public static readonly SavedInputKey moveYneg = new SavedInputKey("moveYneg", Settings.settingsFileName, SavedInputKey.Encode(KeyCode.PageDown, false, false, false), true);

        public static readonly SavedInputKey turnNeg = new SavedInputKey("turnNeg", Settings.settingsFileName, SavedInputKey.Encode(KeyCode.LeftArrow, true, false, false), true);
        public static readonly SavedInputKey turnPos = new SavedInputKey("turnPos", Settings.settingsFileName, SavedInputKey.Encode(KeyCode.RightArrow, true, false, false), true);

        public static readonly SavedInputKey scaleOut = new SavedInputKey("scaleOut", Settings.settingsFileName, SavedInputKey.Encode(KeyCode.Equals, false, false, false), true);
        public static readonly SavedInputKey scaleIn = new SavedInputKey("scaleIn", Settings.settingsFileName, SavedInputKey.Encode(KeyCode.Minus, false, false, false), true);

        public static readonly SavedInputKey selectSingle = new SavedInputKey("selectSingle", Settings.settingsFileName, SavedInputKey.Encode(KeyCode.None, false, false, false), true);
        public static readonly SavedInputKey selectMarquee = new SavedInputKey("selectMarquee", Settings.settingsFileName, SavedInputKey.Encode(KeyCode.None, false, false, false), true);
        public static readonly SavedInputKey deselectAll = new SavedInputKey("deselectAll", Settings.settingsFileName, SavedInputKey.Encode(KeyCode.D, false, false, true), true);
        public static readonly SavedInputKey undo = new SavedInputKey("undo", Settings.settingsFileName, SavedInputKey.Encode(KeyCode.Z, true, false, false), true);
        public static readonly SavedInputKey redo = new SavedInputKey("redo", Settings.settingsFileName, SavedInputKey.Encode(KeyCode.Y, true, false, false), true);

        public static readonly SavedInputKey clone = new SavedInputKey("copy", Settings.settingsFileName, SavedInputKey.Encode(KeyCode.C, true, false, false), true);
        public static readonly SavedInputKey bulldoze = new SavedInputKey("bulldoze", Settings.settingsFileName, SavedInputKey.Encode(KeyCode.B, true, false, false), true);
        
        public static readonly SavedInputKey viewGrid = new SavedInputKey("viewGrid", Settings.settingsFileName, SavedInputKey.Encode(KeyCode.None, false, false, false), true); 
        public static readonly SavedInputKey viewUnderground = new SavedInputKey("viewUnderground", Settings.settingsFileName, SavedInputKey.Encode(KeyCode.None, false, false, false), true);
        //public static readonly SavedInputKey mergeNodes = new SavedInputKey("mergeNodes", Settings.settingsFileName, SavedInputKey.Encode(KeyCode.None, false, false, false), true);
        public static readonly SavedInputKey viewDebug = new SavedInputKey("viewDebug", Settings.settingsFileName, SavedInputKey.Encode(KeyCode.None, false, false, false), true);
        public static readonly SavedInputKey viewSelectors = new SavedInputKey("viewSelectors", Settings.settingsFileName, SavedInputKey.Encode(KeyCode.None, false, false, false), true);
        public static readonly SavedInputKey quickUnderground = new SavedInputKey("quickUnderground", Settings.settingsFileName, SavedInputKey.Encode(KeyCode.U, false, false, false), true);

        public static readonly SavedInputKey activatePO = new SavedInputKey("activatePO", Settings.settingsFileName, SavedInputKey.Encode(KeyCode.None, false, false, false), true);
        public static readonly SavedInputKey convertToPO = new SavedInputKey("convertToPO", Settings.settingsFileName, SavedInputKey.Encode(KeyCode.P, false, true, false), true);

        public static readonly SavedInputKey stepOverKey = new SavedInputKey("stepOverKey", Settings.settingsFileName, SavedInputKey.Encode(KeyCode.Tab, true, false, false), true);

        public static readonly SavedInputKey alignLine = new SavedInputKey("alignLine", Settings.settingsFileName, SavedInputKey.Encode(KeyCode.None, false, false, false), true);
        public static readonly SavedInputKey alignLineUnspaced = new SavedInputKey("alignLineUnspaced", Settings.settingsFileName, SavedInputKey.Encode(KeyCode.None, false, false, false), true);
        public static readonly SavedInputKey alignMirror = new SavedInputKey("alignMirror", Settings.settingsFileName, SavedInputKey.Encode(KeyCode.None, false, false, false), true);
        public static readonly SavedInputKey reset = new SavedInputKey("reset", Settings.settingsFileName, SavedInputKey.Encode(KeyCode.None, false, false, false), true);
        public static readonly SavedInputKey alignMoveTo = new SavedInputKey("alignMoveTo", Settings.settingsFileName, SavedInputKey.Encode(KeyCode.None, false, false, false), true);
        public static readonly SavedInputKey alignRandom = new SavedInputKey("alignRandom", Settings.settingsFileName, SavedInputKey.Encode(KeyCode.None, false, false, false), true);
        public static readonly SavedInputKey alignGroup = new SavedInputKey("alignGroup", Settings.settingsFileName, SavedInputKey.Encode(KeyCode.None, false, false, false), true);
        public static readonly SavedInputKey alignInplace = new SavedInputKey("alignInplace", Settings.settingsFileName, SavedInputKey.Encode(KeyCode.None, false, false, false), true);
        public static readonly SavedInputKey alignSlope = new SavedInputKey("alignSlope", Settings.settingsFileName, SavedInputKey.Encode(KeyCode.None, false, false, false), true);
        public static readonly SavedInputKey alignSlopeQuick = new SavedInputKey("alignSlopeQuick", Settings.settingsFileName, SavedInputKey.Encode(KeyCode.None, false, false, false), true);
        public static readonly SavedInputKey alignSlopeFull = new SavedInputKey("alignSlopeFull", Settings.settingsFileName, SavedInputKey.Encode(KeyCode.None, false, false, false), true);
        public static readonly SavedInputKey alignTerrainHeight = new SavedInputKey("alignTerrainHeight", Settings.settingsFileName, SavedInputKey.Encode(KeyCode.None, false, false, false), true);
        public static readonly SavedInputKey alignHeights = new SavedInputKey("alignHeights", Settings.settingsFileName, SavedInputKey.Encode(KeyCode.H, true, false, false), true);

        public static SavedInputKey[] InToolKeysForSelection => new SavedInputKey[] { 
            moveXpos, moveXneg, 
            moveZpos, moveZneg, 
            moveYpos, moveYneg, 
            turnNeg, turnPos, 
            scaleOut, scaleIn, 
            clone, bulldoze, quickUnderground,

            convertToPO,

            alignLine, alignLineUnspaced, alignMirror, reset, alignMoveTo, alignRandom, alignGroup, alignInplace, alignSlope, alignSlopeQuick, alignSlopeFull, alignTerrainHeight, alignHeights,
        };

        public static SavedInputKey[] InToolKeysAlways => new SavedInputKey[] {
            deselectAll, // after de-selecting, then there is no selection and that can cause confusion. therefore we put de-select all here.
            undo, redo,
            viewGrid, viewUnderground, /*mergeNodes, */viewDebug, viewSelectors, quickUnderground,
            activatePO, 
            stepOverKey,
        };

        protected int count = 0;

        protected void AddKeymapping(string label, SavedInputKey savedInputKey)
        {
            UIPanel uIPanel = component.AttachUIComponent(UITemplateManager.GetAsGameObject(kKeyBindingTemplate)) as UIPanel;
            if (count++ % 2 == 1) uIPanel.backgroundSprite = null;

            UILabel uILabel = uIPanel.Find<UILabel>("Name");
            UIButton uIButton = uIPanel.Find<UIButton>("Binding");
            uIButton.eventKeyDown += new KeyPressHandler(this.OnBindingKeyDown);
            uIButton.eventMouseDown += new MouseEventHandler(this.OnBindingMouseDown);

            uILabel.text = label;
            uIButton.text = savedInputKey.ToLocalizedString("KEYNAME");
            uIButton.objectUserData = savedInputKey;
            uIButton.eventVisibilityChanged += ButtonVisibilityChanged;
        }

        protected bool IsModifierKey(KeyCode code)
        {
            return code == KeyCode.LeftControl || code == KeyCode.RightControl || code == KeyCode.LeftShift || code == KeyCode.RightShift || code == KeyCode.LeftAlt || code == KeyCode.RightAlt;
        }

        protected bool IsControlDown()
        {
            return Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        }

        protected bool IsShiftDown()
        {
            return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        }

        protected bool IsAltDown()
        {
            return Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
        }

        protected bool IsUnbindableMouseButton(UIMouseButton code)
        {
            return code == UIMouseButton.Left || code == UIMouseButton.Right;
        }

        protected KeyCode ButtonToKeycode(UIMouseButton button)
        {
            if (button == UIMouseButton.Left)
            {
                return KeyCode.Mouse0;
            }
            if (button == UIMouseButton.Right)
            {
                return KeyCode.Mouse1;
            }
            if (button == UIMouseButton.Middle)
            {
                return KeyCode.Mouse2;
            }
            if (button == UIMouseButton.Special0)
            {
                return KeyCode.Mouse3;
            }
            if (button == UIMouseButton.Special1)
            {
                return KeyCode.Mouse4;
            }
            if (button == UIMouseButton.Special2)
            {
                return KeyCode.Mouse5;
            }
            if (button == UIMouseButton.Special3)
            {
                return KeyCode.Mouse6;
            }
            return KeyCode.None;
        }

        private static void ButtonVisibilityChanged(UIComponent component, bool isVisible) {
            if (isVisible && component.objectUserData is SavedInputKey savedInputKey) {
                (component as UIButton).text = savedInputKey.ToLocalizedString("KEYNAME");
            }
        }

        protected void OnBindingKeyDown(UIComponent comp, UIKeyEventParameter p)
        {
            if (this.m_EditingBinding != null && !this.IsModifierKey(p.keycode))
            {
                p.Use();
                UIView.PopModal();
                KeyCode keycode = p.keycode;
                InputKey inputKey = (p.keycode == KeyCode.Escape) ? this.m_EditingBinding.value : SavedInputKey.Encode(keycode, p.control, p.shift, p.alt);
                if (p.keycode == KeyCode.Backspace)
                {
                    inputKey = SavedInputKey.Empty;
                }
                this.m_EditingBinding.value = inputKey;
                UITextComponent uITextComponent = p.source as UITextComponent;
                uITextComponent.text = this.m_EditingBinding.ToLocalizedString("KEYNAME");
                this.m_EditingBinding = null;
                this.m_EditingBindingCategory = string.Empty;
            }
        }

        protected void OnBindingMouseDown(UIComponent comp, UIMouseEventParameter p)
        {
            if (this.m_EditingBinding == null)
            {
                p.Use();
                this.m_EditingBinding = (SavedInputKey)p.source.objectUserData;
                this.m_EditingBindingCategory = p.source.stringUserData;
                UIButton uIButton = p.source as UIButton;
                uIButton.buttonsMask = (UIMouseButton.Left | UIMouseButton.Right | UIMouseButton.Middle | UIMouseButton.Special0 | UIMouseButton.Special1 | UIMouseButton.Special2 | UIMouseButton.Special3);
                uIButton.text = "Press any key";
                p.source.Focus();
                UIView.PushModal(p.source);
            }
            else if (!this.IsUnbindableMouseButton(p.buttons))
            {
                p.Use();
                UIView.PopModal();
                InputKey inputKey = SavedInputKey.Encode(this.ButtonToKeycode(p.buttons), this.IsControlDown(), this.IsShiftDown(), this.IsAltDown());

                this.m_EditingBinding.value = inputKey;
                UIButton uIButton2 = p.source as UIButton;
                uIButton2.text = this.m_EditingBinding.ToLocalizedString("KEYNAME");
                uIButton2.buttonsMask = UIMouseButton.Left;
                this.m_EditingBinding = null;
                this.m_EditingBindingCategory = string.Empty;
            }
        }

        protected InputKey GetDefaultEntry(string entryName)
        {
            FieldInfo field = typeof(DefaultSettings).GetField(entryName, BindingFlags.Static | BindingFlags.Public);
            if (field == null)
            {
                return 0;
            }
            object value = field.GetValue(null);
            if (value is InputKey)
            {
                return (InputKey)value;
            }
            return 0;
        }

        protected void RefreshKeyMapping()
        {
            foreach (UIComponent current in component.GetComponentsInChildren<UIComponent>())
            {
                UITextComponent uITextComponent = current.Find<UITextComponent>("Binding");
                SavedInputKey savedInputKey = (SavedInputKey)uITextComponent.objectUserData;
                if (this.m_EditingBinding != savedInputKey)
                {
                    uITextComponent.text = savedInputKey.ToLocalizedString("KEYNAME");
                }
            }
        }
    }
}