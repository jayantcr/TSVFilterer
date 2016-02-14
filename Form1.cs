namespace TSVFilterer
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Drawing;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Windows.Forms;

    public partial class Form1 : Form
    {
        private const int RowHeight = 30;

        private const string ExcludeName = "Exclude_";

        private bool isAlreadyChecked;

        private string SourceFileName { get; set; }
        private string ResultFileName { get; set; }

        private readonly FilterSettings currentFilterSettings = new FilterSettings();
        
        public Form1()
        {
            this.InitializeComponent();
        }

        private void Button1Click(object sender, EventArgs e)
        {
            this.openFileDialog1.ShowDialog();
        }

        private void OpenFileDialog1FileOk(object sender, CancelEventArgs e)
        {
            this.SourceFileName = this.openFileDialog1.FileName;
            this.DisplayFilterOptions();
        }

        private void DisplayFilterOptions()
        {
            string readLine;
            using (var fileStream = new StreamReader(this.SourceFileName))
            {
                // Read until the first non-empty line
                readLine = fileStream.ReadLine();
                while (string.IsNullOrEmpty(readLine) && !fileStream.EndOfStream)
                {
                    readLine = fileStream.ReadLine();
                }
            }

            if (string.IsNullOrEmpty(readLine))
            {
                // TODO: Error message
                return;
            }

            var startPoint = new Point(0, 0);
            var colsCount = 0;
            var firstRow = readLine.Split('\t');

            // Add Column header
            var columnLabel = new Label
                                  {
                                      Text = @"Column", 
                                      Location = new Point(startPoint.X, startPoint.Y + (colsCount * RowHeight))
                                  };
            this.panel1.Controls.Add(columnLabel);

            // Add column names (i.e. first row)
            foreach (var column in firstRow)
            {
                colsCount++;
                var columnText = new TextBox
                                     {
                                         Text = column, 
                                         Location = new Point(startPoint.X, startPoint.Y + (colsCount * RowHeight)), 
                                         ReadOnly = true, 
                                         BorderStyle = BorderStyle.None, 
                                         Width = 180
                                     };
                this.panel1.Controls.Add(columnText);
            }

            // Add headers, check-boxes, and radio-buttons for filters
            this.DisplayFilterColumns(startPoint, firstRow.Length);

            var copyHeaders = new CheckBox
                                  {
                                      Checked = false, 
                                      Location = new Point(startPoint.X, startPoint.Y + (colsCount * RowHeight) + 50), 
                                      Width = 300, 
                                      Text = @"Copy first row as headers", 
                                      Name = @"HeadersPresent"
                                  };
            this.panel1.Controls.Add(copyHeaders);

            this.ResultFileName = this.SourceFileName + @"_filtered.tsv";

            var fileNameTextBox = new TextBox
                                      {
                                          Text = this.ResultFileName, 
                                          Name = @"OutputFileTextBox", 
                                          Location =
                                              new Point(
                                              startPoint.X, 
                                              startPoint.Y + (colsCount * RowHeight) + 100), 
                                          Width = 200
                                      };
            fileNameTextBox.TextChanged += this.FileNameTextBoxTextChanged;
            this.panel1.Controls.Add(fileNameTextBox);

            var filterButton = new Button
                                   {
                                       Text = @"Filter", 
                                       Location = new Point(startPoint.X + 250, startPoint.Y + (colsCount * RowHeight) + 100)
                                   };
            filterButton.Click += this.FilterButtonClick;
            this.panel1.Controls.Add(filterButton);

            var progressLabel = new Label
                                    {
                                        Name = @"ProgressLabel", 
                                        Location = new Point(startPoint.X, startPoint.Y + (colsCount * RowHeight) + 150)
                                    };
            this.panel1.Controls.Add(progressLabel);

            var errorLabel = new Label
                                 {
                                     Name = @"ErrorLabel", 
                                     Location = new Point(startPoint.X, startPoint.Y + (colsCount * RowHeight) + 200)
                                 };
            this.panel1.Controls.Add(errorLabel);
        }

        void FileNameTextBoxTextChanged(object sender, EventArgs e)
        {
            this.ResultFileName = ((TextBox)sender).Text;
        }

        private void DisplayFilterColumns(Point startPoint, int colsCount)
        {
            this.DisplayFilterHeaders(startPoint);
            
            this.currentFilterSettings.ExclusionColumnsList = new List<int>();
            this.currentFilterSettings.FilterStringsList = new List<KeyValuePair<int, string>>();
            this.currentFilterSettings.OutlierRemovalColumnsList = new List<int>();

            for (var i = 1; i <= colsCount; i++)
            {
                var checkBox = new CheckBox
                {
                    Checked = false,
                    Location =
                        new Point(startPoint.X + 200, startPoint.Y + (i * RowHeight) - 5),
                    Name = ExcludeName + i,
                    Width = 15
                };
                checkBox.CheckedChanged += this.AddRemoveColumns;
                this.panel1.Controls.Add(checkBox);

                var radioButton = new RadioButton
                                      {
                                          Location =
                                              new Point(
                                              startPoint.X + 345, 
                                              startPoint.Y + (i * RowHeight) - 5), 
                                          Name = i.ToString(CultureInfo.InvariantCulture), 
                                          Width = 15
                                      };
                radioButton.CheckedChanged += this.RadioButtonCheckedChanged;
                radioButton.Click += this.RadioButtonClick;
                this.panel1.Controls.Add(radioButton);

                checkBox = new CheckBox
                                   {
                                       Checked = false, 
                                       Location =
                                           new Point(startPoint.X + 430, startPoint.Y + (i * RowHeight) - 5), 
                                       Name = ConvertColumnNumberToName(i), 
                                       Width = 15
                                   };
                checkBox.CheckedChanged += this.AddRemoveOutliers;
                this.panel1.Controls.Add(checkBox);

                checkBox = new CheckBox
                               {
                                   Checked = false, 
                                   Location = new Point(startPoint.X + 490, startPoint.Y + (i * RowHeight) - 5), 
                                   Name = i.ToString(CultureInfo.InvariantCulture), 
                                   Width = 15
                               };
                checkBox.CheckedChanged += this.AddRemoveFilter;
                this.panel1.Controls.Add(checkBox);
            }
        }

        private void DisplayFilterHeaders(Point startPoint)
        {
            var excludeAllColumns = new CheckBox
            {
                Checked = false,
                Location = new Point(startPoint.X + 200, startPoint.Y - 5),
                Width = 110,
                Text = @"Exclude column"
            };
            excludeAllColumns.CheckedChanged += ExcludeAllColumnsCheckedChanged;
            this.panel1.Controls.Add(excludeAllColumns);

            var columnLabel = new Label
            {
                Text = @"Split by distinct",
                Location = new Point(startPoint.X + 310, startPoint.Y),
                Width = 80
            };
            this.panel1.Controls.Add(columnLabel);

            columnLabel = new Label
            {
                Text = @"Remove outliers",
                Location = new Point(startPoint.X + 390, startPoint.Y),
                Width = 90
            };
            this.panel1.Controls.Add(columnLabel);

            columnLabel = new Label
            {
                Text = @"Filter",
                Location = new Point(startPoint.X + 480, startPoint.Y),
                Width = 30
            };
            this.panel1.Controls.Add(columnLabel);
        }

        void RadioButtonClick(object sender, EventArgs e)
        {
            var radioButton = sender as RadioButton;
            if (radioButton == null)
            {
                return;
            }

            if (radioButton.Checked && !this.isAlreadyChecked)
            {
                radioButton.Checked = false;
            }
            else
            {
                radioButton.Checked = true;
                this.isAlreadyChecked = false;
            }
        }

        void RadioButtonCheckedChanged(object sender, EventArgs e)
        {
            var radioButton = sender as RadioButton;
            if (radioButton != null)
            {
                this.isAlreadyChecked = radioButton.Checked;
            }
        }

        private void ExcludeAllColumnsCheckedChanged(object sender, EventArgs e)
        {
            foreach (var currentControl in
                this.panel1.Controls.Cast<object>()
                    .Where(
                        currentControl =>
                        currentControl.GetType() == typeof(CheckBox)
                        && ((CheckBox)currentControl).Name.StartsWith(ExcludeName)))
            {
                ((CheckBox)currentControl).CheckState = ((CheckBox)sender).CheckState;
            }
        }

        private void AddRemoveColumns(object sender, EventArgs e)
        {
            var senderCheckBox = sender as CheckBox;
            if (senderCheckBox == null)
            {
                return;
            }

            var columnNumber = Convert.ToInt32(senderCheckBox.Name.Replace(ExcludeName, string.Empty));

            if (senderCheckBox.Checked)
            {
                this.currentFilterSettings.ExclusionColumnsList.Add(columnNumber);
            }
            else
            {
                this.currentFilterSettings.ExclusionColumnsList.Remove(columnNumber);
            }
        }

        private void AddRemoveOutliers(object sender, EventArgs e)
        {
            var senderCheckBox = sender as CheckBox;
            if (senderCheckBox == null)
            {
                return;
            }

            if (senderCheckBox.Checked)
            {
                this.currentFilterSettings.OutlierRemovalColumnsList.Add(Convert.ToInt32(senderCheckBox.Name));
            }
            else
            {
                this.currentFilterSettings.OutlierRemovalColumnsList.Remove(Convert.ToInt32(senderCheckBox.Name));
            }
        }

        private void AddRemoveFilter(object sender, EventArgs e)
        {
            var sourceCheckBox = sender as CheckBox;
            if (sourceCheckBox != null && sourceCheckBox.Checked)
            {
                var textBox = new TextBox
                {
                    Location =
                        new Point(sourceCheckBox.Location.X + 20, sourceCheckBox.Location.Y), 
                    Name = sourceCheckBox.Name,
                };
                this.panel1.Controls.Add(textBox);
            }
            else
            {
                if (sourceCheckBox == null)
                {
                    return;
                }

                var textBox = this.FindControlByName(sourceCheckBox.Name, typeof(TextBox));
                this.panel1.Controls.Remove(textBox);
            }
        }

        //private string GenerateOutlierRemovalString(int colsCount)
        //{
        //    var returnString = string.Empty;

        //    var lastCol = ConvertColumnNumberToName(colsCount + this.currentFilterSettings.OutlierRemovalColumnsList.Count + 1);
        //    foreach (string colName in this.outlierList)
        //    {
        //        var startColumn = colName
        //                          + (((CheckBox)this.FindControlByName("HeadersPresent", typeof(CheckBox))).Checked
        //                                 ? "2"
        //                                 : "1");
        //        var endColumn = ":INDIRECT(\"" + colName + "\"&(SUM(" + lastCol + ":" + lastCol;

        //        returnString += "\t=IF((ABS(" + startColumn + endColumn + ")+1)))-MEDIAN(" + startColumn + endColumn
        //                        + ")+1))))>(2*STDEV(" + startColumn + endColumn + ")+1)))), \"\", " + startColumn
        //                        + endColumn + ")+1)))";
        //    }

        //    return returnString;
        //}

        private Control FindControlByName(string name, Type type)
        {
            return this.panel1.Controls.Cast<Control>().FirstOrDefault(c => c.Name == name && c.GetType() == type);
        }
        
        private static string ConvertColumnNumberToName(long colNum)
        {
            var charArray = new ArrayList();
            while (colNum > 0)
            {
                charArray.Insert(0, (char)((colNum % 26) - 1 + 'A'));
                colNum /= 26;
            }

            return new string(charArray.ToArray(typeof(char)) as char[]);
        }

        private void FilterButtonClick(object sender, EventArgs e)
        {
            StartThread();
        }

        private void StartThread()
        {
            // Add filters
            foreach (var currentBox in from object control in this.panel1.Controls
                                       where control.GetType() == typeof(TextBox)
                                       select control as TextBox
                                           into currentBox
                                           where
                                               currentBox != null
                                               && (!string.IsNullOrEmpty(currentBox.Name)
                                                   && !string.IsNullOrEmpty(currentBox.Text))
                                           select currentBox)
            {
                // Empty catch block to filter out textboxes that don't have numbers as their name
                try
                {
                    this.currentFilterSettings.FilterStringsList.Add(
                        new KeyValuePair<int, string>(Convert.ToInt32(currentBox.Name), currentBox.Text));
                }
                catch (FormatException)
                {
                }
            }

            // Initialize the object that the background worker calls.
            var wc = new Filterer
                         {
                             SourceFile = this.SourceFileName,
                             ResultFile = this.ResultFileName,
                             InstanceFilterSettings = this.currentFilterSettings
                         };

            // Start the asynchronous operation.
            backgroundWorker1.RunWorkerAsync(wc);
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            var worker = (BackgroundWorker)sender;
            var wc = (Filterer)e.Argument;
            wc.Filter(worker, e);
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            var state = (Filterer.CurrentState)e.UserState;
            var label = this.FindControlByName("ProgressLabel", typeof(Label));
            label.Text = state.LinesParsed.ToString(CultureInfo.InvariantCulture);
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {

        }
    }
}
