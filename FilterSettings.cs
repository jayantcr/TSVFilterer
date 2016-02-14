using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TSVFilterer
{
    using System.ComponentModel;

    public class FilterSettings
    {
        public int SplitByDistinctColumn { get; set; }

        public List<int> ExclusionColumnsList { get; set; }

        public List<int> OutlierRemovalColumnsList { get; set; }

        public List<KeyValuePair<int, string>> FilterStringsList { get; set; }

        public bool CopyFirstRow { get; set; }
    }
}
