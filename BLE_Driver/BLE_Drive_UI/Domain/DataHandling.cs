﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BLE_Drive_UI.Domain
{
    /// <summary>
    /// Tihs class provides a very simple string buffer with only necessary functions
    /// </summary>
    public class StringBuffer
    {
        //Length of Buffer
        private static UInt16 _bufferLength;
        //Internal Buffer of strings
        private static String[] SBuffer;
        //Number of elements in Buffer
        public UInt16 Count { get; private set; }
        //Bufferstatus
        public bool Active { get; set; }

        /// <summary>
        /// Create Buffer with an initial Count of 0
        /// </summary>
        /// <param name="bufferlength">Number of elements allowed in Buffer</param>
        public StringBuffer(UInt16 bufferlength)
        {
            _bufferLength = bufferlength;
            SBuffer = new String[_bufferLength];
            Count = 0;
        }

        /// <summary>
        /// Add element to Buffer
        /// </summary>
        /// <param name="data">This string is added to Buffer</param>
        public void Add(String data)
        {
            //Add element only if Buffer is not full
            if (Count <= _bufferLength)
            {
                SBuffer[Count] = data;
                Count++;
            }
            else
            {
                throw new Exception("StringBuffer Overflow");
            }
        }

        /// <summary>
        /// This function return Buffer and clears it afterwards
        /// </summary>
        /// <returns></returns>
        public String[] Flush()
        {
            var tmp = new String[_bufferLength];
            SBuffer.CopyTo(tmp, 0);
            //Console.WriteLine(tmp[0]);
            Clear();
            Count = 0;
            return tmp;
        }

        /// <summary>
        /// This function clears the buffer and fills it with empty strings
        /// </summary>
        public void Clear()
        {
            for (int i = 0; i < Count; i++)
            {
                SBuffer[i] = String.Empty;
            }
        }
    }


    /// <summary>
    /// This class provides a syncronous solution for a data saver. It utilizes two Buffers which call themselves successively in a new thread. This provides quasi deterministic runtimes and prevents bottlenecking due to slow harddrive speeds
    /// </summary>
    public class SyncDataSaver
    {
        //File settings
        private static String _path;
        private static UInt16 _bufferLength;

        //Stringbuffers
        private StringBuffer buffer1;
        private StringBuffer buffer2;

        //data Mutex
        private static object mThreadLock = new object();

        //Threads
        private Thread Buffer1PollingThread;
        private Thread Buffer2PollingThread;

        //Stopwatch to create timestamps in measurements
        private Stopwatch Watch;

        //Rate of this buffer
        private static double _rate;
        //Cumulative rate to provide even spacing between datapoints
        private double cumulativeRate;

        //Actual Data to save. Currently in a static array with two entries for each sensor
        private String[] dataToSave = new string[2]{ String.Empty,String.Empty};

        //State variables
        public bool isSaving = false;
        public bool Active = false;
        private bool busy = false;

        /// <summary>
        /// Constructor of datasaver class. Initialize components
        /// </summary>
        /// <param name="bufferLength">Length of buffer</param>
        /// <param name="rate">Rate of buffer in ms</param>
        public SyncDataSaver(UInt16 bufferLength, double rate)
        {
            var dir = Directory.GetParent(Directory.GetParent(Directory.GetCurrentDirectory()).FullName).FullName;
            Console.WriteLine(dir);

            _path = dir + @"\Data\";

            //Create data folder if currently not existing
            if (!Directory.Exists(_path))
                Directory.CreateDirectory(_path);

            _bufferLength = bufferLength;
            _rate = rate;

            buffer1 = new StringBuffer(_bufferLength);
            buffer2 = new StringBuffer(_bufferLength);

            buffer1.Active = true;
            buffer2.Active = false;

            Watch = new Stopwatch();
        }

        /// <summary>
        /// This functions starts saving the added data. Here all state flags are set/reset and the initial pollingthread gets started
        /// </summary>
        public void start()
        {
            Console.WriteLine("Start Saving....");
            //Wait for Pollingthreads to finish
            waitForThreadsToFinish();

            //Reset state variables
            Active = true;
            isSaving = true;
            buffer1.Active = true;
            buffer2.Active = false;

            //first timestamp starts at zero
            cumulativeRate = 0;

            //start pollingthread for first buffer
            Buffer1PollingThread = new Thread(Buffer1Polling);
            Buffer1PollingThread.Start();

            Console.WriteLine("PollingThread started....");
        }

        /// <summary>
        /// This function stops the datasaving
        /// </summary>
        public void stop()
        {
            isSaving = false;
            //Empty and save the rest of the data in the currently active buffer
            flush();
            //reset watch
            Watch.Reset();
            //Wait for Pollingthreads to finish
            waitForThreadsToFinish();
            //clear current data
            dataToSave = new String[]{ String.Empty,String.Empty};
            Active = false;
        }

        /// <summary>
        /// Add data to get saved. Data is organised in an array, numbered by their sensor ID
        /// </summary>
        /// <param name="id">correscponding sensor ID</param>
        /// <param name="data">Actual data as a string to save </param>
        public void addData(int id, String data)
        {
            lock (mThreadLock)
            {
                dataToSave[id] = data;
            }
        }

        /// <summary>
        /// This function should be called in a new thread. It polls the provided data at a set rate and fills the first Buffer each time. This function is always the first thats active and always corresponds to Buffer 1
        /// </summary>
        private void Buffer1Polling()
        {
            //Wait for first value to arrive
            while (!isSaving || dataToSave.Count(p => p == String.Empty) == dataToSave.Length) { Thread.Sleep(1); }
            //Start stopwatch
            Watch.Start();
            //As long as the saving flag is set
            while (isSaving)
            {
                //get elapsed time
                double t = Watch.Elapsed.TotalMilliseconds;
                //if time exceeded threshhold for next value
                if (t >= cumulativeRate)
                {
                    //calulate stopwatch threshold for polling next value
                    cumulativeRate += _rate;
                    //get timestamp
                    String timestamp = (t / 1000).ToString("0.0000");

                    lock (mThreadLock)
                    {
                        //Add data and timestamp to Buffer
                        buffer1.Add(timestamp + dataToSave[0] + dataToSave[1]);
                    }
                    if (buffer1.Count >= _bufferLength)
                    {
                        //switch to second buffer if first buffer is full
                        buffer1.Active = false;
                        buffer2.Active = true;
                        //start Pollingthread for second buffer
                        Buffer2PollingThread = new Thread(Buffer2Polling);
                        Buffer2PollingThread.Start();
                        //save first buffer, while second Buffer takes over the polling
                        save(buffer1);
                        //return
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// This function should be called in a new thread, as soon as the first pollingthread finishes. Check Buffer1Polling for detailed functionality
        /// </summary>
        private void Buffer2Polling()
        {
            while (isSaving)
            {
                //if (!buffer2.Active){ var n = _rate / 5; Thread.Sleep((int)n); continue; }
                double t = Watch.Elapsed.TotalMilliseconds;
                if (t >= cumulativeRate)
                {
                    cumulativeRate += _rate;
                    String timestamp = (t / 1000).ToString("0.0000");
                    lock (mThreadLock)
                    {
                        buffer2.Add(timestamp + dataToSave[0] + dataToSave[1]);
                    }
                    if (buffer2.Count >= _bufferLength)
                    {
                        buffer1.Active = true;
                        buffer2.Active = false;
                        Buffer1PollingThread = new Thread(Buffer1Polling);
                        Buffer1PollingThread.Start();
                        save(buffer2);
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// This function waits for all pollingthreads to finish
        /// </summary>
        /// <returns>True if all threads succsesfully finished</returns>
        private bool waitForThreadsToFinish()
        {
            try
            {
                //Check if Pollingthread 1 is active
                if (Buffer1PollingThread != null)
                {
                    //Wait for it to finish
                    while (Buffer1PollingThread.IsAlive)
                    {
                        Console.WriteLine(" Threads still alive....");
                        Thread.Sleep(200);
                    }
                }
                //Check if Pollingthread 2 is active
                if (Buffer2PollingThread != null)
                {
                    //wait for it to finish
                    while (Buffer2PollingThread.IsAlive)
                    {
                        Console.WriteLine(" Threads still alive....");
                        Thread.Sleep(200);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed while waiting for pollingthreads to finish: " + e.Message);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Save buffer to .txt file
        /// </summary>
        /// <param name="buffer">Stringbuffer to save to file</param>
        private void save(StringBuffer buffer)
        {
            busy = true;
            String filename = DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss");
            File.WriteAllLines(_path + filename + ".txt", buffer.Flush());
            busy = false;
        }

        /// <summary>
        /// Save currently active Buffer
        /// </summary>
        private void flush()
        {
            while (busy) { Thread.Sleep(20); }
            if (buffer1.Active)
            {
                save(buffer1);
            }
            else if (buffer2.Active)
            {
                save(buffer2);
            }
        }
    }

    public class DoubleDataBufferAsync
    {
        //private float[][] fBuffer1;
        //private float[][] fBuffer2;

        //private static List<String> sBuffer1 = new List<String>();
        //private static List<String> sBuffer2 = new List<String>();
        private static bool _oneActive = true;
        private static bool _twoActive = false;

        private static String _path;
        private static UInt16 _bufferLength;
        //private static UInt16 _dataPoints;

        //private UInt16 _fBufC1;
        //private UInt16 _fBufC2;
        private static UInt16 _sBufC1;
        private static UInt16 _sBufC2;

        private static String[] sBuffer1;
        private static String[] sBuffer2;

        private static object mThreadLock = new object();

        public DoubleDataBufferAsync(UInt16 bufferLength)//, UInt16 dataPoints)
        {
            var dir = Directory.GetParent(Directory.GetParent(Directory.GetCurrentDirectory()).FullName).FullName;
            Console.WriteLine(dir);

            _path = dir + @"\Data\";

            if (!Directory.Exists(_path))
                Directory.CreateDirectory(_path);

            //_dataPoints = dataPoints;
            _bufferLength = bufferLength;
            _oneActive = true;
            _twoActive = false;

            //_fBufC1 = 0;
            //_fBufC2 = 0;

            _sBufC1 = 0;
            _sBufC2 = 0;

            //fBuffer1 = new float[_bufferLength][];
            //fBuffer2 = new float[_bufferLength][];

            sBuffer1 = new String[_bufferLength];
            sBuffer2 = new String[_bufferLength];
        }

        //public async void addData(float[] data)
        //{
        //    if (_oneActive) // Buffer 1 active
        //    {
        //        //Console.WriteLine(Buffer1.Count);
        //        fBuffer1[_fBufC1] = data;
        //        _fBufC1++;
        //        if (_fBufC1 >= _bufferLength)
        //        {
        //            await save();
        //            _twoActive = true;
        //            _oneActive = false;
        //            _fBufC1 = 0;
        //        }
        //    }
        //    else if (_twoActive)
        //    {
        //        fBuffer2[_fBufC2] = data;
        //      _fBufC2++;
        //        if (_fBufC2 >= _bufferLength)
        //        {
        //            await save();
        //            _oneActive = true;
        //            _twoActive = false;
        //            _fBufC2 = 0;
        //        }
        //    }
        //}

        public async void addData(String data)
        {
            var t = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fffffff");
            String toSave = t + " " + data; // + "\n";

            if (_oneActive) // Buffer 1 active
            {
                //Console.WriteLine(Buffer1.Count);
                sBuffer1[_sBufC1] = toSave;
                _sBufC1++;
                if (_sBufC1 >= _bufferLength)
                {
                    _twoActive = true;
                    _oneActive = false;
                    _sBufC1 = 0;
                    await save(sBuffer1);
                }
            }
            else if (_twoActive)
            {
                sBuffer2[_sBufC2] = toSave;
                _sBufC2++;
                if (_sBufC2 >= _bufferLength)
                {
                    _oneActive = true;
                    _twoActive = false;
                    _sBufC2 = 0;
                    await save(sBuffer2);
                }
            }
        }

        static async Task save(String[] buffer)
        {
            //Console.WriteLine("Save from " + Thread.CurrentThread.ManagedThreadId);
            String name = DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss");
            await Task.Run(() => {
                File.WriteAllLines(_path + name + ".txt", buffer);
            });
            buffer = new String[] { };
        }

        static async Task save()
        {
            if (_oneActive)
            {
                await save(sBuffer1);
            }
            else if (_twoActive)
            {
                await save(sBuffer2);
            }
            _oneActive = true;
            _twoActive = false;
            _sBufC2 = 0;
            _sBufC1 = 0;
        }


        public async void flush()
        {
            await save();
        }
    }
}
