﻿using MahApps.Metro.Controls;
using Microsoft.DirectX.DirectInput;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace FalconBMS_Alternative_Launcher_Cs
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        /// <summary>
        /// Let's write DataGrid cells at KeyMapping page a keyfile informarion.
        /// </summary>
        public void WriteDataGrid()
        {
            foreach (KeyAssgn Assgn in keyFile.keyAssign)
                Assgn.Visibility = Assgn.GetVisibility();

            //string target = "MFD";

            //foreach (KeyAssgn Assgn in keyFile.keyAssign)
            //{
            //    if (Assgn.Mapping.Trim().Contains(target))
            //        Assgn.Visibility = Assgn.GetVisibility();
            //    else
            //    {
            //        Assgn.Visibility = "Hidden";
            //    }
            //}

            this.KeyMappingGrid.ItemsSource = keyFile.keyAssign;
        }

        /// <summary>
        /// Initialize Datagrid Columns.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DataGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "Mapping":
                    e.Column.Header = "Mapping";
                    e.Column.DisplayIndex = 0;
                    break;
                case "Key":
                    e.Column.Header = "Key";
                    e.Column.DisplayIndex = 1;
                    break;
                case "Visibility":
                    // Do not show
                    e.Column.DisplayIndex = 2;
                    e.Cancel = true;
                    break;
            }
            Category.SelectedIndex = 0;
            if (!e.PropertyName.Contains("Z_Joy_"))
                return;
            int target = int.Parse(e.PropertyName.Replace("Z_Joy_", ""));
            if (target >= deviceControl.devList.Count)
            {
                e.Cancel = true;
                return;
            }
            e.Column.Header = deviceControl.joyAssign[target].GetProductName();
            e.Column.Width = 128;
            e.Column.DisplayIndex = 3 + target;
        }
        
        /// <summary>
        /// Unassign keyboard key or joystick button when double clicked a Datagrid cell.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DataGrid_MouseButtonDoubleClick(object sender, MouseButtonEventArgs e)
        {
            KeyMappingGrid.ScrollIntoView(KeyMappingGrid.Items[currentIndex]);
            KeyMappingGrid.SelectedIndex = currentIndex;
            if (KeyMappingGrid.CurrentColumn == null)
                return;
            int Rows = KeyMappingGrid.SelectedIndex;
            int Columns = KeyMappingGrid.CurrentColumn.DisplayIndex;

            if (Columns == 1)
            {
                if (Rows < 0)
                    return;
                if (keyFile.keyAssign[currentIndex].Visibility != "White")
                    return;
                keyFile.keyAssign[currentIndex].UnassignKeyboard();
            }
            if (Columns > 1)
            {
                if (Rows < 0)
                    return;
                string target = keyFile.keyAssign[currentIndex].GetCallback();
                deviceControl.joyAssign[Columns - 3].UnassigntargetCallback(target);
            }
            KeyMappingGrid.Items.Refresh();
            KeyMappingGrid.UnselectAllCells();
            statusSearch = Search.Search;
        }
        
        private byte[] buttons;
        private int[] povs;
        private NeutralButtons[] neutralButtons;

        /// <summary>
        /// What was your joystick buttons were like when you clicked somewhere? Are they pressed or released?
        /// </summary>
        public class NeutralButtons
        {
            public byte[] buttons { get; set; }
            public int[] povs { get; set; }

            public NeutralButtons(Device joyStick)
            {
                this.buttons = joyStick.CurrentJoystickState.GetButtons();
                this.povs = joyStick.CurrentJoystickState.GetPointOfView();
            }
        }
        
        /// <summary>
        /// Is KeyMapping page currently trying to get neutral button positions? or waiting for your input?
        /// </summary>
        private Status statusAssign = Status.GetNeutralPos;
        private enum Status
        {
            GetNeutralPos = 0,
            WaitingforInput = 1
        }

        /// <summary>
        /// Is keyMapping page going to assign your button when it is pressed? or going to search which callback is it assigned to?
        /// </summary>
        private Search statusSearch = Search.Search;
        private enum Search
        {
            Assign = 0,
            Search = 1
        }

        /// <summary>
        /// When a DataGrid cell has been clicked once and highlighted.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DataGrid_GotFocus(object sender, RoutedEventArgs e)
        {
            statusSearch = Search.Assign;
            Label_AssgnStatus.Content = "AWAITING INPUTS";
        }

        /// <summary>
        /// Unfocus a Datagrid cell and KeyMapping page backs to search mode.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void KeyMappingGrid_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            KeyMappingGrid.UnselectAllCells();
            statusSearch = Search.Search;
            Label_AssgnStatus.Content = "KEYSEARCH MODE";
        }

        /// <summary>
        /// Check your keyboard/joysticks button behaviour every 60 frames per seconds.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void KeyMappingTimer_Tick(object sender, EventArgs e)
        {
            try
            {

                directInputDevice.GetCurrentKeyboardState();
                for (int i = 1; i < 238; i++)
                    if (directInputDevice.KeyboardState[(Microsoft.DirectX.DirectInput.Key)i])
                        KeyMappingGrid_KeyDown();

                int Rows = KeyMappingGrid.SelectedIndex;
                if (Rows == -1 | statusSearch == Search.Search)
                {
                    if (!this.inSaveDialog)
                    {
                        JumptoAssignedKey();
                    }
                    return;
                }
                if (KeyMappingGrid.CurrentColumn == null)
                    return;
                if (keyFile.keyAssign[Rows].GetVisibility() != "White")
                    return;


                switch (statusAssign)
                {
                    case Status.GetNeutralPos:
                        for (int i = 0; i < deviceControl.devList.Count; i++)
                            neutralButtons[i] = new NeutralButtons(deviceControl.joyStick[i]);
                        statusAssign = Status.WaitingforInput;
                        break;
                    case Status.WaitingforInput:
                        for (int i = 0; i < deviceControl.devList.Count; i++)
                        {
                            buttons = deviceControl.joyStick[i].CurrentJoystickState.GetButtons();
                            for (int ii = 0; ii < 32; ii++)
                            {
                                if (buttons[ii] == neutralButtons[i].buttons[ii])
                                    continue;
                                statusAssign = Status.GetNeutralPos;
                                if (buttons[ii] == 0)
                                    continue;

                                Pinky pinkyStatus = Pinky.UnShift;
                                Behaviour behaviourStatus = Behaviour.Press;
                                if (Select_PinkyShift.IsChecked == false)
                                    pinkyStatus = Pinky.Shift;
                                if (Select_DX_Release.IsChecked == false)
                                    behaviourStatus = Behaviour.Release;

                                // Construct DX button instance.
                                if (keyFile.keyAssign[Rows].GetCallback() == "SimHotasPinkyShift")
                                {
                                    deviceControl.joyAssign[i].dx[ii].Assign(keyFile.keyAssign[Rows].GetCallback(), Pinky.UnShift, Behaviour.Press, Invoke.Default, 0);
                                    deviceControl.joyAssign[i].dx[ii].Assign(keyFile.keyAssign[Rows].GetCallback(), Pinky.Shift,   Behaviour.Press, Invoke.Default, 0);
                                }
                                else
                                {
                                    deviceControl.joyAssign[i].dx[ii].Assign(keyFile.keyAssign[Rows].GetCallback(), pinkyStatus, behaviourStatus, invokeStatus, 0);
                                }

                                KeyMappingGrid.Items.Refresh();
                                KeyMappingGrid.UnselectAllCells();
                            }
                            povs = deviceControl.joyStick[i].CurrentJoystickState.GetPointOfView();
                            for (int ii = 0; ii < 4; ii++)
                            {
                                if (povs[ii] == neutralButtons[i].povs[ii])
                                    continue;
                                statusAssign = Status.GetNeutralPos;
                                if (povs[ii] == -1)
                                    continue;

                                Pinky pinkyStatus = Pinky.UnShift;
                                if (Select_PinkyShift.IsChecked == false)
                                    pinkyStatus = Pinky.Shift;

                                // Construct POV button instance.
                                deviceControl.joyAssign[i].pov[ii].Assign(povs[ii], keyFile.keyAssign[Rows].GetCallback(), pinkyStatus, 0);

                                KeyMappingGrid.Items.Refresh();
                                KeyMappingGrid.UnselectAllCells();
                            }
                        }
                        break;
                }
            }
            catch (System.IO.FileNotFoundException ex)
            {
                System.Console.WriteLine(ex.Message);

                System.IO.StreamWriter sw = new System.IO.StreamWriter(appReg.GetInstallDir() + "\\Error.txt", false, System.Text.Encoding.GetEncoding("shift_jis"));
                sw.Write(ex.Message);
                sw.Close();
                System.Windows.MessageBox.Show("Error Log Saved To " + appReg.GetInstallDir() + "\\Error.txt", "WARNING", MessageBoxButton.OK, MessageBoxImage.Information);

                this.Close();
            }
        }

        /// <summary>
        /// You pressed a joystick button to search which callback is it assigned to? OK let's go there.
        /// </summary>
        public void JumptoAssignedKey()
        {
            string target = "";
            switch (statusAssign)
            {
                case Status.GetNeutralPos:
                    for (int i = 0; i < deviceControl.devList.Count; i++)
                        neutralButtons[i] = new NeutralButtons(deviceControl.joyStick[i]);
                    statusAssign = Status.WaitingforInput;
                    break;
                case Status.WaitingforInput:
                    for (int i = 0; i < deviceControl.devList.Count; i++)
                    {
                        buttons = deviceControl.joyStick[i].CurrentJoystickState.GetButtons(); //Microsoft.DirectX.DirectInput.InputLostException: 'アプリケーションでエラーが発生しました。'
                        for (int ii = 0; ii < 32; ii++)
                        {
                            if (buttons[ii] == neutralButtons[i].buttons[ii])
                                continue;
                            statusAssign = Status.GetNeutralPos;
                            if (buttons[ii] == 0) // RELEASE SHIFT
                                continue;
                            
                            Pinky pinkyStatus = Pinky.UnShift;
                            Behaviour behaviourStatus = Behaviour.Press;
                            if (Select_PinkyShift.IsChecked == false)
                                pinkyStatus = Pinky.Shift;
                            if (Select_DX_Release.IsChecked == false)
                                behaviourStatus = Behaviour.Release;

                            target = deviceControl.joyAssign[i].dx[ii].assign[(int)pinkyStatus + (int)behaviourStatus].GetCallback();

                            Label_AssgnStatus.Content = "DX" + (ii+1) + "\t: " + deviceControl.joyAssign[i].GetProductName();
                        }
                        povs = deviceControl.joyStick[i].CurrentJoystickState.GetPointOfView();
                        for (int ii = 0; ii < 4; ii++)
                        {
                            if (povs[ii] == neutralButtons[i].povs[ii])
                                continue;
                            statusAssign = Status.GetNeutralPos;
                            if (povs[ii] == -1)
                                continue;
                            
                            Pinky pinkyStatus = Pinky.UnShift;
                            if (Select_PinkyShift.IsChecked == false)
                                pinkyStatus = Pinky.Shift;

                            target = deviceControl.joyAssign[i].pov[ii].direction[povs[ii] / 4500].GetCallback(pinkyStatus);

                            string direction = deviceControl.joyAssign[i].pov[ii].GetDirection(povs[ii]);
                            Label_AssgnStatus.Content = "POV" + (ii + 1) + "." + direction + "\t: " + deviceControl.joyAssign[i].GetProductName();
                        }
                    }
                    break;
            }
            
            if (target == "")
                return;
            if (target == "SimDoNothing")
                return;
            for (int i = 0; i < keyFile.keyAssign.Length; i++)
            {
                if (keyFile.keyAssign[i].GetCallback() == target)
                {
                    Label_AssgnStatus.Content += "   / " + keyFile.keyAssign[i].Mapping;

                    KeyMappingGrid.UpdateLayout();
                    KeyMappingGrid.ScrollIntoView(KeyMappingGrid.Items[i]);
                    KeyMappingGrid.SelectedIndex = i;
                    statusSearch = Search.Search;
                }
            }
        }

        /// <summary>
        /// You pressed keyboard keys? I will check which key was pressed with Shift/Ctrl/Alt.
        /// </summary>
        private void KeyMappingGrid_KeyDown()
        {
            if (this.inSaveDialog)
            {
                return;
            }
            if (currentIndex < 0)
            {
                currentIndex = 1;
                statusSearch = Search.Search;
                return;
            }
            if (SearchBox.IsSelectionActive == true)
                return;
            if (SearchBox.IsFocused == true)
                return;
            if (SearchBox.IsKeyboardFocused == true)
                return;

            bool Shift = false;
            bool Ctrl = false;
            bool Alt = false;

            int catchedScanCode = 0;

            directInputDevice.GetCurrentKeyboardState();

            for (int i = 1; i < 238; i++)
            {
                if (directInputDevice.KeyboardState[(Microsoft.DirectX.DirectInput.Key)i])
                {
                    if (i == (int)Microsoft.DirectX.DirectInput.Key.LeftShift |
                        i == (int)Microsoft.DirectX.DirectInput.Key.RightShift)
                    {
                        Shift = true;
                        continue;
                    }
                    if (i == (int)Microsoft.DirectX.DirectInput.Key.LeftControl |
                        i == (int)Microsoft.DirectX.DirectInput.Key.RightControl)
                    {
                        Ctrl = true;
                        continue;
                    }
                    if (i == (int)Microsoft.DirectX.DirectInput.Key.LeftAlt |
                        i == (int)Microsoft.DirectX.DirectInput.Key.RightAlt)
                    {
                        Alt = true;
                        continue;
                    }
                    catchedScanCode = i;
                }
            }
            if (catchedScanCode == 0)
                return;
            if (statusSearch == Search.Search)
            {
                KeyAssgn keytmp = new KeyAssgn("SimDoNothing - 1 0 0XFFFFFFFF 0 0 0 - 1 \"nothing\"");
                keytmp.SetKeyboard(catchedScanCode, Shift, Ctrl, Alt);
                Label_AssgnStatus.Content = "INPUT " + keytmp.GetKeyAssignmentStatus();
                for (int i = 0; i < keyFile.keyAssign.Length; i++)
                {
                    if (keytmp.GetKeyAssignmentStatus() != keyFile.keyAssign[i].GetKeyAssignmentStatus())
                        continue;

                    Label_AssgnStatus.Content += "\t/" + keyFile.keyAssign[i].Mapping;

                    KeyMappingGrid.UpdateLayout();
                    KeyMappingGrid.ScrollIntoView(KeyMappingGrid.Items[i]);
                    KeyMappingGrid.SelectedIndex = i;
                }
                return;
            }
            if (KeyMappingGrid.SelectedIndex == -1)
                return;
            if (keyFile.keyAssign[currentIndex].GetVisibility() != "White")
                return;

            Pinky pinkyStatus = Pinky.UnShift;
            if (Select_PinkyShift.IsChecked == false)
                pinkyStatus = Pinky.Shift;

            KeyMappingGrid.ScrollIntoView(KeyMappingGrid.Items[currentIndex]);
            KeyMappingGrid.SelectedIndex = currentIndex;
            if (pinkyStatus == Pinky.UnShift)
                keyFile.keyAssign[currentIndex].SetKeyboard(catchedScanCode, Shift, Ctrl, Alt);
            if (pinkyStatus == Pinky.Shift)
                keyFile.keyAssign[currentIndex].Setkeycombo(catchedScanCode, Shift, Ctrl, Alt);

            for (int i = 0; i < keyFile.keyAssign.Length; i++)
            {
                if (keyFile.keyAssign[i].GetKeyAssignmentStatus() != keyFile.keyAssign[currentIndex].GetKeyAssignmentStatus())
                    continue;
                if (i == currentIndex)
                    continue;
                if (keyFile.keyAssign[i].GetVisibility() != "White")
                    continue;
                keyFile.keyAssign[i].UnassignKeyboard();
            }

            KeyMappingGrid.Items.Refresh();
            KeyMappingGrid.UnselectAllCells();
            statusSearch = Search.Search;
        }

        /// <summary>
        /// So this was... keyboard, I suppose.
        /// </summary>
        DirectInputKeyboard directInputDevice = new DirectInputKeyboard();
        class DirectInputKeyboard
        {
            Microsoft.DirectX.DirectInput.Device device;
            Microsoft.DirectX.DirectInput.KeyboardState keyState;
            public KeyboardState KeyboardState
            {
                get { return keyState; }
            }
            public DirectInputKeyboard()
            {
                device = new Microsoft.DirectX.DirectInput.Device(SystemGuid.Keyboard);
                device.Acquire();
            }
            public void GetCurrentKeyboardState()
            {
                keyState = device.GetCurrentKeyboardState();
            }
        }

        /// <summary>
        /// Invoke Status.
        /// </summary>
        private Invoke invokeStatus = Invoke.Default;
        private void Select_Invoke_Click(object sender, RoutedEventArgs e)
        {
            switch (invokeStatus)
            {
                case Invoke.Default:
                    invokeStatus = Invoke.Down;
                    Select_Invoke.Content = "INVOKE KEYDN";
                    Select_Invoke.Background = new SolidColorBrush(Color.FromArgb(0xFF, 0x99, 0xD9, 0xEA));
                    break;
                case Invoke.Down:
                    invokeStatus = Invoke.Up;
                    Select_Invoke.Content = "INVOKE KEYUP";
                    Select_Invoke.Background = new SolidColorBrush(Color.FromArgb(0xFF, 0x99, 0xD9, 0xEA));
                    break;
                case Invoke.Up:
                    invokeStatus = Invoke.Default;
                    Select_Invoke.Content = "INVOKE BOTH";
                    Select_Invoke.Background = new SolidColorBrush(Color.FromArgb(0xFF, 0xF7, 0xF7, 0xF7));
                    break;
            }
        }

        private void Save_As_Click(object sender, RoutedEventArgs e)
        {
            this.inSaveDialog = true;
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.InitialDirectory = this.appReg.GetInstallDir() + "\\User\\Config";
            sfd.Filter = "BMS Key files|*.key";
            sfd.OverwritePrompt = true;
         
            sfd.ShowDialog();
            if (sfd.FileName != "")
            {
                this.appReg.getOverrideWriter().SaveKeyFile(inGameAxis, deviceControl, keyFile, sfd.FileName);
            }
            this.inSaveDialog = false;
        }

        /// <summary>
        /// Let's jump to a category you have selected.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Category_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string target = "";
            switch (Category.SelectedIndex)
            {
                case 0:
                    target = "BMS - Full";
                    break;
                case 1:
                    target = "1. UI & 3RD PARTY SOFTWARE";
                    break;
                case 2:
                    target = "2. LEFT CONSOLE";
                    break;
                case 3:
                    target = "======== 2.19     THROTTLE QUADRANT SYSTEM ========";
                    break;
                case 4:
                    target = "3. LEFT AUX CONSOLE";
                    break;
                case 5:
                    target = "4. CENTER CONSOLE";
                    break;
                case 6:
                    target = "======== 4.05     LEFT MFD ========";
                    break;
                case 7:
                    target = "======== 4.10     RIGHT MFD ========";
                    break;
                case 8:
                    target = "5. RIGHT CONSOLE";
                    break;
                case 9:
                    target = "======== 5.11     FLIGHT STICK  ========";
                    break;
                case 10:
                    target = "6. MISCELLANEOUS";
                    break;
                case 11:
                    target = "7. VIEWS";
                    break;
                case 12:
                    target = "8. RADIO COMMS";
                    break;
            }

            int i = 0;
            foreach (KeyAssgn keys in keyFile.keyAssign)
            {
                if (keys.Mapping.Trim() == target)
                {
                    KeyMappingGrid.ScrollIntoView(KeyMappingGrid.Items[KeyMappingGrid.Items.Count - 1]);
                    KeyMappingGrid.UpdateLayout();
                    KeyMappingGrid.ScrollIntoView(KeyMappingGrid.Items[i]);
                }
                i += 1;
            }
        }

        /// <summary>
        /// Something has been entered to a search box.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Search_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Input.Keyboard.ClearFocus();

            if (SearchBox.Text == "")
                return;

            KeyMappingGrid.UnselectAllCells();
            KeyMappingGrid.ItemsSource = null;

            //if (SearchBox.Text == "")
            //{
            //    foreach (KeyAssgn Assgn in keyFile.keyAssign)
            //        Assgn.Visibility = Assgn.GetVisibility();
            //    KeyMappingGrid.Items.Refresh();
            //    KeyMappingGrid.UnselectAllCells();
            //    return;
            //}

            string target = SearchBox.Text;

            //foreach (KeyAssgn Assgn in keyFile.keyAssign)
            //{
            //    if (Assgn.Mapping.Trim().Contains(target))
            //        Assgn.Visibility = Assgn.GetVisibility();
            //    else
            //    {
            //        Assgn.Visibility = "Hidden";
            //    }
            //}
            
            this.KeyMappingGrid.ItemsSource = keyFile.keyAssign;
            KeyMappingGrid.Items.Refresh();

            int i = 0;
            foreach (KeyAssgn keys in keyFile.keyAssign)
            {
                if (keys.Mapping.Trim().Contains(target))
                {
                    KeyMappingGrid.ScrollIntoView(KeyMappingGrid.Items[KeyMappingGrid.Items.Count - 1]);
                    KeyMappingGrid.UpdateLayout();
                    KeyMappingGrid.ScrollIntoView(KeyMappingGrid.Items[i]);

                    return;
                }
                i += 1;
            }
        }

        /// <summary>
        /// Cullent selected row number.
        /// </summary>
        private int currentIndex;
        private void KeyMappingGrid_MouseUp(object sender, MouseButtonEventArgs e)
        {
            currentIndex = KeyMappingGrid.SelectedIndex;
        }
    }
}