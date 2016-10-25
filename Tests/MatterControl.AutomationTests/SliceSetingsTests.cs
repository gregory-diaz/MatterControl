﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MatterHackers.Agg.PlatformAbstract;
using MatterHackers.Agg.UI;
using MatterHackers.Agg.UI.Tests;
using MatterHackers.GuiAutomation;
using MatterHackers.MatterControl.SlicerConfiguration;
using NUnit.Framework;

namespace MatterHackers.MatterControl.Tests.Automation
{
	[TestFixture, Category("MatterControl.UI.Automation"), RunInApplicationDomain]
	public class SliceSetingsTests
	{
		[Test, Apartment(ApartmentState.STA)]
		public async Task RaftEnabledPassedToSliceEngine()
		{
			AutomationTest testToRun = (testRunner) =>
			{
				MatterControlUtilities.PrepForTestRun(testRunner);

				MatterControlUtilities.AddAndSelectPrinter(testRunner, "Airwolf 3D", "HD");

				//Navigate to Local Library 
				testRunner.ClickByName("Library Tab");
				MatterControlUtilities.NavigateToFolder(testRunner, "Local Library Row Item Collection");
				testRunner.Wait(1);
				testRunner.ClickByName("Row Item Calibration - Box");
				testRunner.ClickByName("Row Item Calibration - Box Print Button");
				testRunner.Wait(1);

				testRunner.ClickByName("Layer View Tab");

				testRunner.ClickByName("Bread Crumb Button Home", 1);

				MatterControlUtilities.SwitchToAdvancedSettings(testRunner);

				testRunner.ClickByName("Raft / Priming Tab", 1);

				testRunner.ClickByName("Create Raft Checkbox", 1);
				testRunner.Wait(1.5);
				testRunner.ClickByName("Generate Gcode Button", 1);
				testRunner.Wait(5);

				//Call compare slice settings method here
				Assert.IsTrue(MatterControlUtilities.CompareExpectedSliceSettingValueWithActualVaue("enableRaft", "True"));

				return Task.FromResult(0);
			};

			await MatterControlUtilities.RunTest(testToRun, overrideWidth: 1224, overrideHeight: 800);
		}

		[Test, Apartment(ApartmentState.STA)]
		public async Task PauseOnLayerDoesPauseOnPrint()
		{
			Process emulatorProcess = null;

			AutomationTest testToRun = (testRunner) =>
			{
				MatterControlUtilities.PrepForTestRun(testRunner, MatterControlUtilities.PrepAction.CloseSignInAndPrinterSelect);

				emulatorProcess = MatterControlUtilities.LaunchAndConnectToPrinterEmulator(testRunner);

				Assert.IsTrue(ProfileManager.Instance.ActiveProfile != null);

				MatterControlUtilities.SwitchToAdvancedSettings(testRunner);

				Assert.IsTrue(testRunner.ClickByName("General Tab", 1));
				Assert.IsTrue(testRunner.ClickByName("Single Print Tab", 1));
				Assert.IsTrue(testRunner.ClickByName("Layer(s) To Pause:" + " Edit"));
				testRunner.Type("4;2;a;not;6");

				Assert.IsTrue(testRunner.ClickByName("Layer View Tab"));

				Assert.IsTrue(testRunner.ClickByName("Generate Gcode Button", 1));
				Assert.IsTrue(testRunner.ClickByName("Display Checkbox", 10));
				Assert.IsTrue(testRunner.ClickByName("Sync To Print Checkbox", 1));

				Assert.IsTrue(testRunner.ClickByName("Start Print Button", 1));

				WaitForLayerAndResume(testRunner, 2);
				WaitForLayerAndResume(testRunner, 4);
				WaitForLayerAndResume(testRunner, 6);

				Assert.IsTrue(testRunner.WaitForName("Done Button", 30));
				Assert.IsTrue(testRunner.WaitForName("Print Again Button", 1));

				return Task.FromResult(0);
			};

			await MatterControlUtilities.RunTest(testToRun, maxTimeToRun: 90);

			try
			{
				emulatorProcess?.Kill();
			}
			catch { }
		}

		private static void WaitForLayerAndResume(AutomationRunner testRunner, int indexToWaitFor)
		{
			testRunner.WaitForName("Resume Button", 30);

			SystemWindow containingWindow;
			GuiWidget layerNumber = testRunner.GetWidgetByName("Current GCode Layer Edit", out containingWindow, 20);

			layerNumber.Invalidate();
			testRunner.WaitUntil(() => layerNumber.Text == indexToWaitFor.ToString(), 2);

			Assert.IsTrue(layerNumber.Text == indexToWaitFor.ToString());
			Assert.IsTrue(testRunner.ClickByName("Resume Button", 1));
			testRunner.Wait(.1);
		}

		[Test, Apartment(ApartmentState.STA)]
		public async Task ClearingCheckBoxClearsUserOverride()
		{
			AutomationTest testToRun = (testRunner) =>
			{
				MatterControlUtilities.PrepForTestRun(testRunner);

				MatterControlUtilities.AddAndSelectPrinter(testRunner, "Airwolf 3D", "HD");

				//Navigate to Local Library 
				MatterControlUtilities.SwitchToAdvancedSettings(testRunner);

				Assert.IsTrue(testRunner.ClickByName("Printer Tab", 1), "Switch to Printers tab");
				Assert.IsTrue(testRunner.ClickByName("Features Tab", 1), "Switch to Features tab");

				CheckAndUncheckSetting(testRunner, SettingsKey.heat_extruder_before_homing, "Heat Before Homing Checkbox", false);

				CheckAndUncheckSetting(testRunner, SettingsKey.has_fan, "Has Fan Checkbox", true);

				return Task.FromResult(0);
			};

			await MatterControlUtilities.RunTest(testToRun, overrideWidth: 1224, overrideHeight: 900);
		}

		[Test, Apartment(ApartmentState.STA)]
		public async Task DeleteProfileWorksForGuest()
		{
			AutomationTest testToRun = (testRunner) =>
			{
				MatterControlUtilities.PrepForTestRun(testRunner);

				// assert no profiles
				Assert.IsTrue(ProfileManager.Instance.ActiveProfiles.Count() == 0);

				MatterControlUtilities.AddAndSelectPrinter(testRunner, "Airwolf 3D", "HD");

				// assert one profile
				Assert.IsTrue(ProfileManager.Instance.ActiveProfiles.Count() == 1);

				MatterControlUtilities.DeleteSelectedPrinter(testRunner);

				// assert no profiles
				Assert.IsTrue(ProfileManager.Instance.ActiveProfiles.Count() == 0);

				return Task.FromResult(0);
			};

			await MatterControlUtilities.RunTest(testToRun, overrideWidth: 1224, overrideHeight: 900);
		}

		private static void CheckAndUncheckSetting(AutomationRunner testRunner, string settingToChange, string checkBoxName, bool expected)
		{
			// Assert that the checkbox is currently unchecked, and there is no user override
			Assert.IsTrue(ActiveSliceSettings.Instance.GetValue<bool>(settingToChange) == expected);
			Assert.IsTrue(ActiveSliceSettings.Instance.UserLayer.ContainsKey(settingToChange) == false);

			// Click the checkbox
			Assert.IsTrue(testRunner.ClickByName(checkBoxName, 1));
			testRunner.Wait(2);

			// Assert the checkbox is checked and the user override is set
			Assert.IsTrue(ActiveSliceSettings.Instance.GetValue<bool>(settingToChange) != expected);
			Assert.IsTrue(ActiveSliceSettings.Instance.UserLayer.ContainsKey(settingToChange) == true);

			// Click the cancel user override button
			Assert.IsTrue(testRunner.ClickByName("Restore " + settingToChange, 1));
			testRunner.Wait(2);

			// Assert the checkbox is unchecked and there is no user override
			Assert.IsTrue(ActiveSliceSettings.Instance.GetValue<bool>(settingToChange) == expected);
			Assert.IsTrue(ActiveSliceSettings.Instance.UserLayer.ContainsKey(settingToChange) == false);
		}

		//Stress Test check & uncheck 1000x
		[Test, Apartment(ApartmentState.STA), Category("FixNeeded" /* Not Finished */)]
		public async Task HasHeatedBedCheckUncheck()
		{
			AutomationTest testToRun = (testRunner) =>
			{
				MatterControlUtilities.PrepForTestRun(testRunner);

				MatterControlUtilities.AddAndSelectPrinter(testRunner, "Airwolf 3D", "HD");

				//Navigate to Local Library 
				MatterControlUtilities.SwitchToAdvancedSettings(testRunner);

				Assert.IsTrue(testRunner.ClickByName("Printer Tab"));
				testRunner.Wait(1);

				Assert.IsTrue(testRunner.ClickByName("Features Tab"));
				testRunner.Wait(2);

				for (int i = 0; i <= 1000; i++)
				{
					Assert.IsTrue(testRunner.ClickByName("Has Heated Bed Checkbox"));
					testRunner.Wait(.5);
				}

				return Task.FromResult(0);
			};

			await MatterControlUtilities.RunTest(testToRun);
		}

		[Test, Apartment(ApartmentState.STA)]
		public async Task HasHeatedBedCheckedHidesBedTemperatureOptions()
		{
			AutomationTest testToRun = (testRunner) =>
			{
				MatterControlUtilities.PrepForTestRun(testRunner);

				MatterControlUtilities.AddAndSelectPrinter(testRunner, "Airwolf 3D", "HD");

				//Navigate to Settings Tab and make sure Bed Temp Text box is visible 
				MatterControlUtilities.SwitchToAdvancedSettings(testRunner);

				testRunner.ClickByName("Filament Tab", 1);
				testRunner.ClickByName("Temperatures Tab", 1);
				Assert.IsTrue(testRunner.WaitForName("Bed Temperature Textbox", 2));

				//Uncheck Has Heated Bed checkbox and make sure Bed Temp Textbox is not visible
				testRunner.ClickByName("Printer Tab", 1);
				testRunner.ClickByName("Features Tab", 1);
				testRunner.DragByName("Show Reset Connection Checkbox", 1, offset: new Agg.Point2D(-40, 0));
				testRunner.MoveToByName("Show Reset Connection Checkbox", 1, offset: new Agg.Point2D(0, 120));
				testRunner.Drop();
				testRunner.ClickByName("Has Heated Bed Checkbox", 1);
				testRunner.Wait(.5);
				testRunner.ClickByName("Filament Tab", 1);
				bool bedTemperatureTextBoxVisible = testRunner.WaitForName("Bed Temperature Textbox", 2);
				Assert.IsTrue(bedTemperatureTextBoxVisible == false);

				//Make sure Bed Temperature Options are not visible in printer controls
				testRunner.ClickByName("Controls Tab");
				bool bedTemperatureControlsWidget = testRunner.WaitForName("Bed Temperature Controls Widget", 2);
				Assert.IsTrue(bedTemperatureTextBoxVisible == false);

				return Task.FromResult(0);
			};

			await MatterControlUtilities.RunTest(testToRun, overrideWidth: 550);
		}
	}
}
