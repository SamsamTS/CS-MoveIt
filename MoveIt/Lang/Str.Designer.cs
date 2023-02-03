namespace MoveIt.Lang
{
	public class Str
	{
		public static System.Globalization.CultureInfo Culture {get; set;}
		public static QCommonLib.Lang.LocalizeManager LocaleManager {get;} = new QCommonLib.Lang.LocalizeManager("Str", typeof(Str).Assembly);

		/// <summary>
		/// Bulldoze
		/// </summary>
		public static string baseUI_Bulldoze_Tooltip => LocaleManager.GetString("baseUI_Bulldoze_Tooltip", Culture);

		/// <summary>
		/// Copy (Alt+Click to duplicate in-place)
		/// </summary>
		public static string baseUI_Clone_Tooltip => LocaleManager.GetString("baseUI_Clone_Tooltip", Culture);

		/// <summary>
		/// Follow Terrain
		/// </summary>
		public static string baseUI_FollowTerrain_Tooltip => LocaleManager.GetString("baseUI_FollowTerrain_Tooltip", Culture);

		/// <summary>
		/// Marquee Selection
		/// </summary>
		public static string baseUI_Marquee_Tooltip => LocaleManager.GetString("baseUI_Marquee_Tooltip", Culture);

		/// <summary>
		/// Autoconnect Cloned Nodes
		/// </summary>
		public static string baseUI_MergeNodesTooltip => LocaleManager.GetString("baseUI_MergeNodesTooltip", Culture);

		/// <summary>
		/// Move It
		/// </summary>
		public static string baseUI_MoveItButton_Tooltip => LocaleManager.GetString("baseUI_MoveItButton_Tooltip", Culture);

		/// <summary>
		/// Single Selection
		/// </summary>
		public static string baseUI_Single_Tooltip => LocaleManager.GetString("baseUI_Single_Tooltip", Culture);

		/// <summary>
		/// Toggle Grid
		/// </summary>
		public static string baseUI_ToggleGrid_Tooltip => LocaleManager.GetString("baseUI_ToggleGrid_Tooltip", Culture);

		/// <summary>
		/// Toggle Procedural Objects
		/// </summary>
		public static string baseUI_TogglePO_Tooltip => LocaleManager.GetString("baseUI_TogglePO_Tooltip", Culture);

		/// <summary>
		/// Toggle Snapping
		/// </summary>
		public static string baseUI_ToggleSnapping_Tooltip => LocaleManager.GetString("baseUI_ToggleSnapping_Tooltip", Culture);

		/// <summary>
		/// Toggle Underground View
		/// </summary>
		public static string baseUI_ToggleUnderground_Tooltip => LocaleManager.GetString("baseUI_ToggleUnderground_Tooltip", Culture);

		/// <summary>
		/// Toolbox
		/// </summary>
		public static string baseUI_Toolbox_Tooltip => LocaleManager.GetString("baseUI_Toolbox_Tooltip", Culture);

		/// <summary>
		/// Buildings
		/// </summary>
		public static string filter_Buildings => LocaleManager.GetString("filter_Buildings", Culture);

		/// <summary>
		/// Decals
		/// </summary>
		public static string filter_Decals => LocaleManager.GetString("filter_Decals", Culture);

		/// <summary>
		/// Fences
		/// </summary>
		public static string filter_Fences => LocaleManager.GetString("filter_Fences", Culture);

		/// <summary>
		/// Nodes
		/// </summary>
		public static string filter_Nodes => LocaleManager.GetString("filter_Nodes", Culture);

		/// <summary>
		/// Others
		/// </summary>
		public static string filter_Others => LocaleManager.GetString("filter_Others", Culture);

		/// <summary>
		/// Paths
		/// </summary>
		public static string filter_Paths => LocaleManager.GetString("filter_Paths", Culture);

		/// <summary>
		/// Picker
		/// </summary>
		public static string filter_Picker => LocaleManager.GetString("filter_Picker", Culture);

		/// <summary>
		/// Pick an object to filter for objects of the same type
		/// </summary>
		public static string filter_Picker_Tooltip => LocaleManager.GetString("filter_Picker_Tooltip", Culture);

		/// <summary>
		/// PO
		/// </summary>
		public static string filter_PO => LocaleManager.GetString("filter_PO", Culture);

		/// <summary>
		/// Powerlines
		/// </summary>
		public static string filter_Powerlines => LocaleManager.GetString("filter_Powerlines", Culture);

		/// <summary>
		/// Props
		/// </summary>
		public static string filter_Props => LocaleManager.GetString("filter_Props", Culture);

		/// <summary>
		/// Roads
		/// </summary>
		public static string filter_Roads => LocaleManager.GetString("filter_Roads", Culture);

		/// <summary>
		/// Segments
		/// </summary>
		public static string filter_Segments => LocaleManager.GetString("filter_Segments", Culture);

		/// <summary>
		/// Surfaces
		/// </summary>
		public static string filter_Surfaces => LocaleManager.GetString("filter_Surfaces", Culture);

		/// <summary>
		/// Tracks
		/// </summary>
		public static string filter_Tracks => LocaleManager.GetString("filter_Tracks", Culture);

		/// <summary>
		/// Trees
		/// </summary>
		public static string filter_Trees => LocaleManager.GetString("filter_Trees", Culture);

		/// <summary>
		/// Network Skins 2 found, integration enabled!
		/// </summary>
		public static string integration_NS2_Found => LocaleManager.GetString("integration_NS2_Found", Culture);

		/// <summary>
		/// Network Skins 2 not found, or NS1 and NS2 both subscribed, integration disabled.
		/// </summary>
		public static string integration_NS2_Notfound => LocaleManager.GetString("integration_NS2_Notfound", Culture);

		/// <summary>
		/// PO version {0} found, integration enabled!
		/// </summary>
		public static string integration_PO_Found => LocaleManager.GetString("integration_PO_Found", Culture);

		/// <summary>
		/// PO not found, integration disabled.
		/// </summary>
		public static string integration_PO_Notfound => LocaleManager.GetString("integration_PO_Notfound", Culture);

		/// <summary>
		/// PO integration failed - found version {0} (required: 1.7)
		/// </summary>
		public static string integration_PO_WrongVersion => LocaleManager.GetString("integration_PO_WrongVersion", Culture);

		/// <summary>
		/// Bulldoze
		/// </summary>
		public static string key_Bulldoze => LocaleManager.GetString("key_Bulldoze", Culture);

		/// <summary>
		/// Copy
		/// </summary>
		public static string key_Clone => LocaleManager.GetString("key_Clone", Culture);

		/// <summary>
		/// Convert selected objects to PO
		/// </summary>
		public static string key_ConvertToPO => LocaleManager.GetString("key_ConvertToPO", Culture);

		/// <summary>
		/// Deselect All
		/// </summary>
		public static string key_DeselectAll => LocaleManager.GetString("key_DeselectAll", Culture);

		/// <summary>
		/// Move Down
		/// </summary>
		public static string key_MoveDown => LocaleManager.GetString("key_MoveDown", Culture);

		/// <summary>
		/// Move East
		/// </summary>
		public static string key_MoveEast => LocaleManager.GetString("key_MoveEast", Culture);

		/// <summary>
		/// Move North
		/// </summary>
		public static string key_MoveNorth => LocaleManager.GetString("key_MoveNorth", Culture);

		/// <summary>
		/// Move South
		/// </summary>
		public static string key_MoveSouth => LocaleManager.GetString("key_MoveSouth", Culture);

		/// <summary>
		/// Move Up
		/// </summary>
		public static string key_MoveUp => LocaleManager.GetString("key_MoveUp", Culture);

		/// <summary>
		/// Move West
		/// </summary>
		public static string key_MoveWest => LocaleManager.GetString("key_MoveWest", Culture);

		/// <summary>
		/// Hold for Underground View
		/// </summary>
		public static string key_QuickUndergroundView => LocaleManager.GetString("key_QuickUndergroundView", Culture);

		/// <summary>
		/// Redo
		/// </summary>
		public static string key_Redo => LocaleManager.GetString("key_Redo", Culture);

		/// <summary>
		/// Rotate Counterclockwise
		/// </summary>
		public static string key_RotateCCW => LocaleManager.GetString("key_RotateCCW", Culture);

		/// <summary>
		/// Rotate Clockwise
		/// </summary>
		public static string key_RotateCW => LocaleManager.GetString("key_RotateCW", Culture);

		/// <summary>
		/// Scale Inwards
		/// </summary>
		public static string key_ScaleIn => LocaleManager.GetString("key_ScaleIn", Culture);

		/// <summary>
		/// Scale Outwards
		/// </summary>
		public static string key_ScaleOut => LocaleManager.GetString("key_ScaleOut", Culture);

		/// <summary>
		/// Show/Hide Selectors
		/// </summary>
		public static string key_ShowSelectors => LocaleManager.GetString("key_ShowSelectors", Culture);

		/// <summary>
		/// Step Over
		/// </summary>
		public static string key_StepOver => LocaleManager.GetString("key_StepOver", Culture);

		/// <summary>
		/// Toggle Debug Panel
		/// </summary>
		public static string key_ToggleDebugPanel => LocaleManager.GetString("key_ToggleDebugPanel", Culture);

		/// <summary>
		/// Toggle Grid View
		/// </summary>
		public static string key_ToggleGridView => LocaleManager.GetString("key_ToggleGridView", Culture);

		/// <summary>
		/// Toggle Autoconnect Cloned Nodes
		/// </summary>
		public static string key_ToggleNodeMerging => LocaleManager.GetString("key_ToggleNodeMerging", Culture);

		/// <summary>
		/// Toggle PO Active/Inactive
		/// </summary>
		public static string key_TogglePO => LocaleManager.GetString("key_TogglePO", Culture);

		/// <summary>
		/// Toggle Tool
		/// </summary>
		public static string key_ToggleTool => LocaleManager.GetString("key_ToggleTool", Culture);

		/// <summary>
		/// Toggle Underground View
		/// </summary>
		public static string key_ToggleUndergroundView => LocaleManager.GetString("key_ToggleUndergroundView", Culture);

		/// <summary>
		/// Full Slope
		/// </summary>
		public static string key_ToolFullSlope => LocaleManager.GetString("key_ToolFullSlope", Culture);

		/// <summary>
		/// Line Up (Spaced)
		/// </summary>
		public static string key_ToolLineUpSpaced => LocaleManager.GetString("key_ToolLineUpSpaced", Culture);

		/// <summary>
		/// Line Up (Unspaced)
		/// </summary>
		public static string key_ToolLineUpUnspaced => LocaleManager.GetString("key_ToolLineUpUnspaced", Culture);

		/// <summary>
		/// Mirror Objects
		/// </summary>
		public static string key_ToolMirrorObjects => LocaleManager.GetString("key_ToolMirrorObjects", Culture);

		/// <summary>
		/// Quick Slope Node
		/// </summary>
		public static string key_ToolQuickSlopeNode => LocaleManager.GetString("key_ToolQuickSlopeNode", Culture);

		/// <summary>
		/// Reset Objects
		/// </summary>
		public static string key_ToolResetObjects => LocaleManager.GetString("key_ToolResetObjects", Culture);

		/// <summary>
		/// Rotate At Centre
		/// </summary>
		public static string key_ToolRotateAtCentre => LocaleManager.GetString("key_ToolRotateAtCentre", Culture);

		/// <summary>
		/// Rotate In-Place
		/// </summary>
		public static string key_ToolRotateInPlace => LocaleManager.GetString("key_ToolRotateInPlace", Culture);

		/// <summary>
		/// Rotate Randomly
		/// </summary>
		public static string key_ToolRotateRandomly => LocaleManager.GetString("key_ToolRotateRandomly", Culture);

		/// <summary>
		/// Set Position
		/// </summary>
		public static string key_ToolSetPosition => LocaleManager.GetString("key_ToolSetPosition", Culture);

		/// <summary>
		/// Slope Objects
		/// </summary>
		public static string key_ToolSlopeObjects => LocaleManager.GetString("key_ToolSlopeObjects", Culture);

		/// <summary>
		/// Align To Object Height
		/// </summary>
		public static string key_ToolToObjectHeight => LocaleManager.GetString("key_ToolToObjectHeight", Culture);

		/// <summary>
		/// Align To Terrain Height
		/// </summary>
		public static string key_ToolToTerrainHeight => LocaleManager.GetString("key_ToolToTerrainHeight", Culture);

		/// <summary>
		/// Undo
		/// </summary>
		public static string key_Undo => LocaleManager.GetString("key_Undo", Culture);

		/// <summary>
		/// Move things
		/// </summary>
		public static string mod_description => LocaleManager.GetString("mod_description", Culture);

		/// <summary>
		/// Use Advanced Pillar Control
		/// </summary>
		public static string options_AdvancedPillarControl => LocaleManager.GetString("options_AdvancedPillarControl", Culture);

		/// <summary>
		/// Allows fine control of pillars and pylons - the game will not reset their position, but can cause te
		/// </summary>
		public static string options_AdvancedPillarControl_Tooltip => LocaleManager.GetString("options_AdvancedPillarControl_Tooltip", Culture);

		/// <summary>
		/// Select pylons and pillars by holding Alt only
		/// </summary>
		public static string options_AltForPillars => LocaleManager.GetString("options_AltForPillars", Culture);

		/// <summary>
		/// Auto-close Toolbox menu
		/// </summary>
		public static string options_AutoCloseToolbox => LocaleManager.GetString("options_AutoCloseToolbox", Culture);

		/// <summary>
		/// Check this to close the Toolbox menu after choosing a tool.
		/// </summary>
		public static string options_AutoCloseToolbox_Tooltip => LocaleManager.GetString("options_AutoCloseToolbox_Tooltip", Culture);

		/// <summary>
		/// Please support the development of Move It and my other mods.
		/// </summary>
		public static string options_Beg => LocaleManager.GetString("options_Beg", Culture);

		/// <summary>
		/// Disable debug messages logging
		/// </summary>
		public static string options_DisableDebugLogging => LocaleManager.GetString("options_DisableDebugLogging", Culture);

		/// <summary>
		/// If checked, debug messages won't be logged.
		/// </summary>
		public static string options_DisableDebugLogging_Tooltip => LocaleManager.GetString("options_DisableDebugLogging_Tooltip", Culture);

		/// <summary>
		/// Extra Options
		/// </summary>
		public static string options_ExtraOptions => LocaleManager.GetString("options_ExtraOptions", Culture);

		/// <summary>
		/// Fix Tree Snapping
		/// </summary>
		public static string options_fixTreeSnapping => LocaleManager.GetString("options_fixTreeSnapping", Culture);

		/// <summary>
		/// If Tree Snapping is causing tree heights to be off, click this straight after loading your city.
		/// </summary>
		public static string options_fixTreeSnappingTooltip => LocaleManager.GetString("options_fixTreeSnappingTooltip", Culture);

		/// <summary>
		/// Hide the PO deletion warning
		/// </summary>
		public static string options_HidePODeletionWarning => LocaleManager.GetString("options_HidePODeletionWarning", Culture);

		/// <summary>
		/// Options
		/// </summary>
		public static string options_Options => LocaleManager.GetString("options_Options", Culture);

		/// <summary>
		/// Patreon
		/// </summary>
		public static string options_Patreon => LocaleManager.GetString("options_Patreon", Culture);

		/// <summary>
		/// Support me on Patreon
		/// </summary>
		public static string options_PatreonTooltip => LocaleManager.GetString("options_PatreonTooltip", Culture);

		/// <summary>
		/// Paypal
		/// </summary>
		public static string options_Paypal => LocaleManager.GetString("options_Paypal", Culture);

		/// <summary>
		/// Support me on Paypal
		/// </summary>
		public static string options_PaypalTooltip => LocaleManager.GetString("options_PaypalTooltip", Culture);

		/// <summary>
		/// Please note: you can not undo Bulldozed PO. This means if you delete
		/// </summary>
		public static string options_PODeleteWarning => LocaleManager.GetString("options_PODeleteWarning", Culture);

		/// <summary>
		/// Prefer fast, low-detail moving (hold Shift to temporarily switch)
		/// </summary>
		public static string options_PreferFastmove => LocaleManager.GetString("options_PreferFastmove", Culture);

		/// <summary>
		/// Helps you position objects when your frame-rate is poor.
		/// </summary>
		public static string options_PreferFastmove_Tooltip => LocaleManager.GetString("options_PreferFastmove_Tooltip", Culture);

		/// <summary>
		/// Procedural Objects
		/// </summary>
		public static string options_ProceduralObjects => LocaleManager.GetString("options_ProceduralObjects", Culture);

		/// <summary>
		/// Remove Ghost Nodes
		/// </summary>
		public static string options_RemoveGhostNodes => LocaleManager.GetString("options_RemoveGhostNodes", Culture);

		/// <summary>
		/// Use this button when in-game to remove ghost nodes (nodes with no segments attached). Note: this wil
		/// </summary>
		public static string options_RemoveGhostNodes_Tooltip => LocaleManager.GetString("options_RemoveGhostNodes_Tooltip", Culture);

		/// <summary>
		/// Reset Button Position
		/// </summary>
		public static string options_ResetButtonPosition => LocaleManager.GetString("options_ResetButtonPosition", Culture);

		/// <summary>
		/// Right click cancels cloning
		/// </summary>
		public static string options_RightClickCancel => LocaleManager.GetString("options_RightClickCancel", Culture);

		/// <summary>
		/// If checked, Right click will cancel cloning instead of rotating 45°.
		/// </summary>
		public static string options_RightClickCancel_Tooltip => LocaleManager.GetString("options_RightClickCancel_Tooltip", Culture);

		/// <summary>
		/// General Shortcuts
		/// </summary>
		public static string options_ShortcutsGeneral => LocaleManager.GetString("options_ShortcutsGeneral", Culture);

		/// <summary>
		/// Toolbox Shortcuts
		/// </summary>
		public static string options_ShortcutsToolbox => LocaleManager.GetString("options_ShortcutsToolbox", Culture);

		/// <summary>
		/// Show Move It debug panel
		/// </summary>
		public static string options_ShowDebugPanel => LocaleManager.GetString("options_ShowDebugPanel", Culture);

		/// <summary>
		/// Allow Move It to select everything. Use with care!
		/// </summary>
		public static string options_superSelect => LocaleManager.GetString("options_superSelect", Culture);

		/// <summary>
		/// This will let Move It select everything. Do not use this unless you know exactly what you are doing.
		/// </summary>
		public static string options_superSelect_Tooltip => LocaleManager.GetString("options_superSelect_Tooltip", Culture);

		/// <summary>
		/// Use compass movements
		/// </summary>
		public static string options_UseCompass => LocaleManager.GetString("options_UseCompass", Culture);

		/// <summary>
		/// If checked, the Up key will move in the North direction, Down is South, Left is West, Right is East.
		/// </summary>
		public static string options_UseCompass_Tooltip => LocaleManager.GetString("options_UseCompass_Tooltip", Culture);

		/// <summary>
		/// Use UnifiedUI
		/// </summary>
		public static string options_UseUUI => LocaleManager.GetString("options_UseUUI", Culture);

		/// <summary>
		/// Angle
		/// </summary>
		public static string setpos_A_Tooltip => LocaleManager.GetString("setpos_A_Tooltip", Culture);

		/// <summary>
		/// Go
		/// </summary>
		public static string setpos_Go => LocaleManager.GetString("setpos_Go", Culture);

		/// <summary>
		/// Height
		/// </summary>
		public static string setpos_H_Tooltip => LocaleManager.GetString("setpos_H_Tooltip", Culture);

		/// <summary>
		/// Position
		/// </summary>
		public static string setpos_Title => LocaleManager.GetString("setpos_Title", Culture);

		/// <summary>
		/// Convert To PO
		/// </summary>
		public static string toolbox_ConvertToPO => LocaleManager.GetString("toolbox_ConvertToPO", Culture);

		/// <summary>
		/// Export Selection
		/// </summary>
		public static string toolbox_ExportSelection => LocaleManager.GetString("toolbox_ExportSelection", Culture);

		/// <summary>
		/// Height Tools
		/// </summary>
		public static string toolbox_HeightTools_Tooltip => LocaleManager.GetString("toolbox_HeightTools_Tooltip", Culture);

		/// <summary>
		/// Import Selection
		/// </summary>
		public static string toolbox_ImportSelection => LocaleManager.GetString("toolbox_ImportSelection", Culture);

		/// <summary>
		/// Line Up Objects
		/// </summary>
		public static string toolbox_LineUpObjects => LocaleManager.GetString("toolbox_LineUpObjects", Culture);

		/// <summary>
		/// Mirror Objects
		/// </summary>
		public static string toolbox_MirrorObjects => LocaleManager.GetString("toolbox_MirrorObjects", Culture);

		/// <summary>
		/// Other Tools
		/// </summary>
		public static string toolbox_OtherTools_Tooltip => LocaleManager.GetString("toolbox_OtherTools_Tooltip", Culture);

		/// <summary>
		/// Reset Objects
		/// </summary>
		public static string toolbox_ResetObjects => LocaleManager.GetString("toolbox_ResetObjects", Culture);

		/// <summary>
		/// Rotate At Centre
		/// </summary>
		public static string toolbox_RotateAtCentre => LocaleManager.GetString("toolbox_RotateAtCentre", Culture);

		/// <summary>
		/// Rotate In-Place
		/// </summary>
		public static string toolbox_RotateInPlace => LocaleManager.GetString("toolbox_RotateInPlace", Culture);

		/// <summary>
		/// Rotate Randomly
		/// </summary>
		public static string toolbox_RotateRandomly => LocaleManager.GetString("toolbox_RotateRandomly", Culture);

		/// <summary>
		/// Rotation Tools
		/// </summary>
		public static string toolbox_RotationTools_Tooltip => LocaleManager.GetString("toolbox_RotationTools_Tooltip", Culture);

		/// <summary>
		/// Set Position
		/// </summary>
		public static string toolbox_SetPosition => LocaleManager.GetString("toolbox_SetPosition", Culture);

		/// <summary>
		/// Slope Objects
		/// </summary>
		public static string toolbox_SlopeObjects => LocaleManager.GetString("toolbox_SlopeObjects", Culture);

		/// <summary>
		/// To Object Height
		/// </summary>
		public static string toolbox_ToObjectHeight => LocaleManager.GetString("toolbox_ToObjectHeight", Culture);

		/// <summary>
		/// To Terrain Height
		/// </summary>
		public static string toolbox_ToTerrainHeight => LocaleManager.GetString("toolbox_ToTerrainHeight", Culture);

		//
		public static string whatsNew => LocaleManager.GetString("whatsNew", Culture);

		/// <summary>
		/// Asc
		/// </summary>
		public static string xml_Asc => LocaleManager.GetString("xml_Asc", Culture);

		/// <summary>
		/// Date
		/// </summary>
		public static string xml_Date => LocaleManager.GetString("xml_Date", Culture);

		/// <summary>
		/// Do you want to delete the file '{0}' permanently?
		/// </summary>
		public static string xml_DeleteConfirmMessage => LocaleManager.GetString("xml_DeleteConfirmMessage", Culture);

		/// <summary>
		/// Delete file
		/// </summary>
		public static string xml_DeleteConfirmTitle => LocaleManager.GetString("xml_DeleteConfirmTitle", Culture);

		/// <summary>
		/// Delete saved selection
		/// </summary>
		public static string xml_DeleteLabel => LocaleManager.GetString("xml_DeleteLabel", Culture);

		/// <summary>
		/// Desc
		/// </summary>
		public static string xml_Desc => LocaleManager.GetString("xml_Desc", Culture);

		/// <summary>
		/// Export
		/// </summary>
		public static string xml_Export => LocaleManager.GetString("xml_Export", Culture);

		/// <summary>
		/// Import
		/// </summary>
		public static string xml_Import => LocaleManager.GetString("xml_Import", Culture);

		/// <summary>
		/// Name
		/// </summary>
		public static string xml_Name => LocaleManager.GetString("xml_Name", Culture);

		/// <summary>
		/// Open Folder
		/// </summary>
		public static string xml_OpenFolder => LocaleManager.GetString("xml_OpenFolder", Culture);

		/// <summary>
		/// The file '{0}' already exists.
		/// </summary>
		public static string xml_OverwriteMessage => LocaleManager.GetString("xml_OverwriteMessage", Culture);

		/// <summary>
		/// Overwrite file
		/// </summary>
		public static string xml_OverwriteTitle => LocaleManager.GetString("xml_OverwriteTitle", Culture);

		/// <summary>
		/// Restore
		/// </summary>
		public static string xml_Restore => LocaleManager.GetString("xml_Restore", Culture);

		/// <summary>
		/// Import the selection to the position it was exported
		/// </summary>
		public static string xml_Restore_Tooltip => LocaleManager.GetString("xml_Restore_Tooltip", Culture);

		/// <summary>
		/// The selection is empty or invalid.
		/// </summary>
		public static string xml_SelectionInvalidMessage => LocaleManager.GetString("xml_SelectionInvalidMessage", Culture);

		/// <summary>
		/// Selection invalid
		/// </summary>
		public static string xml_SelectionInvalidTitle => LocaleManager.GetString("xml_SelectionInvalidTitle", Culture);

		/// <summary>
		/// Size
		/// </summary>
		public static string xml_Size => LocaleManager.GetString("xml_Size", Culture);
	}
}