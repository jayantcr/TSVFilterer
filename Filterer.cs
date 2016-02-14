using System;
using System.Collections;
using System.ComponentModel;
using System.IO;
using System.Linq;

namespace TSVFilterer
{
    public class Filterer
    {
        // Object to store the current state, for passing to the caller.
        public class CurrentState
        {
            public int LinesParsed;
        }

        public string SourceFile { get; set; }

        public string ResultFile { get; set; }

        public FilterSettings InstanceFilterSettings { get; set; }

        internal void Filter(BackgroundWorker worker, DoWorkEventArgs e)
        {
            // var state = new CurrentState();
            

            var errorRowNumbers = new ArrayList();
            var rowNum = 0;

            //var outlierRemoval = this.GenerateOutlierRemovalString(this.ColCount);
            //if (!string.IsNullOrEmpty(outlierRemoval))
            //{
            //    outlierRemoval += "\t1";
            //}

            var firstRowParsed = false;

            // Reorder excludeList in descending order in order to exclude columns later
            InstanceFilterSettings.ExclusionColumnsList.Sort();
            InstanceFilterSettings.ExclusionColumnsList.Reverse();

            using (var inputFile = new StreamReader(SourceFile))
            {
                using (var outputFile = new StreamWriter(ResultFile))
                {
                    while (!inputFile.EndOfStream)
                    {
                        var readLine = inputFile.ReadLine();
                        rowNum++;
                        while (string.IsNullOrEmpty(readLine) && !inputFile.EndOfStream)
                        {
                            readLine = inputFile.ReadLine();
                            rowNum++;
                        }

                        if (string.IsNullOrEmpty(readLine))
                        {
                            continue;
                        }

                        if (!firstRowParsed)
                        {
                            ColCount = readLine.Split('\t').Length;
                            if (InstanceFilterSettings.CopyFirstRow)
                            {
                                outputFile.WriteLine(readLine);
                                readLine = inputFile.ReadLine();
                                rowNum++;
                            }
                            
                            firstRowParsed = true;
                        }

                        if (string.IsNullOrEmpty(readLine))
                        {
                            continue;
                        }

                        var columns = readLine.Split('\t').ToList();

                        if (columns.Count == ColCount)
                        {
                            var copyLine =
                                InstanceFilterSettings.FilterStringsList.Any(
                                    keyValuePair =>
                                        string.Equals(columns[keyValuePair.Key - 1], keyValuePair.Value,
                                            StringComparison.InvariantCultureIgnoreCase));

                            if (!copyLine && InstanceFilterSettings.FilterStringsList.Count > 0)
                            {
                                continue;
                            }

                            // excludeList should already contain columns in descending order
                            foreach (var excludeColumn in InstanceFilterSettings.ExclusionColumnsList )
                            {
                                columns.RemoveAt(excludeColumn - 1);
                            }
                            readLine = String.Join("\t", columns);

                            outputFile.WriteLine(readLine);

                            // state.LinesParsed = rowNum;
                            // worker.ReportProgress(0, state);
                        }
                        else
                        {
                            errorRowNumbers.Add(rowNum);
                        }
                    }

                    // var averageCalc = string.Empty;
                    // var totalColCount = this.colCount + this.outlierList.Count;
                    // for (var i = 0; i < totalColCount; i++)
                    // {
                    // averageCalc += "=ROUND(AVERAGE(INDIRECT(ADDRESS(2,COLUMN())):INDIRECT(ADDRESS(ROW()-1,COLUMN()))),0)\t";
                    // }

                    // outputFile.WriteLine(averageCalc);
                }
            }
        }

        public int ColCount { get; set; }
    }
}
