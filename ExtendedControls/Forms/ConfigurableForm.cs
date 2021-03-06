﻿/*
 * Copyright © 2017-2019 EDDiscovery development team
 *
 * Licensed under the Apache License, Version 2.0 (the "License"); you may not use this
 * file except in compliance with the License. You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software distributed under
 * the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
 * ANY KIND, either express or implied. See the License for the specific language
 * governing permissions and limitations under the License.
 * 
 * EDDiscovery is not affiliated with Frontier Developments plc.
 */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ExtendedControls
{
    public class ConfigurableForm : DraggableForm
    {
        // You give an array of Entries describing the controls
        // either added programatically by Add(entry) or via a string descriptor Add(string) (See action document for string descriptor format)
        // Directly Supported Types (string name/base type)
        //      "button" ExtButton, "textbox" ExtTextBox, "checkbox" ExtCheckBox 
        //      "label" Label, "datetime" ExtDateTimePicker,
        //      "numberboxdouble" NumberBoxDouble, "numberboxlong" NumberBoxLong, 
        //      "combobox" ExtComboBox
        // Or any type if you use Add(control, name..)

        // Lay the thing out like its in the normal dialog editor, with 8.25f font.  Leave space for the window less title bar/close icon.

        // returns dialog logical name, name of control (plus options), caller tag object
        // name of control on click for button / Checkbox / ComboBox
        // name:Return for number box, textBox.  Set SwallowReturn to true before returning to swallow the return key
        // name:Validity:true/false for Number boxes
        // Close if the close button is pressed
        // Escape if escape pressed

        public event Action<string, string, Object> Trigger;

        public int BottomMargin { get; set; } = 8;      // Extra space right/bot to allow for extra space past the controls
        public int RightMargin { get; set; } = 8;       // Size this at 8.25f font size, it will be scaled to suit. 

        public bool SwallowReturn { get; set; }     // set in your trigger handler to swallow the return. Otherwise, return is return

        public class Entry
        {
            public string controlname;                  // logical name of control
            public Type controltype;                    // if non null, activate this type.  Else if null, control should be filled up with your specific type

            public string text;                         // for certain types, the text
            public System.Drawing.Point pos;
            public System.Drawing.Size size;
            public string tooltip;                      // can be null.

            // ButtonExt, TextBoxBorder, Label, CheckBoxCustom, DateTime (t=time)
            public Entry(string nam, Type c, string t, System.Drawing.Point p, System.Drawing.Size s, string tt)
            {
                controltype = c; text = t; pos = p; size = s; tooltip = tt; controlname = nam; customdateformat = "long";
            }

            public Entry(string nam, Type c, string t, System.Drawing.Point p, System.Drawing.Size s, string tt, float fontscale, ContentAlignment align = ContentAlignment.MiddleCenter)
            {
                controltype = c; text = t; pos = p; size = s; tooltip = tt; controlname = nam; customdateformat = "long"; PostThemeFontScale = fontscale; textalign = align;
            }

            // ComboBoxCustom
            public Entry(string nam, string t, System.Drawing.Point p, System.Drawing.Size s, string tt, List<string> comboitems)
            {
                controltype = typeof(ExtendedControls.ExtComboBox); text = t; pos = p; size = s; tooltip = tt; controlname = nam;
                comboboxitems = string.Join(",", comboitems);
            }

            // custom

            public Entry(Control c, string nam, string t, System.Drawing.Point p, System.Drawing.Size s, string tt)
            {
                controlname = nam; control = c; text = t; pos = p; size = s; tooltip = tt; textalign = ContentAlignment.TopLeft;
            }

            public ContentAlignment? textalign;  // label,button. nominal not applied
            public bool checkboxchecked;        // fill in for checkbox
            public bool textboxmultiline;       // fill in for textbox
            public bool clearonfirstchar;       // fill in for textbox
            public string comboboxitems;        // fill in for combobox. comma separ list.
            public string customdateformat;     // fill in for datetimepicker
            public double numberboxdoubleminimum = double.MinValue;   // for double box
            public double numberboxdoublemaximum = double.MaxValue;
            public long numberboxlongminimum = long.MinValue;   // for long box
            public long numberboxlongmaximum = long.MaxValue;
            public string numberboxformat;      // for both number boxes

            public float PostThemeFontScale = 1.0f;   // post theme font scaler

            public Control control; // if controltype is set, don't set.  If controltype=null, pass your control type.
        }

        private System.ComponentModel.IContainer components = null;     // replicate normal component container, so controls which look this
                                                                        // up for finding the tooltip can (TextBoxBorder)

        #region Public interface

        public ConfigurableForm()
        {
            this.components = new System.ComponentModel.Container();
            entries = new List<Entry>();
            lastpos = new System.Drawing.Point(0, 0);
        }

        public string Add(string instr)       // add a string definition dynamically add to list.  errmsg if something is wrong
        {
            Entry e;
            string errmsg = MakeEntry(instr, out e, ref lastpos);
            if (errmsg == null)
                entries.Add(e);
            return errmsg;
        }

        public void Add(Entry e)               // add an entry..
        {
            entries.Add(e);
        }

        public void AddOK(Point p, string tooltip = null, Size? sz = null)
        {
            if (sz == null)
                sz = new Size(80, 24);
            Add(new Entry("OK", typeof(ExtendedControls.ExtButton), "OK".Tx(), p, sz.Value, tooltip));
        }

        public void AddCancel(Point p, string tooltip = null, Size? sz = null)
        {
            if (sz == null)
                sz = new Size(80, 24);
            Add(new Entry("Cancel", typeof(ExtendedControls.ExtButton), "Cancel".Tx(), p, sz.Value, tooltip));
        }

        public Entry Last { get { return entries.Last(); } }

        // pos.x <= -999 means autocentre to parent.

        public DialogResult ShowDialogCentred(Form p, Icon icon, string caption, string lname = null, Object callertag = null, Action callback = null, bool closeicon = false)
        {
            InitCentred(p, icon, caption, lname, callertag, closeicon: closeicon);
            callback?.Invoke();
            return ShowDialog(p);
        }

        public void InitCentred(Form p, Icon icon, string caption, string lname = null, Object callertag = null, AutoScaleMode asm = AutoScaleMode.Font, bool closeicon = false)
        {
            Init(icon, new Point((p.Left + p.Right) / 2, (p.Top + p.Bottom) / 2), caption, lname, callertag, closeicon, HorizontalAlignment.Center, ControlHelpersStaticFunc.VerticalAlignment.Middle, asm);
        }

        public void Init(Point pos, Icon icon, string caption, string lname = null, Object callertag = null, AutoScaleMode asm = AutoScaleMode.Font, bool closeicon = false)
        {
            Init(icon, pos, caption, lname, callertag, closeicon, null, null, asm);
        }

        public void ReturnResult(DialogResult result)           // MUST call to return result and close.  DO NOT USE DialogResult directly
        {
            ProgClose = true;
            DialogResult = result;
            base.Close();
        }

        public T GetControl<T>(string controlname) where T : Control      // return value of dialog control
        {
            Entry t = entries.Find(x => x.controlname.Equals(controlname, StringComparison.InvariantCultureIgnoreCase));
            if (t != null)
                return (T)t.control;
            else
                return null;
        }

        public string Get(string controlname)      // return value of dialog control
        {
            Entry t = entries.Find(x => x.controlname.Equals(controlname, StringComparison.InvariantCultureIgnoreCase));
            if (t != null)
            {
                Control c = t.control;
                if (c is ExtendedControls.ExtTextBox)
                    return (c as ExtendedControls.ExtTextBox).Text;
                else if (c is ExtendedControls.ExtCheckBox)
                    return (c as ExtendedControls.ExtCheckBox).Checked ? "1" : "0";
                else if (c is ExtendedControls.ExtDateTimePicker)
                    return (c as ExtendedControls.ExtDateTimePicker).Value.ToString("yyyy/dd/MM HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
                else if (c is ExtendedControls.NumberBoxDouble)
                {
                    var cn = c as ExtendedControls.NumberBoxDouble;
                    return cn.IsValid ? cn.Value.ToStringInvariant() : "INVALID";
                }
                else if (c is ExtendedControls.NumberBoxLong)
                {
                    var cn = c as ExtendedControls.NumberBoxLong;
                    return cn.IsValid ? cn.Value.ToStringInvariant() : "INVALID";
                }
                else if (c is ExtendedControls.ExtComboBox)
                {
                    ExtendedControls.ExtComboBox cb = c as ExtendedControls.ExtComboBox;
                    return (cb.SelectedIndex != -1) ? cb.Text : "";
                }
            }

            return null;
        }

        public double? GetDouble(string controlname)     // Null if not valid
        {
            Entry t = entries.Find(x => x.controlname.Equals(controlname, StringComparison.InvariantCultureIgnoreCase));
            if (t != null)
            {
                var cn = t.control as ExtendedControls.NumberBoxDouble;
                if (cn.IsValid)
                    return cn.Value;
            }
            return null;
        }

        public long? GetLong(string controlname)     // Null if not valid
        {
            Entry t = entries.Find(x => x.controlname.Equals(controlname, StringComparison.InvariantCultureIgnoreCase));
            if (t != null)
            {
                var cn = t.control as ExtendedControls.NumberBoxLong;
                if (cn.IsValid)
                    return cn.Value;
            }
            return null;
        }

        public DateTime? GetDateTime(string controlname)
        {
            Entry t = entries.Find(x => x.controlname.Equals(controlname, StringComparison.InvariantCultureIgnoreCase));
            if (t != null)
            {
                ExtDateTimePicker c = t.control as ExtDateTimePicker;
                if (c != null)
                    return c.Value;
            }

            return null;
        }

        public bool Set(string controlname, string value)      // set value of dialog control
        {
            Entry t = entries.Find(x => x.controlname.Equals(controlname, StringComparison.InvariantCultureIgnoreCase));
            if (t != null)
            {
                Control c = t.control;
                if (c is ExtendedControls.ExtTextBox)
                {
                    (c as ExtendedControls.ExtTextBox).Text = value;
                    return true;
                }
                else if (c is ExtendedControls.ExtCheckBox)
                {
                    (c as ExtendedControls.ExtCheckBox).Checked = !value.Equals("0");
                    return true;
                }
                else if (c is ExtendedControls.ExtComboBox)
                {
                    ExtendedControls.ExtComboBox cb = c as ExtendedControls.ExtComboBox;
                    if (cb.Items.Contains(value))
                    {
                        cb.Enabled = false;
                        cb.SelectedItem = value;
                        cb.Enabled = true;
                        return true;
                    }
                }
                else if (c is ExtendedControls.NumberBoxDouble)
                {
                    var cn = c as ExtendedControls.NumberBoxDouble;
                    double? v = value.InvariantParseDoubleNull();
                    if (v.HasValue)
                    {
                        cn.Value = v.Value;
                        return true;
                    }
                }
                else if (c is ExtendedControls.NumberBoxLong)
                {
                    var cn = c as ExtendedControls.NumberBoxLong;
                    long? v = value.InvariantParseLongNull();
                    if (v.HasValue)
                    {
                        cn.Value = v.Value;
                        return true;
                    }
                }
            }

            return false;
        }

        public bool SetEnabled(string controlname, bool state)      // set enable state of dialog control
        {
            Entry t = entries.Find(x => x.controlname.Equals(controlname, StringComparison.InvariantCultureIgnoreCase));
            if (t != null)
            {
                var cn = t.control as Control;
                cn.Enabled = state;
                return true;
            }
            else
                return false;
        }


        #endregion

        #region Implementation

        private void Init(Icon icon, System.Drawing.Point pos, string caption, string lname, Object callertag, bool closeicon,
                                HorizontalAlignment? halign = null, ControlHelpersStaticFunc.VerticalAlignment? valign = null, AutoScaleMode asm = AutoScaleMode.Font)
        {
            this.logicalname = lname;    // passed back to caller via trigger
            this.callertag = callertag;      // passed back to caller via trigger

            this.halign = halign;
            this.valign = valign;

            ITheme theme = ThemeableFormsInstance.Instance;

            FormBorderStyle = FormBorderStyle.FixedDialog;

            outer = new ExtPanelScroll() { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle, Margin = new Padding(0), Padding = new Padding(0) };
            outer.MouseDown += FormMouseDown;
            outer.MouseUp += FormMouseUp;
            Controls.Add(outer);

            ExtScrollBar scr = new ExtScrollBar();
            scr.HideScrollBar = true;
            outer.Controls.Add(scr);

            this.Text = caption;

            int yoffset = 0;                            // adjustment to move controls up if windows frame present.

            if (theme.WindowsFrame)
            {
                yoffset = int.MaxValue;
                for (int i = 0; i < entries.Count; i++)             // find minimum control Y
                    yoffset = Math.Min(yoffset, entries[i].pos.Y);

                yoffset -= 8;           // place X spaces below top
            }
            else
            {
                titlelabel = new Label() { Left = 4, Top = 8, Width = 10, Text = caption, AutoSize = true }; // autosize it, and set width small so it does not mess up the computation below
                titlelabel.MouseDown += FormMouseDown;
                titlelabel.MouseUp += FormMouseUp;
                titlelabel.Name = "title";
                outer.Controls.Add(titlelabel);

                if (closeicon)
                {
                    closebutton = new ExtButtonDrawn();
                    closebutton.Size = new Size(18, 18);
                    closebutton.Location = new Point(0, 0);     // purposely at top left to make it not contribute to overall size

                    closebutton.ImageSelected = ExtButtonDrawn.ImageType.Close;
                    closebutton.Click += (sender, f) =>
                    {
                        if (!ProgClose)
                        {
                            Trigger?.Invoke(logicalname, "Close", callertag);
                        }
                    };

                    outer.Controls.Add(closebutton);            // add now so it gets themed
                }
            }

            ToolTip tt = new ToolTip(components);
            tt.ShowAlways = true;

            for (int i = 0; i < entries.Count; i++)
            {
                Entry ent = entries[i];
                Control c = ent.controltype != null ? (Control)Activator.CreateInstance(ent.controltype) : ent.control;
                ent.control = c;
                c.Size = ent.size;
                c.Location = new Point(ent.pos.X, ent.pos.Y - yoffset);
                c.Name = ent.controlname;
                //System.Diagnostics.Debug.WriteLine("Control " + c.GetType().ToString() + " at " + c.Location + " " + c.Size);
                if (!(ent.controltype == null || c is ExtendedControls.ExtComboBox || c is ExtendedControls.ExtDateTimePicker || c is ExtendedControls.NumberBoxDouble || c is ExtendedControls.NumberBoxLong))        // everything but get text
                    c.Text = ent.text;
                c.Tag = ent;     // point control tag at ent structure
                outer.Controls.Add(c);
                if (ent.tooltip != null)
                    tt.SetToolTip(c, ent.tooltip);

                if (c is Label)
                {
                    Label l = c as Label;
                    if (ent.textalign.HasValue)
                        l.TextAlign = ent.textalign.Value;
                }
                else if (c is ExtendedControls.ExtButton)
                {
                    ExtendedControls.ExtButton b = c as ExtendedControls.ExtButton;
                    if (ent.textalign.HasValue)
                        b.TextAlign = ent.textalign.Value;
                    b.Click += (sender, ev) =>
                    {
                        if (!ProgClose)
                        {
                            Entry en = (Entry)(((Control)sender).Tag);
                            Trigger?.Invoke(logicalname, en.controlname, this.callertag);       // pass back the logical name of dialog, the name of the control, the caller tag
                        }
                    };
                }
                else if (c is ExtendedControls.NumberBoxDouble)
                {
                    ExtendedControls.NumberBoxDouble cb = c as ExtendedControls.NumberBoxDouble;
                    cb.Minimum = ent.numberboxdoubleminimum;
                    cb.Maximum = ent.numberboxdoublemaximum;
                    double? v = ent.text.InvariantParseDoubleNull();
                    cb.Value = v.HasValue ? v.Value : cb.Minimum;
                    if (ent.numberboxformat != null)
                        cb.Format = ent.numberboxformat;
                    cb.ReturnPressed += (box) =>
                    {
                        SwallowReturn = false;
                        if (!ProgClose)
                        {
                            Entry en = (Entry)(box.Tag);
                            Trigger?.Invoke(logicalname, en.controlname + ":Return", this.callertag);       // pass back the logical name of dialog, the name of the control, the caller tag
                        }

                        return SwallowReturn;
                    };
                    cb.ValidityChanged += (box, s) =>
                    {
                        if (!ProgClose)
                        {
                            Entry en = (Entry)(box.Tag);
                            Trigger?.Invoke(logicalname, en.controlname + ":Validity:" + s.ToString(), this.callertag);       // pass back the logical name of dialog, the name of the control, the caller tag
                        }
                    };
                }
                else if (c is ExtendedControls.NumberBoxLong)
                {
                    ExtendedControls.NumberBoxLong cb = c as ExtendedControls.NumberBoxLong;
                    cb.Minimum = ent.numberboxlongminimum;
                    cb.Maximum = ent.numberboxlongmaximum;
                    long? v = ent.text.InvariantParseLongNull();
                    cb.Value = v.HasValue ? v.Value : cb.Minimum;
                    if (ent.numberboxformat != null)
                        cb.Format = ent.numberboxformat;
                    cb.ReturnPressed += (box) =>
                    {
                        SwallowReturn = false;
                        if (!ProgClose)
                        {
                            Entry en = (Entry)(box.Tag);
                            Trigger?.Invoke(logicalname, en.controlname + ":Return", this.callertag);       // pass back the logical name of dialog, the name of the control, the caller tag
                        }
                        return SwallowReturn;
                    };
                    cb.ValidityChanged += (box, s) =>
                    {
                        if (!ProgClose)
                        {
                            Entry en = (Entry)(box.Tag);
                            Trigger?.Invoke(logicalname, en.controlname + ":Validity:" + s.ToString(), this.callertag);       // pass back the logical name of dialog, the name of the control, the caller tag
                        }
                    };
                }
                else if (c is ExtendedControls.ExtTextBox)
                {
                    ExtendedControls.ExtTextBox tb = c as ExtendedControls.ExtTextBox;
                    tb.Multiline = tb.WordWrap = ent.textboxmultiline;
                    tb.Size = ent.size;     // restate size in case multiline is on
                    tb.ClearOnFirstChar = ent.clearonfirstchar;
                    tb.ReturnPressed += (box) =>
                    {
                        SwallowReturn = false;
                        if (!ProgClose)
                        {
                            Entry en = (Entry)(box.Tag);
                            Trigger?.Invoke(logicalname, en.controlname + ":Return", this.callertag);       // pass back the logical name of dialog, the name of the control, the caller tag
                        }
                        return SwallowReturn;
                    };
                }
                else if (c is ExtendedControls.ExtCheckBox)
                {
                    ExtendedControls.ExtCheckBox cb = c as ExtendedControls.ExtCheckBox;
                    cb.Checked = ent.checkboxchecked;
                    cb.Click += (sender, ev) =>
                    {
                        if (!ProgClose)
                        {
                            Entry en = (Entry)(((Control)sender).Tag);
                            Trigger?.Invoke(logicalname, en.controlname, this.callertag);       // pass back the logical name of dialog, the name of the control, the caller tag
                        }
                    };
                }


                if (c is ExtendedControls.ExtDateTimePicker)
                {
                    ExtendedControls.ExtDateTimePicker dt = c as ExtendedControls.ExtDateTimePicker;
                    DateTime t;
                    if (DateTime.TryParse(ent.text, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeLocal, out t))     // assume local, so no conversion
                        dt.Value = t;

                    switch (ent.customdateformat.ToLowerInvariant())
                    {
                        case "short":
                            dt.Format = DateTimePickerFormat.Short;
                            break;
                        case "long":
                            dt.Format = DateTimePickerFormat.Long;
                            break;
                        case "time":
                            dt.Format = DateTimePickerFormat.Time;
                            break;
                        default:
                            dt.CustomFormat = ent.customdateformat;
                            break;
                    }
                }

                if (c is ExtendedControls.ExtComboBox)
                {
                    ExtendedControls.ExtComboBox cb = c as ExtendedControls.ExtComboBox;

                    cb.Items.AddRange(ent.comboboxitems.Split(','));
                    if (cb.Items.Contains(ent.text))
                        cb.SelectedItem = ent.text;
                    cb.SelectedIndexChanged += (sender, ev) =>
                    {
                        Control ctr = (Control)sender;
                        if (ctr.Enabled && !ProgClose)
                        {
                            Entry en = (Entry)(ctr.Tag);
                            Trigger?.Invoke(logicalname, en.controlname, this.callertag);       // pass back the logical name of dialog, the name of the control, the caller tag
                        }
                    };

                }
            }

            ShowInTaskbar = false;

            this.Icon = icon;

            this.AutoScaleMode = asm;

            //this.DumpTree(0);
            theme.ApplyStd(this);
            //theme.Apply(this, new Font("ms Sans Serif", 16f));
            //this.DumpTree(0);

            for (int i = 0; i < entries.Count; i++)     // post scale any controls which ask for different font ratio sizes
            {
                if (entries[i].PostThemeFontScale != 1.0f)
                {
                    entries[i].control.Font = new Font(entries[i].control.Font.Name, entries[i].control.Font.SizeInPoints * entries[i].PostThemeFontScale);
                }
            }

            // position 
            StartPosition = FormStartPosition.Manual;
            this.Location = pos;

            //System.Diagnostics.Debug.WriteLine("Bounds " + Bounds + " ClientRect " + ClientRectangle);
            //System.Diagnostics.Debug.WriteLine("Outer Bounds " + outer.Bounds + " ClientRect " + outer.ClientRectangle);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            int boundsh = Bounds.Height - ClientRectangle.Height;                   // allow for window border..  Only works after OnLoad.
            int boundsw = Bounds.Width - ClientRectangle.Width;
            int outerh = ClientRectangle.Height - outer.ClientRectangle.Height;     // any border on outer panel
            int outerw = ClientRectangle.Width - outer.ClientRectangle.Width;

            // get the scaling factor, we adjust the right/bottom margins accordingly

            var currentautocale = this.CurrentAutoScaleFactor();            // how much did we scale up?

            // measure the items after scaling. Exclude the scroll bar. Add on bounds/outers/margin

            Size measureitemsinwindow = outer.FindMaxSubControlArea(boundsw + outerw + (int)(RightMargin * currentautocale.Width),
                                                                   boundsh + outerh + (int)(BottomMargin * currentautocale.Height),
                                                                   new Type[] { typeof(ExtScrollBar) }, true);


            // now position in the screen, allowing for a scroll bar if required due to height restricted

            this.PositionSizeWithinScreen(measureitemsinwindow.Width, measureitemsinwindow.Height, false, 64, halign, valign, outer.ScrollBarWidth);

            if (closebutton != null)      // now position close at correct place, its not contributed to overall size
                closebutton.Location = new Point(outer.Width - closebutton.Width - titlelabel.Left, titlelabel.Top + titlelabel.Height / 2 - closebutton.Height / 2);

            //System.Diagnostics.Debug.WriteLine("Form Load" + Bounds + " " + ClientRectangle);
        }

        protected override void OnShown(EventArgs e)
        {
            Control firsttextbox = outer.Controls.FirstY(new Type[] { typeof(ExtRichTextBox), typeof(ExtTextBox), typeof(ExtTextBoxAutoComplete), typeof(NumberBoxDouble), typeof(NumberBoxFloat), typeof(NumberBoxLong) });
            if (firsttextbox != null)
                firsttextbox.Focus();       // focus on first text box
            base.OnShown(e);
            //System.Diagnostics.Debug.WriteLine("Form Shown " + Bounds + " " + ClientRectangle);

        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (ProgClose == false)
            {
                Trigger?.Invoke(logicalname, "Close", callertag);
                e.Cancel = ProgClose == false;     // if ProgClose is false, we don't want to close. Callback did not call ReturnResponse
            }

            base.OnFormClosing(e);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                Trigger?.Invoke(logicalname, "Escape", callertag);
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void FormMouseDown(object sender, MouseEventArgs e)
        {
            OnCaptionMouseDown((Control)sender, e);
        }

        private void FormMouseUp(object sender, MouseEventArgs e)
        {
            OnCaptionMouseUp((Control)sender, e);
        }

        #endregion

        #region Text creator

        static private string MakeEntry(string instr, out Entry entry, ref System.Drawing.Point lastpos)
        {
            entry = null;

            BaseUtils.StringParser sp = new BaseUtils.StringParser(instr);

            string name = sp.NextQuotedWordComma();

            if (name == null)
                return "Missing name";

            string type = sp.NextWordCommaLCInvariant();

            Type ctype = null;

            if (type == null)
                return "Missing type";
            else if (type.Equals("button"))
                ctype = typeof(ExtendedControls.ExtButton);
            else if (type.Equals("textbox"))
                ctype = typeof(ExtendedControls.ExtTextBox);
            else if (type.Equals("checkbox"))
                ctype = typeof(ExtendedControls.ExtCheckBox);
            else if (type.Equals("label"))
                ctype = typeof(System.Windows.Forms.Label);
            else if (type.Equals("combobox"))
                ctype = typeof(ExtendedControls.ExtComboBox);
            else if (type.Equals("datetime"))
                ctype = typeof(ExtendedControls.ExtDateTimePicker);
            else if (type.Equals("numberboxlong"))
                ctype = typeof(ExtendedControls.NumberBoxLong);
            else if (type.Equals("numberboxdouble"))
                ctype = typeof(ExtendedControls.NumberBoxDouble);
            else
                return "Unknown control type " + type;

            string text = sp.NextQuotedWordComma();     // normally text..

            if (text == null)
                return "Missing text";

            int? x = sp.NextWordComma().InvariantParseIntNullOffset(lastpos.X);
            int? y = sp.NextWordComma().InvariantParseIntNullOffset(lastpos.Y);
            int? w = sp.NextWordComma().InvariantParseIntNull();
            int? h = sp.NextWordComma().InvariantParseIntNull();

            if (x == null || y == null || w == null || h == null)
                return "Missing position/size";

            string tip = sp.NextQuotedWordComma();      // tip can be null

            entry = new ConfigurableForm.Entry(name, ctype,
                        text, new System.Drawing.Point(x.Value, y.Value), new System.Drawing.Size(w.Value, h.Value), tip);

            if (type.Contains("textbox") && tip != null)
            {
                int? v = sp.NextWordComma().InvariantParseIntNull();
                entry.textboxmultiline = v.HasValue && v.Value != 0;
            }

            if (type.Contains("checkbox") && tip != null)
            {
                int? v = sp.NextWordComma().InvariantParseIntNull();
                entry.checkboxchecked = v.HasValue && v.Value != 0;

                v = sp.NextWordComma().InvariantParseIntNull();
                entry.clearonfirstchar = v.HasValue && v.Value != 0;
            }

            if (type.Contains("combobox"))
            {
                entry.comboboxitems = sp.LineLeft.Trim();
                if (tip == null || entry.comboboxitems.Length == 0)
                    return "Missing paramters for combobox";
            }

            if (type.Contains("datetime"))
            {
                entry.customdateformat = sp.NextWord();
            }

            if (type.Contains("numberboxdouble"))
            {
                double? min = sp.NextWordComma().InvariantParseDoubleNull();
                double? max = sp.NextWordComma().InvariantParseDoubleNull();
                entry.numberboxdoubleminimum = min.HasValue ? min.Value : double.MinValue;
                entry.numberboxdoublemaximum = max.HasValue ? max.Value : double.MaxValue;
                entry.numberboxformat = sp.NextWordComma();
            }

            if (type.Contains("numberboxlong"))
            {
                long? min = sp.NextWordComma().InvariantParseLongNull();
                long? max = sp.NextWordComma().InvariantParseLongNull();
                entry.numberboxlongminimum = min.HasValue ? min.Value : long.MinValue;
                entry.numberboxlongmaximum = max.HasValue ? max.Value : long.MaxValue;
                entry.numberboxformat = sp.NextWordComma();
            }

            lastpos = new System.Drawing.Point(x.Value, y.Value);
            return null;
        }



        #endregion

        private List<Entry> entries;
        private Object callertag;
        private string logicalname;

        private bool ProgClose = false;

        private System.Drawing.Point lastpos; // used for dynamically making the list up

        private HorizontalAlignment? halign;
        private ControlHelpersStaticFunc.VerticalAlignment? valign;

        private ExtPanelScroll outer;
        private ExtButtonDrawn closebutton;
        private Label titlelabel;

    }
}
