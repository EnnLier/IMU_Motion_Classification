using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Globalization;

namespace BLE_Drive_UI.Domain
{
    /// <summary>
    /// This Class provides log writing functions
    /// </summary>
    class LogWriter
    {
        //filename of logfile
        private string _filename;

        //Actual logfile string
        private StringBuilder _log;

        /// <summary>
        /// Initialize Logfile and create overhead
        /// </summary>
        /// <param name="filepath">Default path is in programms folder</param>
        public LogWriter(String filepath = null)
        {
            CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en-US");
            if (filepath == null)
            {
                filepath = Directory.GetParent(Directory.GetParent(Directory.GetCurrentDirectory()).FullName).FullName; ;
            }
            _filename = filepath + @"/log/" + "Logfile_" + DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss") + ".txt";
            _log = new StringBuilder("This Logfile belongs to the BLE driver \n" + DateTime.Now.ToString() + ": Session started\n");
        }

        /// <summary>
        /// this function write an entry to the logfile
        /// </summary>
        /// <param name="entry">Write this string to log</param>
        public void Write(String entry)
        {
            var time = DateTime.Now.ToString();
            _log.Append(time + ": " + entry + "\n");
        }

        /// <summary>
        /// this function writes the logfile
        /// </summary>
        public void Save()
        {
            File.WriteAllText(_filename,_log.ToString());
        }
    }
}
