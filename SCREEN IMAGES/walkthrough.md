# Screen Images Walkthrough

## Structure

- `References/V1 P1 Mockups/Prompt_V1_P1_Mockups.txt`: active prompt for the second visual mockup generation pass.
- `Onboarding/Onboarding_FirstRun_Default.png`: generated first-run setup mockup.
- `Onboarding/Onboarding_FirstRun_Ready.png`: alternate generated first-run setup mockup with readiness checks.
- `Presets/Presets_Customize_Edit.png`: generated preset customization mockup.
- `Monitors/Monitors_Override_Detail.png`: generated per-monitor override detail mockup.
- `Automation/Automation_RuleBuilder_Default.png`: generated automation rule builder mockup.
- `Diagnostics/Diagnostics_Error_NoTaskbar.png`: generated no-taskbar diagnostics error mockup.
- `Settings/Settings_Hotkeys_Edit.png`: generated hotkey settings edit mockup.
- `Tray/Tray_Menu_Open.png`: generated system tray menu mockup.
- `About/About_Update_Default.png`: generated about and update status mockup.

## Expected Screens

### V1 P0 Mockups

- `Dashboard_Home_Default.png`: main live control dashboard.
- `Presets_Selection_Default.png`: preset cards for Clear, Glass, and Solid modes.
- `Monitors_Overrides_Default.png`: monitor list and sync controls.
- `Automation_Rules_Default.png`: automation toggles and policy overview.
- `Diagnostics_Runtime_Default.png`: runtime status and state simulation controls.
- `Settings_System_Default.png`: tray and hotkey settings.

### V1 P1 Mockups

- `Onboarding_FirstRun_Default.png`: first-run setup for choosing the starter profile or importing settings. Generated and organized in `Onboarding/`.
- `Presets_Customize_Edit.png`: focused preset customization state for material, opacity, and easing. Generated and organized in `Presets/`.
- `Monitors_Override_Detail.png`: per-display override state with sync disabled for one monitor. Generated and organized in `Monitors/`.
- `Automation_RuleBuilder_Default.png`: richer automation policy builder with resolved-state preview. Generated and organized in `Automation/`.
- `Diagnostics_Error_NoTaskbar.png`: diagnostics error state for no detected taskbar windows. Generated and organized in `Diagnostics/`.
- `Settings_Hotkeys_Edit.png`: hotkey capture and tray/startup settings edit state. Generated and organized in `Settings/`.
- `Tray_Menu_Open.png`: compact system tray menu state. Generated and organized in `Tray/`.
- `About_Update_Default.png`: product, version, repository, logs, settings path, and update surface. Generated and organized in `About/`.

## Current State

Dashboard reference was received in chat on 2026-06-07 and used to refine the live WinUI Dashboard screen. The V1 P1 first-run onboarding mockup was generated, visually inspected as a single standalone full-screen UI mockup, renamed, and moved into `Onboarding/`. The remaining V1 P1 generated mockups were also visually inspected, renamed, and organized into their matching logical folders. The tray menu mockup is a desktop tray context state rather than an app-window screen, but it is a single standalone state and not a collage.

The active prompt folder is `References/V1 P1 Mockups/`. No local source screen images are available yet to copy as references; use the prompt file as the handoff artifact for external generation.

After generated images are placed in this root `SCREEN IMAGES/` directory, verify each file is a single standalone full-screen UI mockup, then rename and move each image into a logical screen folder. Reject and regenerate any storyboard sheet, collage, split-screen, comparison image, contact sheet, or multi-panel grid.
