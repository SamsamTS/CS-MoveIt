using ColossalFramework;
using ColossalFramework.Globalization;
using ColossalFramework.UI;

using System.Reflection;
using UnityEngine;

namespace MoveIt
{
    public class OptionsKeymapping : UICustomControl
    {
        private static readonly string kKeyBindingTemplate = "KeyBindingTemplate";

        private SavedInputKey m_EditingBinding;

        private string m_EditingBindingCategory;

        public static readonly SavedInputKey toggleTool = new SavedInputKey("toggleTool", MoveItTool.settingsFileName, SavedInputKey.Encode(KeyCode.M, false, false, false), true);

        public static readonly SavedInputKey moveXpos = new SavedInputKey("moveXpos", MoveItTool.settingsFileName, SavedInputKey.Encode(KeyCode.RightArrow, false, false, false), true);
        public static readonly SavedInputKey moveXneg = new SavedInputKey("moveXneg", MoveItTool.settingsFileName, SavedInputKey.Encode(KeyCode.LeftArrow, false, false, false), true);

        public static readonly SavedInputKey moveZpos = new SavedInputKey("moveZpos", MoveItTool.settingsFileName, SavedInputKey.Encode(KeyCode.UpArrow, false, false, false), true);
        public static readonly SavedInputKey moveZneg = new SavedInputKey("moveZneg", MoveItTool.settingsFileName, SavedInputKey.Encode(KeyCode.DownArrow, false, false, false), true);

        public static readonly SavedInputKey moveYpos = new SavedInputKey("moveYpos", MoveItTool.settingsFileName, SavedInputKey.Encode(KeyCode.PageUp, false, false, false), true);
        public static readonly SavedInputKey moveYneg = new SavedInputKey("moveYneg", MoveItTool.settingsFileName, SavedInputKey.Encode(KeyCode.PageDown, false, false, false), true);

        public static readonly SavedInputKey turnPos = new SavedInputKey("turnPos", MoveItTool.settingsFileName, SavedInputKey.Encode(KeyCode.LeftArrow, true, false, false), true);
        public static readonly SavedInputKey turnNeg = new SavedInputKey("turnNeg", MoveItTool.settingsFileName, SavedInputKey.Encode(KeyCode.RightArrow, true, false, false), true);

        public static readonly SavedInputKey undo = new SavedInputKey("undo", MoveItTool.settingsFileName, SavedInputKey.Encode(KeyCode.Z, true, false, false), true);
        public static readonly SavedInputKey redo = new SavedInputKey("redo", MoveItTool.settingsFileName, SavedInputKey.Encode(KeyCode.Y, true, false, false), true);

        public static readonly SavedInputKey copy = new SavedInputKey("copy", MoveItTool.settingsFileName, SavedInputKey.Encode(KeyCode.C, true, false, false), true);

        public static readonly SavedInputKey alignHeights = new SavedInputKey("alignHeights", MoveItTool.settingsFileName, SavedInputKey.Encode(KeyCode.H, true, false, false), true);
        public static readonly SavedInputKey alignInplace = new SavedInputKey("alignInplace", MoveItTool.settingsFileName, SavedInputKey.Encode(KeyCode.None, false, false, false), true);
        public static readonly SavedInputKey alignGroup = new SavedInputKey("alignGroup", MoveItTool.settingsFileName, SavedInputKey.Encode(KeyCode.None, false, false, false), true);
        public static readonly SavedInputKey alignRandom = new SavedInputKey("alignRandom", MoveItTool.settingsFileName, SavedInputKey.Encode(KeyCode.None, false, false, false), true);

        /*public static readonly SavedInputKey export = new SavedInputKey("export", MoveItTool.settingsFileName, SavedInputKey.Encode(KeyCode.X, true, false, false), false);
        public static readonly SavedInputKey import = new SavedInputKey("import", MoveItTool.settingsFileName, SavedInputKey.Encode(KeyCode.V, true, false, false), false);
        */

        private int count = 0;

        private void Awake()
        {
            AddKeymapping("Toggle Tool", toggleTool);
            AddKeymapping("Move North", moveZpos);
            AddKeymapping("Move South", moveZneg);
            AddKeymapping("Move East", moveXpos);
            AddKeymapping("Move West", moveXneg);
            AddKeymapping("Move Up", moveYpos);
            AddKeymapping("Move Down", moveYneg);
            AddKeymapping("Rotate Counterclockwise", turnNeg);
            AddKeymapping("Rotate Clockwise", turnPos);
            AddKeymapping("Undo", undo);
            AddKeymapping("Redo", redo);
            AddKeymapping("Copy", copy);
            AddKeymapping("Align Heights", alignHeights);
            AddKeymapping("Align In-Place", alignInplace);
            AddKeymapping("Align As Group", alignGroup);
            AddKeymapping("Align Randomly", alignRandom);
        }

        private void AddKeymapping(string label, SavedInputKey savedInputKey)
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
        }

        private void OnEnable()
        {
            LocaleManager.eventLocaleChanged += new LocaleManager.LocaleChangedHandler(this.OnLocaleChanged);
        }

        private void OnDisable()
        {
            LocaleManager.eventLocaleChanged -= new LocaleManager.LocaleChangedHandler(this.OnLocaleChanged);
        }

        private void OnLocaleChanged()
        {
            this.RefreshBindableInputs();
        }

        private bool IsModifierKey(KeyCode code)
        {
            return code == KeyCode.LeftControl || code == KeyCode.RightControl || code == KeyCode.LeftShift || code == KeyCode.RightShift || code == KeyCode.LeftAlt || code == KeyCode.RightAlt;
        }

        private bool IsControlDown()
        {
            return Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        }

        private bool IsShiftDown()
        {
            return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        }

        private bool IsAltDown()
        {
            return Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
        }

        private bool IsUnbindableMouseButton(UIMouseButton code)
        {
            return code == UIMouseButton.Left || code == UIMouseButton.Right;
        }

        private KeyCode ButtonToKeycode(UIMouseButton button)
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

        private void OnBindingKeyDown(UIComponent comp, UIKeyEventParameter p)
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

        private void OnBindingMouseDown(UIComponent comp, UIMouseEventParameter p)
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

        private void RefreshBindableInputs()
        {
            foreach (UIComponent current in component.GetComponentsInChildren<UIComponent>())
            {
                UITextComponent uITextComponent = current.Find<UITextComponent>("Binding");
                if (uITextComponent != null)
                {
                    SavedInputKey savedInputKey = uITextComponent.objectUserData as SavedInputKey;
                    if (savedInputKey != null)
                    {
                        uITextComponent.text = savedInputKey.ToLocalizedString("KEYNAME");
                    }
                }
                UILabel uILabel = current.Find<UILabel>("Name");
                if (uILabel != null)
                {
                    uILabel.text = Locale.Get("KEYMAPPING", uILabel.stringUserData);
                }
            }
        }

        internal InputKey GetDefaultEntry(string entryName)
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

        private void RefreshKeyMapping()
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