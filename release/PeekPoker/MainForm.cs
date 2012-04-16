﻿using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace PeekPoker
{
    public partial class MainForm : Form
    {
        #region global varibales
        private RealTimeMemory _rtm;//DLL is now in the Important File Folder
        private readonly AutoCompleteStringCollection _data = new AutoCompleteStringCollection();
        private readonly string _filepath = (Application.StartupPath + "\\XboxIP.txt"); //For IP address loading - 8Ball
        private uint _searchRangeDumpLength;
        #endregion

        public MainForm()
        {
            InitializeComponent();
        }
        private void Form1Load(Object sender, EventArgs e)
        {
            //This is for handling automatic loading of the IP address and txt file creation. -8Ball
            if (!File.Exists(_filepath)) File.Create(_filepath).Dispose();
            else ipAddressTextBox.Text = File.ReadAllText(_filepath);
        }

        #region button clicks
        private void ConnectButtonClick(object sender, EventArgs e)
        {
            _rtm = new RealTimeMemory(ipAddressTextBox.Text,0,0);//initialize real time memory
            try
            {
                if (!_rtm.Connect())throw new Exception("Connection Failed!");
                panel1.Enabled = true;
                statusStripLabel.Text = String.Format("Connected");
                MessageBox.Show(this, String.Format("Connected"), String.Format("Peek Poker"), MessageBoxButtons.OK,MessageBoxIcon.Information);
                var objWriter = new StreamWriter(_filepath); //Writer Declaration
                objWriter.Write(ipAddressTextBox.Text); //Writes IP address to text file
                objWriter.Close(); //Close Writer
                connectButton.Text = String.Format("Re-Connect"); 
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, String.Format("Peek Poker"), MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
            }
        }

        private void PeekButtonClick(object sender, EventArgs e)
        {
            AutoComplete();//run function
            try
            {
                if (string.IsNullOrEmpty(peekLengthTextBox.Text))
                    throw new Exception("Invalide peek length!");
                byte[] retValue = Functions.StringToByteArray(_rtm.Peek(PeekPokeAddressTextBox.Text, peekLengthTextBox.Text, PeekPokeAddressTextBox.Text, peekLengthTextBox.Text));
                Be.Windows.Forms.DynamicByteProvider buffer = new Be.Windows.Forms.DynamicByteProvider(retValue);
                buffer.IsWriteByte = true;
                hexBox.ByteProvider = buffer;
                hexBox.Refresh();
                // the changed are handled automatically with my modifications of Be.HexBox

                MessageBox.Show(this, String.Format("Done!"), String.Format("Peek Poker"), MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, String.Format("Peek Poker"), MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
            }
        }

        private void PokeButtonClick(object sender, EventArgs e)
        {
            AutoComplete(); //run function
            try
            {
                _rtm.DumpOffset = Functions.Convert(PeekPokeAddressTextBox.Text);//Set the dump offset
                _rtm.DumpLength = (uint)hexBox.ByteProvider.Length / 2;//The length of data to dump

                Be.Windows.Forms.DynamicByteProvider buffer = (Be.Windows.Forms.DynamicByteProvider)hexBox.ByteProvider;

                Console.WriteLine(Functions.ByteArrayToString(buffer.Bytes.ToArray()));
                _rtm.Poke(PeekPokeAddressTextBox.Text, Functions.ByteArrayToString(buffer.Bytes.ToArray()));
                MessageBox.Show(this, String.Format("Done!"), String.Format("Peek Poker"), MessageBoxButtons.OK,MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, String.Format("Peek Poker"), MessageBoxButtons.OK,MessageBoxIcon.Error);
            }
        }

        private void ToolStripStatusLabel2Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("www.360haven.com");
        }

        private void NewPeekButtonClick(object sender, EventArgs e)
        {
            NewPeek();
        }

        private void SearchRangeButtonClick(object sender, EventArgs e)
        {
            //if (peekResultTextBox.Text.Equals("")) // Check if you have peeked code yet.
            //{ ShowMessageBox("Please peek the memory first.", string.Format("Peek Poker"), MessageBoxButtons.OK, MessageBoxIcon.Error); }
            //else //If you have peeked it continues
            //{
                try
                {
                    _searchRangeDumpLength = (Functions.Convert(endRangeAddressTextBox.Text) - Functions.Convert(startRangeAddressTextBox.Text));
                    dumpLengthTextBoxReadOnly.Text = _searchRangeDumpLength.ToString();
                    var oThread = new Thread(SearchRange);
                    oThread.Start();
                }
                catch (Exception ex)
                {
                    ShowMessageBox(ex.Message, string.Format("Peek Poker"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            //}
        }
        
        #endregion

        #region functions
        private void NewPeek()
        {
            //Clean up
            PeekPokeAddressTextBox.Clear();
            peekLengthTextBox.Clear();
            hexBox.ByteProvider = null;
            hexBox.Refresh();
        }
        private void AutoComplete()
        {
            PeekPokeAddressTextBox.AutoCompleteCustomSource = _data;//put the auto complete data into the textbox
            var count = _data.Count;
            for (var index = 0; index < count; index++)
            {
                var value = _data[index];
                //if the text in peek or poke text box is not in autocomplete data - Add it
                if (!ReferenceEquals(value, PeekPokeAddressTextBox.Text))
                    _data.Add(PeekPokeAddressTextBox.Text);
            }
        }
        private void SearchRange()
        {
            try
            {
                //You can always use: 
                //CheckForIllegalCrossThreadCalls = false; when dealing with threads
                _rtm.DumpOffset = GetStartRangeAddressTextBoxText();
                _rtm.DumpLength = _searchRangeDumpLength;

                //The FindHexOffset function is slow in searching - I might use Mojo's algorithm
                var offsets = _rtm.FindHexOffset(GetSearchRangeValueTextBoxText());//pointer
                if(offsets == null)
                {
                    ShowMessageBox(string.Format("No result/s found!"), string.Format("Peek Poker"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return; //We don't want it to continue
                }
                SearchRangeListViewListClean();//Clean list view
                var i = 0; //The index number
                foreach (var offset in offsets)
                {
                    //Collection initializer or use array either will do
                    //put the numnber @index 0
                    //put the hex offset @index 1
                    var newOffset = new[]{i.ToString(), offset};
                    //send the newOffset details to safe thread which adds to listview
                    SearchRangeListViewListUpdate(newOffset);
                    i++;
                }
            }
            catch (Exception e)
            {
                ShowMessageBox(e.Message, string.Format("Peek Poker"),MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            
        }
        #endregion

        #region safeThreadingProperties
        //==================================================================
        // This method demonstrates a pattern for making thread-safe
        // calls on a Windows Forms control. 
        //
        // If the calling thread is different from the thread that
        // created the TextBox control, this method creates a
        // Set/Get Method and calls itself asynchronously using the
        // Invoke method.
        //
        // If the calling thread is the same as the thread that created
        // the TextBox control, the Text property is set directly. 
        //Reference: http://msdn.microsoft.com/en-us/library/ms171728.aspx
        //===================================================================

        private String GetSearchRangeValueTextBoxText()//Get the value from the textbox - safe
        {
            //recursion
            var returnVal = "";
            if(searchRangeValueTextBox.InvokeRequired)searchRangeValueTextBox.Invoke((MethodInvoker)
                delegate{returnVal = GetSearchRangeValueTextBoxText();});
            else 
                return searchRangeValueTextBox.Text;
            return returnVal;
        }
        private uint GetStartRangeAddressTextBoxText()
        {
            //recursion
            uint returnVal = 0;
            if (startRangeAddressTextBox.InvokeRequired) startRangeAddressTextBox.Invoke((MethodInvoker)
                  delegate { returnVal = GetStartRangeAddressTextBoxText(); });
            else
                return Functions.Convert(startRangeAddressTextBox.Text);
            return returnVal;
        }
        private void ShowMessageBox(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon)
        {
            //Using lambda express - I believe its slower - Just an example
            if (InvokeRequired)
                Invoke((MethodInvoker)(() => ShowMessageBox(text, caption, buttons, icon)));
            else MessageBox.Show(this, text, caption, buttons, icon);
        }
        private void SearchRangeListViewListUpdate(string[] data)
        {
            //IList or represents a collection of objects(String)
            if (searchRangeResultListView.InvokeRequired)
                //lambda expression empty delegate that calls a recursive function if InvokeRequired
                searchRangeResultListView.Invoke((MethodInvoker)(() => SearchRangeListViewListUpdate(data)));
            else
            {
                searchRangeResultListView.Items.Add(new ListViewItem(data));
            }
        }
        private void SearchRangeListViewListClean()
        {
            if (searchRangeResultListView.InvokeRequired)
                searchRangeResultListView.Invoke((MethodInvoker)(SearchRangeListViewListClean));
            else
                searchRangeResultListView.Items.Clear();
        }
        #endregion

        #region Addressbox Autocorrection
        // These will automatically add "0x" to an offset if it hasn't been added already - 8Ball
        private void FixTheAddresses(object sender, EventArgs e)
        {
            if (!PeekPokeAddressTextBox.Text.StartsWith("0x")) //Peek Address Box, Formatting Check.
            {
                if (!PeekPokeAddressTextBox.Text.Equals("")) //Empty Check
                    PeekPokeAddressTextBox.Text = (string.Format("0x" + PeekPokeAddressTextBox.Text)); //Formatting
            }
            if (peekLengthTextBox.Text.StartsWith("0x")) // Checks if peek length is hex value or not based on 0x
            { //This could probably do with some cleanup -8Ball
                string result = (peekLengthTextBox.Text.ToUpper().Substring(2));
                uint result2 = UInt32.Parse(result, System.Globalization.NumberStyles.HexNumber);
                peekLengthTextBox.Text = result2.ToString();
            }
            else if (System.Text.RegularExpressions.Regex.IsMatch(peekLengthTextBox.Text.ToUpper(), "^[A-Z]$")) //Checks if hex, based on uppercase alphabet presence.
            {
                string result = (peekLengthTextBox.Text.ToUpper());
                uint result2 = UInt32.Parse(result, System.Globalization.NumberStyles.HexNumber);
                peekLengthTextBox.Text = result2.ToString();
            }
            if (!startRangeAddressTextBox.Text.StartsWith("0x"))
            {
                if (!startRangeAddressTextBox.Text.Equals(""))
                    startRangeAddressTextBox.Text = (string.Format("0x" + startRangeAddressTextBox.Text));
            }
            if (!endRangeAddressTextBox.Text.StartsWith("0x"))
            {
                if (!endRangeAddressTextBox.Text.Equals(""))
                    endRangeAddressTextBox.Text = (string.Format("0x" + endRangeAddressTextBox.Text));
            }
        }
        #endregion 
    }
}
