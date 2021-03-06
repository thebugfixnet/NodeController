using ColossalFramework.UI;
using ICities;
using ColossalFramework;

namespace NodeController.GUI {
    using Tool;
    public static class Settings {
        public const string FileName = nameof(NodeController);
        static Settings() {
            // Creating setting file - from SamsamTS
            if (GameSettings.FindSettingsFileByName(FileName) == null) {
                GameSettings.AddSettingsFile(new SettingsFile[] { new SettingsFile() { fileName = FileName } });
            }
        }

        public static void OnSettingsUI(UIHelperBase helper) {
            UIHelper group = helper.AddGroup("Node Controller") as UIHelper;
            UIPanel panel = group.self as UIPanel;
            var keymappings = panel.gameObject.AddComponent<KeymappingsPanel>();
            keymappings.AddKeymapping("Activation Shortcut", NodeControllerTool.ActivationShortcut);

            UICheckBox snapToggle = group.AddCheckbox(
                "Snap to middle node",
                NodeControllerTool.SnapToMiddleNode.value,
                val => NodeControllerTool.SnapToMiddleNode.value = val) as UICheckBox;
            //snapToggle.tooltip = "?";

            UICheckBox TMPE_Overlay = group.AddCheckbox(
                "Hide TMPE overlay on the selected node",
                NodeControllerTool.Hide_TMPE_Overlay.value,
                val => NodeControllerTool.Hide_TMPE_Overlay.value = val) as UICheckBox;
            TMPE_Overlay.tooltip = "Holding control hides all TMPE overlay.\n" +
                "but if this is checked, you don't have to (excluding Corssings/Uturn)";
        }
    }
}
